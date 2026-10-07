import { useCallback, useEffect, useRef, useState } from 'react'
import { parseServerErrors } from '../utils/validators'

// Field-level validation for forms that keep their values in useState.
// schema: { fieldName: (value, allValues) => message | undefined } built from utils/validators.js.
// Errors appear after a field is blurred, and for every field after a submit attempt.
// Pass the current values to re-check already-flagged fields while the user types.
export function useFormValidation(schema, values) {
  const [errors, setErrors] = useState({})
  const [touched, setTouched] = useState({})
  const [submitted, setSubmitted] = useState(false)
  const schemaRef = useRef(schema)
  schemaRef.current = schema

  const check = useCallback((name, values) => schemaRef.current[name]?.(values[name], values), [])

  // Call from onBlur: marks the field touched and validates it.
  const touch = useCallback((name, values) => {
    setTouched((current) => ({ ...current, [name]: true }))
    setErrors((current) => ({ ...current, [name]: check(name, values) }))
  }, [check])

  // Call after a value changes: re-validates fields the user has already seen errors for.
  const revalidate = useCallback((values) => {
    setErrors((current) => {
      const next = { ...current }
      for (const name of Object.keys(schemaRef.current)) {
        if (name in current || submitted) next[name] = check(name, values)
      }
      return next
    })
  }, [check, submitted])

  // Call on submit: validates everything (or only `fields`), focuses the first invalid field, returns true when valid.
  const validateAll = useCallback((values, formElement, fields) => {
    const result = {}
    for (const name of fields || Object.keys(schemaRef.current)) {
      const message = check(name, values)
      if (message) result[name] = message
    }
    if (fields) {
      setErrors((current) => ({ ...current, ...Object.fromEntries(fields.map((name) => [name, result[name]])) }))
      setTouched((current) => ({ ...current, ...Object.fromEntries(fields.map((name) => [name, true])) }))
    } else {
      setErrors(result)
      setSubmitted(true)
    }
    const firstInvalid = (fields || Object.keys(schemaRef.current)).find((name) => result[name])
    if (firstInvalid) {
      const element = formElement?.elements?.namedItem?.(firstInvalid) || document.querySelector(`[name="${firstInvalid}"]`)
      if (element?.focus) {
        element.scrollIntoView?.({ block: 'center', behavior: 'smooth' })
        element.focus({ preventScroll: true })
      }
    }
    return !firstInvalid
  }, [check])

  // Maps an API error onto fields; returns the general message (or '') for a banner.
  const applyServerErrors = useCallback((error, options = {}) => {
    const { fieldErrors, message } = parseServerErrors(error, { fields: Object.keys(schemaRef.current), ...options })
    if (Object.keys(fieldErrors).length) {
      setErrors((current) => ({ ...current, ...fieldErrors }))
      setSubmitted(true)
    }
    return message
  }, [])

  const reset = useCallback(() => {
    setErrors({})
    setTouched({})
    setSubmitted(false)
  }, [])

  const valuesKey = values === undefined ? undefined : JSON.stringify(values)
  useEffect(() => {
    if (valuesKey !== undefined) revalidate(JSON.parse(valuesKey))
  }, [valuesKey, revalidate])

  const errorFor = (name) => (touched[name] || submitted ? errors[name] : undefined)

  // Accessibility props for the input: aria-invalid drives the red border.
  const fieldProps = (name) => {
    const message = errorFor(name)
    return { 'aria-invalid': message ? 'true' : undefined, 'aria-describedby': message ? `${name}-error` : undefined }
  }

  return { errors, errorFor, fieldProps, touch, revalidate, validateAll, applyServerErrors, reset }
}

// Shared form validation. Rules and messages mirror the backend
// (backend/SmartHospital.Api/Validation/ValidationAttributes.cs and the request DTOs)
// and the Flutter app (frontend_Flutter/lib/core/utils/validators.dart).

const NAME_PATTERN = /^\p{L}[\p{L}\p{M} .'-]*$/u
const PHONE_PATTERN = /^\+?[0-9 ()-]+$/
const EMAIL_PATTERN = /^[^\s@]+@[^\s@]+\.[^\s@]{2,}$/
const HTML_PATTERN = /<\s*\/?\s*[A-Za-z!?]|javascript\s*:/i

export const PASSWORD_MESSAGE =
  'Password must be 8 to 128 characters and include an uppercase letter, a lowercase letter, a number and a special character.'
export const PHONE_MESSAGE = 'Phone number must contain 7 to 15 digits and may include +, spaces, hyphens or brackets.'

const isBlank = (value) => value === undefined || value === null || String(value).trim() === ''

// ── Plain validators: return an error message, or undefined when valid ──────────

export function required(value, label) {
  return isBlank(value) ? `${label} is required.` : undefined
}

export function maxLength(value, max, label) {
  return !isBlank(value) && String(value).trim().length > max ? `${label} cannot exceed ${max} characters.` : undefined
}

export function noHtml(value, label) {
  return !isBlank(value) && HTML_PATTERN.test(String(value)) ? `${label} must not contain HTML or script tags.` : undefined
}

export function email(value, { isRequired = true } = {}) {
  if (isBlank(value)) return isRequired ? 'Email is required.' : undefined
  const text = String(value).trim()
  if (text.length > 255) return 'Email cannot exceed 255 characters.'
  return EMAIL_PATTERN.test(text) ? undefined : 'Email must be a valid email address.'
}

export function personName(value, label, { isRequired = true, max = 100 } = {}) {
  if (isBlank(value)) return isRequired ? `${label} is required.` : undefined
  const text = String(value).trim()
  if (text.length < 2) return `${label} must be at least 2 characters.`
  if (text.length > max) return `${label} cannot exceed ${max} characters.`
  if (!NAME_PATTERN.test(text)) return `${label} may only contain letters, spaces, hyphens, apostrophes and dots.`
  return undefined
}

export function phone(value, { isRequired = true, label = 'Phone number' } = {}) {
  if (isBlank(value)) return isRequired ? `${label} is required.` : undefined
  const text = String(value).trim()
  const digits = (text.match(/[0-9]/g) || []).length
  return PHONE_PATTERN.test(text) && digits >= 7 && digits <= 15 ? undefined : PHONE_MESSAGE
}

export function strongPassword(value) {
  if (isBlank(value)) return 'Password is required.'
  const text = String(value)
  const ok = text.length >= 8 && text.length <= 128 && /[A-Z]/.test(text) && /[a-z]/.test(text) && /[0-9]/.test(text) && /[^A-Za-z0-9\s]/.test(text)
  return ok ? undefined : PASSWORD_MESSAGE
}

export function text(value, label, { isRequired = false, min, max, mustContainLetters = false } = {}) {
  if (isBlank(value)) return isRequired ? `${label} is required.` : undefined
  const trimmed = String(value).trim()
  if (min && trimmed.length < min) return `${label} must be at least ${min} characters.`
  if (max && trimmed.length > max) return `${label} cannot exceed ${max} characters.`
  if (mustContainLetters && !/\p{L}/u.test(trimmed)) return `${label} must contain words, not only numbers or symbols.`
  return noHtml(trimmed, label)
}

export function number(value, label, { isRequired = false, min, max, integer = false, unit = '', message } = {}) {
  if (isBlank(value)) return isRequired ? `${label} is required.` : undefined
  const parsed = Number(value)
  if (Number.isNaN(parsed)) return `${label} must be a number.`
  if (integer && !Number.isInteger(parsed)) return `${label} must be a whole number.`
  const suffix = unit ? ` ${unit}` : ''
  if ((min !== undefined && parsed < min) || (max !== undefined && parsed > max)) {
    return message || `${label} must be between ${min} and ${max}${suffix}.`
  }
  return undefined
}

export function selection(value, label) {
  return isBlank(value) || value === '0' || value === 0 ? `Please select ${label}.` : undefined
}

export function dateNotFuture(value, label = 'Date of birth') {
  if (isBlank(value)) return undefined
  const date = new Date(value)
  if (Number.isNaN(date.getTime())) return `${label} must be a valid date.`
  const tomorrow = new Date()
  tomorrow.setDate(tomorrow.getDate() + 1)
  if (date > tomorrow) return `${label} cannot be in the future.`
  if (date < new Date('1900-01-01')) return `${label} must be on or after 1 January 1900.`
  return undefined
}

export function notInPast(value, label) {
  if (isBlank(value)) return undefined
  const date = new Date(value)
  if (Number.isNaN(date.getTime())) return `${label} must be a valid date.`
  return date.getTime() < Date.now() ? `${label} must be in the future.` : undefined
}

// For yyyy-mm-dd inputs: the date must be today or later.
export function notBeforeToday(value, message) {
  if (isBlank(value)) return undefined
  const now = new Date()
  const today = `${now.getFullYear()}-${String(now.getMonth() + 1).padStart(2, '0')}-${String(now.getDate()).padStart(2, '0')}`
  return String(value).slice(0, 10) < today ? message : undefined
}

// For yyyy-mm-dd inputs: a date after today and no more than `maxYears` ahead.
export function futureDate(value, label, maxYears) {
  if (isBlank(value)) return undefined
  const now = new Date()
  const today = `${now.getFullYear()}-${String(now.getMonth() + 1).padStart(2, '0')}-${String(now.getDate()).padStart(2, '0')}`
  const day = String(value).slice(0, 10)
  if (day <= today) return `${label} must be in the future.`
  if (maxYears) {
    const limit = `${now.getFullYear() + maxYears}-${today.slice(5)}`
    if (day > limit) return `${label} cannot be more than ${maxYears} years in the future.`
  }
  return undefined
}

export function httpUrl(value, label, { max = 500 } = {}) {
  if (isBlank(value)) return undefined
  const trimmed = String(value).trim()
  if (trimmed.length > max) return `${label} cannot exceed ${max} characters.`
  try {
    const parsed = new URL(trimmed)
    return parsed.protocol === 'http:' || parsed.protocol === 'https:' ? undefined : `${label} must be a valid HTTP or HTTPS URL.`
  } catch {
    return `${label} must be a valid HTTP or HTTPS URL.`
  }
}

export function onOrAfter(endValue, startValue, message) {
  if (isBlank(endValue) || isBlank(startValue)) return undefined
  return String(endValue) < String(startValue) ? message : undefined
}

export function after(endValue, startValue, message) {
  if (isBlank(endValue) || isBlank(startValue)) return undefined
  return String(endValue) <= String(startValue) ? message : undefined
}

// Runs several validators and returns the first message.
export const first = (...messages) => messages.find(Boolean)

// ── react-hook-form adapters: use as register('field', rules.x(...)) ───────────

const asRule = (fn) => ({ validate: (value) => fn(value) || true })

export const rules = {
  required: (label) => asRule((v) => required(v, label)),
  email: (options) => asRule((v) => email(v, options)),
  personName: (label, options) => asRule((v) => personName(v, label, options)),
  phone: (options) => asRule((v) => phone(v, options)),
  strongPassword: () => asRule(strongPassword),
  text: (label, options) => asRule((v) => text(v, label, options)),
  number: (label, options) => asRule((v) => number(v, label, options)),
  selection: (label) => asRule((v) => selection(v, label)),
  dateNotFuture: (label) => asRule((v) => dateNotFuture(v, label)),
  notInPast: (label) => asRule((v) => notInPast(v, label)),
  notBeforeToday: (message) => asRule((v) => notBeforeToday(v, message)),
  futureDate: (label, maxYears) => asRule((v) => futureDate(v, label, maxYears)),
  httpUrl: (label, options) => asRule((v) => httpUrl(v, label, options)),
  // fn(value, allValues) -> message | undefined; deps re-validate this field when others change.
  custom: (fn, deps) => ({ validate: (value, values) => fn(value, values) || true, ...(deps ? { deps } : {}) }),
}

// Validate a whole object for forms that keep their values in useState.
// schema: { fieldName: (value, values) => message | undefined }
export function validateValues(values, schema) {
  const errors = {}
  for (const [field, check] of Object.entries(schema)) {
    const message = check(values[field], values)
    if (message) errors[field] = message
  }
  return errors
}

// Focus and scroll to the first invalid field (by element id or name).
export function focusFirstError(errors, order = Object.keys(errors)) {
  const field = order.find((key) => errors[key])
  if (!field) return
  const element = document.getElementById(field) || document.querySelector(`[name="${field}"]`)
  if (element) {
    element.scrollIntoView({ block: 'center', behavior: 'smooth' })
    element.focus({ preventScroll: true })
  }
}

// ── Server error mapping ─────────────────────────────────────────────────────

const camel = (key) => key.replace(/^\$\./, '').split('.').map((part) => part.charAt(0).toLowerCase() + part.slice(1)).join('.')

// Converts an axios error into { fieldErrors: { field: message }, message }.
// - 400 ValidationProblemDetails: { errors: { FieldName: ['message'] } } -> field errors
// - 409/400 { message } for uniqueness rules: mapped to a field via `conflicts`,
//   e.g. [{ match: /email/i, field: 'email' }]
// `rename` maps server field names to form field names, e.g. { diagnosis: 'initialDiagnosis' }.
export function parseServerErrors(error, { fields = [], conflicts = [], rename = {}, fallback = 'Something went wrong. Please try again.' } = {}) {
  const data = error?.response?.data
  if (!error?.response) return { fieldErrors: {}, message: 'Unable to connect to the server. Please try again.' }
  const fieldErrors = {}
  const unmatched = []
  if (data?.errors && typeof data.errors === 'object') {
    for (const [key, messages] of Object.entries(data.errors)) {
      const message = Array.isArray(messages) ? messages[0] : String(messages)
      const field = rename[camel(key)] || camel(key)
      const known = fields.length === 0 || fields.includes(field) ? field
        : fields.find((name) => name.toLowerCase() === field.toLowerCase())
      if (known) fieldErrors[known] = message
      else unmatched.push(message)
    }
  }
  const serverMessage = typeof data === 'string' ? data : data?.message || data?.detail
  if (serverMessage && Object.keys(fieldErrors).length === 0) {
    const conflict = conflicts.find((rule) => rule.match.test(serverMessage))
    if (conflict) fieldErrors[conflict.field] = serverMessage
    else unmatched.push(serverMessage)
  }
  const message = unmatched.length ? unmatched.join(' ') : Object.keys(fieldErrors).length ? '' : (data?.title && !data?.errors ? data.title : fallback)
  return { fieldErrors, message }
}

// react-hook-form version: puts server errors under the matching fields and the rest in errors.root.
export function applyServerErrors(error, setError, options = {}) {
  const { fieldErrors, message } = parseServerErrors(error, options)
  let firstField = true
  for (const [field, fieldMessage] of Object.entries(fieldErrors)) {
    setError(field, { type: 'server', message: fieldMessage }, { shouldFocus: firstField })
    firstField = false
  }
  if (message) setError('root', { type: 'server', message })
}

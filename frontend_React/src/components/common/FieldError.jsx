// Red message shown directly under an invalid field (styled by .field-error in index.css).
export default function FieldError({ name, message, className = '' }) {
  if (!message) return null
  return <p id={`${name}-error`} className={`field-error ${className}`.trim()} role="alert">{message}</p>
}

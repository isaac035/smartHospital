export default function EditIconButton({
  onClick,
  title = 'Edit',
  'aria-label': ariaLabelProp,
  ariaLabel,
  disabled = false,
  className = 'secondary-button text-xs px-2 py-1 inline-flex items-center justify-center',
  style,
}) {
  const label = ariaLabelProp || ariaLabel || title

  return (
    <button
      type="button"
      onClick={onClick}
      disabled={disabled}
      className={className}
      title={title}
      aria-label={label}
      style={style}
    >
      <svg
        xmlns="http://www.w3.org/2000/svg"
        width="13"
        height="13"
        viewBox="0 0 24 24"
        fill="none"
        stroke="currentColor"
        strokeWidth="2"
        strokeLinecap="round"
        strokeLinejoin="round"
        aria-hidden="true"
      >
        <path d="M17 3a2.828 2.828 0 1 1 4 4L7.5 20.5 2 22l1.5-5.5L17 3z" />
      </svg>
    </button>
  )
}

export default function ConfirmationModal({
  isOpen = false,
  title = 'Confirm Action',
  message = 'Are you sure you want to proceed?',
  confirmText = 'Confirm',
  cancelText = 'Cancel',
  isDanger = false,
  onConfirm,
  onCancel,
  loading = false,
}) {
  if (!isOpen) return null

  return (
    <div className="modal-overlay" role="dialog" aria-modal="true">
      <div className="modal-card" style={{ width: 'min(100%, 460px)' }}>
        <h2 style={{ color: isDanger ? '#b3261e' : 'var(--color-accent)' }}>
          {title}
        </h2>
        <p className="text-sm my-3" style={{ color: 'color-mix(in srgb, var(--color-secondary) 80%, var(--color-primary))', lineHeight: 1.6 }}>
          {message}
        </p>
        <div className="modal-actions mt-4 pt-3 border-t" style={{ borderColor: 'color-mix(in srgb, var(--color-secondary) 15%, var(--color-primary))' }}>
          <button
            type="button"
            className="secondary-button"
            onClick={onCancel}
            disabled={loading}
          >
            {cancelText}
          </button>
          <button
            type="button"
            className={isDanger ? 'primary-button' : 'primary-button'}
            style={isDanger ? { backgroundColor: '#b3261e', borderColor: '#b3261e' } : {}}
            onClick={onConfirm}
            disabled={loading}
          >
            {loading ? 'Processing...' : confirmText}
          </button>
        </div>
      </div>
    </div>
  )
}

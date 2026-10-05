import React, { useState } from 'react'

/**
 * Reusable button for toggling Active/Inactive status of any resource.
 * Automatically handles confirmation dialogs and loading states.
 */
export default function ToggleActiveButton({ 
  isActive, 
  resourceName, 
  identifier, // e.g., name of the item for the confirmation message
  onToggle, 
  disabled = false,
  className = ''
}) {
  const [showConfirm, setShowConfirm] = useState(false)
  const [isBusy, setIsBusy] = useState(false)
  const [error, setError] = useState(null)

  const handleToggleClick = () => {
    setShowConfirm(true)
    setError(null)
  }

  const handleConfirm = async () => {
    setIsBusy(true)
    setError(null)
    try {
      await onToggle()
      setShowConfirm(false)
    } catch (err) {
      setError(err.response?.data?.message || err.message || 'An error occurred.')
    } finally {
      setIsBusy(false)
    }
  }

  const actionText = isActive ? 'Deactivate' : 'Activate'
  
  return (
    <>
      <button 
        type="button" 
        className={`link-button ${isActive ? 'danger' : 'doctor-activate'} ${className}`} 
        onClick={handleToggleClick}
        disabled={disabled || isBusy}
        title={`${actionText} ${resourceName}`}
      >
        {isBusy ? '...' : actionText}
      </button>

      {showConfirm && (
        <div className="modal-overlay" role="alertdialog" aria-modal="true" aria-labelledby="toggle-active-title">
          <div className="modal-card">
            <h2 id="toggle-active-title">{actionText} {resourceName || 'item'}</h2>
            <p>
              Are you sure you want to {actionText.toLowerCase()} <strong>{identifier || 'this item'}</strong>?
            </p>
            {isActive && (
              <p className="text-sm text-gray-400 mt-2">
                This {(resourceName || 'item').toLowerCase()} will no longer be available for new assignments/bookings.
                Existing records are not affected.
              </p>
            )}
            {!isActive && (
              <p className="text-sm text-gray-400 mt-2">
                This {(resourceName || 'item').toLowerCase()} will become available for use again.
              </p>
            )}
            
            {error && <p className="form-error mt-4" role="alert">{error}</p>}
            
            <div className="modal-actions mt-6">
              <button 
                type="button" 
                className="secondary-button" 
                disabled={isBusy} 
                onClick={() => setShowConfirm(false)}
              >
                Cancel
              </button>
              <button 
                type="button" 
                className={`primary-button ${isActive ? 'doctor-deactivate' : 'doctor-activate'}`} 
                disabled={isBusy} 
                onClick={handleConfirm}
              >
                {isBusy ? `${actionText}ing...` : actionText}
              </button>
            </div>
          </div>
        </div>
      )}
    </>
  )
}

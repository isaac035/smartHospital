import { useState, useEffect } from 'react'
import { getBedById } from '../../../services/hospitalResourceService'

export default function MaintenanceDetailsModal({
  isOpen,
  onClose,
  record,
}) {
  const [bedDetails, setBedDetails] = useState(null)
  const [loadingBed, setLoadingBed] = useState(false)

  useEffect(() => {
    if (isOpen && record?.targetType === 'Bed' && record.bedId) {
      let isCancelled = false
      setLoadingBed(true)
      setBedDetails(null)

      getBedById(record.bedId)
        .then((data) => {
          if (!isCancelled) {
            setBedDetails(data)
          }
        })
        .catch(() => {
          if (!isCancelled) {
            setBedDetails(null)
          }
        })
        .finally(() => {
          if (!isCancelled) {
            setLoadingBed(false)
          }
        })

      return () => {
        isCancelled = true
      }
    } else {
      setBedDetails(null)
      setLoadingBed(false)
    }
  }, [isOpen, record?.id, record?.bedId, record?.targetType])

  if (!isOpen || !record) return null

  const formatDateTime = (dateStr) => {
    if (!dateStr) return '—'
    try {
      const d = new Date(dateStr)
      return d.toLocaleString(undefined, {
        year: 'numeric',
        month: 'short',
        day: 'numeric',
        hour: '2-digit',
        minute: '2-digit',
      })
    } catch {
      return dateStr
    }
  }

  const getStatusBadge = (status) => {
    switch (status) {
      case 'Scheduled':
        return (
          <span className="inline-block px-2.5 py-0.5 rounded text-xs font-semibold bg-amber-100 text-amber-800 border border-amber-200">
            Scheduled
          </span>
        )
      case 'InProgress':
        return (
          <span className="inline-block px-2.5 py-0.5 rounded text-xs font-semibold bg-blue-100 text-blue-800 border border-blue-200">
            In Progress
          </span>
        )
      case 'Completed':
        return (
          <span className="inline-block px-2.5 py-0.5 rounded text-xs font-semibold bg-emerald-100 text-emerald-800 border border-emerald-200">
            Completed
          </span>
        )
      case 'Cancelled':
        return (
          <span className="inline-block px-2.5 py-0.5 rounded text-xs font-semibold bg-rose-100 text-rose-800 border border-rose-200">
            Cancelled
          </span>
        )
      default:
        return (
          <span className="inline-block px-2.5 py-0.5 rounded text-xs font-semibold bg-slate-100 text-slate-700 border border-slate-200">
            {status || 'Unknown'}
          </span>
        )
    }
  }

  const isBed = record.targetType === 'Bed'

  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center p-4 bg-black/50 overflow-y-auto">
      <div
        className="w-full max-w-xl my-8 p-6 rounded-2xl shadow-2xl relative"
        style={{
          background: 'var(--color-primary)',
          border: '1px solid color-mix(in srgb, var(--color-secondary) 18%, var(--color-primary))',
        }}
      >
        {/* Header */}
        <div
          className="flex items-center justify-between pb-3 mb-4 border-b"
          style={{ borderColor: 'color-mix(in srgb, var(--color-secondary) 15%, var(--color-primary))' }}
        >
          <div>
            <h2 className="text-xl font-bold" style={{ color: 'var(--color-accent)' }}>
              Maintenance Task Details
            </h2>
            <p className="text-xs opacity-70 m-0 font-mono">
              {record.maintenanceCode}
            </p>
          </div>
          <button
            type="button"
            onClick={onClose}
            className="text-gray-400 hover:text-gray-600 text-2xl leading-none font-bold"
            aria-label="Close"
          >
            &times;
          </button>
        </div>

        {/* Content Body */}
        <div className="space-y-4 text-xs">
          {/* Status & Code Overview */}
          <div
            className="p-3 rounded-xl border flex items-center justify-between"
            style={{
              background: 'color-mix(in srgb, var(--color-secondary) 4%, var(--color-primary))',
              borderColor: 'color-mix(in srgb, var(--color-secondary) 15%, var(--color-primary))',
            }}
          >
            <div>
              <span className="opacity-60 block">Maintenance Code</span>
              <strong className="text-sm font-mono" style={{ color: 'var(--color-accent)' }}>
                {record.maintenanceCode}
              </strong>
            </div>
            <div>
              {getStatusBadge(record.status)}
            </div>
          </div>

          {/* Target & Type Grid */}
          <div className="grid grid-cols-1 sm:grid-cols-2 gap-3">
            <div
              className="p-3 rounded-xl border"
              style={{
                borderColor: 'color-mix(in srgb, var(--color-secondary) 15%, var(--color-primary))',
              }}
            >
              <span className="opacity-60 block mb-1">Target Asset</span>
              <strong className="block text-sm" style={{ color: 'var(--color-accent)' }}>
                {record.targetName}
              </strong>

              {/* Show Ward and Room information when target is a Bed */}
              {isBed && (
                <div className="my-1.5 text-xs space-y-0.5">
                  {loadingBed ? (
                    <span className="opacity-60 italic">Loading location details...</span>
                  ) : bedDetails ? (
                    <>
                      <div>
                        <span className="opacity-70">Ward: </span>
                        <span className="font-semibold">{bedDetails.wardName || '—'}</span>
                      </div>
                      <div>
                        <span className="opacity-70">Room: </span>
                        <span className="font-semibold">{bedDetails.roomNumber || '—'}</span>
                      </div>
                    </>
                  ) : null}
                </div>
              )}

              <span className="inline-block mt-1 px-2 py-0.5 rounded text-xs font-medium bg-slate-100 text-slate-700 border border-slate-200">
                {isBed ? 'Hospital Bed' : 'Medical Equipment'}
              </span>
            </div>

            <div
              className="p-3 rounded-xl border"
              style={{
                borderColor: 'color-mix(in srgb, var(--color-secondary) 15%, var(--color-primary))',
              }}
            >
              <span className="opacity-60 block mb-1">Maintenance Type</span>
              <strong className="block text-sm" style={{ color: 'var(--color-accent)' }}>
                {record.type}
              </strong>
              <span className="text-xs opacity-60 block mt-1">
                Performed By: {record.performedByStaffName || 'Staff Member'}
              </span>
            </div>
          </div>

          {/* Description */}
          <div
            className="p-3 rounded-xl border"
            style={{
              borderColor: 'color-mix(in srgb, var(--color-secondary) 15%, var(--color-primary))',
            }}
          >
            <span className="opacity-60 block mb-1">Description / Reason</span>
            <p className="m-0 leading-relaxed font-medium">
              {record.description || 'No description provided.'}
            </p>
          </div>

          {/* Schedule & Timestamps */}
          <div
            className="p-3 rounded-xl border grid grid-cols-1 sm:grid-cols-3 gap-2"
            style={{
              borderColor: 'color-mix(in srgb, var(--color-secondary) 15%, var(--color-primary))',
            }}
          >
            <div>
              <span className="opacity-60 block">Scheduled Start</span>
              <strong className="font-semibold">{formatDateTime(record.scheduledStart)}</strong>
            </div>

            <div>
              <span className="opacity-60 block">Scheduled End</span>
              <strong className="font-semibold">{formatDateTime(record.scheduledEnd)}</strong>
            </div>

            <div>
              <span className="opacity-60 block">Actual Completed</span>
              <strong className="font-semibold text-emerald-700">
                {formatDateTime(record.actualCompletedAt)}
              </strong>
            </div>
          </div>

          {/* Resolution Notes (if completed) */}
          {record.resolutionNotes && (
            <div
              className="p-3 rounded-xl border bg-emerald-50/50"
              style={{
                borderColor: 'color-mix(in srgb, #059669 25%, var(--color-primary))',
              }}
            >
              <span className="block font-semibold text-emerald-800 mb-1">
                Resolution Notes
              </span>
              <p className="m-0 text-emerald-950 leading-relaxed">
                {record.resolutionNotes}
              </p>
            </div>
          )}

          {/* Record Metadata */}
          <div className="flex justify-between text-xs opacity-50 pt-1">
            <span>Created: {formatDateTime(record.createdAt)}</span>
            <span>Updated: {formatDateTime(record.updatedAt)}</span>
          </div>
        </div>

        {/* Action Button */}
        <div
          className="flex justify-end pt-4 mt-4 border-t"
          style={{ borderColor: 'color-mix(in srgb, var(--color-secondary) 15%, var(--color-primary))' }}
        >
          <button
            type="button"
            onClick={onClose}
            className="secondary-button text-xs px-5 py-2"
          >
            Close
          </button>
        </div>
      </div>
    </div>
  )
}

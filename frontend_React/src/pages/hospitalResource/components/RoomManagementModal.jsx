import { useState, useEffect, useCallback } from 'react'
import { getRoomsByWard, deactivateRoom } from '../../../services/hospitalResourceService'
import RoomFormModal from './RoomFormModal'

export default function RoomManagementModal({ isOpen, onClose, ward, role, wards = [] }) {
  const [rooms, setRooms] = useState([])
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState(null)
  const [successMessage, setSuccessMessage] = useState(null)

  // Sub-modal states
  const [showRoomForm, setShowRoomForm] = useState(false)
  const [editingRoom, setEditingRoom] = useState(null)
  const [deactivatingRoom, setDeactivatingRoom] = useState(null)
  const [deactivating, setDeactivating] = useState(false)

  const fetchRooms = useCallback(async () => {
    if (!ward?.id) return
    try {
      setLoading(true)
      setError(null)
      const data = await getRoomsByWard(ward.id)
      setRooms(Array.isArray(data) ? data : [])
    } catch (err) {
      setError(err.response?.data?.message || 'Failed to load rooms for this ward.')
    } finally {
      setLoading(false)
    }
  }, [ward?.id])

  useEffect(() => {
    if (isOpen && ward) {
      fetchRooms()
      setSuccessMessage(null)
      setError(null)
    }
  }, [isOpen, ward, fetchRooms])

  if (!isOpen || !ward) return null

  const handleSuccess = (msg) => {
    setSuccessMessage(msg)
    fetchRooms()
    setTimeout(() => setSuccessMessage(null), 5000)
  }

  const handleOpenAdd = () => {
    setEditingRoom(null)
    setShowRoomForm(true)
  }

  const handleOpenEdit = (room) => {
    setEditingRoom(room)
    setShowRoomForm(true)
  }

  const handleConfirmDeactivate = async () => {
    if (!deactivatingRoom) return
    try {
      setDeactivating(true)
      setError(null)
      await deactivateRoom(deactivatingRoom.id)
      setSuccessMessage(`Room '${deactivatingRoom.roomNumber}' has been deactivated.`)
      setDeactivatingRoom(null)
      fetchRooms()
      setTimeout(() => setSuccessMessage(null), 5000)
    } catch (err) {
      setError(err.response?.data?.message || 'Failed to deactivate room.')
    } finally {
      setDeactivating(false)
    }
  }

  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center p-4 bg-black/50 overflow-y-auto">
      <div
        className="w-full max-w-4xl my-8 p-6 rounded-2xl shadow-2xl relative"
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
              Rooms – {ward.name}
            </h2>
            <p className="text-xs opacity-70 m-0">
              Ward Code: <span className="font-mono font-bold">{ward.code}</span> · Floor: {ward.floor || '—'} · Capacity: {ward.capacity} beds
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

        {/* Success Alert */}
        {successMessage && (
          <div className="mb-4 p-3 rounded-lg text-xs bg-emerald-50 text-emerald-800 border border-emerald-200 flex items-center justify-between">
            <span>{successMessage}</span>
            <button
              type="button"
              onClick={() => setSuccessMessage(null)}
              className="text-emerald-700 hover:text-emerald-900 font-bold ml-4"
            >
              &times;
            </button>
          </div>
        )}

        {/* Error Alert */}
        {error && (
          <div className="form-error mb-4 flex items-center justify-between">
            <span>{error}</span>
            <button
              type="button"
              onClick={() => setError(null)}
              className="font-bold ml-4"
            >
              &times;
            </button>
          </div>
        )}

        {/* Action Bar */}
        <div className="flex items-center justify-between gap-3 mb-4">
          <span className="text-xs font-semibold" style={{ color: 'var(--color-accent)' }}>
            {rooms.length} {rooms.length === 1 ? 'Room' : 'Rooms'} Configured
          </span>
          <div className="flex items-center gap-2">
            <button
              type="button"
              onClick={fetchRooms}
              disabled={loading}
              className="secondary-button text-xs px-3 py-1.5"
            >
              {loading ? 'Refreshing...' : 'Refresh'}
            </button>
            {(role === 'Admin' || role === 'Staff') && (
              <button
                type="button"
                onClick={handleOpenAdd}
                className="primary-button text-xs px-4 py-1.5"
                style={{ marginTop: 0 }}
              >
                + Add Room
              </button>
            )}
          </div>
        </div>

        {/* Rooms Table */}
        <div className="stat-card mb-4" style={{ padding: 0, overflow: 'hidden' }}>
          <div className="overflow-x-auto">
            <table className="w-full text-left border-collapse" style={{ minWidth: '650px' }}>
              <thead>
                <tr
                  style={{
                    borderBottom: '1px solid color-mix(in srgb, var(--color-secondary) 15%, var(--color-primary))',
                    background: 'color-mix(in srgb, var(--color-accent) 4%, var(--color-primary))',
                  }}
                >
                  <th className="p-3 text-xs font-bold uppercase tracking-wider" style={{ color: 'var(--color-accent)' }}>
                    Room Number
                  </th>
                  <th className="p-3 text-xs font-bold uppercase tracking-wider" style={{ color: 'var(--color-accent)' }}>
                    Type
                  </th>
                  <th className="p-3 text-xs font-bold uppercase tracking-wider" style={{ color: 'var(--color-accent)' }}>
                    Capacity
                  </th>
                  <th className="p-3 text-xs font-bold uppercase tracking-wider" style={{ color: 'var(--color-accent)' }}>
                    Total Beds
                  </th>
                  <th className="p-3 text-xs font-bold uppercase tracking-wider" style={{ color: 'var(--color-accent)' }}>
                    Available Beds
                  </th>
                  <th className="p-3 text-xs font-bold uppercase tracking-wider" style={{ color: 'var(--color-accent)' }}>
                    Status
                  </th>
                  <th className="p-3 text-xs font-bold uppercase tracking-wider text-right" style={{ color: 'var(--color-accent)' }}>
                    Actions
                  </th>
                </tr>
              </thead>
              <tbody>
                {loading ? (
                  <tr>
                    <td colSpan="7" className="p-8 text-center" style={{ color: 'color-mix(in srgb, var(--color-secondary) 50%, var(--color-primary))' }}>
                      Loading rooms...
                    </td>
                  </tr>
                ) : rooms.length === 0 ? (
                  <tr>
                    <td colSpan="7" className="p-8 text-center" style={{ color: 'color-mix(in srgb, var(--color-secondary) 50%, var(--color-primary))' }}>
                      No rooms found for this ward. Click &quot;+ Add Room&quot; to configure one.
                    </td>
                  </tr>
                ) : (
                  rooms.map((room) => {
                    const hasOccupied = (room.occupiedBeds || 0) > 0

                    return (
                      <tr
                        key={room.id}
                        style={{
                          borderBottom: '1px solid color-mix(in srgb, var(--color-secondary) 10%, var(--color-primary))',
                        }}
                      >
                        <td className="p-3 text-xs font-mono font-bold" style={{ color: 'var(--color-accent)' }}>
                          {room.roomNumber}
                        </td>

                        <td className="p-3 text-xs">
                          <span className="inline-block px-2 py-0.5 rounded text-xs font-medium bg-slate-100 text-slate-700 border border-slate-200">
                            {room.type}
                          </span>
                        </td>

                        <td className="p-3 text-xs font-medium">
                          {room.capacity} beds
                        </td>

                        <td className="p-3 text-xs font-medium">
                          {room.totalBeds}
                        </td>

                        <td className="p-3 text-xs font-medium">
                          <span style={{ color: room.availableBeds > 0 ? '#0d7a42' : '#c43a1a', fontWeight: 600 }}>
                            {room.availableBeds}
                          </span>
                        </td>

                        <td className="p-3 text-xs">
                          {room.isActive ? (
                            <span className="inline-block px-2 py-0.5 rounded text-xs font-semibold bg-emerald-100 text-emerald-800 border border-emerald-200">
                              Active
                            </span>
                          ) : (
                            <span className="inline-block px-2 py-0.5 rounded text-xs font-semibold bg-gray-100 text-gray-600 border border-gray-300">
                              Inactive
                            </span>
                          )}
                        </td>

                        <td className="p-3 text-xs text-right whitespace-nowrap">
                          <div className="inline-flex items-center gap-1.5 justify-end">
                            {(role === 'Admin' || role === 'Staff') && (
                              <button
                                type="button"
                                onClick={() => handleOpenEdit(room)}
                                className="secondary-button text-xs px-2.5 py-1"
                                title="Edit room"
                              >
                                Edit
                              </button>
                            )}

                            {role === 'Admin' && room.isActive && (
                              <button
                                type="button"
                                onClick={() => setDeactivatingRoom(room)}
                                disabled={hasOccupied}
                                className="secondary-button text-xs px-2.5 py-1"
                                style={{
                                  borderColor: '#dc2626',
                                  color: '#dc2626',
                                  opacity: hasOccupied ? 0.5 : 1,
                                  cursor: hasOccupied ? 'not-allowed' : 'pointer',
                                }}
                                title={
                                  hasOccupied
                                    ? 'Cannot deactivate room with occupied beds'
                                    : 'Deactivate room'
                                }
                              >
                                Deactivate
                              </button>
                            )}
                          </div>
                        </td>
                      </tr>
                    )
                  })
                )}
              </tbody>
            </table>
          </div>
        </div>

        {/* Footer */}
        <div className="flex justify-end pt-3 border-t" style={{ borderColor: 'color-mix(in srgb, var(--color-secondary) 15%, var(--color-primary))' }}>
          <button
            type="button"
            onClick={onClose}
            className="secondary-button text-xs px-4 py-2"
          >
            Close
          </button>
        </div>

        {/* Deactivation Confirmation Sub-Dialog */}
        {deactivatingRoom && (
          <div className="fixed inset-0 z-50 flex items-center justify-center p-4 bg-black/60">
            <div
              className="w-full max-w-sm p-5 rounded-xl shadow-2xl relative"
              style={{
                background: 'var(--color-primary)',
                border: '1px solid color-mix(in srgb, var(--color-secondary) 18%, var(--color-primary))',
              }}
            >
              <h3 className="text-base font-bold text-red-600 mb-2">Deactivate Room</h3>
              <p className="text-xs mb-3">
                Are you sure you want to deactivate room{' '}
                <strong style={{ color: 'var(--color-accent)' }}>{deactivatingRoom.roomNumber}</strong>?
                All beds inside this room will also be deactivated.
              </p>
              <div className="flex justify-end gap-2 pt-2 border-t" style={{ borderColor: 'color-mix(in srgb, var(--color-secondary) 15%, var(--color-primary))' }}>
                <button
                  type="button"
                  disabled={deactivating}
                  onClick={() => setDeactivatingRoom(null)}
                  className="secondary-button text-xs px-3 py-1.5"
                >
                  Cancel
                </button>
                <button
                  type="button"
                  disabled={deactivating}
                  onClick={handleConfirmDeactivate}
                  className="primary-button text-xs px-4 py-1.5"
                  style={{
                    marginTop: 0,
                    backgroundColor: '#dc2626',
                    borderColor: '#dc2626',
                  }}
                >
                  {deactivating ? 'Deactivating...' : 'Confirm'}
                </button>
              </div>
            </div>
          </div>
        )}

        {/* Add / Edit Sub-Modal */}
        <RoomFormModal
          isOpen={showRoomForm}
          onClose={() => setShowRoomForm(false)}
          ward={ward}
          allWards={wards}
          room={editingRoom}
          existingRoomNumbers={rooms.map((r) => r.roomNumber)}
          onSuccess={handleSuccess}
        />
      </div>
    </div>
  )
}

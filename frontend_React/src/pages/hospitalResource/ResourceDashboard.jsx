import { useState, useEffect } from 'react'
import DashboardLayout from '../../layouts/DashboardLayout'
import ResourceNavigation from './ResourceNavigation'
import { getOccupancyOverview, getWardOccupancies } from '../../services/hospitalResourceService'
import { useAuth } from '../../hooks/useAuth'

const adminNav = ['Dashboard', 'User Management', 'Doctor Management', 'Department Management', 'Appointments', 'Reports', 'Settings']
const staffNav = ['Dashboard', 'Patients', 'Appointments', 'Queue Management', 'Resources']

export default function ResourceDashboard() {
  const { user } = useAuth()
  const role = user?.role || 'Staff'
  const navigation = role === 'Admin' ? adminNav : staffNav

  const [overview, setOverview] = useState(null)
  const [wards, setWards] = useState([])
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState(null)

  useEffect(() => {
    fetchDashboardData()
  }, [])

  const fetchDashboardData = async () => {
    try {
      setLoading(true)
      setError(null)
      const [overviewData, wardsData] = await Promise.all([
        getOccupancyOverview(),
        getWardOccupancies(),
      ])
      setOverview(overviewData)
      setWards(wardsData || [])
    } catch (err) {
      setError(err.response?.data?.message || 'Failed to load hospital resource data.')
    } finally {
      setLoading(false)
    }
  }

  return (
    <DashboardLayout
      role={role}
      navigation={navigation}
      title="Resource Management"
      subtitle="Real-time hospital bed occupancy, ward capacity, and medical resource status."
    >
      <ResourceNavigation />

      {error && (
        <div className="form-error mb-4">
          {error}
        </div>
      )}

      {/* Summary KPI Cards */}
      <div className="mb-8">
        <h2 className="text-base font-bold mb-3" style={{ color: 'var(--color-accent)' }}>
          Occupancy & Resource Overview
        </h2>
        {loading ? (
          <div className="p-8 text-center border rounded-xl" style={{ borderColor: 'color-mix(in srgb, var(--color-secondary) 15%, var(--color-primary))', background: 'var(--color-primary)' }}>
            Loading occupancy overview...
          </div>
        ) : (
          <div className="dashboard-cards">
            <article className="stat-card">
              <p>Total Beds</p>
              <strong>{overview?.totalBeds ?? overview?.TotalBeds ?? 0}</strong>
              <span>Registered hospital beds</span>
            </article>

            <article className="stat-card">
              <p>Available Beds</p>
              <strong style={{ color: '#0d7a42' }}>{overview?.availableBeds ?? overview?.AvailableBeds ?? 0}</strong>
              <span>Ready for admission</span>
            </article>

            <article className="stat-card">
              <p>Occupied Beds</p>
              <strong style={{ color: '#c43a1a' }}>{overview?.occupiedBeds ?? overview?.OccupiedBeds ?? 0}</strong>
              <span>Currently in use</span>
            </article>

            <article className="stat-card">
              <p>Maintenance Beds</p>
              <strong style={{ color: '#d97706' }}>{overview?.maintenanceBeds ?? overview?.MaintenanceBeds ?? 0}</strong>
              <span>Under service or repair</span>
            </article>

            <article className="stat-card">
              <p>Hospital Occupancy</p>
              <strong>{overview?.hospitalOccupancyRate ?? overview?.HospitalOccupancyRate ?? 0}%</strong>
              <span>Current bed utilization rate</span>
            </article>

            <article className="stat-card">
              <p>Active Wards</p>
              <strong>
                {overview?.activeWards ?? overview?.ActiveWards ?? 0}
                <span className="text-sm font-normal text-gray-500"> / {overview?.totalWards ?? overview?.TotalWards ?? 0}</span>
              </strong>
              <span>
                {(overview?.lowAvailabilityWardCount ?? overview?.LowAvailabilityWardCount ?? 0) > 0 ? (
                  <span style={{ color: '#c43a1a', fontWeight: 600 }}>
                    {overview?.lowAvailabilityWardCount ?? overview?.LowAvailabilityWardCount} low on beds
                  </span>
                ) : (
                  'Normal availability'
                )}
              </span>
            </article>

            <article className="stat-card">
              <p>Medical Resources</p>
              <strong>
                {overview?.availableMedicalResources ?? overview?.AvailableMedicalResources ?? 0}
                <span className="text-sm font-normal text-gray-500"> / {overview?.totalMedicalResources ?? overview?.TotalMedicalResources ?? 0}</span>
              </strong>
              <span>
                {overview?.inUseMedicalResources ?? overview?.InUseMedicalResources ?? 0} in use, {overview?.maintenanceMedicalResources ?? overview?.MaintenanceMedicalResources ?? 0} maint.
              </span>
            </article>
          </div>
        )}
      </div>

      {/* Ward Occupancy Table */}
      <div>
        <div className="flex justify-between items-center mb-3">
          <h2 className="text-base font-bold m-0" style={{ color: 'var(--color-accent)' }}>
            Ward Occupancy & Availability
          </h2>
          <span className="text-xs" style={{ color: 'color-mix(in srgb, var(--color-secondary) 60%, var(--color-primary))' }}>
            {wards.length} Wards Listed
          </span>
        </div>

        <div className="stat-card" style={{ padding: 0, overflow: 'hidden' }}>
          <div className="overflow-x-auto">
            <table className="w-full text-left border-collapse" style={{ minWidth: '700px' }}>
              <thead>
                <tr style={{ borderBottom: '1px solid color-mix(in srgb, var(--color-secondary) 15%, var(--color-primary))', background: 'color-mix(in srgb, var(--color-accent) 4%, var(--color-primary))' }}>
                  <th className="p-3 text-xs font-bold uppercase tracking-wider" style={{ color: 'var(--color-accent)' }}>Ward Name</th>
                  <th className="p-3 text-xs font-bold uppercase tracking-wider" style={{ color: 'var(--color-accent)' }}>Code</th>
                  <th className="p-3 text-xs font-bold uppercase tracking-wider" style={{ color: 'var(--color-accent)' }}>Type</th>
                  <th className="p-3 text-xs font-bold uppercase tracking-wider" style={{ color: 'var(--color-accent)' }}>Floor</th>
                  <th className="p-3 text-xs font-bold uppercase tracking-wider" style={{ color: 'var(--color-accent)' }}>Capacity</th>
                  <th className="p-3 text-xs font-bold uppercase tracking-wider" style={{ color: 'var(--color-accent)' }}>Total Beds</th>
                  <th className="p-3 text-xs font-bold uppercase tracking-wider" style={{ color: 'var(--color-accent)' }}>Available</th>
                  <th className="p-3 text-xs font-bold uppercase tracking-wider" style={{ color: 'var(--color-accent)' }}>Occupied</th>
                  <th className="p-3 text-xs font-bold uppercase tracking-wider" style={{ color: 'var(--color-accent)' }}>Maintenance</th>
                  <th className="p-3 text-xs font-bold uppercase tracking-wider" style={{ color: 'var(--color-accent)' }}>Occupancy Rate</th>
                  <th className="p-3 text-xs font-bold uppercase tracking-wider" style={{ color: 'var(--color-accent)' }}>Status</th>
                </tr>
              </thead>
              <tbody>
                {loading ? (
                  <tr>
                    <td colSpan="11" className="p-8 text-center" style={{ color: 'color-mix(in srgb, var(--color-secondary) 50%, var(--color-primary))' }}>
                      Loading ward occupancy data...
                    </td>
                  </tr>
                ) : wards.length === 0 ? (
                  <tr>
                    <td colSpan="11" className="p-8 text-center" style={{ color: 'color-mix(in srgb, var(--color-secondary) 50%, var(--color-primary))' }}>
                      No ward records found.
                    </td>
                  </tr>
                ) : (
                  wards.map((ward) => {
                    const id = ward.wardId ?? ward.WardId
                    const name = ward.wardName ?? ward.WardName
                    const code = ward.wardCode ?? ward.WardCode
                    const type = ward.wardType ?? ward.WardType
                    const floor = ward.floor ?? ward.Floor
                    const capacity = ward.capacity ?? ward.Capacity
                    const total = ward.totalBeds ?? ward.TotalBeds ?? 0
                    const available = ward.availableBeds ?? ward.AvailableBeds ?? 0
                    const occupied = ward.occupiedBeds ?? ward.OccupiedBeds ?? 0
                    const maintenance = ward.maintenanceBeds ?? ward.MaintenanceBeds ?? 0
                    const rate = ward.occupancyRate ?? ward.OccupancyRate ?? 0
                    const isLow = ward.isLowAvailability ?? ward.IsLowAvailability

                    return (
                      <tr key={id} style={{ borderBottom: '1px solid color-mix(in srgb, var(--color-secondary) 10%, var(--color-primary))' }}>
                        <td className="p-3 font-semibold text-sm" style={{ color: 'var(--color-accent)' }}>
                          {name}
                        </td>
                        <td className="p-3 text-sm">{code}</td>
                        <td className="p-3 text-sm">{type}</td>
                        <td className="p-3 text-sm">{floor || '-'}</td>
                        <td className="p-3 text-sm">{capacity}</td>
                        <td className="p-3 text-sm font-medium">{total}</td>
                        <td className="p-3 text-sm font-semibold" style={{ color: available > 0 ? '#0d7a42' : '#c43a1a' }}>
                          {available}
                        </td>
                        <td className="p-3 text-sm">{occupied}</td>
                        <td className="p-3 text-sm">{maintenance}</td>
                        <td className="p-3 text-sm font-medium">
                          {rate}%
                        </td>
                        <td className="p-3 text-sm">
                          {isLow ? (
                            <span
                              className="inline-block px-2 py-0.5 rounded text-xs font-semibold"
                              style={{ background: '#fef2f2', color: '#b91c1c', border: '1px solid #fecaca' }}
                            >
                              Low Availability
                            </span>
                          ) : (
                            <span
                              className="inline-block px-2 py-0.5 rounded text-xs font-semibold"
                              style={{ background: '#f0fdf4', color: '#15803d', border: '1px solid #bbf7d0' }}
                            >
                              Normal
                            </span>
                          )}
                        </td>
                      </tr>
                    )
                  })
                )}
              </tbody>
            </table>
          </div>
        </div>
      </div>
    </DashboardLayout>
  )
}

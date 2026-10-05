import { useState } from 'react'

export default function LabOrdersList({ labOrders = [], onNewOrder, onRecordReport }) {
  const [selectedReport, setSelectedReport] = useState(null)

  if (labOrders.length === 0) {
    return (
      <div className="panel" style={{ padding: 24 }}>
        <div className="flex justify-between items-center mb-3">
          <h3 className="font-bold text-lg m-0" style={{ color: 'var(--color-accent)' }}>Diagnostic & Lab Orders</h3>
          {onNewOrder && (
            <button className="primary-button text-xs px-3 py-1.5" style={{ marginTop: 0 }} onClick={onNewOrder}>
              + Order Lab Test
            </button>
          )}
        </div>
        <p className="placeholder-text text-sm">No laboratory orders recorded for this patient.</p>
      </div>
    )
  }

  return (
    <div className="flex flex-col gap-4">
      <div className="flex justify-between items-center">
        <div>
          <h3 className="font-bold text-lg m-0" style={{ color: 'var(--color-accent)' }}>
            Laboratory & Diagnostic Orders ({labOrders.length})
          </h3>
          <span className="text-xs" style={{ color: 'color-mix(in srgb, var(--color-secondary) 65%, var(--color-primary))' }}>
            Lab investigations, pathology orders, and diagnostic results
          </span>
        </div>
        {onNewOrder && (
          <button className="primary-button text-xs px-3 py-1.5" style={{ marginTop: 0 }} onClick={onNewOrder}>
            + Order Lab Test
          </button>
        )}
      </div>

      <div className="panel overflow-hidden">
        <div className="table-responsive">
          <table className="data-table">
            <thead>
              <tr>
                <th>Order #</th>
                <th>Test Name</th>
                <th>Category</th>
                <th>Priority</th>
                <th>Status</th>
                <th>Ordered At</th>
                <th>Result Summary</th>
                <th>Actions</th>
              </tr>
            </thead>
            <tbody>
              {labOrders.map((order) => {
                const isUrgent = order.priority === 'Stat' || order.priority === 'Urgent'
                const isCompleted = order.status === 'Completed'

                return (
                  <tr key={order.id}>
                    <td><strong>{order.orderNumber || `#${order.id}`}</strong></td>
                    <td>
                      <div>
                        <strong>{order.testName}</strong>
                        {order.clinicalNotes && (
                          <div className="text-xs text-gray-500 max-w-xs truncate" title={order.clinicalNotes}>
                            Note: {order.clinicalNotes}
                          </div>
                        )}
                      </div>
                    </td>
                    <td>{order.category || 'General'}</td>
                    <td>
                      <span className={`badge ${isUrgent ? 'badge-danger' : 'badge-info'}`}>
                        {order.priority || 'Routine'}
                      </span>
                    </td>
                    <td>
                      <span className={`badge ${isCompleted ? 'badge-success' : order.status === 'InProgress' ? 'badge-warning' : 'badge-info'}`}>
                        {order.status || 'Pending'}
                      </span>
                    </td>
                    <td>{order.orderedAt ? new Date(order.orderedAt).toLocaleDateString() : 'N/A'}</td>
                    <td>
                      {order.report ? (
                        <div>
                          <span className="font-semibold text-green-700">{order.report.resultSummary}</span>
                          {order.report.referenceRange && (
                            <div className="text-xs text-gray-500">Ref: {order.report.referenceRange}</div>
                          )}
                        </div>
                      ) : (
                        <span className="text-gray-400 italic">Pending analysis</span>
                      )}
                    </td>
                    <td>
                      <div className="flex gap-2">
                        {order.report ? (
                          <button
                            type="button"
                            className="link-button"
                            onClick={() => setSelectedReport(order)}
                          >
                            View Findings
                          </button>
                        ) : null}
                        {onRecordReport && (
                          <button
                            type="button"
                            className="link-button"
                            onClick={() => onRecordReport(order)}
                          >
                            {order.report ? 'Update Report' : 'Enter Report'}
                          </button>
                        )}
                      </div>
                    </td>
                  </tr>
                )
              })}
            </tbody>
          </table>
        </div>
      </div>

      {selectedReport && (
        <div className="modal-overlay" role="dialog" aria-modal="true">
          <div className="modal-card">
            <h2>Diagnostic Report: {selectedReport.testName}</h2>
            <div className="flex flex-col gap-3 my-4 text-sm">
              <div className="field-row">
                <div>
                  <label>Order Number</label>
                  <p className="font-semibold">{selectedReport.orderNumber}</p>
                </div>
                <div>
                  <label>Test Category</label>
                  <p>{selectedReport.category || 'General'}</p>
                </div>
              </div>
              <div>
                <label>Result Summary</label>
                <p className="p-2 rounded bg-gray-50 font-semibold">{selectedReport.report?.resultSummary}</p>
              </div>
              <div>
                <label>Detailed Findings</label>
                <p className="p-3 rounded bg-gray-50 whitespace-pre-wrap">{selectedReport.report?.findings}</p>
              </div>
              <div className="field-row">
                <div>
                  <label>Reference Range</label>
                  <p>{selectedReport.report?.referenceRange || 'N/A'}</p>
                </div>
                <div>
                  <label>Report Date</label>
                  <p>{selectedReport.report?.reportDate ? new Date(selectedReport.report.reportDate).toLocaleString() : 'N/A'}</p>
                </div>
              </div>
              {selectedReport.report?.doctorRemarks && (
                <div>
                  <label>Remarks</label>
                  <p className="p-2 rounded bg-blue-50 text-blue-900">{selectedReport.report.doctorRemarks}</p>
                </div>
              )}
            </div>
            <div className="modal-actions">
              <button type="button" className="secondary-button" onClick={() => setSelectedReport(null)}>
                Close
              </button>
            </div>
          </div>
        </div>
      )}
    </div>
  )
}

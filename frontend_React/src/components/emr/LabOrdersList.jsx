export default function LabOrdersList({ labOrders = [] }) {
  if (labOrders.length === 0) {
    return <p className="placeholder-text">No laboratory orders recorded.</p>
  }

  return (
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
          </tr>
        </thead>
        <tbody>
          {labOrders.map((order) => (
            <tr key={order.id}>
              <td><strong>{order.orderNumber}</strong></td>
              <td>{order.testName}</td>
              <td>{order.category || 'General'}</td>
              <td>
                <span className={`badge ${order.priority === 'Stat' || order.priority === 'Urgent' ? 'badge-danger' : 'badge-info'}`}>
                  {order.priority}
                </span>
              </td>
              <td>
                <span className={`badge ${order.status === 'Completed' ? 'badge-success' : 'badge-warning'}`}>
                  {order.status}
                </span>
              </td>
              <td>{new Date(order.orderedAt).toLocaleDateString()}</td>
              <td>{order.report ? order.report.resultSummary : 'Pending analysis'}</td>
            </tr>
          ))}
        </tbody>
      </table>
    </div>
  )
}

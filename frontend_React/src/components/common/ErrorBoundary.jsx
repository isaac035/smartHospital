import React from 'react';

class ErrorBoundary extends React.Component {
  constructor(props) {
    super(props);
    this.state = { hasError: false, error: null };
  }

  static getDerivedStateFromError(error) {
    return { hasError: true, error };
  }

  componentDidCatch(error, errorInfo) {
    console.error("ErrorBoundary caught an error", error, errorInfo);
  }

  render() {
    if (this.state.hasError) {
      return (
        <div className="empty-state" style={{ 
          margin: '2rem', 
          padding: '2rem', 
          background: '#111A2E', 
          border: '1px solid #1E3054', 
          borderRadius: '16px',
          textAlign: 'center'
        }}>
          <h2 style={{ color: '#EF5350', marginBottom: '1rem' }}>Something went wrong</h2>
          <p style={{ color: '#8899AA', marginBottom: '2rem' }}>
            We encountered an unexpected error while rendering this page.
          </p>
          <div style={{ padding: '1rem', background: '#0B1220', borderRadius: '8px', color: '#EF5350', fontFamily: 'monospace', marginBottom: '2rem', overflowX: 'auto', textAlign: 'left' }}>
            {this.state.error?.toString()}
          </div>
          <button 
            className="primary-button" 
            onClick={() => window.location.reload()}
          >
            Reload Page
          </button>
        </div>
      );
    }

    return this.props.children;
  }
}

export default ErrorBoundary;

import { Component } from 'react';
import type { ErrorInfo, ReactNode } from 'react';

interface Props {
    children: ReactNode;
}

interface State {
    hasError: boolean;
    error: Error | null;
}

class ErrorBoundary extends Component<Props, State> {
    state: State = { hasError: false, error: null };

    static getDerivedStateFromError(error: Error): State {
        return { hasError: true, error };
    }

    componentDidCatch(error: Error, info: ErrorInfo) {
        console.error('ErrorBoundary caught:', error, info.componentStack);
    }

    render() {
        if (!this.state.hasError) return this.props.children;

        return (
            <div className="error-boundary">
                <div className="error-boundary-card">
                    <p className="error-boundary-icon">⚠️</p>
                    <h1>Something went wrong</h1>
                    <p className="error-boundary-message">
                        {this.state.error?.message || 'An unexpected error occurred.'}
                    </p>
                    <button
                        className="error-boundary-btn"
                        onClick={() => {
                            this.setState({ hasError: false, error: null });
                            window.location.href = '/';
                        }}
                    >
                        Back to home
                    </button>
                </div>
            </div>
        );
    }
}

export default ErrorBoundary;

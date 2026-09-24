import { Navigate, Outlet } from 'react-router-dom'
import { useAuth } from '../auth/useAuth'

function ProtectedRoute() {
  const { isAuthenticated, isLoading } = useAuth()

  if (isLoading) {
    return (
      <p className="status" role="status">
        Carregando…
      </p>
    )
  }

  if (!isAuthenticated) {
    return <Navigate to="/login" replace />
  }

  return <Outlet />
}

export default ProtectedRoute

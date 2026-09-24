import { useAuth } from '../auth/useAuth'

function HomePage() {
  const { user, logout } = useAuth()

  return (
    <main className="home">
      <h1>GameHub</h1>
      <p>
        Autenticado como <strong>{user?.username}</strong>
      </p>
      <button type="button" onClick={logout}>
        Sair
      </button>
    </main>
  )
}

export default HomePage

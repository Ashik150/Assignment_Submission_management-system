import { useEffect, useState } from 'react'
import './App.css'
import { AdminLayout } from './components/AdminLayout'
import { LoginPage } from './pages/LoginPage'
import { DashboardPage } from './pages/DashboardPage'
import { UsersPage } from './pages/UsersPage'
import { CoursesPage } from './pages/CoursesPage'
import { SubjectsPage } from './pages/SubjectsPage'
import { AssignmentsPage } from './pages/AssignmentsPage'
import { SubmissionsPage } from './pages/SubmissionsPage'
import type { AuthSession, ViewName } from './types'

const sessionKey = 'onnorokom-admin-session'

function readSession(): AuthSession | null {
  try {
    const stored = localStorage.getItem(sessionKey)
    if (!stored) return null

    const session = JSON.parse(stored) as AuthSession
    if (new Date(session.expiresAt).getTime() <= Date.now()) {
      localStorage.removeItem(sessionKey)
      return null
    }

    return session.user.role === 'Admin' ? session : null
  } catch {
    localStorage.removeItem(sessionKey)
    return null
  }
}

function App() {
  const [session, setSession] = useState<AuthSession | null>(readSession)
  const [activeView, setActiveView] = useState<ViewName>('dashboard')

  useEffect(() => {
    if (session) localStorage.setItem(sessionKey, JSON.stringify(session))
  }, [session])

  if (!session) {
    return <LoginPage onAuthenticated={setSession} />
  }

  const logout = () => {
    localStorage.removeItem(sessionKey)
    setSession(null)
  }

  return (
    <AdminLayout
      activeView={activeView}
      onNavigate={setActiveView}
      onLogout={logout}
      user={session.user}
    >
      {activeView === 'dashboard' ? (
        <DashboardPage token={session.token} onNavigate={setActiveView} />
      ) : activeView === 'users' ? (
        <UsersPage token={session.token} />
      ) : activeView === 'courses' ? (
        <CoursesPage token={session.token} />
      ) : activeView === 'subjects' ? (
        <SubjectsPage token={session.token} />
      ) : activeView === 'assignments' ? (
        <AssignmentsPage token={session.token} />
      ) : activeView === 'submissions' ? (
        <SubmissionsPage token={session.token} />
      ) : (
        <DashboardPage token={session.token} onNavigate={setActiveView} />
      )}
    </AdminLayout>
  )
}

export default App

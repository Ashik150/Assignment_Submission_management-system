import { useEffect, useState } from 'react'
import './App.css'
import { WorkspaceLayout } from './components/WorkspaceLayout'
import { LoginPage } from './pages/LoginPage'
import { DashboardPage } from './pages/DashboardPage'
import { UsersPage } from './pages/UsersPage'
import { CoursesPage } from './pages/CoursesPage'
import { SubjectsPage } from './pages/SubjectsPage'
import { AssignmentsPage } from './pages/AssignmentsPage'
import { SubmissionsPage } from './pages/SubmissionsPage'
import { TeacherDashboardPage } from './pages/TeacherDashboardPage'
import { TeacherAssignmentsPage } from './pages/TeacherAssignmentsPage'
import { TeacherSubmissionsPage } from './pages/TeacherSubmissionsPage'
import { StudentDashboardPage } from './pages/StudentDashboardPage'
import { StudentAssignmentsPage } from './pages/StudentAssignmentsPage'
import { StudentSubmissionsPage } from './pages/StudentSubmissionsPage'
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

    return session
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
    return <LoginPage onAuthenticated={(nextSession) => {
      setActiveView('dashboard')
      setSession(nextSession)
    }} />
  }

  const logout = () => {
    localStorage.removeItem(sessionKey)
    setActiveView('dashboard')
    setSession(null)
  }

  const isAdmin = session.user.role === 'Admin'
  const isTeacher = session.user.role === 'Teacher'

  return (
    <WorkspaceLayout
      activeView={activeView}
      onNavigate={setActiveView}
      onLogout={logout}
      user={session.user}
    >
      {isAdmin && activeView === 'dashboard' ? (
        <DashboardPage token={session.token} onNavigate={setActiveView} />
      ) : isAdmin && activeView === 'users' ? (
        <UsersPage token={session.token} />
      ) : isAdmin && activeView === 'courses' ? (
        <CoursesPage token={session.token} />
      ) : isAdmin && activeView === 'subjects' ? (
        <SubjectsPage token={session.token} />
      ) : isAdmin && activeView === 'assignments' ? (
        <AssignmentsPage token={session.token} />
      ) : isAdmin && activeView === 'submissions' ? (
        <SubmissionsPage token={session.token} />
      ) : isTeacher && activeView === 'dashboard' ? (
        <TeacherDashboardPage teacherName={session.user.fullName} token={session.token} onNavigate={setActiveView} />
      ) : isTeacher && activeView === 'assignments' ? (
        <TeacherAssignmentsPage token={session.token} />
      ) : isTeacher && activeView === 'submissions' ? (
        <TeacherSubmissionsPage token={session.token} />
      ) : activeView === 'dashboard' ? (
        <StudentDashboardPage studentName={session.user.fullName} token={session.token} onNavigate={setActiveView} />
      ) : activeView === 'assignments' ? (
        <StudentAssignmentsPage token={session.token} />
      ) : (
        <StudentSubmissionsPage token={session.token} />
      )}
    </WorkspaceLayout>
  )
}

export default App

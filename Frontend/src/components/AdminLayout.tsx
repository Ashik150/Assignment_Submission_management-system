import { useState, type ReactNode } from 'react'
import type { AuthenticatedUser, ViewName } from '../types'
import { Icon } from './Icon'

interface AdminLayoutProps {
  activeView: ViewName
  children: ReactNode
  onLogout: () => void
  onNavigate: (view: ViewName) => void
  user: AuthenticatedUser
}

const navigation: { label: string; icon: Parameters<typeof Icon>[0]['name']; view: ViewName }[] = [
  { label: 'Dashboard', icon: 'dashboard', view: 'dashboard' },
  { label: 'People', icon: 'users', view: 'users' },
  { label: 'Courses', icon: 'courses', view: 'courses' },
  { label: 'Subjects', icon: 'subjects', view: 'subjects' },
  { label: 'Assignments', icon: 'assignments', view: 'assignments' },
  { label: 'Submissions', icon: 'submissions', view: 'submissions' },
]

export function AdminLayout({ activeView, children, onLogout, onNavigate, user }: AdminLayoutProps) {
  const [menuOpen, setMenuOpen] = useState(false)
  const navigate = (view: ViewName) => {
    onNavigate(view)
    setMenuOpen(false)
  }

  return (
    <div className="admin-app">
      <aside className={`sidebar ${menuOpen ? 'sidebar-open' : ''}`}>
        <div className="brand">
          <span className="brand-mark"><Icon name="school" size={25} /></span>
          <span><strong>Shikkha</strong><small>Admin workspace</small></span>
        </div>
        <nav aria-label="Admin navigation">
          <span className="nav-label">Workspace</span>
          {navigation.map((item) => (
            <button
              className={activeView === item.view ? 'nav-item active' : 'nav-item'}
              key={item.view}
              onClick={() => navigate(item.view)}
              type="button"
            >
              <Icon name={item.icon} />
              <span>{item.label}</span>
            </button>
          ))}
        </nav>
        <div className="sidebar-footer">
          <div className="user-avatar">{user.fullName.slice(0, 2).toUpperCase()}</div>
          <div className="sidebar-user"><strong>{user.fullName}</strong><small>{user.email}</small></div>
          <button aria-label="Log out" className="icon-button" onClick={onLogout} type="button">
            <Icon name="logout" />
          </button>
        </div>
      </aside>
      {menuOpen && <button aria-label="Close menu" className="menu-scrim" onClick={() => setMenuOpen(false)} />}
      <div className="main-shell">
        <header className="topbar">
          <button aria-label="Open menu" className="mobile-menu" onClick={() => setMenuOpen(true)} type="button">
            <Icon name="menu" />
          </button>
          <div>
            <span className="topbar-label">Admin portal</span>
            <strong>{navigation.find((item) => item.view === activeView)?.label}</strong>
          </div>
          <span className="role-chip">Administrator</span>
        </header>
        <main className="content">{children}</main>
      </div>
    </div>
  )
}

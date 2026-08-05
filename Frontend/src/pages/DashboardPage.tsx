import { useEffect, useState } from 'react'
import { apiRequest, ApiError } from '../lib/api'
import type { DashboardSummary, ViewName } from '../types'
import { Icon } from '../components/Icon'

interface DashboardPageProps {
  token: string
  onNavigate: (view: ViewName) => void
}

const initialSummary: DashboardSummary = {
  teachers: 0,
  students: 0,
  courses: 0,
  subjects: 0,
  assignments: 0,
  submissions: 0,
  pendingReviews: 0,
}

export function DashboardPage({ token, onNavigate }: DashboardPageProps) {
  const [summary, setSummary] = useState(initialSummary)
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState('')

  useEffect(() => {
    apiRequest<DashboardSummary>('/api/admin/dashboard', {}, token)
      .then(setSummary)
      .catch((requestError: unknown) =>
        setError(requestError instanceof ApiError ? requestError.message : 'Could not load dashboard data.'),
      )
      .finally(() => setLoading(false))
  }, [token])

  const cards = [
    { label: 'Teachers', value: summary.teachers, icon: 'users' as const, tone: 'violet' },
    { label: 'Students', value: summary.students, icon: 'users' as const, tone: 'blue' },
    { label: 'Courses', value: summary.courses, icon: 'courses' as const, tone: 'green' },
    { label: 'Subjects', value: summary.subjects, icon: 'subjects' as const, tone: 'amber' },
  ]

  return (
    <>
      <header className="page-heading">
        <div><span className="eyebrow">Overview</span><h1>Good to see you.</h1><p>Here is what is happening across your academic workspace.</p></div>
        <button className="primary-button" onClick={() => onNavigate('users')} type="button">Manage people <Icon name="arrow" size={18} /></button>
      </header>
      {error && <div className="alert error" role="alert">{error}</div>}
      <section aria-label="Academic totals" className="stat-grid">
        {cards.map((card) => (
          <article className="stat-card panel" key={card.label}>
            <span className={`stat-icon ${card.tone}`}><Icon name={card.icon} /></span>
            <span>{card.label}</span>
            <strong>{loading ? '—' : card.value.toLocaleString()}</strong>
          </article>
        ))}
      </section>
      <section className="dashboard-grid">
        <article className="panel activity-card">
          <div className="panel-heading"><div><span className="eyebrow">Learning activity</span><h2>Assignments & submissions</h2></div></div>
          <div className="activity-numbers">
            <button onClick={() => onNavigate('assignments')} type="button"><span>Assignments</span><strong>{loading ? '—' : summary.assignments}</strong><Icon name="arrow" /></button>
            <button onClick={() => onNavigate('submissions')} type="button"><span>Submissions</span><strong>{loading ? '—' : summary.submissions}</strong><Icon name="arrow" /></button>
          </div>
        </article>
        <article className="panel review-card">
          <span className="stat-icon amber"><Icon name="submissions" /></span>
          <div><span className="eyebrow">Needs attention</span><strong>{loading ? '—' : summary.pendingReviews}</strong><p>submissions waiting for teacher review</p></div>
          <button className="text-button" onClick={() => onNavigate('submissions')} type="button">Review queue <Icon name="arrow" size={17} /></button>
        </article>
      </section>
    </>
  )
}

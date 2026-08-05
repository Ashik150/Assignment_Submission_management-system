import { useEffect, useState } from 'react'
import { Icon } from '../components/Icon'
import { ApiError, apiRequest } from '../lib/api'
import type { TeacherDashboardSummary, ViewName } from '../types'

interface TeacherDashboardPageProps {
  token: string
  teacherName: string
  onNavigate: (view: ViewName) => void
}

const initialSummary: TeacherDashboardSummary = {
  assignedSubjects: 0,
  assignments: 0,
  publishedAssignments: 0,
  submissions: 0,
  pendingReviews: 0,
}

export function TeacherDashboardPage({ token, teacherName, onNavigate }: TeacherDashboardPageProps) {
  const [summary, setSummary] = useState(initialSummary)
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState('')

  useEffect(() => {
    apiRequest<TeacherDashboardSummary>('/api/teacher/dashboard', {}, token)
      .then(setSummary)
      .catch((requestError: unknown) =>
        setError(requestError instanceof ApiError ? requestError.message : 'Could not load your dashboard.'),
      )
      .finally(() => setLoading(false))
  }, [token])

  const firstName = teacherName.trim().split(/\s+/)[0]
  const cards = [
    { label: 'Assigned subjects', value: summary.assignedSubjects, icon: 'subjects' as const, tone: 'violet' },
    { label: 'My assignments', value: summary.assignments, icon: 'assignments' as const, tone: 'blue' },
    { label: 'Published', value: summary.publishedAssignments, icon: 'assignments' as const, tone: 'green' },
    { label: 'Submissions', value: summary.submissions, icon: 'submissions' as const, tone: 'amber' },
  ]

  return (
    <>
      <header className="page-heading">
        <div><span className="eyebrow">Teaching overview</span><h1>Welcome back, {firstName}.</h1><p>Manage your assignments and keep student reviews moving.</p></div>
        <button className="primary-button" onClick={() => onNavigate('assignments')} type="button"><Icon name="plus" size={18} /> New assignment</button>
      </header>
      {error && <div className="alert error page-alert" role="alert">{error}</div>}
      <section aria-label="Teaching totals" className="stat-grid">
        {cards.map((card) => (
          <article className="stat-card panel" key={card.label}>
            <span className={`stat-icon ${card.tone}`}><Icon name={card.icon} /></span>
            <span>{card.label}</span>
            <strong>{loading ? '—' : card.value.toLocaleString()}</strong>
          </article>
        ))}
      </section>
      <section className="dashboard-grid teacher-dashboard-grid">
        <article className="panel teacher-action-card">
          <span className="catalog-icon assignment"><Icon name="assignments" /></span>
          <div><span className="eyebrow">Assignment studio</span><h2>Create and publish learning tasks</h2><p>Choose one of your assigned subjects, set a deadline and marks, then publish when ready.</p></div>
          <button className="text-button" onClick={() => onNavigate('assignments')} type="button">Manage assignments <Icon name="arrow" size={17} /></button>
        </article>
        <article className="panel review-card">
          <span className="stat-icon amber"><Icon name="submissions" /></span>
          <div><span className="eyebrow">Needs your review</span><strong>{loading ? '—' : summary.pendingReviews}</strong><p>submissions awaiting marks or feedback</p></div>
          <button className="text-button" onClick={() => onNavigate('submissions')} type="button">Open review queue <Icon name="arrow" size={17} /></button>
        </article>
      </section>
    </>
  )
}

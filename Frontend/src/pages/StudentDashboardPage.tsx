import { useEffect, useState } from 'react'
import { Icon } from '../components/Icon'
import { ApiError, apiRequest } from '../lib/api'
import type { StudentDashboardSummary, ViewName } from '../types'

interface StudentDashboardPageProps {
  token: string
  studentName: string
  onNavigate: (view: ViewName) => void
}

const initialSummary: StudentDashboardSummary = {
  courseId: '',
  courseName: '',
  availableAssignments: 0,
  dueThisWeek: 0,
  submittedAssignments: 0,
  awaitingSubmission: 0,
}

export function StudentDashboardPage({ token, studentName, onNavigate }: StudentDashboardPageProps) {
  const [summary, setSummary] = useState(initialSummary)
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState('')

  useEffect(() => {
    apiRequest<StudentDashboardSummary>('/api/student/dashboard', {}, token)
      .then(setSummary)
      .catch((requestError: unknown) =>
        setError(requestError instanceof ApiError ? requestError.message : 'Could not load your dashboard.'),
      )
      .finally(() => setLoading(false))
  }, [token])

  const firstName = studentName.trim().split(/\s+/)[0]
  const cards = [
    { label: 'Available assignments', value: summary.availableAssignments, icon: 'assignments' as const, tone: 'violet' },
    { label: 'Due this week', value: summary.dueThisWeek, icon: 'assignments' as const, tone: 'amber' },
    { label: 'Submitted', value: summary.submittedAssignments, icon: 'submissions' as const, tone: 'green' },
    { label: 'Awaiting submission', value: summary.awaitingSubmission, icon: 'subjects' as const, tone: 'blue' },
  ]

  return (
    <>
      <header className="page-heading">
        <div><span className="eyebrow">Learning overview</span><h1>Hello, {firstName}.</h1><p>Stay on top of your assignments and feedback.</p></div>
        <button className="primary-button" onClick={() => onNavigate('assignments')} type="button">View assignments <Icon name="arrow" size={18} /></button>
      </header>
      {error && <div className="alert error page-alert" role="alert">{error}</div>}
      {!error && <div className="course-banner"><span className="catalog-icon"><Icon name="courses" /></span><div><span>Your class / course</span><strong>{loading ? 'Loading…' : summary.courseName}</strong></div></div>}
      <section aria-label="Student totals" className="stat-grid student-stat-grid">
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
          <div><span className="eyebrow">Next steps</span><h2>Check your assigned work</h2><p>Open an assignment to read its instructions, deadline, and maximum marks before submitting.</p></div>
          <button className="text-button" onClick={() => onNavigate('assignments')} type="button">Open assignments <Icon name="arrow" size={17} /></button>
        </article>
        <article className="panel review-card">
          <span className="stat-icon green"><Icon name="submissions" /></span>
          <div><span className="eyebrow">Your progress</span><strong>{loading ? '—' : summary.submittedAssignments}</strong><p>answers submitted across your course</p></div>
          <button className="text-button" onClick={() => onNavigate('submissions')} type="button">View results <Icon name="arrow" size={17} /></button>
        </article>
      </section>
    </>
  )
}

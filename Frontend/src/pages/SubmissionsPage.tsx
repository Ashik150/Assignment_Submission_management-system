import { useCallback, useEffect, useState } from 'react'
import { Icon } from '../components/Icon'
import { Modal } from '../components/Modal'
import { ApiError, apiRequest } from '../lib/api'
import type { AdminAssignment, AdminSubmission, SubmissionStatus } from '../types'

interface SubmissionsPageProps { token: string }

export function SubmissionsPage({ token }: SubmissionsPageProps) {
  const [submissions, setSubmissions] = useState<AdminSubmission[]>([])
  const [assignments, setAssignments] = useState<AdminAssignment[]>([])
  const [assignmentFilter, setAssignmentFilter] = useState('')
  const [statusFilter, setStatusFilter] = useState<'' | SubmissionStatus>('')
  const [search, setSearch] = useState('')
  const [selected, setSelected] = useState<AdminSubmission | null>(null)
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState('')

  const loadSubmissions = useCallback(async () => {
    setLoading(true)
    setError('')
    const params = new URLSearchParams()
    if (assignmentFilter) params.set('assignmentId', assignmentFilter)
    if (statusFilter) params.set('status', statusFilter)
    if (search.trim()) params.set('search', search.trim())
    try {
      setSubmissions(await apiRequest<AdminSubmission[]>(`/api/admin/submissions?${params}`, {}, token))
    } catch (requestError) {
      setError(requestError instanceof ApiError ? requestError.message : 'Could not load submissions.')
    } finally {
      setLoading(false)
    }
  }, [assignmentFilter, search, statusFilter, token])

  useEffect(() => {
    apiRequest<AdminAssignment[]>('/api/admin/assignments', {}, token)
      .then(setAssignments)
      .catch(() => undefined)
  }, [token])

  useEffect(() => {
    const timer = window.setTimeout(loadSubmissions, 250)
    return () => window.clearTimeout(timer)
  }, [loadSubmissions])

  return (
    <>
      <header className="page-heading">
        <div><span className="eyebrow">System oversight</span><h1>All submissions</h1><p>Track student work, marking progress, and teacher feedback.</p></div>
      </header>
      {error && <div className="alert error page-alert" role="alert">{error}</div>}
      <section className="panel data-panel">
        <div className="table-toolbar assignment-filters">
          <span className="record-count">{submissions.length} {submissions.length === 1 ? 'submission' : 'submissions'}</span>
          <select aria-label="Filter by assignment" className="filter-select assignment-select" onChange={(event) => setAssignmentFilter(event.target.value)} value={assignmentFilter}><option value="">All assignments</option>{assignments.map((assignment) => <option key={assignment.id} value={assignment.id}>{assignment.title}</option>)}</select>
          <select aria-label="Filter by status" className="filter-select" onChange={(event) => setStatusFilter(event.target.value as '' | SubmissionStatus)} value={statusFilter}><option value="">Any status</option><option value="Submitted">Submitted</option><option value="Late">Late</option><option value="Reviewed">Reviewed</option><option value="Returned">Returned</option></select>
          <label className="search-field"><Icon name="search" size={18} /><input aria-label="Search submissions" onChange={(event) => setSearch(event.target.value)} placeholder="Student or assignment" value={search} /></label>
        </div>
        <div className="table-scroll">
          <table>
            <thead><tr><th>Student</th><th>Assignment</th><th>Submitted</th><th>Status</th><th>Marks</th><th><span className="sr-only">Actions</span></th></tr></thead>
            <tbody>{!loading && submissions.map((submission) => (
              <tr key={submission.id}>
                <td><div className="person-cell"><span className="table-avatar student">{submission.studentName.slice(0, 2).toUpperCase()}</span><span><strong>{submission.studentName}</strong><small>{submission.studentEmail}</small></span></div></td>
                <td><strong className="table-title">{submission.assignmentTitle}</strong></td>
                <td><span className="deadline">{new Date(submission.submittedAt).toLocaleDateString(undefined, { day: 'numeric', month: 'short', year: 'numeric' })}<small>{new Date(submission.submittedAt).toLocaleTimeString(undefined, { hour: '2-digit', minute: '2-digit' })}</small></span></td>
                <td><span className={`submission-status ${submission.status.toLowerCase()}`}>{submission.status}</span></td>
                <td><span className="marks">{submission.marks ?? '—'}<small> / {submission.maximumMarks}</small></span></td>
                <td><button className="view-button" onClick={() => setSelected(submission)} type="button">View <Icon name="arrow" size={15} /></button></td>
              </tr>
            ))}</tbody>
          </table>
          {loading && <div className="table-message">Loading submissions…</div>}
          {!loading && submissions.length === 0 && <div className="table-message"><strong>No submissions found</strong><span>Student submissions will appear here when available.</span></div>}
        </div>
      </section>
      {selected && <Modal onClose={() => setSelected(null)} subtitle={`${selected.studentName} · ${new Date(selected.submittedAt).toLocaleString()}`} title={selected.assignmentTitle}>
        <div className="submission-detail">
          <div className="detail-meta"><div><span>Status</span><strong className={`submission-status ${selected.status.toLowerCase()}`}>{selected.status}</strong></div><div><span>Marks</span><strong>{selected.marks ?? 'Not marked'}{selected.marks !== null && ` / ${selected.maximumMarks}`}</strong></div></div>
          <section><span className="detail-label">Student answer</span><p>{selected.answer}</p></section>
          <section><span className="detail-label">Teacher feedback</span><p className={!selected.feedback ? 'muted-copy' : ''}>{selected.feedback || 'No feedback has been provided yet.'}</p></section>
          <footer className="detail-footer">Last updated {new Date(selected.updatedAt).toLocaleString()}</footer>
        </div>
      </Modal>}
    </>
  )
}

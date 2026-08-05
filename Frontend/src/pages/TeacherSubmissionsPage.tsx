import { useCallback, useEffect, useState, type FormEvent } from 'react'
import { Icon } from '../components/Icon'
import { Modal } from '../components/Modal'
import { PdfAttachment } from '../components/PdfAttachment'
import { ApiError, apiRequest } from '../lib/api'
import type { AdminSubmission, SubmissionStatus, TeacherAssignment } from '../types'

interface TeacherSubmissionsPageProps { token: string }

interface ReviewForm {
  marks: string
  feedback: string
  status: SubmissionStatus
}

export function TeacherSubmissionsPage({ token }: TeacherSubmissionsPageProps) {
  const [submissions, setSubmissions] = useState<AdminSubmission[]>([])
  const [assignments, setAssignments] = useState<TeacherAssignment[]>([])
  const [assignmentFilter, setAssignmentFilter] = useState('')
  const [statusFilter, setStatusFilter] = useState<'' | SubmissionStatus>('')
  const [search, setSearch] = useState('')
  const [selected, setSelected] = useState<AdminSubmission | null>(null)
  const [form, setForm] = useState<ReviewForm>({ marks: '', feedback: '', status: 'Submitted' })
  const [loading, setLoading] = useState(true)
  const [saving, setSaving] = useState(false)
  const [error, setError] = useState('')

  const loadSubmissions = useCallback(async () => {
    setLoading(true)
    setError('')
    const params = new URLSearchParams()
    if (assignmentFilter) params.set('assignmentId', assignmentFilter)
    if (statusFilter) params.set('status', statusFilter)
    if (search.trim()) params.set('search', search.trim())
    try {
      setSubmissions(await apiRequest<AdminSubmission[]>(`/api/teacher/submissions?${params}`, {}, token))
    } catch (requestError) {
      setError(requestError instanceof ApiError ? requestError.message : 'Could not load student submissions.')
    } finally {
      setLoading(false)
    }
  }, [assignmentFilter, search, statusFilter, token])

  useEffect(() => {
    apiRequest<TeacherAssignment[]>('/api/teacher/assignments', {}, token)
      .then(setAssignments)
      .catch(() => undefined)
  }, [token])

  useEffect(() => {
    const timer = window.setTimeout(loadSubmissions, 250)
    return () => window.clearTimeout(timer)
  }, [loadSubmissions])

  const openReview = (submission: AdminSubmission) => {
    setSelected(submission)
    setForm({
      marks: submission.marks === null ? '' : String(submission.marks),
      feedback: submission.feedback,
      status: submission.status,
    })
    setError('')
  }

  const saveReview = async (event: FormEvent<HTMLFormElement>) => {
    event.preventDefault()
    if (!selected) return
    setSaving(true)
    setError('')
    try {
      const updated = await apiRequest<AdminSubmission>(
        `/api/teacher/submissions/${selected.id}/review`,
        {
          method: 'PUT',
          body: JSON.stringify({
            marks: form.marks === '' ? null : Number(form.marks),
            feedback: form.feedback,
            status: form.status,
          }),
        },
        token,
      )
      setSubmissions((current) => current.map((submission) => submission.id === updated.id ? updated : submission))
      setSelected(null)
    } catch (requestError) {
      setError(requestError instanceof ApiError ? requestError.message : 'Could not save this review.')
    } finally {
      setSaving(false)
    }
  }

  return (
    <>
      <header className="page-heading">
        <div><span className="eyebrow">Student work</span><h1>Submissions</h1><p>Read answers, provide feedback, assign marks, and update review status.</p></div>
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
                <td><button className="view-button" onClick={() => openReview(submission)} type="button">Review <Icon name="arrow" size={15} /></button></td>
              </tr>
            ))}</tbody>
          </table>
          {loading && <div className="table-message">Loading submissions…</div>}
          {!loading && submissions.length === 0 && <div className="table-message"><strong>No submissions found</strong><span>Student work for your assignments will appear here.</span></div>}
        </div>
      </section>
      {selected && <Modal onClose={() => setSelected(null)} subtitle={`${selected.studentName} · Submitted ${new Date(selected.submittedAt).toLocaleString()}`} title={selected.assignmentTitle}>
        <form className="submission-review-form" onSubmit={saveReview}>
          <section className="answer-panel"><span className="detail-label">Student answer</span><p className={!selected.answer ? 'muted-copy' : ''}>{selected.answer || 'No written answer was included.'}</p>{selected.pdfFileName && selected.pdfFileSize !== null && <PdfAttachment fileName={selected.pdfFileName} fileSize={selected.pdfFileSize} submissionId={selected.id} token={token} />}</section>
          <div className="form-grid review-fields">
            <label>Marks (maximum {selected.maximumMarks})<input max={selected.maximumMarks} min="0" onChange={(event) => setForm({ ...form, marks: event.target.value })} required={form.status === 'Reviewed'} step="0.01" type="number" value={form.marks} /></label>
            <label>Submission status<select onChange={(event) => setForm({ ...form, status: event.target.value as SubmissionStatus })} value={form.status}><option value="Submitted">Submitted</option><option value="Late">Late</option><option value="Reviewed">Reviewed</option><option value="Returned">Returned for revision</option></select></label>
            <label className="span-2">Feedback<textarea maxLength={2000} onChange={(event) => setForm({ ...form, feedback: event.target.value })} placeholder="Share clear, constructive feedback with the student" rows={5} value={form.feedback} /></label>
          </div>
          {error && <div className="alert error form-alert" role="alert">{error}</div>}
          <footer className="form-actions"><button className="secondary-button" onClick={() => setSelected(null)} type="button">Cancel</button><button className="primary-button" disabled={saving} type="submit">{saving ? 'Saving…' : 'Save review'}</button></footer>
        </form>
      </Modal>}
    </>
  )
}

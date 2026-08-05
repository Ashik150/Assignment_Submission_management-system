import { useCallback, useEffect, useState, type FormEvent } from 'react'
import { Icon } from '../components/Icon'
import { Modal } from '../components/Modal'
import { PdfAttachment } from '../components/PdfAttachment'
import { ApiError, apiRequest } from '../lib/api'
import type { StudentSubmission, SubmissionStatus } from '../types'

interface StudentSubmissionsPageProps { token: string }

export function StudentSubmissionsPage({ token }: StudentSubmissionsPageProps) {
  const [submissions, setSubmissions] = useState<StudentSubmission[]>([])
  const [statusFilter, setStatusFilter] = useState<'' | SubmissionStatus>('')
  const [selected, setSelected] = useState<StudentSubmission | null>(null)
  const [answer, setAnswer] = useState('')
  const [pdf, setPdf] = useState<File | null>(null)
  const [removePdf, setRemovePdf] = useState(false)
  const [editing, setEditing] = useState(false)
  const [loading, setLoading] = useState(true)
  const [saving, setSaving] = useState(false)
  const [error, setError] = useState('')

  const loadSubmissions = useCallback(async () => {
    setLoading(true)
    setError('')
    const params = new URLSearchParams()
    if (statusFilter) params.set('status', statusFilter)
    try {
      setSubmissions(await apiRequest<StudentSubmission[]>(`/api/student/submissions?${params}`, {}, token))
    } catch (requestError) {
      setError(requestError instanceof ApiError ? requestError.message : 'Could not load your submissions.')
    } finally {
      setLoading(false)
    }
  }, [statusFilter, token])

  useEffect(() => { loadSubmissions() }, [loadSubmissions])

  const openSubmission = (submission: StudentSubmission) => {
    setSelected(submission)
    setAnswer(submission.answer)
    setPdf(null)
    setRemovePdf(false)
    setEditing(false)
    setError('')
  }

  const updateAnswer = async (event: FormEvent<HTMLFormElement>) => {
    event.preventDefault()
    if (!selected) return
    setSaving(true)
    setError('')
    if (!answer.trim() && !pdf && (!selected.pdfFileName || removePdf)) {
      setError('Write an answer, attach a PDF, or provide both.')
      setSaving(false)
      return
    }

    const formData = new FormData()
    formData.append('answer', answer)
    if (pdf) formData.append('pdf', pdf)
    if (removePdf) formData.append('removePdf', 'true')
    try {
      const updated = await apiRequest<StudentSubmission>(
        `/api/student/submissions/${selected.id}`,
        { method: 'PUT', body: formData },
        token,
      )
      setSubmissions((current) => current.map((submission) => submission.id === updated.id ? updated : submission))
      setSelected(updated)
      setPdf(null)
      setRemovePdf(false)
      setEditing(false)
    } catch (requestError) {
      setError(requestError instanceof ApiError ? requestError.message : 'Could not update your answer.')
    } finally {
      setSaving(false)
    }
  }

  return (
    <>
      <header className="page-heading">
        <div><span className="eyebrow">Your progress</span><h1>My submissions</h1><p>Track review status, marks, teacher feedback, and your submitted answers.</p></div>
      </header>
      {error && !selected && <div className="alert error page-alert" role="alert">{error}</div>}
      <section className="panel data-panel">
        <div className="table-toolbar table-toolbar-right">
          <span className="record-count">{submissions.length} {submissions.length === 1 ? 'submission' : 'submissions'}</span>
          <select aria-label="Filter by status" className="filter-select" onChange={(event) => setStatusFilter(event.target.value as '' | SubmissionStatus)} value={statusFilter}><option value="">Any status</option><option value="Submitted">Submitted</option><option value="Late">Late</option><option value="Reviewed">Reviewed</option><option value="Returned">Returned</option></select>
        </div>
        <div className="table-scroll">
          <table>
            <thead><tr><th>Assignment</th><th>Submitted</th><th>Status</th><th>Marks</th><th>Deadline</th><th><span className="sr-only">Actions</span></th></tr></thead>
            <tbody>{!loading && submissions.map((submission) => (
              <tr key={submission.id}>
                <td><div className="title-cell assignment-title"><span className="catalog-icon assignment"><Icon name="submissions" size={19} /></span><span><strong>{submission.assignmentTitle}</strong><small>{submission.subjectName}</small></span></div></td>
                <td><span className="deadline">{new Date(submission.submittedAt).toLocaleDateString(undefined, { day: 'numeric', month: 'short', year: 'numeric' })}<small>{new Date(submission.submittedAt).toLocaleTimeString(undefined, { hour: '2-digit', minute: '2-digit' })}</small></span></td>
                <td><span className={`submission-status ${submission.status.toLowerCase()}`}>{submission.status}</span></td>
                <td><span className="marks">{submission.marks ?? '—'}<small> / {submission.maximumMarks}</small></span></td>
                <td><span className={new Date(submission.deadline).getTime() <= Date.now() ? 'deadline overdue' : 'deadline'}>{new Date(submission.deadline).toLocaleDateString(undefined, { day: 'numeric', month: 'short', year: 'numeric' })}<small>{submission.canUpdate ? 'Updates allowed' : 'Updates closed'}</small></span></td>
                <td><button className="view-button" onClick={() => openSubmission(submission)} type="button">View <Icon name="arrow" size={15} /></button></td>
              </tr>
            ))}</tbody>
          </table>
          {loading && <div className="table-message">Loading submissions…</div>}
          {!loading && submissions.length === 0 && <div className="table-message"><strong>No submissions found</strong><span>Your submitted assignment answers will appear here.</span></div>}
        </div>
      </section>
      {selected && <Modal onClose={() => setSelected(null)} subtitle={`${selected.subjectName} · Submitted ${new Date(selected.submittedAt).toLocaleString()}`} title={selected.assignmentTitle}>
        <div className="student-submission-detail">
          <div className="detail-meta"><div><span>Status</span><strong className={`submission-status ${selected.status.toLowerCase()}`}>{selected.status}</strong></div><div><span>Marks</span><strong>{selected.marks === null ? 'Not marked' : `${selected.marks} / ${selected.maximumMarks}`}</strong></div></div>
          {selected.feedback && <section className="feedback-panel"><span className="detail-label">Teacher feedback</span><p>{selected.feedback}</p></section>}
          {!editing ? <section><span className="detail-label">Your answer</span><p className={selected.answer ? 'submitted-answer' : 'submitted-answer muted-copy'}>{selected.answer || 'No written answer was included.'}</p>{selected.pdfFileName && selected.pdfFileSize !== null && <PdfAttachment fileName={selected.pdfFileName} fileSize={selected.pdfFileSize} submissionId={selected.id} token={token} />}</section> : <form className="answer-form submission-update-form" onSubmit={updateAnswer}><label>Update your written answer (optional)<textarea autoFocus maxLength={10000} onChange={(event) => setAnswer(event.target.value)} placeholder="Write your answer here, attach a PDF below, or do both…" rows={7} value={answer} /></label><div className="pdf-upload"><label>Replace or add a PDF (optional)<input accept=".pdf,application/pdf" onChange={(event) => { const file = event.target.files?.[0] ?? null; if (file && file.size > 10 * 1024 * 1024) { setError('The PDF must be 10 MB or smaller.'); event.target.value = ''; setPdf(null); return } setError(''); setPdf(file); if (file) setRemovePdf(false) }} type="file" /></label><small>PDF only, maximum 10 MB.</small>{selected.pdfFileName && selected.pdfFileSize !== null && !pdf && <>{!removePdf && <PdfAttachment fileName={selected.pdfFileName} fileSize={selected.pdfFileSize} submissionId={selected.id} token={token} />}<label className="remove-file"><input checked={removePdf} onChange={(event) => setRemovePdf(event.target.checked)} type="checkbox" /> Remove the current PDF</label></>}</div>{error && <div className="alert error" role="alert">{error}</div>}<footer className="form-actions"><button className="secondary-button" onClick={() => { setEditing(false); setAnswer(selected.answer); setPdf(null); setRemovePdf(false) }} type="button">Cancel</button><button className="primary-button" disabled={saving} type="submit">{saving ? 'Updating…' : 'Update submission'}</button></footer></form>}
          {!editing && <footer className="submission-detail-actions"><span>{selected.canUpdate ? `You can update this answer until ${new Date(selected.deadline).toLocaleString()}.` : 'This submission can no longer be updated.'}</span>{selected.canUpdate && <button className="primary-button" onClick={() => setEditing(true)} type="button"><Icon name="edit" size={17} /> Update answer</button>}</footer>}
        </div>
      </Modal>}
    </>
  )
}

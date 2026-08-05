import { useCallback, useEffect, useMemo, useState, type FormEvent } from 'react'
import { Icon } from '../components/Icon'
import { Modal } from '../components/Modal'
import { ApiError, apiRequest } from '../lib/api'
import type { StudentAssignment, StudentSubmission } from '../types'

interface StudentAssignmentsPageProps { token: string }

export function StudentAssignmentsPage({ token }: StudentAssignmentsPageProps) {
  const [assignments, setAssignments] = useState<StudentAssignment[]>([])
  const [subjectFilter, setSubjectFilter] = useState('')
  const [search, setSearch] = useState('')
  const [selected, setSelected] = useState<StudentAssignment | null>(null)
  const [submission, setSubmission] = useState<StudentSubmission | null>(null)
  const [answer, setAnswer] = useState('')
  const [loading, setLoading] = useState(true)
  const [loadingDetail, setLoadingDetail] = useState(false)
  const [saving, setSaving] = useState(false)
  const [error, setError] = useState('')

  const subjects = useMemo(() => {
    const map = new Map<string, string>()
    assignments.forEach((assignment) => map.set(assignment.subjectId, assignment.subjectName))
    return Array.from(map, ([id, name]) => ({ id, name })).sort((a, b) => a.name.localeCompare(b.name))
  }, [assignments])

  const loadAssignments = useCallback(async () => {
    setLoading(true)
    setError('')
    const params = new URLSearchParams()
    if (subjectFilter) params.set('subjectId', subjectFilter)
    if (search.trim()) params.set('search', search.trim())
    try {
      setAssignments(await apiRequest<StudentAssignment[]>(`/api/student/assignments?${params}`, {}, token))
    } catch (requestError) {
      setError(requestError instanceof ApiError ? requestError.message : 'Could not load your assignments.')
    } finally {
      setLoading(false)
    }
  }, [search, subjectFilter, token])

  useEffect(() => {
    const timer = window.setTimeout(loadAssignments, 250)
    return () => window.clearTimeout(timer)
  }, [loadAssignments])

  const openAssignment = async (assignment: StudentAssignment) => {
    setSelected(assignment)
    setSubmission(null)
    setAnswer('')
    setError('')
    if (!assignment.submissionId) return

    setLoadingDetail(true)
    try {
      const detail = await apiRequest<StudentSubmission>(
        `/api/student/submissions/${assignment.submissionId}`,
        {},
        token,
      )
      setSubmission(detail)
      setAnswer(detail.answer)
    } catch (requestError) {
      setError(requestError instanceof ApiError ? requestError.message : 'Could not load your answer.')
    } finally {
      setLoadingDetail(false)
    }
  }

  const saveAnswer = async (event: FormEvent<HTMLFormElement>) => {
    event.preventDefault()
    if (!selected) return
    setSaving(true)
    setError('')
    try {
      await apiRequest<StudentSubmission>(
        submission
          ? `/api/student/submissions/${submission.id}`
          : `/api/student/assignments/${selected.id}/submission`,
        { method: submission ? 'PUT' : 'POST', body: JSON.stringify({ answer }) },
        token,
      )
      setSelected(null)
      setSubmission(null)
      await loadAssignments()
    } catch (requestError) {
      setError(requestError instanceof ApiError ? requestError.message : 'Could not save your answer.')
    } finally {
      setSaving(false)
    }
  }

  return (
    <>
      <header className="page-heading">
        <div><span className="eyebrow">Your coursework</span><h1>Assignments</h1><p>Review instructions, watch deadlines, and submit your answers.</p></div>
      </header>
      {error && !selected && <div className="alert error page-alert" role="alert">{error}</div>}
      <section className="panel assignment-browser">
        <div className="table-toolbar assignment-filters">
          <span className="record-count">{assignments.length} {assignments.length === 1 ? 'assignment' : 'assignments'}</span>
          <select aria-label="Filter by subject" className="filter-select" onChange={(event) => setSubjectFilter(event.target.value)} value={subjectFilter}><option value="">All subjects</option>{subjects.map((subject) => <option key={subject.id} value={subject.id}>{subject.name}</option>)}</select>
          <label className="search-field"><Icon name="search" size={18} /><input aria-label="Search assignments" onChange={(event) => setSearch(event.target.value)} placeholder="Search assignment title" value={search} /></label>
        </div>
        {loading ? <div className="table-message">Loading assignments…</div> : assignments.length === 0 ? <div className="table-message"><strong>No assignments found</strong><span>Published assignments for your course will appear here.</span></div> : <div className="student-assignment-grid">
          {assignments.map((assignment) => {
            const deadline = new Date(assignment.deadline)
            const overdue = deadline.getTime() <= Date.now()
            return <article className="student-assignment-card" key={assignment.id}>
              <header><span className="catalog-icon assignment"><Icon name="assignments" size={19} /></span><span className={assignment.submissionStatus ? `submission-status ${assignment.submissionStatus.toLowerCase()}` : overdue ? 'assignment-status closed' : 'assignment-status open'}>{assignment.submissionStatus || (overdue ? 'Closed' : 'Open')}</span></header>
              <div><span className="eyebrow">{assignment.subjectName}</span><h2>{assignment.title}</h2><p>{assignment.description || 'Open the assignment to view its details.'}</p></div>
              <dl><div><dt>Teacher</dt><dd>{assignment.teacherName}</dd></div><div><dt>Maximum marks</dt><dd>{assignment.maximumMarks}</dd></div><div><dt>Deadline</dt><dd className={overdue ? 'overdue' : ''}>{deadline.toLocaleString(undefined, { day: 'numeric', month: 'short', hour: '2-digit', minute: '2-digit' })}</dd></div></dl>
              <button className="secondary-button card-button" onClick={() => openAssignment(assignment)} type="button">View details <Icon name="arrow" size={16} /></button>
            </article>
          })}
        </div>}
      </section>
      {selected && <Modal onClose={() => setSelected(null)} subtitle={`${selected.subjectName} · ${selected.teacherName}`} title={selected.title}>
        <div className="student-assignment-detail">
          <div className="detail-meta assignment-meta"><div><span>Deadline</span><strong>{new Date(selected.deadline).toLocaleString()}</strong></div><div><span>Maximum marks</span><strong>{selected.maximumMarks}</strong></div></div>
          <section><span className="detail-label">Instructions</span><p>{selected.description || 'No additional instructions were provided.'}</p></section>
          {selected.submissionStatus && <section className="current-result"><span className="detail-label">Current submission</span><div><span className={`submission-status ${selected.submissionStatus.toLowerCase()}`}>{selected.submissionStatus}</span>{selected.marks !== null && <strong>{selected.marks} / {selected.maximumMarks}</strong>}</div>{selected.feedback && <p><strong>Teacher feedback:</strong> {selected.feedback}</p>}</section>}
          {loadingDetail ? <div className="table-message compact">Loading your answer…</div> : (selected.canSubmit || selected.canUpdateSubmission) && <form className="answer-form" onSubmit={saveAnswer}><label>{submission ? 'Update your answer' : 'Your answer'}<textarea autoFocus maxLength={10000} minLength={1} onChange={(event) => setAnswer(event.target.value)} placeholder="Write your answer here…" required rows={8} value={answer} /></label>{error && <div className="alert error" role="alert">{error}</div>}<footer className="form-actions"><button className="secondary-button" onClick={() => setSelected(null)} type="button">Cancel</button><button className="primary-button" disabled={saving} type="submit">{saving ? 'Saving…' : submission ? 'Update submission' : 'Submit answer'}</button></footer></form>}
          {!loadingDetail && !selected.canSubmit && !selected.canUpdateSubmission && !selected.submissionId && <div className="alert info detail-notice">The deadline has passed. This assignment no longer accepts submissions.</div>}
        </div>
      </Modal>}
    </>
  )
}

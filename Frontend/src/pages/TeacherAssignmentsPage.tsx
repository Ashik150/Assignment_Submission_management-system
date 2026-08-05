import { useCallback, useEffect, useMemo, useState, type FormEvent } from 'react'
import { Icon } from '../components/Icon'
import { Modal } from '../components/Modal'
import { ApiError, apiRequest } from '../lib/api'
import type { AssignmentStatus, TeacherAssignment, TeacherSubject } from '../types'

interface TeacherAssignmentsPageProps { token: string }

interface AssignmentForm {
  title: string
  description: string
  courseId: string
  subjectId: string
  deadline: string
  maximumMarks: string
  status: AssignmentStatus
}

function toLocalDateTime(value: string | Date) {
  const date = new Date(value)
  const local = new Date(date.getTime() - date.getTimezoneOffset() * 60_000)
  return local.toISOString().slice(0, 16)
}

function tomorrow() {
  const date = new Date()
  date.setDate(date.getDate() + 1)
  date.setHours(23, 59, 0, 0)
  return toLocalDateTime(date)
}

export function TeacherAssignmentsPage({ token }: TeacherAssignmentsPageProps) {
  const [assignments, setAssignments] = useState<TeacherAssignment[]>([])
  const [subjects, setSubjects] = useState<TeacherSubject[]>([])
  const [courseFilter, setCourseFilter] = useState('')
  const [statusFilter, setStatusFilter] = useState<'' | AssignmentStatus>('')
  const [search, setSearch] = useState('')
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState('')
  const [editing, setEditing] = useState<TeacherAssignment | null>(null)
  const [formOpen, setFormOpen] = useState(false)
  const [saving, setSaving] = useState(false)
  const [form, setForm] = useState<AssignmentForm>({
    title: '', description: '', courseId: '', subjectId: '', deadline: tomorrow(), maximumMarks: '100', status: 'Draft',
  })

  const activeSubjects = useMemo(
    () => subjects.filter((subject) => subject.isActive && subject.isCourseActive),
    [subjects],
  )
  const courses = useMemo(() => {
    const map = new Map<string, string>()
    subjects.forEach((subject) => map.set(subject.courseId, subject.courseName))
    return Array.from(map, ([id, name]) => ({ id, name })).sort((a, b) => a.name.localeCompare(b.name))
  }, [subjects])

  const loadAssignments = useCallback(async () => {
    setLoading(true)
    setError('')
    const params = new URLSearchParams()
    if (courseFilter) params.set('courseId', courseFilter)
    if (statusFilter) params.set('status', statusFilter)
    if (search.trim()) params.set('search', search.trim())
    try {
      setAssignments(await apiRequest<TeacherAssignment[]>(`/api/teacher/assignments?${params}`, {}, token))
    } catch (requestError) {
      setError(requestError instanceof ApiError ? requestError.message : 'Could not load your assignments.')
    } finally {
      setLoading(false)
    }
  }, [courseFilter, search, statusFilter, token])

  useEffect(() => {
    apiRequest<TeacherSubject[]>('/api/teacher/subjects', {}, token)
      .then(setSubjects)
      .catch((requestError: unknown) =>
        setError(requestError instanceof ApiError ? requestError.message : 'Could not load your assigned subjects.'),
      )
  }, [token])

  useEffect(() => {
    const timer = window.setTimeout(loadAssignments, 250)
    return () => window.clearTimeout(timer)
  }, [loadAssignments])

  const openCreate = () => {
    const firstSubject = activeSubjects[0]
    setEditing(null)
    setForm({
      title: '',
      description: '',
      courseId: firstSubject?.courseId || '',
      subjectId: firstSubject?.id || '',
      deadline: tomorrow(),
      maximumMarks: '100',
      status: 'Draft',
    })
    setError('')
    setFormOpen(true)
  }

  const openEdit = (assignment: TeacherAssignment) => {
    setEditing(assignment)
    setForm({
      title: assignment.title,
      description: assignment.description,
      courseId: assignment.courseId,
      subjectId: assignment.subjectId,
      deadline: toLocalDateTime(assignment.deadline),
      maximumMarks: String(assignment.maximumMarks),
      status: assignment.status,
    })
    setError('')
    setFormOpen(true)
  }

  const chooseCourse = (courseId: string) => {
    const firstSubject = activeSubjects.find((subject) => subject.courseId === courseId)
    setForm({ ...form, courseId, subjectId: firstSubject?.id || '' })
  }

  const save = async (event: FormEvent<HTMLFormElement>) => {
    event.preventDefault()
    setSaving(true)
    setError('')
    try {
      await apiRequest<TeacherAssignment>(
        editing ? `/api/teacher/assignments/${editing.id}` : '/api/teacher/assignments',
        {
          method: editing ? 'PUT' : 'POST',
          body: JSON.stringify({
            ...form,
            deadline: new Date(form.deadline).toISOString(),
            maximumMarks: Number(form.maximumMarks),
          }),
        },
        token,
      )
      setFormOpen(false)
      await loadAssignments()
    } catch (requestError) {
      setError(requestError instanceof ApiError ? requestError.message : 'Could not save this assignment.')
    } finally {
      setSaving(false)
    }
  }

  const remove = async (assignment: TeacherAssignment) => {
    if (!window.confirm(`Delete “${assignment.title}”? This cannot be undone.`)) return
    setError('')
    try {
      await apiRequest<void>(`/api/teacher/assignments/${assignment.id}`, { method: 'DELETE' }, token)
      await loadAssignments()
    } catch (requestError) {
      setError(requestError instanceof ApiError ? requestError.message : 'Could not delete this assignment.')
    }
  }

  return (
    <>
      <header className="page-heading">
        <div><span className="eyebrow">Assignment studio</span><h1>My assignments</h1><p>Create learning tasks for the courses and subjects assigned to you.</p></div>
        <button className="primary-button" disabled={activeSubjects.length === 0} onClick={openCreate} type="button"><Icon name="plus" size={18} /> New assignment</button>
      </header>
      {error && <div className="alert error page-alert" role="alert">{error}</div>}
      {subjects.length === 0 && !loading && <div className="alert info page-alert">An administrator must assign you to a subject before you can create assignments.</div>}
      {subjects.length > 0 && activeSubjects.length === 0 && <div className="alert info page-alert">Your assigned courses or subjects are inactive. Ask an administrator to activate them.</div>}
      <section className="panel data-panel">
        <div className="table-toolbar assignment-filters">
          <span className="record-count">{assignments.length} {assignments.length === 1 ? 'assignment' : 'assignments'}</span>
          <select aria-label="Filter by course" className="filter-select" onChange={(event) => setCourseFilter(event.target.value)} value={courseFilter}><option value="">All courses</option>{courses.map((course) => <option key={course.id} value={course.id}>{course.name}</option>)}</select>
          <select aria-label="Filter by status" className="filter-select" onChange={(event) => setStatusFilter(event.target.value as '' | AssignmentStatus)} value={statusFilter}><option value="">Any status</option><option value="Published">Published</option><option value="Draft">Draft</option></select>
          <label className="search-field"><Icon name="search" size={18} /><input aria-label="Search your assignments" onChange={(event) => setSearch(event.target.value)} placeholder="Search assignment title" value={search} /></label>
        </div>
        <div className="table-scroll">
          <table>
            <thead><tr><th>Assignment</th><th>Deadline</th><th>Marks</th><th>Submissions</th><th>Status</th><th><span className="sr-only">Actions</span></th></tr></thead>
            <tbody>{!loading && assignments.map((assignment) => {
              const deadline = new Date(assignment.deadline)
              const overdue = deadline.getTime() < Date.now()
              return <tr key={assignment.id}>
                <td><div className="title-cell assignment-title"><span className="catalog-icon assignment"><Icon name="assignments" size={19} /></span><span><strong>{assignment.title}</strong><small>{assignment.courseName} · {assignment.subjectName}</small></span></div></td>
                <td><span className={overdue ? 'deadline overdue' : 'deadline'}>{deadline.toLocaleDateString(undefined, { day: 'numeric', month: 'short', year: 'numeric' })}<small>{overdue ? 'Past due' : deadline.toLocaleTimeString(undefined, { hour: '2-digit', minute: '2-digit' })}</small></span></td>
                <td>{assignment.maximumMarks}</td>
                <td><span className="submission-count">{assignment.submissionCount}</span></td>
                <td><span className={`assignment-status ${assignment.status.toLowerCase()}`}>{assignment.status}</span></td>
                <td><div className="row-actions"><button aria-label={`Edit ${assignment.title}`} onClick={() => openEdit(assignment)} type="button"><Icon name="edit" size={17} /></button><button aria-label={`Delete ${assignment.title}`} className="danger" onClick={() => remove(assignment)} type="button"><Icon name="trash" size={17} /></button></div></td>
              </tr>
            })}</tbody>
          </table>
          {loading && <div className="table-message">Loading assignments…</div>}
          {!loading && assignments.length === 0 && <div className="table-message"><strong>No assignments found</strong><span>Create an assignment or adjust the current filters.</span></div>}
        </div>
      </section>
      {formOpen && <Modal onClose={() => setFormOpen(false)} subtitle="Save as a draft or publish it for students." title={editing ? 'Update assignment' : 'Create an assignment'}>
        <form className="entity-form" onSubmit={save}>
          <div className="form-grid">
            <label className="span-2">Title<input autoFocus maxLength={180} minLength={3} onChange={(event) => setForm({ ...form, title: event.target.value })} placeholder="e.g. Chapter 4 problem set" required value={form.title} /></label>
            <label>Course<select onChange={(event) => chooseCourse(event.target.value)} required value={form.courseId}><option disabled value="">Select a course</option>{courses.filter((course) => activeSubjects.some((subject) => subject.courseId === course.id)).map((course) => <option key={course.id} value={course.id}>{course.name}</option>)}</select></label>
            <label>Subject<select onChange={(event) => setForm({ ...form, subjectId: event.target.value })} required value={form.subjectId}><option disabled value="">Select a subject</option>{activeSubjects.filter((subject) => subject.courseId === form.courseId).map((subject) => <option key={subject.id} value={subject.id}>{subject.name} ({subject.code})</option>)}</select></label>
            <label>Deadline<input onChange={(event) => setForm({ ...form, deadline: event.target.value })} required type="datetime-local" value={form.deadline} /></label>
            <label>Maximum marks<input max="10000" min="0.01" onChange={(event) => setForm({ ...form, maximumMarks: event.target.value })} required step="0.01" type="number" value={form.maximumMarks} /></label>
            <label className="span-2">Description<textarea maxLength={5000} onChange={(event) => setForm({ ...form, description: event.target.value })} placeholder="Instructions, learning goals, and submission requirements" rows={5} value={form.description} /></label>
            <label className="span-2">Publishing status<select onChange={(event) => setForm({ ...form, status: event.target.value as AssignmentStatus })} value={form.status}><option value="Draft">Draft — only you can see it</option><option value="Published">Published — available to students</option></select></label>
          </div>
          {error && <div className="alert error form-alert" role="alert">{error}</div>}
          <footer className="form-actions"><button className="secondary-button" onClick={() => setFormOpen(false)} type="button">Cancel</button><button className="primary-button" disabled={saving} type="submit">{saving ? 'Saving…' : editing ? 'Save changes' : form.status === 'Published' ? 'Create & publish' : 'Save draft'}</button></footer>
        </form>
      </Modal>}
    </>
  )
}

import { useCallback, useEffect, useState, type FormEvent } from 'react'
import { Icon } from '../components/Icon'
import { Modal } from '../components/Modal'
import { ApiError, apiRequest } from '../lib/api'
import type { Course, ManagedUser, Subject } from '../types'

interface SubjectsPageProps { token: string }
interface SubjectForm { name: string; code: string; courseId: string; teacherId: string; isActive: boolean }
const emptyForm: SubjectForm = { name: '', code: '', courseId: '', teacherId: '', isActive: true }

export function SubjectsPage({ token }: SubjectsPageProps) {
  const [subjects, setSubjects] = useState<Subject[]>([])
  const [courses, setCourses] = useState<Course[]>([])
  const [teachers, setTeachers] = useState<ManagedUser[]>([])
  const [courseFilter, setCourseFilter] = useState('')
  const [search, setSearch] = useState('')
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState('')
  const [editing, setEditing] = useState<Subject | null>(null)
  const [form, setForm] = useState<SubjectForm>(emptyForm)
  const [formOpen, setFormOpen] = useState(false)
  const [saving, setSaving] = useState(false)

  const loadSubjects = useCallback(async () => {
    setLoading(true)
    setError('')
    const params = new URLSearchParams()
    if (courseFilter) params.set('courseId', courseFilter)
    if (search.trim()) params.set('search', search.trim())
    try {
      setSubjects(await apiRequest<Subject[]>(`/api/admin/subjects?${params}`, {}, token))
    } catch (requestError) {
      setError(requestError instanceof ApiError ? requestError.message : 'Could not load subjects.')
    } finally {
      setLoading(false)
    }
  }, [courseFilter, search, token])

  useEffect(() => {
    Promise.all([
      apiRequest<Course[]>('/api/admin/courses', {}, token),
      apiRequest<ManagedUser[]>('/api/admin/users?role=Teacher', {}, token),
    ]).then(([courseData, teacherData]) => {
      setCourses(courseData)
      setTeachers(teacherData)
    }).catch((requestError: unknown) =>
      setError(requestError instanceof ApiError ? requestError.message : 'Could not load form options.'),
    )
  }, [token])

  useEffect(() => {
    const timer = window.setTimeout(loadSubjects, 250)
    return () => window.clearTimeout(timer)
  }, [loadSubjects])

  const openCreate = () => {
    setEditing(null)
    setForm({ ...emptyForm, courseId: courseFilter || courses[0]?.id || '' })
    setError('')
    setFormOpen(true)
  }
  const openEdit = (subject: Subject) => {
    setEditing(subject)
    setForm({ name: subject.name, code: subject.code, courseId: subject.courseId, teacherId: subject.teacherId || '', isActive: subject.isActive })
    setError('')
    setFormOpen(true)
  }

  const save = async (event: FormEvent<HTMLFormElement>) => {
    event.preventDefault()
    setSaving(true)
    setError('')
    try {
      await apiRequest<Subject>(editing ? `/api/admin/subjects/${editing.id}` : '/api/admin/subjects', {
        method: editing ? 'PUT' : 'POST',
        body: JSON.stringify({ ...form, teacherId: form.teacherId || null }),
      }, token)
      setFormOpen(false)
      await loadSubjects()
    } catch (requestError) {
      setError(requestError instanceof ApiError ? requestError.message : 'Could not save this subject.')
    } finally {
      setSaving(false)
    }
  }

  const remove = async (subject: Subject) => {
    if (!window.confirm(`Delete ${subject.name}? This cannot be undone.`)) return
    setError('')
    try {
      await apiRequest<void>(`/api/admin/subjects/${subject.id}`, { method: 'DELETE' }, token)
      await loadSubjects()
    } catch (requestError) {
      setError(requestError instanceof ApiError ? requestError.message : 'Could not delete this subject.')
    }
  }

  return (
    <>
      <header className="page-heading">
        <div><span className="eyebrow">Curriculum catalog</span><h1>Subjects</h1><p>Connect subjects with a course and the teacher responsible for them.</p></div>
        <button className="primary-button" disabled={courses.length === 0} onClick={openCreate} type="button"><Icon name="plus" size={18} /> Add subject</button>
      </header>
      {error && <div className="alert error page-alert" role="alert">{error}</div>}
      {courses.length === 0 && !loading && <div className="alert info page-alert">Create a course before adding subjects.</div>}
      <section className="panel data-panel">
        <div className="table-toolbar table-toolbar-right">
          <span className="record-count">{subjects.length} {subjects.length === 1 ? 'subject' : 'subjects'}</span>
          <select aria-label="Filter by course" className="filter-select" onChange={(event) => setCourseFilter(event.target.value)} value={courseFilter}><option value="">All courses</option>{courses.map((course) => <option key={course.id} value={course.id}>{course.name}</option>)}</select>
          <label className="search-field"><Icon name="search" size={18} /><input aria-label="Search subjects" onChange={(event) => setSearch(event.target.value)} placeholder="Search name or code" value={search} /></label>
        </div>
        <div className="table-scroll">
          <table>
            <thead><tr><th>Subject</th><th>Course</th><th>Teacher</th><th>Status</th><th><span className="sr-only">Actions</span></th></tr></thead>
            <tbody>{!loading && subjects.map((subject) => (
              <tr key={subject.id}>
                <td><div className="title-cell"><span className="catalog-icon subject"><Icon name="subjects" size={19} /></span><span><strong>{subject.name}</strong><small><span className="code-badge">{subject.code}</span></small></span></div></td>
                <td>{subject.courseName}</td>
                <td>{subject.teacherName ? <div className="compact-person"><span>{subject.teacherName.slice(0, 2).toUpperCase()}</span>{subject.teacherName}</div> : <span className="unassigned">Unassigned</span>}</td>
                <td><span className={subject.isActive ? 'status active' : 'status inactive'}><i />{subject.isActive ? 'Active' : 'Inactive'}</span></td>
                <td><div className="row-actions"><button aria-label={`Edit ${subject.name}`} onClick={() => openEdit(subject)} type="button"><Icon name="edit" size={17} /></button><button aria-label={`Delete ${subject.name}`} className="danger" onClick={() => remove(subject)} type="button"><Icon name="trash" size={17} /></button></div></td>
              </tr>
            ))}</tbody>
          </table>
          {loading && <div className="table-message">Loading subjects…</div>}
          {!loading && subjects.length === 0 && <div className="table-message"><strong>No subjects found</strong><span>Add a subject or adjust the current filters.</span></div>}
        </div>
      </section>
      {formOpen && <Modal onClose={() => setFormOpen(false)} subtitle="Assigning a teacher is optional and can be changed later." title={editing ? 'Update subject' : 'Create a subject'}>
        <form className="entity-form" onSubmit={save}>
          <div className="form-grid">
            <label className="span-2">Subject name<input autoFocus maxLength={120} minLength={2} onChange={(event) => setForm({ ...form, name: event.target.value })} placeholder="e.g. Mathematics" required value={form.name} /></label>
            <label>Subject code<input maxLength={30} minLength={2} onChange={(event) => setForm({ ...form, code: event.target.value })} placeholder="MATH" required value={form.code} /></label>
            <label>Status<select onChange={(event) => setForm({ ...form, isActive: event.target.value === 'active' })} value={form.isActive ? 'active' : 'inactive'}><option value="active">Active</option><option value="inactive">Inactive</option></select></label>
            <label className="span-2">Course<select onChange={(event) => setForm({ ...form, courseId: event.target.value })} required value={form.courseId}><option disabled value="">Select a course</option>{courses.map((course) => <option key={course.id} value={course.id}>{course.name} ({course.code})</option>)}</select></label>
            <label className="span-2">Assigned teacher<select onChange={(event) => setForm({ ...form, teacherId: event.target.value })} value={form.teacherId}><option value="">Unassigned</option>{teachers.map((teacher) => <option key={teacher.id} value={teacher.id}>{teacher.fullName} — {teacher.email}</option>)}</select></label>
          </div>
          {error && <div className="alert error form-alert" role="alert">{error}</div>}
          <footer className="form-actions"><button className="secondary-button" onClick={() => setFormOpen(false)} type="button">Cancel</button><button className="primary-button" disabled={saving} type="submit">{saving ? 'Saving…' : editing ? 'Save changes' : 'Create subject'}</button></footer>
        </form>
      </Modal>}
    </>
  )
}

import { useCallback, useEffect, useState, type FormEvent } from 'react'
import { Icon } from '../components/Icon'
import { Modal } from '../components/Modal'
import { ApiError, apiRequest } from '../lib/api'
import type { Course } from '../types'

interface CoursesPageProps { token: string }
interface CourseForm { name: string; code: string; description: string; isActive: boolean }
const emptyForm: CourseForm = { name: '', code: '', description: '', isActive: true }

export function CoursesPage({ token }: CoursesPageProps) {
  const [courses, setCourses] = useState<Course[]>([])
  const [search, setSearch] = useState('')
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState('')
  const [editing, setEditing] = useState<Course | null>(null)
  const [form, setForm] = useState<CourseForm>(emptyForm)
  const [formOpen, setFormOpen] = useState(false)
  const [saving, setSaving] = useState(false)

  const loadCourses = useCallback(async () => {
    setLoading(true)
    setError('')
    const params = new URLSearchParams()
    if (search.trim()) params.set('search', search.trim())
    try {
      setCourses(await apiRequest<Course[]>(`/api/admin/courses?${params}`, {}, token))
    } catch (requestError) {
      setError(requestError instanceof ApiError ? requestError.message : 'Could not load courses.')
    } finally {
      setLoading(false)
    }
  }, [search, token])

  useEffect(() => {
    const timer = window.setTimeout(loadCourses, 250)
    return () => window.clearTimeout(timer)
  }, [loadCourses])

  const openCreate = () => { setEditing(null); setForm(emptyForm); setError(''); setFormOpen(true) }
  const openEdit = (course: Course) => {
    setEditing(course)
    setForm({ name: course.name, code: course.code, description: course.description, isActive: course.isActive })
    setError('')
    setFormOpen(true)
  }

  const save = async (event: FormEvent<HTMLFormElement>) => {
    event.preventDefault()
    setSaving(true)
    setError('')
    try {
      await apiRequest<Course>(editing ? `/api/admin/courses/${editing.id}` : '/api/admin/courses', {
        method: editing ? 'PUT' : 'POST',
        body: JSON.stringify(form),
      }, token)
      setFormOpen(false)
      await loadCourses()
    } catch (requestError) {
      setError(requestError instanceof ApiError ? requestError.message : 'Could not save this course.')
    } finally {
      setSaving(false)
    }
  }

  const remove = async (course: Course) => {
    if (!window.confirm(`Delete ${course.name}? This cannot be undone.`)) return
    setError('')
    try {
      await apiRequest<void>(`/api/admin/courses/${course.id}`, { method: 'DELETE' }, token)
      await loadCourses()
    } catch (requestError) {
      setError(requestError instanceof ApiError ? requestError.message : 'Could not delete this course.')
    }
  }

  return (
    <>
      <header className="page-heading">
        <div><span className="eyebrow">Academic structure</span><h1>Classes & courses</h1><p>Organize the learning groups used across subjects and assignments.</p></div>
        <button className="primary-button" onClick={openCreate} type="button"><Icon name="plus" size={18} /> Add course</button>
      </header>
      {error && <div className="alert error page-alert" role="alert">{error}</div>}
      <section className="panel data-panel">
        <div className="table-toolbar table-toolbar-right">
          <span className="record-count">{courses.length} {courses.length === 1 ? 'course' : 'courses'}</span>
          <label className="search-field"><Icon name="search" size={18} /><input aria-label="Search courses" onChange={(event) => setSearch(event.target.value)} placeholder="Search name or code" value={search} /></label>
        </div>
        <div className="table-scroll">
          <table>
            <thead><tr><th>Course</th><th>Code</th><th>Subjects</th><th>Status</th><th><span className="sr-only">Actions</span></th></tr></thead>
            <tbody>{!loading && courses.map((course) => (
              <tr key={course.id}>
                <td><div className="title-cell"><span className="catalog-icon"><Icon name="courses" size={19} /></span><span><strong>{course.name}</strong><small>{course.description || 'No description provided'}</small></span></div></td>
                <td><span className="code-badge">{course.code}</span></td>
                <td className="muted-cell">{course.subjectCount}</td>
                <td><span className={course.isActive ? 'status active' : 'status inactive'}><i />{course.isActive ? 'Active' : 'Inactive'}</span></td>
                <td><div className="row-actions"><button aria-label={`Edit ${course.name}`} onClick={() => openEdit(course)} type="button"><Icon name="edit" size={17} /></button><button aria-label={`Delete ${course.name}`} className="danger" onClick={() => remove(course)} type="button"><Icon name="trash" size={17} /></button></div></td>
              </tr>
            ))}</tbody>
          </table>
          {loading && <div className="table-message">Loading courses…</div>}
          {!loading && courses.length === 0 && <div className="table-message"><strong>No courses found</strong><span>Create the first course or try another search.</span></div>}
        </div>
      </section>
      {formOpen && <Modal onClose={() => setFormOpen(false)} subtitle="Courses can represent a class, grade, section, or program." title={editing ? 'Update course' : 'Create a course'}>
        <form className="entity-form" onSubmit={save}>
          <div className="form-grid">
            <label className="span-2">Course name<input autoFocus maxLength={120} minLength={2} onChange={(event) => setForm({ ...form, name: event.target.value })} placeholder="e.g. Grade 10 – Science" required value={form.name} /></label>
            <label>Course code<input maxLength={30} minLength={2} onChange={(event) => setForm({ ...form, code: event.target.value })} placeholder="SCI-10" required value={form.code} /></label>
            <label>Status<select onChange={(event) => setForm({ ...form, isActive: event.target.value === 'active' })} value={form.isActive ? 'active' : 'inactive'}><option value="active">Active</option><option value="inactive">Inactive</option></select></label>
            <label className="span-2">Description<textarea maxLength={500} onChange={(event) => setForm({ ...form, description: event.target.value })} placeholder="A short description of this learning group" rows={4} value={form.description} /></label>
          </div>
          {error && <div className="alert error form-alert" role="alert">{error}</div>}
          <footer className="form-actions"><button className="secondary-button" onClick={() => setFormOpen(false)} type="button">Cancel</button><button className="primary-button" disabled={saving} type="submit">{saving ? 'Saving…' : editing ? 'Save changes' : 'Create course'}</button></footer>
        </form>
      </Modal>}
    </>
  )
}

import { useCallback, useEffect, useState, type FormEvent } from 'react'
import { Icon } from '../components/Icon'
import { Modal } from '../components/Modal'
import { ApiError, apiRequest } from '../lib/api'
import type { Course, ManagedUser } from '../types'

interface UsersPageProps { token: string }
type UserFilter = 'All' | 'Teacher' | 'Student'

interface UserFormState {
  fullName: string
  email: string
  password: string
  role: 'Teacher' | 'Student'
  courseId: string
  isActive: boolean
}

const emptyForm: UserFormState = {
  fullName: '',
  email: '',
  password: '',
  role: 'Teacher',
  courseId: '',
  isActive: true,
}

export function UsersPage({ token }: UsersPageProps) {
  const [users, setUsers] = useState<ManagedUser[]>([])
  const [courses, setCourses] = useState<Course[]>([])
  const [filter, setFilter] = useState<UserFilter>('All')
  const [search, setSearch] = useState('')
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState('')
  const [editing, setEditing] = useState<ManagedUser | null>(null)
  const [formOpen, setFormOpen] = useState(false)
  const [form, setForm] = useState<UserFormState>(emptyForm)
  const [saving, setSaving] = useState(false)

  const loadUsers = useCallback(async () => {
    setLoading(true)
    setError('')
    const params = new URLSearchParams()
    if (filter !== 'All') params.set('role', filter)
    if (search.trim()) params.set('search', search.trim())

    try {
      setUsers(await apiRequest<ManagedUser[]>(`/api/admin/users?${params}`, {}, token))
    } catch (requestError) {
      setError(requestError instanceof ApiError ? requestError.message : 'Could not load people.')
    } finally {
      setLoading(false)
    }
  }, [filter, search, token])

  useEffect(() => {
    const timer = window.setTimeout(loadUsers, 250)
    return () => window.clearTimeout(timer)
  }, [loadUsers])

  useEffect(() => {
    apiRequest<Course[]>('/api/admin/courses', {}, token)
      .then((data) => setCourses(data.filter((course) => course.isActive)))
      .catch(() => undefined)
  }, [token])

  const openCreate = () => {
    setEditing(null)
    setForm(emptyForm)
    setError('')
    setFormOpen(true)
  }

  const openEdit = (user: ManagedUser) => {
    setEditing(user)
    setForm({
      fullName: user.fullName,
      email: user.email,
      password: '',
      role: user.role,
      courseId: user.courseId || '',
      isActive: user.isActive,
    })
    setError('')
    setFormOpen(true)
  }

  const save = async (event: FormEvent<HTMLFormElement>) => {
    event.preventDefault()
    setSaving(true)
    setError('')
    try {
      await apiRequest<ManagedUser>(
        editing ? `/api/admin/users/${editing.id}` : '/api/admin/users',
        {
          method: editing ? 'PUT' : 'POST',
          body: JSON.stringify({
            ...form,
            password: form.password || null,
            courseId: form.role === 'Student' ? form.courseId : null,
          }),
        },
        token,
      )
      setFormOpen(false)
      await loadUsers()
    } catch (requestError) {
      setError(requestError instanceof ApiError ? requestError.message : 'Could not save this user.')
    } finally {
      setSaving(false)
    }
  }

  const remove = async (user: ManagedUser) => {
    if (!window.confirm(`Delete ${user.fullName}? This cannot be undone.`)) return
    setError('')
    try {
      await apiRequest<void>(`/api/admin/users/${user.id}`, { method: 'DELETE' }, token)
      await loadUsers()
    } catch (requestError) {
      setError(requestError instanceof ApiError ? requestError.message : 'Could not delete this user.')
    }
  }

  return (
    <>
      <header className="page-heading">
        <div><span className="eyebrow">People directory</span><h1>Teachers & students</h1><p>Create accounts, update roles, and control access.</p></div>
        <button className="primary-button" onClick={openCreate} type="button"><Icon name="plus" size={18} /> Add person</button>
      </header>
      {error && <div className="alert error page-alert" role="alert">{error}</div>}
      <section className="panel data-panel">
        <div className="table-toolbar">
          <div className="segmented-control">
            {(['All', 'Teacher', 'Student'] as const).map((role) => (
              <button className={filter === role ? 'active' : ''} key={role} onClick={() => setFilter(role)} type="button">{role === 'All' ? 'Everyone' : `${role}s`}</button>
            ))}
          </div>
          <label className="search-field"><Icon name="search" size={18} /><input aria-label="Search people" onChange={(event) => setSearch(event.target.value)} placeholder="Search name or email" value={search} /></label>
        </div>
        <div className="table-scroll">
          <table>
            <thead><tr><th>Person</th><th>Role</th><th>Course</th><th>Status</th><th>Joined</th><th><span className="sr-only">Actions</span></th></tr></thead>
            <tbody>
              {!loading && users.map((user) => (
                <tr key={user.id}>
                  <td><div className="person-cell"><span className={`table-avatar ${user.role.toLowerCase()}`}>{user.fullName.slice(0, 2).toUpperCase()}</span><span><strong>{user.fullName}</strong><small>{user.email}</small></span></div></td>
                  <td><span className={`role-badge ${user.role.toLowerCase()}`}>{user.role}</span></td>
                  <td>{user.role === 'Student' ? user.courseName || <span className="unassigned">Unassigned</span> : <span className="muted-cell">—</span>}</td>
                  <td><span className={user.isActive ? 'status active' : 'status inactive'}><i />{user.isActive ? 'Active' : 'Inactive'}</span></td>
                  <td className="muted-cell">{new Date(user.createdAt).toLocaleDateString(undefined, { day: 'numeric', month: 'short', year: 'numeric' })}</td>
                  <td><div className="row-actions"><button aria-label={`Edit ${user.fullName}`} onClick={() => openEdit(user)} type="button"><Icon name="edit" size={17} /></button><button aria-label={`Delete ${user.fullName}`} className="danger" onClick={() => remove(user)} type="button"><Icon name="trash" size={17} /></button></div></td>
                </tr>
              ))}
            </tbody>
          </table>
          {loading && <div className="table-message">Loading people…</div>}
          {!loading && users.length === 0 && <div className="table-message"><strong>No people found</strong><span>Try another filter or create a new account.</span></div>}
        </div>
      </section>

      {formOpen && (
        <Modal onClose={() => setFormOpen(false)} subtitle="Teacher and student access is managed here." title={editing ? 'Update person' : 'Create a new account'}>
          <form className="entity-form" onSubmit={save}>
            <div className="form-grid">
              <label className="span-2">Full name<input autoFocus maxLength={100} minLength={2} onChange={(event) => setForm({ ...form, fullName: event.target.value })} required value={form.fullName} /></label>
              <label className="span-2">Email address<input maxLength={200} onChange={(event) => setForm({ ...form, email: event.target.value })} required type="email" value={form.email} /></label>
              <label>Role<select onChange={(event) => {
                const role = event.target.value as UserFormState['role']
                setForm({ ...form, role, courseId: role === 'Student' ? form.courseId || courses[0]?.id || '' : '' })
              }} value={form.role}><option value="Teacher">Teacher</option><option value="Student">Student</option></select></label>
              <label>Status<select onChange={(event) => setForm({ ...form, isActive: event.target.value === 'active' })} value={form.isActive ? 'active' : 'inactive'}><option value="active">Active</option><option value="inactive">Inactive</option></select></label>
              {form.role === 'Student' && <label className="span-2">Class / course<select onChange={(event) => setForm({ ...form, courseId: event.target.value })} required value={form.courseId}><option disabled value="">Select a course</option>{courses.map((course) => <option key={course.id} value={course.id}>{course.name} ({course.code})</option>)}</select></label>}
              <label className="span-2">{editing ? 'New password (optional)' : 'Temporary password'}<input minLength={8} onChange={(event) => setForm({ ...form, password: event.target.value })} placeholder={editing ? 'Leave blank to keep current password' : 'At least 8 characters'} required={!editing} type="password" value={form.password} /></label>
            </div>
            {error && <div className="alert error" role="alert">{error}</div>}
            <footer className="form-actions"><button className="secondary-button" onClick={() => setFormOpen(false)} type="button">Cancel</button><button className="primary-button" disabled={saving} type="submit">{saving ? 'Saving…' : editing ? 'Save changes' : 'Create account'}</button></footer>
          </form>
        </Modal>
      )}
    </>
  )
}

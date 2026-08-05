import { useCallback, useEffect, useState } from 'react'
import { Icon } from '../components/Icon'
import { ApiError, apiRequest } from '../lib/api'
import type { AdminAssignment, AssignmentStatus, Course } from '../types'

interface AssignmentsPageProps { token: string }

export function AssignmentsPage({ token }: AssignmentsPageProps) {
  const [assignments, setAssignments] = useState<AdminAssignment[]>([])
  const [courses, setCourses] = useState<Course[]>([])
  const [courseFilter, setCourseFilter] = useState('')
  const [statusFilter, setStatusFilter] = useState<'' | AssignmentStatus>('')
  const [search, setSearch] = useState('')
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState('')

  const loadAssignments = useCallback(async () => {
    setLoading(true)
    setError('')
    const params = new URLSearchParams()
    if (courseFilter) params.set('courseId', courseFilter)
    if (statusFilter) params.set('status', statusFilter)
    if (search.trim()) params.set('search', search.trim())
    try {
      setAssignments(await apiRequest<AdminAssignment[]>(`/api/admin/assignments?${params}`, {}, token))
    } catch (requestError) {
      setError(requestError instanceof ApiError ? requestError.message : 'Could not load assignments.')
    } finally {
      setLoading(false)
    }
  }, [courseFilter, search, statusFilter, token])

  useEffect(() => {
    apiRequest<Course[]>('/api/admin/courses', {}, token).then(setCourses).catch(() => undefined)
  }, [token])

  useEffect(() => {
    const timer = window.setTimeout(loadAssignments, 250)
    return () => window.clearTimeout(timer)
  }, [loadAssignments])

  return (
    <>
      <header className="page-heading">
        <div><span className="eyebrow">System oversight</span><h1>All assignments</h1><p>Monitor teacher-created work across every course and subject.</p></div>
      </header>
      {error && <div className="alert error page-alert" role="alert">{error}</div>}
      <section className="panel data-panel">
        <div className="table-toolbar assignment-filters">
          <span className="record-count">{assignments.length} {assignments.length === 1 ? 'assignment' : 'assignments'}</span>
          <select aria-label="Filter by course" className="filter-select" onChange={(event) => setCourseFilter(event.target.value)} value={courseFilter}><option value="">All courses</option>{courses.map((course) => <option key={course.id} value={course.id}>{course.name}</option>)}</select>
          <select aria-label="Filter by status" className="filter-select" onChange={(event) => setStatusFilter(event.target.value as '' | AssignmentStatus)} value={statusFilter}><option value="">Any status</option><option value="Published">Published</option><option value="Draft">Draft</option></select>
          <label className="search-field"><Icon name="search" size={18} /><input aria-label="Search assignments" onChange={(event) => setSearch(event.target.value)} placeholder="Search assignment title" value={search} /></label>
        </div>
        <div className="table-scroll">
          <table>
            <thead><tr><th>Assignment</th><th>Teacher</th><th>Deadline</th><th>Marks</th><th>Submissions</th><th>Status</th></tr></thead>
            <tbody>{!loading && assignments.map((assignment) => {
              const deadline = new Date(assignment.deadline)
              const overdue = deadline.getTime() < Date.now()
              return (
                <tr key={assignment.id}>
                  <td><div className="title-cell assignment-title"><span className="catalog-icon assignment"><Icon name="assignments" size={19} /></span><span><strong>{assignment.title}</strong><small>{assignment.courseName} · {assignment.subjectName}</small></span></div></td>
                  <td>{assignment.teacherName}</td>
                  <td><span className={overdue ? 'deadline overdue' : 'deadline'}>{deadline.toLocaleDateString(undefined, { day: 'numeric', month: 'short', year: 'numeric' })}<small>{overdue ? 'Past due' : deadline.toLocaleTimeString(undefined, { hour: '2-digit', minute: '2-digit' })}</small></span></td>
                  <td>{assignment.maximumMarks}</td>
                  <td><span className="submission-count">{assignment.submissionCount}</span></td>
                  <td><span className={`assignment-status ${assignment.status.toLowerCase()}`}>{assignment.status}</span></td>
                </tr>
              )
            })}</tbody>
          </table>
          {loading && <div className="table-message">Loading assignments…</div>}
          {!loading && assignments.length === 0 && <div className="table-message"><strong>No assignments found</strong><span>Teacher-created assignments will appear here.</span></div>}
        </div>
      </section>
    </>
  )
}

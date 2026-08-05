export type UserRole = 'Admin' | 'Teacher' | 'Student'

export type ViewName =
  | 'dashboard'
  | 'users'
  | 'courses'
  | 'subjects'
  | 'assignments'
  | 'submissions'

export interface AuthenticatedUser {
  id: string
  fullName: string
  email: string
  role: UserRole
}

export interface AuthSession {
  token: string
  expiresAt: string
  user: AuthenticatedUser
}

export interface DashboardSummary {
  teachers: number
  students: number
  courses: number
  subjects: number
  assignments: number
  submissions: number
  pendingReviews: number
}

export interface ManagedUser {
  id: string
  fullName: string
  email: string
  role: Exclude<UserRole, 'Admin'>
  isActive: boolean
  createdAt: string
  updatedAt: string
}

export interface Course {
  id: string
  name: string
  code: string
  description: string
  isActive: boolean
  subjectCount: number
  createdAt: string
  updatedAt: string
}

export interface Subject {
  id: string
  name: string
  code: string
  courseId: string
  courseName: string
  teacherId: string | null
  teacherName: string | null
  isActive: boolean
  createdAt: string
  updatedAt: string
}

export interface ProblemDetails {
  title?: string
  detail?: string
  errors?: Record<string, string[]>
}

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

export interface ProblemDetails {
  title?: string
  detail?: string
  errors?: Record<string, string[]>
}

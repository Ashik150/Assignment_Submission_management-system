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

export interface TeacherDashboardSummary {
  assignedSubjects: number
  assignments: number
  publishedAssignments: number
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

export type AssignmentStatus = 'Draft' | 'Published'

export interface AdminAssignment {
  id: string
  title: string
  description: string
  courseId: string
  courseName: string
  subjectId: string
  subjectName: string
  teacherId: string
  teacherName: string
  deadline: string
  maximumMarks: number
  status: AssignmentStatus
  submissionCount: number
  createdAt: string
  updatedAt: string
}

export type SubmissionStatus = 'Submitted' | 'Late' | 'Reviewed' | 'Returned'

export interface AdminSubmission {
  id: string
  assignmentId: string
  assignmentTitle: string
  studentId: string
  studentName: string
  studentEmail: string
  answer: string
  status: SubmissionStatus
  marks: number | null
  maximumMarks: number
  feedback: string
  submittedAt: string
  reviewedAt: string | null
  updatedAt: string
}

export interface ProblemDetails {
  title?: string
  detail?: string
  errors?: Record<string, string[]>
}

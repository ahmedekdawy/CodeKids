export type UserRole = 'Student' | 'Parent' | 'Teacher' | 'SuperAdmin';

export interface AuthUser {
  id: string;
  email: string;
  displayName: string;
  role: UserRole;
  parentId?: string | null;
  avatarId?: string | null;
  totalXp: number;
}

export interface AuthResponse {
  token: string;
  user: AuthUser;
}

export interface CourseLesson {
  id: string;
  title: string;
  theme: string;
  description: string;
  difficulty: number;
  xpReward: number;
  sortOrder: number;
  stepCount: number;
}

export interface CourseQuiz {
  id: string;
  title: string;
  description: string;
  xpReward: number;
  questionCount: number;
}

export interface Course {
  id: string;
  title: string;
  theme: string;
  description: string;
  ageMin: number;
  ageMax: number;
  sortOrder: number;
  lessons: CourseLesson[];
  quizzes: CourseQuiz[];
}

export interface LessonStep {
  id: string;
  stepNumber: number;
  title: string;
  prompt: string;
}

export interface Lesson {
  id: string;
  courseId: string;
  title: string;
  theme: string;
  description: string;
  difficulty: number;
  xpReward: number;
  steps: LessonStep[];
}

export interface StudentSummary {
  userId: string;
  studentName: string;
  totalCompletedSteps: number;
  totalXp: number;
  avatarId?: string | null;
  badges: string[];
}

export interface CompleteStepResponse {
  isCorrect: boolean;
  earnedXp: number;
  feedback: string;
  totalXp: number;
  newlyAwardedBadges: string[];
}

export interface QuizQuestion {
  id: string;
  prompt: string;
  optionA: string;
  optionB: string;
  optionC: string;
  sortOrder: number;
}

export interface Quiz {
  id: string;
  courseId: string;
  classroomId?: string | null;
  title: string;
  description: string;
  xpReward: number;
  questions: QuizQuestion[];
}

export interface SubmitQuizResponse {
  score: number;
  totalQuestions: number;
  earnedXp: number;
  totalXp: number;
  feedback: string;
  newlyAwardedBadges: string[];
}

export interface Badge {
  id: string;
  code: string;
  name: string;
  description: string;
  icon: string;
  requiredXp: number;
  requiredSteps: number;
  isEarned: boolean;
}

export interface Avatar {
  id: string;
  name: string;
  theme: string;
  accentColor: string;
  emoji: string;
  unlockXp: number;
  isUnlocked: boolean;
  isSelected: boolean;
}

export interface ChildProgress {
  studentId: string;
  displayName: string;
  totalXp: number;
  completedSteps: number;
  quizAttempts: number;
  avatarId?: string | null;
  badges: string[];
}

export interface ParentDashboard {
  parentId: string;
  parentName: string;
  children: ChildProgress[];
}

export interface TeacherStudent {
  studentId: string;
  displayName: string;
  email: string;
  totalXp: number;
  completedSteps: number;
  quizAttempts: number;
  parentName?: string | null;
}

export interface TeacherDashboard {
  teacherId: string;
  teacherName: string;
  studentCount: number;
  totalCompletedSteps: number;
  averageXp: number;
  students: TeacherStudent[];
}

export interface LiveSession {
  id: string;
  title: string;
  description: string;
  hostUserId: string;
  hostName: string;
  courseId?: string | null;
  courseTitle?: string | null;
  classroomId?: string | null;
  classroomName?: string | null;
  startsAtUtc: string;
  durationMinutes: number;
  joinUrl: string;
  startUrl?: string | null;
  whatsAppNotified?: boolean;
  whatsAppShareUrl?: string | null;
  whatsAppStatus?: string | null;
}

export interface CreateMeetingPayload {
  title: string;
  description?: string;
  startsAtUtc: string;
  durationMinutes: number;
  classroomId: string;
  courseId?: string | null;
  notifyWhatsApp: boolean;
}

export interface ManagedUser {
  id: string;
  email: string;
  displayName: string;
  role: UserRole;
  parentId?: string | null;
  totalXp: number;
  mobilePhone?: string;
}

export interface ZoomConnectionStatus {
  connected: boolean;
  email?: string | null;
  expiresAt?: string | null;
  appFallbackAvailable: boolean;
}

export interface ClassroomStudent {
  studentId: string;
  displayName: string;
  email: string;
}

export interface Classroom {
  id: string;
  name: string;
  description: string;
  teacherId?: string | null;
  teacherName?: string | null;
  courseId?: string | null;
  courseTitle?: string | null;
  whatsAppGroupInviteUrl: string;
  whatsAppNotifyPhones: string;
  students: ClassroomStudent[];
}

export interface AssignmentQuestion {
  id: string;
  prompt: string;
  questionType: string;
  optionA?: string | null;
  optionB?: string | null;
  optionC?: string | null;
  points: number;
  sortOrder: number;
  correctAnswer?: string | null;
}

export interface Assignment {
  id: string;
  classroomId: string;
  classroomName: string;
  title: string;
  description: string;
  dueAtUtc?: string | null;
  xpReward: number;
  createdByUserId: string;
  createdByName: string;
  questions: AssignmentQuestion[];
}

export interface AssignmentAnswerReview {
  questionId: string;
  prompt: string;
  answerText: string;
  correctAnswer?: string | null;
  isCorrect?: boolean | null;
  pointsAwarded?: number | null;
  points: number;
}

export interface AssignmentSubmission {
  id: string;
  assignmentId: string;
  assignmentTitle: string;
  studentId: string;
  studentName: string;
  status: string;
  score?: number | null;
  maxScore?: number | null;
  teacherFeedback?: string | null;
  submittedAtUtc: string;
  gradedAtUtc?: string | null;
  answers: AssignmentAnswerReview[];
}

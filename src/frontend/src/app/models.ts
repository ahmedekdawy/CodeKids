export type UserRole = 'Student' | 'Parent' | 'Teacher' | 'SuperAdmin';
export type TeacherWorkShift = 'Am' | 'Pm' | 'Both';
export type TeacherContractType = 'Session' | 'Monthly';
export type StudentSchoolType = 'Arabic' | 'Language';
export type CourseSchoolType = 'Arabic' | 'Language' | 'All';

export interface TeacherCourseRate {
  courseId: string;
  courseName?: string;
  courseGrade?: number | null;
  sessionAmount?: number | null;
  monthlySalary?: number | null;
}

export interface AuthUser {
  id: string;
  email: string;
  displayName: string;
  role: UserRole;
  parentId?: string | null;
  avatarId?: string | null;
  totalXp: number;
  mobilePhone?: string;
  workShift?: TeacherWorkShift | string | null;
  tenantId?: string | null;
}

export interface AuthResponse {
  token: string;
  user: AuthUser;
}

export interface CourseLesson {
  id: string;
  unitId?: string | null;
  title: string;
  theme: string;
  description: string;
  difficulty: number;
  xpReward: number;
  sortOrder: number;
  stepCount: number;
  studentAskEnabled?: boolean;
}

export interface CourseUnit {
  id: string;
  courseId: string;
  title: string;
  description: string;
  sortOrder: number;
  lessons: CourseLesson[];
  term?: number | null;
  verificationStatus?: string;
  studentAskEnabled?: boolean;
}

export interface CourseQuiz {
  id: string;
  title: string;
  description: string;
  xpReward: number;
  questionCount: number;
  isPublished?: boolean;
}

export type CourseTerm = 'FirstTerm' | 'SecondTerm' | 'FullYear';

export interface Course {
  id: string;
  title: string;
  theme: string;
  description: string;
  ageMin: number;
  ageMax: number;
  term?: CourseTerm | string | null;
  grade?: number | null;
  stageId?: number | null;
  schoolType?: CourseSchoolType | string | null;
  sortOrder: number;
  externalSubjectId?: number | null;
  subjectCode?: string;
  category?: string;
  trackCode?: string;
  trackName?: string;
  verificationStatus?: string;
  sourceTocUrl?: string;
  notes?: string;
  variants?: string;
  studentAskEnabled?: boolean;
  units?: CourseUnit[];
  lessons: CourseLesson[];
  quizzes: CourseQuiz[];
  videos?: CourseVideoSummary[];
}

export interface Stage {
  id: number;
  name: string;
  nameEn: string;
}

export interface Grade {
  id: number;
  name: string;
  nameEn: string;
  stageId: number;
}

export interface Subject {
  id: number;
  title: string;
  stageId: number;
  code?: string;
  category?: string;
  nameEn?: string;
}

export interface SiteSettings {
  siteName: string;
  logoUrl?: string | null;
  bannerUrl?: string | null;
  timetableWeekStartUtc?: string | null;
  amSessionCount: number;
  pmSessionCount: number;
  pmStartMinutes: number;
  updatedAtUtc: string;
}

export interface LessonStep {
  id: string;
  stepNumber: number;
  title: string;
  prompt: string;
}

export interface LessonVideoSummary {
  id: string;
  mediaAssetId: string;
  title: string;
  sortOrder: number;
  durationSeconds?: number | null;
}

export interface CourseVideoSummary {
  id: string;
  mediaAssetId: string;
  title: string;
  sortOrder: number;
  durationSeconds?: number | null;
}

export interface CourseVideoLibraryItem {
  id: string;
  courseId: string;
  courseTitle: string;
  mediaAssetId: string;
  title: string;
  fileName: string;
  sizeBytes: number;
  durationSeconds?: number | null;
  sortOrder: number;
  createdAtUtc: string;
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
  videos?: LessonVideoSummary[];
  unitId?: string | null;
  studentAskEnabled?: boolean;
}

export interface MediaAsset {
  id: string;
  fileName: string;
  contentType: string;
  sizeBytes: number;
  durationSeconds?: number | null;
  createdAtUtc: string;
  externalUrl?: string | null;
}

export interface TeacherLessonVideo {
  id: string;
  lessonId: string;
  lessonTitle: string;
  courseId: string;
  courseTitle: string;
  mediaAssetId: string;
  title: string;
  fileName: string;
  sizeBytes: number;
  durationSeconds?: number | null;
  sortOrder: number;
  createdAtUtc: string;
}

export interface TeacherSolutionVideo {
  assignmentId: string;
  assignmentTitle: string;
  classroomId: string;
  classroomName: string;
  mediaAssetId: string;
  fileName: string;
  sizeBytes: number;
  durationSeconds?: number | null;
  createdAtUtc: string;
}

export interface TeacherVideoLibrary {
  lessonVideos: TeacherLessonVideo[];
  solutionVideos: TeacherSolutionVideo[];
  courseVideos?: CourseVideoLibraryItem[];
}

export interface PlaybackInfo {
  mediaAssetId: string;
  playbackUrl: string;
  watermarkText: string;
  expiresAtUtc: string;
  durationSeconds?: number | null;
  contentType: string;
  fileName: string;
  isExternalLink?: boolean;
  isTeraboxHosted?: boolean;
  teraboxBaseUrl?: string | null;
}

export interface WatchSession {
  id: string;
  mediaAssetId: string;
  studentId: string;
  studentName: string;
  lessonId?: string | null;
  actualWatchSeconds: number;
  maxPositionSeconds: number;
  usedSpeedUp: boolean;
  skippedAhead: boolean;
  startedAtUtc: string;
  lastEventAtUtc: string;
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
  feedbackCode?: string | null;
  totalXp: number;
  newlyAwardedBadges: string[];
}

export interface ChoiceOption {
  key: string;
  text: string;
}

export interface QuizQuestion {
  id: string;
  prompt: string;
  questionType: string;
  passageText?: string;
  optionA: string;
  optionB: string;
  optionC: string;
  options?: ChoiceOption[];
  sortOrder: number;
  promptImageUrl?: string | null;
  children?: QuizQuestion[];
}

export interface Quiz {
  id: string;
  courseId: string;
  classroomId?: string | null;
  title: string;
  description: string;
  xpReward: number;
  isPublished: boolean;
  questions: QuizQuestion[];
}

export interface TeacherQuizListItem {
  id: string;
  courseId: string;
  courseTitle: string;
  courseGrade?: number | null;
  classroomId?: string | null;
  classroomName?: string | null;
  title: string;
  description: string;
  xpReward: number;
  isPublished: boolean;
  questionCount: number;
  attemptCount: number;
  createdAtUtc: string;
}

export interface TeacherQuizQuestionDetail {
  id: string;
  prompt: string;
  questionType?: string;
  passageText?: string;
  options: ChoiceOption[];
  correctOption: string;
  correctAnswer?: string;
  points?: number;
  sortOrder: number;
  promptImageMediaAssetId?: string | null;
  promptImageUrl?: string | null;
  children?: TeacherQuizQuestionDetail[];
}

export interface TeacherQuizDetail {
  id: string;
  courseId: string;
  classroomId?: string | null;
  title: string;
  description: string;
  xpReward: number;
  isPublished: boolean;
  questions: TeacherQuizQuestionDetail[];
}

export interface QuizAnswerReview {
  questionId: string;
  prompt: string;
  sortOrder: number;
  selectedOption: string;
  selectedText: string;
  correctOption: string;
  correctText: string;
  isCorrect: boolean;
  promptImageUrl?: string | null;
}

export interface QuizAttemptReview {
  id: string;
  quizId: string;
  studentId: string;
  studentName: string;
  score: number;
  totalQuestions: number;
  earnedXp: number;
  completedAtUtc: string;
  answers: QuizAnswerReview[];
}

export interface SubmitQuizResponse {
  score: number;
  totalQuestions: number;
  earnedXp: number;
  totalXp: number;
  feedback: string;
  feedbackCode?: string | null;
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

export interface ChildEvaluationSummary {
  weekStartDate: string;
  teacherName?: string | null;
  performancePercent?: number | null;
  attendancePercent?: number | null;
  homeworkPercent?: number | null;
  interactionDuringSession: string;
  openCamera?: boolean | null;
}

export interface ChildProgress {
  studentId: string;
  displayName: string;
  email: string;
  mobilePhone?: string | null;
  grade?: number | null;
  totalXp: number;
  completedSteps: number;
  quizAttempts: number;
  avatarId?: string | null;
  badges: string[];
  latestEvaluation?: ChildEvaluationSummary | null;
}

export interface ParentAssessmentItem {
  id: string;
  title: string;
  description: string;
  dueAtUtc?: string | null;
  status: string;
  score?: number | null;
  maxScore?: number | null;
  teacherFeedback?: string | null;
  completedAtUtc?: string | null;
}

export interface ParentQuizItem {
  id: string;
  title: string;
  description: string;
  xpReward: number;
  totalQuestions: number;
  score?: number | null;
  earnedXp?: number | null;
  completedAtUtc?: string | null;
}

export interface ParentChildCourse {
  courseId: string;
  title: string;
  theme: string;
  description: string;
  grade?: number | null;
  term?: string | null;
  assignments: ParentAssessmentItem[];
  exams: ParentAssessmentItem[];
  quizzes: ParentQuizItem[];
}

export interface ParentChildOverview {
  studentId: string;
  displayName: string;
  grade?: number | null;
  evaluations: ChildEvaluationSummary[];
  courses: ParentChildCourse[];
}

export interface ParentDashboard {
  parentId: string;
  parentName: string;
  parentEmail: string;
  parentMobilePhone: string;
  children: ChildProgress[];
}

export interface ParentManagedAccount {
  userId: string;
  displayName: string;
  role: string;
  email: string;
  mobilePhone: string;
}

export interface TeacherStudent {
  studentId: string;
  displayName: string;
  email: string;
  totalXp: number;
  levelNumber: number;
  levelName: string;
  levelProgressPercent: number;
  completedSteps: number;
  quizAttempts: number;
  weakLessonCount: number;
  parentName?: string | null;
  signal?: string | null;
}

export interface TeacherDashboard {
  teacherId: string;
  teacherName: string;
  studentCount: number;
  totalCompletedSteps: number;
  averageXp: number;
  behindCount: number;
  topWeakLessons: string[];
  students: TeacherStudent[];
}

export interface StudentLevel {
  levelNumber: number;
  code: string;
  name: string;
  minXp: number;
  nextMinXp?: number | null;
  progressPercent: number;
}

export interface LessonWeakness {
  lessonId: string;
  lessonTitle: string;
  wrongAnswers: number;
  totalAnswers: number;
  accuracyPercent: number;
}

export interface LessonMastery {
  lessonId: string;
  lessonTitle: string;
  completedSteps: number;
  totalSteps: number;
  actualWatchSeconds: number;
  videoDurationSeconds?: number | null;
  masteryPercent: number;
}

export interface WatchSummary {
  mediaAssetId: string;
  lessonId?: string | null;
  lessonTitle?: string | null;
  actualWatchSeconds: number;
  usedSpeedUp: boolean;
  skippedAhead: boolean;
  lastEventAtUtc: string;
}

export interface TeacherStudentDetail {
  studentId: string;
  displayName: string;
  email: string;
  mobilePhone?: string | null;
  parentName?: string | null;
  parentMobilePhone?: string | null;
  totalXp: number;
  level: StudentLevel;
  completedSteps: number;
  quizAttempts: number;
  examAttempts: number;
  assignmentSubmissions: number;
  lessonMastery: LessonMastery[];
  weakLessons: LessonWeakness[];
  recentWatch: WatchSummary[];
}

export interface ClassroomDiagnosis {
  classroomId: string;
  classroomName: string;
  weakLessons: LessonWeakness[];
  behindStudents: string[];
  strongStudents: string[];
}

export interface DailyWhatsAppReportsResult {
  studentMessagesAttempted: number;
  parentMessagesAttempted: number;
  sentCount: number;
  failedCount: number;
  skippedCount: number;
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

export interface Appointment {
  id: string;
  teacherId: string;
  teacherName: string;
  courseId: string;
  courseName: string;
  courseGrade?: number | null;
  startsAtUtc: string;
  endsAtUtc: string;
  notes: string;
  label: string;
}

export interface FixedTimetableEntry {
  id: string;
  teacherId: string;
  teacherName: string;
  courseId: string;
  courseName: string;
  courseGrade?: number | null;
  courseStageId?: number | null;
  combinedGrades?: number[];
  dayOfWeek: number;
  sessionNumber: number;
  period: 'am' | 'pm' | string;
  label: string;
}

export interface TeacherSessionAttendance {
  id: string;
  teacherId: string;
  teacherName: string;
  courseId: string;
  courseName: string;
  courseGrade?: number | null;
  sessionDate: string;
  label: string;
}

export type StudentAttendanceStatus = 'Present' | 'Absent';

export interface StudentClassroomAttendance {
  id: string;
  studentId: string;
  studentName: string;
  studentEmail: string;
  studentGradeId?: number | null;
  classroomId: string;
  classroomName: string;
  attendanceDate: string;
  status: StudentAttendanceStatus | string;
  recordedByTeacherId: string;
  recordedByTeacherName: string;
  createdAtUtc: string;
}

export interface PagedStudentClassroomAttendance {
  items: StudentClassroomAttendance[];
  totalCount: number;
  page: number;
  pageSize: number;
}

export interface StudentWeeklyReportGridRow {
  reportId?: string | null;
  studentId: string;
  studentName: string;
  studentGrade?: number | null;
  weekStartDate: string;
  performancePercent?: number | null;
  attendancePercent?: number | null;
  homeworkPercent?: number | null;
  interactionDuringSession: string;
  openCamera?: boolean | null;
}

export interface StudentWeeklyReport {
  id: string;
  teacherId: string;
  studentId: string;
  studentName: string;
  studentGrade?: number | null;
  weekStartDate: string;
  performancePercent?: number | null;
  attendancePercent?: number | null;
  homeworkPercent?: number | null;
  interactionDuringSession: string;
  openCamera?: boolean | null;
}

export interface SaveWeeklyReportEntry {
  studentId: string;
  performancePercent?: number | null;
  attendancePercent?: number | null;
  homeworkPercent?: number | null;
  interactionDuringSession: string;
  openCamera?: boolean | null;
}

export interface WeeklyStudyPlanTopic {
  id: string;
  title: string;
  highlight: boolean;
  sortOrder: number;
}

export interface WeeklyStudyPlanWeek {
  id: string;
  weekNumber: number;
  fromDate: string;
  toDate: string;
  sortOrder: number;
  topics: WeeklyStudyPlanTopic[];
}

export interface WeeklyStudyPlan {
  id: string;
  teacherId: string;
  teacherName: string;
  courseId: string;
  courseName: string;
  courseGrade?: number | null;
  courseStageId?: number | null;
  courseTerm?: string | null;
  fromDate: string;
  toDate: string;
  notes: string;
  weeks: WeeklyStudyPlanWeek[];
}

export interface SaveWeeklyStudyPlanTopic {
  title: string;
  highlight: boolean;
}

export interface SaveWeeklyStudyPlanWeek {
  weekNumber: number;
  fromDate: string;
  toDate: string;
  topics: SaveWeeklyStudyPlanTopic[];
}

export interface GeneratedStudyPlan {
  notes: string;
  weeks: SaveWeeklyStudyPlanWeek[];
}

export interface GeneratedCourseTreeLesson {
  title: string;
  sortOrder: number;
}

export interface GeneratedCourseTreeUnit {
  title: string;
  sortOrder: number;
  lessons: GeneratedCourseTreeLesson[];
}

export interface GeneratedCourseTree {
  notes: string;
  mode: string;
  applied: boolean;
  units: GeneratedCourseTreeUnit[];
}

export interface GeneratedAssessmentQuestion {
  prompt: string;
  questionType: string;
  options: string[];
  correctOption: string;
  correctAnswer: string;
  points: number;
  sortOrder: number;
}

export interface GeneratedAssessmentDraft {
  kind: string;
  title: string;
  description: string;
  questions: GeneratedAssessmentQuestion[];
  questionIds: string[];
}

export interface TeacherPayrollRow {
  teacherId: string;
  teacherName: string;
  primarySessions: number;
  prepSessions: number;
  secondarySessions: number;
  sessionAmount: number;
  monthlySalary: number;
  manualAmount: number;
  totalAmount: number;
}

export interface TeacherPayrollAdjustment {
  id: string;
  teacherId: string;
  teacherName: string;
  amount: number;
  adjustmentDate: string;
  notes: string;
  createdAtUtc: string;
}

export interface TeacherPayrollReport {
  fromDate: string;
  toDate: string;
  rows: TeacherPayrollRow[];
  grandTotal: number;
}

export interface AccountReport {
  fromDate: string;
  toDate: string;
  totalPayrollSalaries: number;
  totalManualSalaries: number;
  totalSalaries: number;
  totalSubscriptions: number;
  totalOtherExpenses: number;
  netAmount: number;
}

export interface StudentAskedQuestion {
  id: string;
  studentId: string;
  studentName: string;
  courseId: string;
  courseTitle: string;
  unitId?: string | null;
  unitTitle: string;
  lessonId?: string | null;
  lessonTitle: string;
  question: string;
  aiAnswer: string;
  aiInScope: boolean;
  teacherAnswer: string;
  teacherName: string;
  createdAtUtc: string;
  teacherAnsweredAtUtc?: string | null;
  isMine: boolean;
}

export interface AdminLoginDashboardDay {
  date: string;
  teachers: number;
  parents: number;
  students: number;
}

export interface AdminLoginUser {
  id: string;
  displayName: string;
  email: string;
  mobilePhone: string;
  lastLoginDateUtc: string;
}

export interface AdminLoginDashboard {
  fromDate: string;
  toDate: string;
  teacherCount: number;
  parentCount: number;
  studentCount: number;
  days: AdminLoginDashboardDay[];
  teachers: AdminLoginUser[];
  parents: AdminLoginUser[];
  students: AdminLoginUser[];
}

export interface TuitionPayment {
  id: string;
  parentId?: string | null;
  parentName?: string | null;
  studentId?: string | null;
  studentName?: string | null;
  year: number;
  month: number;
  amount: number;
  paymentDate: string;
  notes: string;
  payerLabel: string;
}

export interface OtherExpense {
  id: string;
  name: string;
  amount: number;
  expenseDate: string;
  notes: string;
  createdAtUtc: string;
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
  grade?: number | null;
  schoolType?: StudentSchoolType | string | null;
  totalXp: number;
  mobilePhone?: string;
  workShift?: TeacherWorkShift | string | null;
  stages?: number[];
  contractType?: TeacherContractType | string | null;
  primaryAmount?: number | null;
  prepAmount?: number | null;
  secondaryAmount?: number | null;
  monthlySalary?: number | null;
  courseRates?: TeacherCourseRate[];
}

export interface ZoomConnectionStatus {
  connected: boolean;
  email?: string | null;
  expiresAt?: string | null;
  appFallbackAvailable: boolean;
  userOAuthConfigured?: boolean;
  userOAuthRedirectUri?: string | null;
  userOAuthClientIdMasked?: string | null;
}

export interface ZoomOAuthSettings {
  configured: boolean;
  clientId: string;
  clientSecretMasked: string;
  hasClientSecret: boolean;
  redirectUri: string;
  frontendRedirectUri: string;
  suggestedRedirectUri: string;
}

export interface ClassroomStudent {
  studentId: string;
  displayName: string;
  email: string;
  mobilePhone?: string;
  enrolledCourseIds?: string[];
  enrolledCourseTitles?: string[];
}

export interface ClassroomTeacher {
  teacherId: string;
  displayName: string;
}

export interface ClassroomCourse {
  courseId: string;
  courseTitle: string;
  courseGrade?: number | null;
  courseStageId?: number | null;
  courseSchoolType?: CourseSchoolType | string | null;
  teacherId: string;
  teacherName: string;
}

export interface ClassroomCourseAssignment {
  courseId: string;
  teacherId: string;
}

export interface ClassroomZoomLink {
  name: string;
  url: string;
}

export interface Classroom {
  id: string;
  name: string;
  description: string;
  grade?: number | null;
  teachers: ClassroomTeacher[];
  courses?: ClassroomCourse[];
  courseId?: string | null;
  courseTitle?: string | null;
  courseGrade?: number | null;
  courseStageId?: number | null;
  courseSchoolType?: CourseSchoolType | string | null;
  whatsAppGroupInviteUrl: string;
  zoomLinks: ClassroomZoomLink[];
  whatsAppNotifyPhones: string;
  dailyWhatsAppReportsEnabled?: boolean;
  students: ClassroomStudent[];
}

export interface EnrollStudentResult {
  classroom: Classroom;
  whatsAppStatus: string;
}

export interface ClassroomEnrollmentListItem {
  classroomId: string;
  classroomName: string;
  studentId: string;
  studentName: string;
  studentEmail: string;
  enrolledCourseIds: string[];
  enrolledCourseTitles: string[];
}

export interface PagedClassroomEnrollments {
  items: ClassroomEnrollmentListItem[];
  totalCount: number;
  page: number;
  pageSize: number;
}

export interface PagedCourses {
  items: Course[];
  totalCount: number;
  page: number;
  pageSize: number;
}

export interface PagedWeeklyStudyPlans {
  items: WeeklyStudyPlan[];
  totalCount: number;
  page: number;
  pageSize: number;
}

export interface SendClassroomWhatsAppResult {
  sentCount: number;
  failedCount: number;
  status: string;
  groupShareUrl?: string | null;
}

export interface AssignmentQuestion {
  id: string;
  prompt: string;
  questionType: string;
  passageText?: string;
  optionA?: string | null;
  optionB?: string | null;
  optionC?: string | null;
  options?: ChoiceOption[];
  points: number;
  sortOrder: number;
  correctAnswer?: string | null;
  promptImageUrl?: string | null;
  promptImageMediaAssetId?: string | null;
  children?: AssignmentQuestion[];
}

export interface Assignment {
  id: string;
  classroomId: string;
  classroomName: string;
  title: string;
  description: string;
  dueAtUtc?: string | null;
  xpReward: number;
  isPublished: boolean;
  createdByUserId: string;
  createdByName: string;
  solutionVideoMediaAssetId?: string | null;
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
  promptImageUrl?: string | null;
  answerImageUrl?: string | null;
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
  feedbackImageUrl?: string | null;
  startedAtUtc?: string | null;
  submittedAtUtc: string;
  gradedAtUtc?: string | null;
  solutionVideoMediaAssetId?: string | null;
  answers: AssignmentAnswerReview[];
}

export type BankQuestionType =
  | 'Choose'
  | 'TrueFalse'
  | 'SingleChoice'
  | 'MultiChoice'
  | 'Paragraph'
  | 'Underline'
  | 'FreeText';

export interface BankQuestion {
  id: string;
  courseId: string;
  courseTitle: string;
  lessonId?: string | null;
  lessonTitle?: string | null;
  createdByUserId: string;
  parentQuestionId?: string | null;
  questionType: BankQuestionType | string;
  prompt: string;
  passageText: string;
  optionA?: string | null;
  optionB?: string | null;
  optionC?: string | null;
  optionD?: string | null;
  options?: ChoiceOption[];
  correctAnswer: string;
  points: number;
  sortOrder: number;
  promptImageUrl?: string | null;
  children: BankQuestion[];
}

export interface ExamQuestion {
  id: string;
  bankQuestionId?: string | null;
  parentExamQuestionId?: string | null;
  questionType: string;
  prompt: string;
  passageText: string;
  optionA?: string | null;
  optionB?: string | null;
  optionC?: string | null;
  optionD?: string | null;
  options?: ChoiceOption[];
  points: number;
  sortOrder: number;
  correctAnswer?: string | null;
  promptImageUrl?: string | null;
  children: ExamQuestion[];
}

export interface Exam {
  id: string;
  classroomId: string;
  classroomName: string;
  courseId?: string | null;
  courseTitle?: string | null;
  title: string;
  description: string;
  dueAtUtc?: string | null;
  xpReward: number;
  isPublished: boolean;
  createdByUserId: string;
  createdByName: string;
  questions: ExamQuestion[];
}

export interface ExamAnswerReview {
  questionId: string;
  prompt: string;
  questionType: string;
  answerText: string;
  correctAnswer?: string | null;
  isCorrect?: boolean | null;
  pointsAwarded?: number | null;
  points: number;
  promptImageUrl?: string | null;
  answerImageUrl?: string | null;
}

export type ChatKind = 'Direct' | 'Group' | 'Class' | 0 | 1 | 2;

export interface ChatMember {
  userId: string;
  displayName: string;
  role: string;
  isBlocked: boolean;
}

export interface ChatMessage {
  id: string;
  roomId: string;
  senderId: string;
  senderName: string;
  body: string;
  createdAtUtc: string;
  isDeleted: boolean;
}

export interface ChatRoom {
  id: string;
  classroomId: string;
  classroomName: string;
  courseId: string;
  courseTitle: string;
  unitId?: string | null;
  unitTitle: string;
  lessonId?: string | null;
  lessonTitle: string;
  kind: ChatKind;
  title: string;
  isBlocked: boolean;
  unreadCount: number;
  members: ChatMember[];
}

export interface ChatUnreadSummary {
  totalUnread: number;
  roomId?: string | null;
  roomTitle: string;
}

export interface AppNotification {
  id: string;
  kind: string;
  title: string;
  body: string;
  targetUrl: string;
  entityId?: string | null;
  relatedStudentId?: string | null;
  isRead: boolean;
  createdAtUtc: string;
}

export interface NotificationUnreadSummary {
  unreadCount: number;
}

export interface ExamAttempt {
  id: string;
  examId: string;
  examTitle: string;
  studentId: string;
  studentName: string;
  status: string;
  score?: number | null;
  maxScore?: number | null;
  teacherFeedback?: string | null;
  feedbackImageUrl?: string | null;
  startedAtUtc: string;
  submittedAtUtc?: string | null;
  gradedAtUtc?: string | null;
  durationSeconds?: number | null;
  answers: ExamAnswerReview[];
}

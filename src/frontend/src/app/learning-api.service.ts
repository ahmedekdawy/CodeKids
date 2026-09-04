import { HttpClient, HttpEventType } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable, filter, map } from 'rxjs';
import {
  Appointment,
  Assignment,
  AssignmentSubmission,
  Avatar,
  Badge,
  BankQuestion,
  ChatMember,
  ChatMessage,
  ChatRoom,
  ChatUnreadSummary,
  AppNotification,
  NotificationUnreadSummary,
  Classroom,
  ClassroomCourseAssignment,
  ClassroomZoomLink,
  CompleteStepResponse,
  Course,
  CourseLesson,
  CourseUnit,
  CourseVideoLibraryItem,
  CreateMeetingPayload,
  ClassroomDiagnosis,
  DailyWhatsAppReportsResult,
  EnrollStudentResult,
  PagedClassroomEnrollments,
  PagedCourses,
  PagedWeeklyStudyPlans,
  PagedStudentClassroomAttendance,
  StudentClassroomAttendance,
  Exam,
  ExamAttempt,
  FixedTimetableEntry,
  Grade,
  TeacherSessionAttendance,
  StudentWeeklyReportGridRow,
  StudentWeeklyReport,
  SaveWeeklyReportEntry,
  TopWeeklyStudent,
  WeeklyStudyPlan,
  SaveWeeklyStudyPlanWeek,
  GeneratedStudyPlan,
  GeneratedCourseTree,
  GeneratedAssessmentDraft,
  TeacherPayrollReport,
  TeacherPayrollAdjustment,
  AccountReport,
  AdminLoginDashboard,
  TuitionPayment,
  OtherExpense,
  Lesson,
  LiveSession,
  ManagedUser,
  MediaAsset,
  ParentChildOverview,
  ParentDashboard,
  ParentManagedAccount,
  PlaybackInfo,
  Quiz,
  QuizAttemptReview,
  TeacherQuizListItem,
  TeacherQuizDetail,
  SendAdminWhatsAppResult,
  SendClassroomWhatsAppResult,
  StudentSummary,
  SubmitQuizResponse,
  TeacherDashboard,
  TeacherStudentDetail,
  TeacherVideoLibrary,
  WatchSession,
  ZoomConnectionStatus,
  ZoomOAuthSettings,
  SiteSettings,
  Stage,
  StudentAskedQuestion,
  Subject
} from './models';
import { normalizePmStartMinutes } from './fixed-timetable.util';
import { resolveApiBaseUrl } from './api-base-url';

@Injectable({ providedIn: 'root' })
export class LearningApiService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = resolveApiBaseUrl();

  getCourses(includeContent = true): Observable<Course[]> {
    const query = includeContent ? '' : '?includeContent=false';
    return this.http.get<Course[]>(`${this.baseUrl}/courses${query}`);
  }

  getCourse(courseId: string): Observable<Course> {
    return this.http.get<Course>(`${this.baseUrl}/courses/${courseId}`);
  }

  getStages(): Observable<Stage[]> {
    return this.http.get<Stage[]>(`${this.baseUrl}/stages`);
  }

  getGrades(stageId?: number | null): Observable<Grade[]> {
    const query = stageId == null ? '' : `?stageId=${stageId}`;
    return this.http.get<Grade[]>(`${this.baseUrl}/grades${query}`);
  }

  getSubjects(stageId?: number | null): Observable<Subject[]> {
    const query = stageId == null ? '' : `?stageId=${stageId}`;
    return this.http.get<Subject[]>(`${this.baseUrl}/subjects${query}`);
  }

  getLessons(courseId?: string): Observable<Lesson[]> {
    const query = courseId ? `?courseId=${courseId}` : '';
    return this.http.get<Lesson[]>(`${this.baseUrl}/lessons${query}`);
  }

  getLesson(lessonId: string): Observable<Lesson> {
    return this.http.get<Lesson>(`${this.baseUrl}/lessons/${lessonId}`);
  }

  setStudentAskEnabled(
    scope: 'course' | 'unit' | 'lesson',
    id: string,
    enabled: boolean
  ): Observable<{ scope: string; id: string; enabled: boolean }> {
    return this.http.put<{ scope: string; id: string; enabled: boolean }>(`${this.baseUrl}/admin/student-ask`, {
      scope,
      id,
      enabled
    });
  }

  askStudentQuestion(payload: {
    question: string;
    courseId?: string | null;
    unitId?: string | null;
    lessonId?: string | null;
  }): Observable<{ inScope: boolean; answer: string }> {
    return this.http.post<{ inScope: boolean; answer: string }>(`${this.baseUrl}/student-ask`, payload);
  }

  listStudentAskedQuestions(filters: {
    courseId?: string;
    unitId?: string;
    lessonId?: string;
    fromDate?: string;
    toDate?: string;
    q?: string;
  }): Observable<StudentAskedQuestion[]> {
    const params = new URLSearchParams();
    if (filters.courseId) params.set('courseId', filters.courseId);
    if (filters.unitId) params.set('unitId', filters.unitId);
    if (filters.lessonId) params.set('lessonId', filters.lessonId);
    if (filters.fromDate) params.set('fromDate', filters.fromDate);
    if (filters.toDate) params.set('toDate', filters.toDate);
    if (filters.q) params.set('q', filters.q);
    const query = params.toString();
    return this.http.get<StudentAskedQuestion[]>(
      `${this.baseUrl}/student-ask/questions${query ? `?${query}` : ''}`
    );
  }

  answerStudentAskedQuestion(id: string, answer: string): Observable<StudentAskedQuestion> {
    return this.http.put<StudentAskedQuestion>(`${this.baseUrl}/student-ask/questions/${id}/answer`, {
      answer
    });
  }

  updateStudentAskedQuestion(id: string, question: string): Observable<StudentAskedQuestion> {
    return this.http.put<StudentAskedQuestion>(`${this.baseUrl}/student-ask/questions/${id}`, { question });
  }

  deleteStudentAskedQuestion(id: string): Observable<void> {
    return this.http.delete<void>(`${this.baseUrl}/student-ask/questions/${id}`);
  }

  listChatRooms(): Observable<ChatRoom[]> {
    return this.http.get<ChatRoom[]>(`${this.baseUrl}/chat/rooms`);
  }

  getChatUnreadSummary(): Observable<ChatUnreadSummary> {
    return this.http.get<ChatUnreadSummary>(`${this.baseUrl}/chat/unread`);
  }

  listNotifications(limit = 30): Observable<AppNotification[]> {
    return this.http.get<AppNotification[]>(`${this.baseUrl}/notifications`, { params: { limit } });
  }

  getNotificationUnreadSummary(): Observable<NotificationUnreadSummary> {
    return this.http.get<NotificationUnreadSummary>(`${this.baseUrl}/notifications/unread`);
  }

  markNotificationRead(id: string): Observable<AppNotification> {
    return this.http.post<AppNotification>(`${this.baseUrl}/notifications/${id}/read`, {});
  }

  markAllNotificationsRead(): Observable<number> {
    return this.http.post<number>(`${this.baseUrl}/notifications/read-all`, {});
  }

  markChatRoomRead(roomId: string): Observable<void> {
    return this.http.put<void>(`${this.baseUrl}/chat/rooms/${roomId}/read`, {});
  }

  createChatRoom(payload: {
    classroomId: string;
    courseId: string;
    unitId?: string | null;
    lessonId?: string | null;
    kind: 'Direct' | 'Group' | 'Class';
    studentIds?: string[];
  }): Observable<ChatRoom> {
    return this.http.post<ChatRoom>(`${this.baseUrl}/chat/rooms`, payload);
  }

  listChatMessages(roomId: string): Observable<ChatMessage[]> {
    return this.http.get<ChatMessage[]>(`${this.baseUrl}/chat/rooms/${roomId}/messages`);
  }

  sendChatMessage(roomId: string, body: string): Observable<ChatMessage> {
    return this.http.post<ChatMessage>(`${this.baseUrl}/chat/rooms/${roomId}/messages`, { body });
  }

  deleteChatMessage(id: string): Observable<ChatMessage> {
    return this.http.delete<ChatMessage>(`${this.baseUrl}/chat/messages/${id}`);
  }

  setChatMemberBlocked(roomId: string, studentId: string, blocked: boolean): Observable<ChatMember> {
    return this.http.put<ChatMember>(`${this.baseUrl}/chat/rooms/${roomId}/members/${studentId}/block`, {
      blocked
    });
  }

  getStudentSummary(): Observable<StudentSummary> {
    return this.http.get<StudentSummary>(`${this.baseUrl}/progress/me`);
  }

  completeStep(payload: {
    lessonId: string;
    stepId: string;
    submittedAnswer: string;
  }): Observable<CompleteStepResponse> {
    return this.http.post<CompleteStepResponse>(`${this.baseUrl}/progress/complete-step`, payload);
  }

  getQuizzes(courseId?: string): Observable<Quiz[]> {
    const query = courseId ? `?courseId=${courseId}` : '';
    return this.http.get<Quiz[]>(`${this.baseUrl}/quizzes${query}`);
  }

  getQuiz(quizId: string): Observable<Quiz> {
    return this.http.get<Quiz>(`${this.baseUrl}/quizzes/${quizId}`);
  }

  createQuiz(payload: {
    courseId: string;
    classroomId?: string | null;
    title: string;
    description?: string;
    xpReward: number;
    durationMinutes?: number | null;
    isPublished: boolean;
    questions: {
      id?: string | null;
      prompt: string;
      questionType?: string;
      passageText?: string;
      optionA?: string | null;
      optionB?: string | null;
      optionC?: string | null;
      options?: string[];
      correctOption?: string;
      correctAnswer?: string;
      points?: number;
      sortOrder: number;
      promptImageMediaAssetId?: string | null;
      children?: unknown[];
    }[];
  }): Observable<Quiz> {
    return this.http.post<Quiz>(`${this.baseUrl}/quizzes`, payload);
  }

  getTeacherQuizzes(filters?: {
    fromDate?: string;
    toDate?: string;
    grade?: number;
    courseId?: string;
  }): Observable<TeacherQuizListItem[]> {
    const params = new URLSearchParams();
    if (filters?.fromDate) params.set('fromDate', filters.fromDate);
    if (filters?.toDate) params.set('toDate', filters.toDate);
    if (filters?.grade != null) params.set('grade', String(filters.grade));
    if (filters?.courseId) params.set('courseId', filters.courseId);
    const query = params.toString();
    return this.http.get<TeacherQuizListItem[]>(
      `${this.baseUrl}/teacher/quizzes${query ? `?${query}` : ''}`
    );
  }

  getTeacherQuiz(quizId: string): Observable<TeacherQuizDetail> {
    return this.http.get<TeacherQuizDetail>(`${this.baseUrl}/teacher/quizzes/${quizId}`);
  }

  updateQuiz(
    quizId: string,
    payload: {
      courseId: string;
      classroomId?: string | null;
      title: string;
      description?: string;
      xpReward: number;
      durationMinutes?: number | null;
      isPublished: boolean;
      questions: {
        id?: string | null;
        prompt: string;
        questionType?: string;
        passageText?: string;
        optionA?: string | null;
        optionB?: string | null;
        optionC?: string | null;
        options?: string[];
        correctOption?: string;
        correctAnswer?: string;
        points?: number;
        sortOrder: number;
        promptImageMediaAssetId?: string | null;
        children?: unknown[];
      }[];
    }
  ): Observable<Quiz> {
    return this.http.put<Quiz>(`${this.baseUrl}/teacher/quizzes/${quizId}`, payload);
  }

  deleteQuiz(quizId: string): Observable<void> {
    return this.http.delete<void>(`${this.baseUrl}/teacher/quizzes/${quizId}`);
  }

  publishQuiz(quizId: string): Observable<Quiz> {
    return this.http.post<Quiz>(`${this.baseUrl}/teacher/quizzes/${quizId}/publish`, {});
  }

  getQuizAttempts(quizId: string): Observable<QuizAttemptReview[]> {
    return this.http.get<QuizAttemptReview[]>(`${this.baseUrl}/teacher/quizzes/${quizId}/attempts`);
  }

  submitQuiz(payload: {
    quizId: string;
    answers: { questionId: string; selectedOption: string }[];
  }): Observable<SubmitQuizResponse> {
    return this.http.post<SubmitQuizResponse>(`${this.baseUrl}/quizzes/submit`, payload);
  }

  getBadges(): Observable<Badge[]> {
    return this.http.get<Badge[]>(`${this.baseUrl}/badges/me`);
  }

  getAvatars(): Observable<Avatar[]> {
    return this.http.get<Avatar[]>(`${this.baseUrl}/avatars`);
  }

  selectAvatar(avatarId: string): Observable<Avatar> {
    return this.http.post<Avatar>(`${this.baseUrl}/avatars/select`, { avatarId });
  }

  getParentDashboard(): Observable<ParentDashboard> {
    return this.http.get<ParentDashboard>(`${this.baseUrl}/dashboard/parent`);
  }

  getParentChildOverview(childId: string): Observable<ParentChildOverview> {
    return this.http.get<ParentChildOverview>(`${this.baseUrl}/parent/children/${childId}`);
  }

  updateParentManagedAccount(
    userId: string,
    payload: { email?: string | null; mobilePhone?: string | null; password?: string | null }
  ): Observable<ParentManagedAccount> {
    return this.http.put<ParentManagedAccount>(`${this.baseUrl}/parent/accounts/${userId}`, payload);
  }

  getTeacherDashboard(): Observable<TeacherDashboard> {
    return this.http.get<TeacherDashboard>(`${this.baseUrl}/dashboard/teacher`);
  }

  getTeacherStudentDetail(studentId: string): Observable<TeacherStudentDetail> {
    return this.http.get<TeacherStudentDetail>(`${this.baseUrl}/dashboard/teacher/students/${studentId}`);
  }

  getClassroomDiagnosis(classroomId: string): Observable<ClassroomDiagnosis> {
    return this.http.get<ClassroomDiagnosis>(
      `${this.baseUrl}/dashboard/teacher/classrooms/${classroomId}/diagnosis`
    );
  }

  runDailyWhatsAppReports(force = true): Observable<DailyWhatsAppReportsResult> {
    return this.http.post<DailyWhatsAppReportsResult>(
      `${this.baseUrl}/reports/whatsapp/daily?force=${force}`,
      {}
    );
  }

  getMeetings(): Observable<LiveSession[]> {
    return this.http.get<LiveSession[]>(`${this.baseUrl}/meetings`);
  }

  createMeeting(payload: CreateMeetingPayload): Observable<LiveSession> {
    return this.http.post<LiveSession>(`${this.baseUrl}/meetings`, payload);
  }

  getAppointments(fromUtc?: string, toUtc?: string): Observable<Appointment[]> {
    const params = new URLSearchParams();
    if (fromUtc) params.set('fromUtc', fromUtc);
    if (toUtc) params.set('toUtc', toUtc);
    const query = params.toString();
    return this.http.get<Appointment[]>(`${this.baseUrl}/admin/appointments${query ? `?${query}` : ''}`);
  }

  getMyAppointments(fromUtc?: string, toUtc?: string): Observable<Appointment[]> {
    const params = new URLSearchParams();
    if (fromUtc) params.set('fromUtc', fromUtc);
    if (toUtc) params.set('toUtc', toUtc);
    const query = params.toString();
    return this.http.get<Appointment[]>(`${this.baseUrl}/appointments${query ? `?${query}` : ''}`);
  }

  createAppointment(payload: {
    teacherId: string;
    courseId: string;
    startsAtUtc: string;
    endsAtUtc: string;
    notes?: string | null;
    repeatWeekly?: boolean;
    repeatUntilUtc?: string | null;
  }): Observable<{ items: Appointment[] }> {
    return this.http.post<{ items: Appointment[] }>(`${this.baseUrl}/admin/appointments`, payload);
  }

  updateAppointment(
    appointmentId: string,
    payload: {
      teacherId: string;
      courseId: string;
      startsAtUtc: string;
      endsAtUtc: string;
      notes?: string | null;
    }
  ): Observable<Appointment> {
    return this.http.put<Appointment>(`${this.baseUrl}/admin/appointments/${appointmentId}`, payload);
  }

  deleteAppointment(appointmentId: string): Observable<void> {
    return this.http.delete<void>(`${this.baseUrl}/admin/appointments/${appointmentId}`);
  }

  getTimetableEntries(filters?: {
    teacherId?: string;
    grade?: number;
    period?: string;
  }): Observable<FixedTimetableEntry[]> {
    const params = new URLSearchParams();
    if (filters?.teacherId) params.set('teacherId', filters.teacherId);
    if (filters?.grade != null) params.set('grade', String(filters.grade));
    if (filters?.period) params.set('period', filters.period);
    const query = params.toString();
    return this.http.get<FixedTimetableEntry[]>(
      `${this.baseUrl}/admin/timetable-entries${query ? `?${query}` : ''}`
    );
  }

  getMyTimetableEntries(filters?: { grade?: number; period?: string }): Observable<FixedTimetableEntry[]> {
    const params = new URLSearchParams();
    if (filters?.grade != null) params.set('grade', String(filters.grade));
    if (filters?.period) params.set('period', filters.period);
    const query = params.toString();
    return this.http.get<FixedTimetableEntry[]>(
      `${this.baseUrl}/timetable-entries${query ? `?${query}` : ''}`
    );
  }

  createTimetableEntry(payload: {
    teacherId: string;
    courseId: string;
    dayOfWeek: number;
    sessionNumber: number;
    period: string;
    combinedGrades?: number[];
  }): Observable<FixedTimetableEntry> {
    return this.http.post<FixedTimetableEntry>(`${this.baseUrl}/admin/timetable-entries`, payload);
  }

  updateTimetableEntry(
    entryId: string,
    payload: {
      teacherId: string;
      courseId: string;
      dayOfWeek: number;
      sessionNumber: number;
      period: string;
      combinedGrades?: number[];
    }
  ): Observable<FixedTimetableEntry> {
    return this.http.put<FixedTimetableEntry>(`${this.baseUrl}/admin/timetable-entries/${entryId}`, payload);
  }

  deleteTimetableEntry(entryId: string): Observable<void> {
    return this.http.delete<void>(`${this.baseUrl}/admin/timetable-entries/${entryId}`);
  }

  getSessionAttendance(filters?: {
    teacherId?: string;
    grade?: number;
    sessionDate?: string;
    fromDate?: string;
    toDate?: string;
  }): Observable<TeacherSessionAttendance[]> {
    const params = new URLSearchParams();
    if (filters?.teacherId) params.set('teacherId', filters.teacherId);
    if (filters?.grade != null) params.set('grade', String(filters.grade));
    if (filters?.sessionDate) params.set('sessionDate', filters.sessionDate);
    if (filters?.fromDate) params.set('fromDate', filters.fromDate);
    if (filters?.toDate) params.set('toDate', filters.toDate);
    const query = params.toString();
    return this.http.get<TeacherSessionAttendance[]>(
      `${this.baseUrl}/admin/session-attendance${query ? `?${query}` : ''}`
    );
  }

  getMySessionAttendance(filters?: {
    grade?: number;
    sessionDate?: string;
    fromDate?: string;
    toDate?: string;
  }): Observable<TeacherSessionAttendance[]> {
    const params = new URLSearchParams();
    if (filters?.grade != null) params.set('grade', String(filters.grade));
    if (filters?.sessionDate) params.set('sessionDate', filters.sessionDate);
    if (filters?.fromDate) params.set('fromDate', filters.fromDate);
    if (filters?.toDate) params.set('toDate', filters.toDate);
    const query = params.toString();
    return this.http.get<TeacherSessionAttendance[]>(
      `${this.baseUrl}/session-attendance${query ? `?${query}` : ''}`
    );
  }

  createSessionAttendance(payload: {
    teacherId: string;
    courseId: string;
    sessionDate: string;
  }): Observable<TeacherSessionAttendance> {
    return this.http.post<TeacherSessionAttendance>(`${this.baseUrl}/admin/session-attendance`, payload);
  }

  createMySessionAttendance(payload: {
    courseId: string;
    sessionDate: string;
  }): Observable<TeacherSessionAttendance> {
    return this.http.post<TeacherSessionAttendance>(`${this.baseUrl}/session-attendance`, payload);
  }

  deleteSessionAttendance(attendanceId: string): Observable<void> {
    return this.http.delete<void>(`${this.baseUrl}/admin/session-attendance/${attendanceId}`);
  }

  deleteMySessionAttendance(attendanceId: string): Observable<void> {
    return this.http.delete<void>(`${this.baseUrl}/session-attendance/${attendanceId}`);
  }

  getStudentAttendance(params: {
    classroomId?: string;
    gradeId?: number;
    fromDate?: string;
    toDate?: string;
    studentSearch?: string;
    sortKey?: string;
    sortDir?: string;
    page?: number;
    pageSize?: number;
  }): Observable<PagedStudentClassroomAttendance> {
    const query = new URLSearchParams();
    if (params.classroomId) query.set('classroomId', params.classroomId);
    if (params.gradeId != null) query.set('gradeId', String(params.gradeId));
    if (params.fromDate) query.set('fromDate', params.fromDate);
    if (params.toDate) query.set('toDate', params.toDate);
    if (params.studentSearch?.trim()) query.set('studentSearch', params.studentSearch.trim());
    if (params.sortKey) query.set('sortKey', params.sortKey);
    if (params.sortDir) query.set('sortDir', params.sortDir);
    if (params.page) query.set('page', String(params.page));
    if (params.pageSize) query.set('pageSize', String(params.pageSize));
    const qs = query.toString();
    return this.http.get<PagedStudentClassroomAttendance>(`${this.baseUrl}/student-attendance${qs ? `?${qs}` : ''}`);
  }

  createStudentAttendance(payload: {
    studentId: string;
    classroomId: string;
    attendanceDate: string;
    status?: string;
  }): Observable<StudentClassroomAttendance> {
    return this.http.post<StudentClassroomAttendance>(`${this.baseUrl}/student-attendance`, payload);
  }

  deleteStudentAttendance(attendanceId: string): Observable<void> {
    return this.http.delete<void>(`${this.baseUrl}/student-attendance/${attendanceId}`);
  }

  getAdminStudentAttendance(params: {
    classroomId?: string;
    gradeId?: number;
    fromDate?: string;
    toDate?: string;
    studentSearch?: string;
    sortKey?: string;
    sortDir?: string;
    page?: number;
    pageSize?: number;
  }): Observable<PagedStudentClassroomAttendance> {
    const query = new URLSearchParams();
    if (params.classroomId) query.set('classroomId', params.classroomId);
    if (params.gradeId != null) query.set('gradeId', String(params.gradeId));
    if (params.fromDate) query.set('fromDate', params.fromDate);
    if (params.toDate) query.set('toDate', params.toDate);
    if (params.studentSearch?.trim()) query.set('studentSearch', params.studentSearch.trim());
    if (params.sortKey) query.set('sortKey', params.sortKey);
    if (params.sortDir) query.set('sortDir', params.sortDir);
    if (params.page) query.set('page', String(params.page));
    if (params.pageSize) query.set('pageSize', String(params.pageSize));
    const qs = query.toString();
    return this.http.get<PagedStudentClassroomAttendance>(`${this.baseUrl}/admin/student-attendance${qs ? `?${qs}` : ''}`);
  }

  createAdminStudentAttendance(payload: {
    studentId: string;
    classroomId: string;
    attendanceDate: string;
    status?: string;
  }): Observable<StudentClassroomAttendance> {
    return this.http.post<StudentClassroomAttendance>(`${this.baseUrl}/admin/student-attendance`, payload);
  }

  deleteAdminStudentAttendance(attendanceId: string): Observable<void> {
    return this.http.delete<void>(`${this.baseUrl}/admin/student-attendance/${attendanceId}`);
  }

  getWeeklyReportGrid(filters: { weekStart: string; grade?: number }): Observable<StudentWeeklyReportGridRow[]> {
    const params = new URLSearchParams();
    params.set('weekStart', filters.weekStart);
    if (filters.grade != null) params.set('grade', String(filters.grade));
    return this.http.get<StudentWeeklyReportGridRow[]>(`${this.baseUrl}/weekly-reports/grid?${params}`);
  }

  listWeeklyReports(filters?: {
    grade?: number;
    fromDate?: string;
    toDate?: string;
  }): Observable<StudentWeeklyReport[]> {
    const params = new URLSearchParams();
    if (filters?.grade != null) params.set('grade', String(filters.grade));
    if (filters?.fromDate) params.set('fromDate', filters.fromDate);
    if (filters?.toDate) params.set('toDate', filters.toDate);
    const query = params.toString();
    return this.http.get<StudentWeeklyReport[]>(`${this.baseUrl}/weekly-reports${query ? `?${query}` : ''}`);
  }

  listAdminWeeklyReports(filters?: {
    teacherId?: string;
    grade?: number;
    fromDate?: string;
    toDate?: string;
  }): Observable<StudentWeeklyReport[]> {
    const params = new URLSearchParams();
    if (filters?.teacherId) params.set('teacherId', filters.teacherId);
    if (filters?.grade != null) params.set('grade', String(filters.grade));
    if (filters?.fromDate) params.set('fromDate', filters.fromDate);
    if (filters?.toDate) params.set('toDate', filters.toDate);
    const query = params.toString();
    return this.http.get<StudentWeeklyReport[]>(`${this.baseUrl}/admin/weekly-reports${query ? `?${query}` : ''}`);
  }

  saveWeeklyReports(payload: {
    weekStartDate: string;
    entries: SaveWeeklyReportEntry[];
  }): Observable<StudentWeeklyReportGridRow[]> {
    return this.http.put<StudentWeeklyReportGridRow[]>(`${this.baseUrl}/weekly-reports`, payload);
  }

  listTopWeeklyStudents(weekStart?: string): Observable<TopWeeklyStudent[]> {
    const params = new URLSearchParams();
    if (weekStart) params.set('weekStart', weekStart);
    const query = params.toString();
    return this.http.get<TopWeeklyStudent[]>(
      `${this.baseUrl}/weekly-reports/top-students${query ? `?${query}` : ''}`
    );
  }

  listStudyPlans(filters?: {
    courseId?: string;
    teacherId?: string;
    studentId?: string;
    fromDate?: string;
    toDate?: string;
  }): Observable<WeeklyStudyPlan[]> {
    const params = new URLSearchParams();
    if (filters?.courseId) params.set('courseId', filters.courseId);
    if (filters?.teacherId) params.set('teacherId', filters.teacherId);
    if (filters?.studentId) params.set('studentId', filters.studentId);
    if (filters?.fromDate) params.set('fromDate', filters.fromDate);
    if (filters?.toDate) params.set('toDate', filters.toDate);
    const query = params.toString();
    return this.http.get<WeeklyStudyPlan[]>(`${this.baseUrl}/study-plans${query ? `?${query}` : ''}`);
  }

  getAdminStudyPlans(params: {
    teacherId?: string;
    courseId?: string;
    fromDate?: string;
    toDate?: string;
    sortKey?: string;
    sortDir?: string;
    page?: number;
    pageSize?: number;
  }): Observable<PagedWeeklyStudyPlans> {
    const query = new URLSearchParams();
    if (params.teacherId) query.set('teacherId', params.teacherId);
    if (params.courseId) query.set('courseId', params.courseId);
    if (params.fromDate) query.set('fromDate', params.fromDate);
    if (params.toDate) query.set('toDate', params.toDate);
    if (params.sortKey) query.set('sortKey', params.sortKey);
    if (params.sortDir) query.set('sortDir', params.sortDir);
    if (params.page) query.set('page', String(params.page));
    if (params.pageSize) query.set('pageSize', String(params.pageSize));
    const qs = query.toString();
    return this.http.get<PagedWeeklyStudyPlans>(`${this.baseUrl}/admin/study-plans${qs ? `?${qs}` : ''}`);
  }

  saveStudyPlan(payload: {
    id?: string | null;
    courseId: string;
    fromDate: string;
    toDate: string;
    notes?: string;
    weeks: SaveWeeklyStudyPlanWeek[];
  }): Observable<WeeklyStudyPlan> {
    return this.http.put<WeeklyStudyPlan>(`${this.baseUrl}/study-plans`, payload);
  }

  generateStudyPlan(payload: {
    courseId: string;
    fromDate: string;
    toDate: string;
    language?: string;
    prompt?: string;
  }): Observable<GeneratedStudyPlan> {
    return this.http.post<GeneratedStudyPlan>(`${this.baseUrl}/study-plans/generate`, payload);
  }

  generateAssessment(payload: {
    kind: 'Quiz' | 'Exam' | 'Assignment';
    courseId?: string | null;
    classroomId?: string | null;
    unitIds?: string[] | null;
    lessonIds?: string[] | null;
    questionCount?: number;
    questionType?: string;
    language?: string;
  }): Observable<GeneratedAssessmentDraft> {
    return this.http.post<GeneratedAssessmentDraft>(`${this.baseUrl}/assessments/generate`, payload);
  }

  deleteStudyPlan(planId: string): Observable<void> {
    return this.http.delete<void>(`${this.baseUrl}/study-plans/${planId}`);
  }

  getPayrollReport(filters: {
    fromDate: string;
    toDate: string;
    teacherId?: string;
    stage?: number;
    grade?: number;
  }): Observable<TeacherPayrollReport> {
    const params = new URLSearchParams();
    params.set('fromDate', filters.fromDate);
    params.set('toDate', filters.toDate);
    if (filters.teacherId) params.set('teacherId', filters.teacherId);
    if (filters.stage != null) params.set('stage', String(filters.stage));
    if (filters.grade != null) params.set('grade', String(filters.grade));
    return this.http.get<TeacherPayrollReport>(
      `${this.baseUrl}/admin/payroll-report?${params.toString()}`
    );
  }

  getPayrollAdjustments(filters?: {
    fromDate?: string;
    toDate?: string;
    teacherId?: string;
  }): Observable<TeacherPayrollAdjustment[]> {
    const params = new URLSearchParams();
    if (filters?.fromDate) params.set('fromDate', filters.fromDate);
    if (filters?.toDate) params.set('toDate', filters.toDate);
    if (filters?.teacherId) params.set('teacherId', filters.teacherId);
    const query = params.toString();
    return this.http.get<TeacherPayrollAdjustment[]>(
      `${this.baseUrl}/admin/payroll-adjustments${query ? `?${query}` : ''}`
    );
  }

  createPayrollAdjustment(payload: {
    teacherId: string;
    amount: number;
    adjustmentDate: string;
    notes?: string;
  }): Observable<TeacherPayrollAdjustment> {
    return this.http.post<TeacherPayrollAdjustment>(`${this.baseUrl}/admin/payroll-adjustments`, payload);
  }

  deletePayrollAdjustment(adjustmentId: string): Observable<void> {
    return this.http.delete<void>(`${this.baseUrl}/admin/payroll-adjustments/${adjustmentId}`);
  }

  getAccountReport(filters: {
    fromDate: string;
    toDate: string;
  }): Observable<AccountReport> {
    const params = new URLSearchParams();
    params.set('fromDate', filters.fromDate);
    params.set('toDate', filters.toDate);
    return this.http.get<AccountReport>(
      `${this.baseUrl}/admin/account-report?${params.toString()}`
    );
  }

  getAdminLoginDashboard(filters: {
    fromDate: string;
    toDate: string;
  }): Observable<AdminLoginDashboard> {
    const params = new URLSearchParams();
    params.set('fromDate', filters.fromDate);
    params.set('toDate', filters.toDate);
    return this.http.get<AdminLoginDashboard>(
      `${this.baseUrl}/admin/dashboard/logins?${params.toString()}`
    );
  }

  getTuitionPayments(filters?: {
    parentId?: string;
    studentId?: string;
    fromDate?: string;
    toDate?: string;
    year?: number;
    month?: number;
  }): Observable<TuitionPayment[]> {
    const params = new URLSearchParams();
    if (filters?.parentId) params.set('parentId', filters.parentId);
    if (filters?.studentId) params.set('studentId', filters.studentId);
    if (filters?.fromDate) params.set('fromDate', filters.fromDate);
    if (filters?.toDate) params.set('toDate', filters.toDate);
    if (filters?.year != null) params.set('year', String(filters.year));
    if (filters?.month != null) params.set('month', String(filters.month));
    const query = params.toString();
    return this.http.get<TuitionPayment[]>(
      `${this.baseUrl}/admin/payments${query ? `?${query}` : ''}`
    );
  }

  createTuitionPayment(payload: {
    parentId?: string | null;
    studentId?: string | null;
    year: number;
    month: number;
    amount: number;
    paymentDate: string;
    notes?: string | null;
  }): Observable<TuitionPayment> {
    return this.http.post<TuitionPayment>(`${this.baseUrl}/admin/payments`, payload);
  }

  deleteTuitionPayment(paymentId: string): Observable<void> {
    return this.http.delete<void>(`${this.baseUrl}/admin/payments/${paymentId}`);
  }

  getOtherExpenses(filters?: {
    fromDate?: string;
    toDate?: string;
    name?: string;
  }): Observable<OtherExpense[]> {
    const params = new URLSearchParams();
    if (filters?.fromDate) params.set('fromDate', filters.fromDate);
    if (filters?.toDate) params.set('toDate', filters.toDate);
    if (filters?.name) params.set('name', filters.name);
    const query = params.toString();
    return this.http.get<OtherExpense[]>(
      `${this.baseUrl}/admin/other-expenses${query ? `?${query}` : ''}`
    );
  }

  createOtherExpense(payload: {
    name: string;
    amount: number;
    expenseDate: string;
    notes?: string | null;
  }): Observable<OtherExpense> {
    return this.http.post<OtherExpense>(`${this.baseUrl}/admin/other-expenses`, payload);
  }

  deleteOtherExpense(expenseId: string): Observable<void> {
    return this.http.delete<void>(`${this.baseUrl}/admin/other-expenses/${expenseId}`);
  }

  getUsers(role?: string): Observable<ManagedUser[]> {
    const query = role ? `?role=${role}` : '';
    return this.http.get<ManagedUser[]>(`${this.baseUrl}/admin/users${query}`);
  }

  createUser(payload: {
    email?: string | null;
    displayName: string;
    password: string;
    role: string;
    parentId?: string | null;
    grade?: number | null;
    schoolType?: string | null;
    mobilePhone?: string | null;
    workShift?: string | null;
    stages?: number[] | null;
    contractType?: string | null;
    primaryAmount?: number | null;
    prepAmount?: number | null;
    secondaryAmount?: number | null;
    monthlySalary?: number | null;
    courseRates?: Array<{
      courseId: string;
      sessionAmount?: number | null;
      monthlySalary?: number | null;
    }> | null;
  }): Observable<ManagedUser> {
    return this.http.post<ManagedUser>(`${this.baseUrl}/admin/users`, payload);
  }

  updateUser(
    userId: string,
    payload: {
      email?: string | null;
      displayName: string;
      role: string;
      parentId?: string | null;
      password?: string | null;
      grade?: number | null;
      schoolType?: string | null;
      mobilePhone?: string | null;
      workShift?: string | null;
      stages?: number[] | null;
      contractType?: string | null;
      primaryAmount?: number | null;
      prepAmount?: number | null;
      secondaryAmount?: number | null;
      monthlySalary?: number | null;
      courseRates?: Array<{
        courseId: string;
        sessionAmount?: number | null;
        monthlySalary?: number | null;
      }> | null;
    }
  ): Observable<ManagedUser> {
    return this.http.put<ManagedUser>(`${this.baseUrl}/admin/users/${userId}`, payload);
  }

  deleteUser(userId: string): Observable<void> {
    return this.http.delete<void>(`${this.baseUrl}/admin/users/${userId}`);
  }

  setUserActive(userId: string, isActive: boolean): Observable<ManagedUser> {
    return this.http.post<ManagedUser>(`${this.baseUrl}/admin/users/${userId}/active`, { isActive });
  }

  getZoomStatus(): Observable<ZoomConnectionStatus> {
    return this.http.get<ZoomConnectionStatus>(`${this.baseUrl}/zoom/status`);
  }

  getZoomConnectUrl(): Observable<{ authorizeUrl: string; userOAuthConfigured: boolean }> {
    return this.http.get<{ authorizeUrl: string; userOAuthConfigured: boolean }>(`${this.baseUrl}/zoom/connect`);
  }

  disconnectZoom(): Observable<void> {
    return this.http.post<void>(`${this.baseUrl}/zoom/disconnect`, {});
  }

  getZoomOAuthSettings(): Observable<ZoomOAuthSettings> {
    return this.http.get<ZoomOAuthSettings>(`${this.baseUrl}/zoom/oauth-settings`);
  }

  saveZoomOAuthSettings(payload: {
    clientId: string;
    clientSecret?: string | null;
    redirectUri?: string | null;
    frontendRedirectUri?: string | null;
  }): Observable<ZoomOAuthSettings> {
    return this.http.put<ZoomOAuthSettings>(`${this.baseUrl}/zoom/oauth-settings`, payload);
  }

  getAdminCourses(params: {
    titleSearch?: string;
    stageId?: number;
    grade?: number;
    sortKey?: string;
    sortDir?: string;
    page?: number;
    pageSize?: number;
  }): Observable<PagedCourses> {
    const query = new URLSearchParams();
    if (params.titleSearch?.trim()) query.set('titleSearch', params.titleSearch.trim());
    if (params.stageId != null) query.set('stageId', String(params.stageId));
    if (params.grade != null) query.set('grade', String(params.grade));
    if (params.sortKey) query.set('sortKey', params.sortKey);
    if (params.sortDir) query.set('sortDir', params.sortDir);
    if (params.page) query.set('page', String(params.page));
    if (params.pageSize) query.set('pageSize', String(params.pageSize));
    const qs = query.toString();
    return this.http.get<PagedCourses>(`${this.baseUrl}/admin/courses${qs ? `?${qs}` : ''}`);
  }

  createCourse(payload: {
    title: string;
    theme: string;
    description: string;
    ageMin?: number | null;
    ageMax?: number | null;
    term?: string | null;
    grades?: number[] | null;
    stageId?: number | null;
    schoolType?: string | null;
    sortOrder?: number | null;
    isPublished?: boolean;
  }): Observable<Course[]> {
    return this.http.post<Course[]>(`${this.baseUrl}/admin/courses`, payload);
  }

  setCoursePublished(courseId: string, isPublished: boolean): Observable<Course> {
    return this.http.post<Course>(`${this.baseUrl}/admin/courses/${courseId}/published`, { isPublished });
  }

  updateCourse(
    courseId: string,
    payload: {
      title: string;
      theme: string;
      description: string;
      ageMin?: number | null;
      ageMax?: number | null;
      term?: string | null;
      grade?: number | null;
      stageId?: number | null;
      schoolType?: string | null;
      sortOrder?: number | null;
      isPublished?: boolean;
    }
  ): Observable<Course> {
    return this.http.put<Course>(`${this.baseUrl}/admin/courses/${courseId}`, payload);
  }

  createCourseUnit(
    courseId: string,
    payload: { title: string; description?: string | null; sortOrder?: number | null }
  ): Observable<CourseUnit> {
    return this.http.post<CourseUnit>(`${this.baseUrl}/admin/courses/${courseId}/units`, payload);
  }

  updateCourseUnit(
    unitId: string,
    payload: { title: string; description?: string | null; sortOrder?: number | null }
  ): Observable<CourseUnit> {
    return this.http.put<CourseUnit>(`${this.baseUrl}/admin/units/${unitId}`, payload);
  }

  deleteCourseUnit(unitId: string): Observable<void> {
    return this.http.delete<void>(`${this.baseUrl}/admin/units/${unitId}`);
  }

  createCourseLesson(
    unitId: string,
    payload: {
      title: string;
      theme: string;
      description?: string | null;
      difficulty?: number | null;
      xpReward?: number | null;
      sortOrder?: number | null;
    }
  ): Observable<CourseLesson> {
    return this.http.post<CourseLesson>(`${this.baseUrl}/admin/units/${unitId}/lessons`, payload);
  }

  updateCourseLesson(
    lessonId: string,
    payload: {
      unitId?: string | null;
      title: string;
      theme: string;
      description?: string | null;
      difficulty?: number | null;
      xpReward?: number | null;
      sortOrder?: number | null;
    }
  ): Observable<CourseLesson> {
    return this.http.put<CourseLesson>(`${this.baseUrl}/admin/lessons/${lessonId}`, payload);
  }

  deleteCourseLesson(lessonId: string): Observable<void> {
    return this.http.delete<void>(`${this.baseUrl}/admin/lessons/${lessonId}`);
  }

  generateCourseTree(
    courseId: string,
    payload: {
      mode: 'rebuild' | 'update';
      prompt?: string;
      language?: string;
      apply?: boolean;
    }
  ): Observable<GeneratedCourseTree> {
    return this.http.post<GeneratedCourseTree>(
      `${this.baseUrl}/admin/courses/${courseId}/tree/generate`,
      payload
    );
  }

  getSiteSettings(): Observable<SiteSettings> {
    return this.http
      .get<SiteSettings>(`${this.baseUrl}/site-settings`)
      .pipe(map((settings) => normalizeSiteSettings(settings)));
  }

  updateSiteSettings(payload: {
    siteName: string;
    clearLogo?: boolean;
    clearBanner?: boolean;
    timetableWeekStartUtc?: string | null;
    clearTimetableWeek?: boolean;
    amSessionCount?: number;
    pmSessionCount?: number;
    pmStartMinutes?: number;
  }): Observable<SiteSettings> {
    return this.http
      .put<SiteSettings>(`${this.baseUrl}/admin/site-settings`, payload)
      .pipe(map((settings) => normalizeSiteSettings(settings)));
  }

  uploadSiteImage(kind: 'logo' | 'banner', file: File): Observable<SiteSettings> {
    const form = new FormData();
    form.append('file', file);
    form.append('kind', kind);
    return this.http
      .post<SiteSettings>(`${this.baseUrl}/admin/site-settings/upload`, form)
      .pipe(map((settings) => normalizeSiteSettings(settings)));
  }

  siteAssetUrl(path: string | null | undefined): string | null {
    if (!path) return null;
    if (path.startsWith('http://') || path.startsWith('https://')) return path;
    const root = this.baseUrl.replace(/\/api\/?$/, '');
    return `${root}${path.startsWith('/') ? path : `/${path}`}`;
  }

  deleteCourse(courseId: string): Observable<void> {
    return this.http.delete<void>(`${this.baseUrl}/admin/courses/${courseId}`);
  }

  getClassrooms(): Observable<Classroom[]> {
    return this.http.get<Classroom[]>(`${this.baseUrl}/classrooms`);
  }

  getClassroomEnrollments(params: {
    classroomId?: string;
    courseId?: string;
    studentSearch?: string;
    sortKey?: string;
    sortDir?: string;
    page?: number;
    pageSize?: number;
  }): Observable<PagedClassroomEnrollments> {
    const query = new URLSearchParams();
    if (params.classroomId) query.set('classroomId', params.classroomId);
    if (params.courseId) query.set('courseId', params.courseId);
    if (params.studentSearch?.trim()) query.set('studentSearch', params.studentSearch.trim());
    if (params.sortKey) query.set('sortKey', params.sortKey);
    if (params.sortDir) query.set('sortDir', params.sortDir);
    if (params.page) query.set('page', String(params.page));
    if (params.pageSize) query.set('pageSize', String(params.pageSize));
    const qs = query.toString();
    return this.http.get<PagedClassroomEnrollments>(
      `${this.baseUrl}/classrooms/enrollments${qs ? `?${qs}` : ''}`
    );
  }

  createClassroom(payload: {
    name: string;
    description?: string;
    grade?: number | null;
    courses?: ClassroomCourseAssignment[] | null;
    whatsAppGroupInviteUrl?: string;
    zoomLinks?: ClassroomZoomLink[];
    whatsAppNotifyPhones?: string;
  }): Observable<Classroom> {
    return this.http.post<Classroom>(`${this.baseUrl}/classrooms`, payload);
  }

  updateClassroom(
    classroomId: string,
    payload: {
      name: string;
      description?: string;
      grade?: number | null;
      courses?: ClassroomCourseAssignment[] | null;
      whatsAppGroupInviteUrl?: string;
      zoomLinks?: ClassroomZoomLink[];
      whatsAppNotifyPhones?: string;
    }
  ): Observable<Classroom> {
    return this.http.put<Classroom>(`${this.baseUrl}/classrooms/${classroomId}`, payload);
  }

  deleteClassroom(classroomId: string): Observable<void> {
    return this.http.delete<void>(`${this.baseUrl}/classrooms/${classroomId}`);
  }

  assignClassroom(
    classroomId: string,
    payload: { courses?: ClassroomCourseAssignment[] | null }
  ): Observable<Classroom> {
    return this.http.put<Classroom>(`${this.baseUrl}/classrooms/${classroomId}/assignments`, payload);
  }

  addStudentToClassroom(
    classroomId: string,
    studentId: string,
    courseIds?: string[]
  ): Observable<EnrollStudentResult> {
    return this.http.post<EnrollStudentResult>(`${this.baseUrl}/classrooms/${classroomId}/students`, {
      studentId,
      courseIds: courseIds?.length ? courseIds : null
    });
  }

  removeStudentFromClassroom(classroomId: string, studentId: string): Observable<Classroom> {
    return this.http.delete<Classroom>(`${this.baseUrl}/classrooms/${classroomId}/students/${studentId}`);
  }

  updateClassroomWhatsApp(
    classroomId: string,
    payload: {
      whatsAppGroupInviteUrl?: string;
      whatsAppNotifyPhones?: string;
      dailyWhatsAppReportsEnabled?: boolean;
    }
  ): Observable<Classroom> {
    return this.http.put<Classroom>(`${this.baseUrl}/classrooms/${classroomId}/whatsapp`, payload);
  }

  updateClassroomZoom(classroomId: string, payload: { zoomLinks?: ClassroomZoomLink[] }): Observable<Classroom> {
    return this.http.put<Classroom>(`${this.baseUrl}/classrooms/${classroomId}/zoom`, payload);
  }

  sendClassroomWhatsApp(
    classroomId: string,
    payload: {
      message: string;
      studentIds?: string[] | null;
      includeGroupInviteLink?: boolean;
    }
  ): Observable<SendClassroomWhatsAppResult> {
    return this.http.post<SendClassroomWhatsAppResult>(
      `${this.baseUrl}/classrooms/${classroomId}/whatsapp/send`,
      payload
    );
  }

  sendAdminWhatsApp(payload: { phones: string[]; message: string }): Observable<SendAdminWhatsAppResult> {
    return this.http.post<SendAdminWhatsAppResult>(`${this.baseUrl}/admin/whatsapp/send`, payload);
  }

  getAssignments(classroomId?: string): Observable<Assignment[]> {
    const query = classroomId ? `?classroomId=${classroomId}` : '';
    return this.http.get<Assignment[]>(`${this.baseUrl}/assignments${query}`);
  }

  getAssignment(assignmentId: string): Observable<Assignment> {
    return this.http.get<Assignment>(`${this.baseUrl}/assignments/${assignmentId}`);
  }

  createAssignment(payload: {
    classroomId: string;
    title: string;
    description?: string;
    dueAtUtc?: string | null;
    xpReward: number;
    isPublished: boolean;
    questions: {
      prompt: string;
      questionType: string;
      passageText?: string;
      optionA?: string | null;
      optionB?: string | null;
      optionC?: string | null;
      options?: string[];
      correctAnswer: string;
      points: number;
      sortOrder: number;
      promptImageMediaAssetId?: string | null;
      id?: string | null;
      children?: unknown[];
    }[];
  }): Observable<Assignment> {
    return this.http.post<Assignment>(`${this.baseUrl}/assignments`, payload);
  }

  updateAssignment(
    assignmentId: string,
    payload: {
      classroomId: string;
      title: string;
      description?: string;
      dueAtUtc?: string | null;
      xpReward: number;
      isPublished: boolean;
      questions: {
        prompt: string;
        questionType: string;
        passageText?: string;
        optionA?: string | null;
        optionB?: string | null;
        optionC?: string | null;
        options?: string[];
        correctAnswer: string;
        points: number;
        sortOrder: number;
        promptImageMediaAssetId?: string | null;
        id?: string | null;
        children?: unknown[];
      }[];
    }
  ): Observable<Assignment> {
    return this.http.put<Assignment>(`${this.baseUrl}/assignments/${assignmentId}`, payload);
  }

  deleteAssignment(assignmentId: string): Observable<void> {
    return this.http.delete<void>(`${this.baseUrl}/assignments/${assignmentId}`);
  }

  publishAssignment(assignmentId: string): Observable<Assignment> {
    return this.http.post<Assignment>(`${this.baseUrl}/assignments/${assignmentId}/publish`, {});
  }

  submitAssignment(payload: {
    assignmentId: string;
    answers: { questionId: string; answerText: string; answerImageMediaAssetId?: string | null }[];
  }): Observable<AssignmentSubmission> {
    return this.http.post<AssignmentSubmission>(`${this.baseUrl}/assignments/submit`, payload);
  }

  getAssignmentSubmissions(assignmentId: string): Observable<AssignmentSubmission[]> {
    return this.http.get<AssignmentSubmission[]>(`${this.baseUrl}/assignments/${assignmentId}/submissions`);
  }

  gradeSubmission(payload: {
    submissionId: string;
    teacherFeedback?: string;
    feedbackImageMediaAssetId?: string | null;
    answers?: { questionId: string; isCorrect: boolean; pointsAwarded: number }[];
  }): Observable<AssignmentSubmission> {
    return this.http.post<AssignmentSubmission>(`${this.baseUrl}/assignments/submissions/grade`, payload);
  }

  getBankQuestions(courseId?: string): Observable<BankQuestion[]> {
    const query = courseId ? `?courseId=${courseId}` : '';
    return this.http.get<BankQuestion[]>(`${this.baseUrl}/question-bank${query}`);
  }

  createBankQuestion(payload: {
    courseId: string;
    lessonId?: string | null;
    questionType: string;
    prompt: string;
    passageText?: string;
    optionA?: string | null;
    optionB?: string | null;
    optionC?: string | null;
    optionD?: string | null;
    options?: string[];
    correctAnswer?: string;
    points: number;
    sortOrder: number;
    promptImageMediaAssetId?: string | null;
    children?: {
      prompt: string;
      questionType: string;
      optionA?: string | null;
      optionB?: string | null;
      optionC?: string | null;
      optionD?: string | null;
      options?: string[];
      correctAnswer: string;
      points: number;
      sortOrder: number;
      promptImageMediaAssetId?: string | null;
    }[];
  }): Observable<BankQuestion> {
    return this.http.post<BankQuestion>(`${this.baseUrl}/question-bank`, payload);
  }

  deleteBankQuestion(questionId: string): Observable<void> {
    return this.http.delete<void>(`${this.baseUrl}/question-bank/${questionId}`);
  }

  getExams(classroomId?: string): Observable<Exam[]> {
    const query = classroomId ? `?classroomId=${classroomId}` : '';
    return this.http.get<Exam[]>(`${this.baseUrl}/exams${query}`);
  }

  getExam(examId: string): Observable<Exam> {
    return this.http.get<Exam>(`${this.baseUrl}/exams/${examId}`);
  }

  createExam(payload: {
    classroomId: string;
    courseId?: string | null;
    title: string;
    description?: string;
    dueAtUtc?: string | null;
    xpReward: number;
    durationMinutes?: number | null;
    isPublished: boolean;
    questionIds: string[];
  }): Observable<Exam> {
    return this.http.post<Exam>(`${this.baseUrl}/exams`, payload);
  }

  publishExam(examId: string): Observable<Exam> {
    return this.http.post<Exam>(`${this.baseUrl}/exams/${examId}/publish`, {});
  }

  submitExam(payload: {
    examId: string;
    answers: { questionId: string; answerText: string; answerImageMediaAssetId?: string | null }[];
  }): Observable<ExamAttempt> {
    return this.http.post<ExamAttempt>(`${this.baseUrl}/exams/submit`, payload);
  }

  startExam(examId: string): Observable<ExamAttempt> {
    return this.http.post<ExamAttempt>(`${this.baseUrl}/exams/${examId}/start`, {});
  }

  getExamAttempts(examId: string): Observable<ExamAttempt[]> {
    return this.http.get<ExamAttempt[]>(`${this.baseUrl}/exams/${examId}/attempts`);
  }

  gradeExamAttempt(payload: {
    attemptId: string;
    teacherFeedback?: string;
    feedbackImageMediaAssetId?: string | null;
    answers?: { questionId: string; isCorrect: boolean; pointsAwarded: number }[];
  }): Observable<ExamAttempt> {
    return this.http.post<ExamAttempt>(`${this.baseUrl}/exams/attempts/grade`, payload);
  }

  uploadMedia(file: File, durationSeconds?: number): Observable<MediaAsset> {
    const form = new FormData();
    form.append('file', file, file.name);
    if (durationSeconds && durationSeconds > 0) {
      form.append('durationSeconds', String(Math.round(durationSeconds)));
    }
    return this.http.post<MediaAsset>(`${this.baseUrl}/media/upload`, form);
  }

  uploadMediaWithProgress(
    file: File,
    durationSeconds?: number
  ): Observable<{ progress: number; asset?: MediaAsset }> {
    const form = new FormData();
    form.append('file', file, file.name);
    if (durationSeconds && durationSeconds > 0) {
      form.append('durationSeconds', String(Math.round(durationSeconds)));
    }

    return this.http
      .post<MediaAsset>(`${this.baseUrl}/media/upload`, form, {
        reportProgress: true,
        observe: 'events'
      })
      .pipe(
        map((event) => {
          if (event.type === HttpEventType.UploadProgress) {
            const total = event.total ?? 0;
            const progress = total > 0 ? Math.round((100 * event.loaded) / total) : 0;
            return { progress };
          }
          if (event.type === HttpEventType.Response) {
            return { progress: 100, asset: event.body ?? undefined };
          }
          return null;
        }),
        filter((update): update is { progress: number; asset?: MediaAsset } => update !== null)
      );
  }

  uploadQuestionImage(file: File): Observable<{ id: string; url: string; contentType: string }> {
    const form = new FormData();
    form.append('file', file, file.name);
    return this.http.post<{ id: string; url: string; contentType: string }>(
      `${this.baseUrl}/question-images/upload`,
      form
    );
  }

  registerMediaFromUrl(payload: { url: string; title?: string | null }): Observable<MediaAsset> {
    return this.http.post<MediaAsset>(`${this.baseUrl}/media/from-url`, payload);
  }

  getVideoLibrary(): Observable<TeacherVideoLibrary> {
    return this.http.get<TeacherVideoLibrary>(`${this.baseUrl}/media/library`);
  }

  attachLessonVideo(
    lessonId: string,
    payload: { mediaAssetId: string; title?: string; sortOrder?: number }
  ): Observable<{ id: string; lessonId: string; mediaAssetId: string; title: string }> {
    return this.http.post<{ id: string; lessonId: string; mediaAssetId: string; title: string }>(
      `${this.baseUrl}/lessons/${lessonId}/videos`,
      payload
    );
  }

  deleteLessonVideo(lessonVideoId: string): Observable<void> {
    return this.http.delete<void>(`${this.baseUrl}/lessons/videos/${lessonVideoId}`);
  }

  getCourseVideoLibrary(): Observable<CourseVideoLibraryItem[]> {
    return this.http.get<CourseVideoLibraryItem[]>(`${this.baseUrl}/media/course-videos`);
  }

  attachCourseVideo(
    courseId: string,
    payload: { mediaAssetId: string; title?: string; sortOrder?: number }
  ): Observable<{ id: string; courseId: string; mediaAssetId: string; title: string }> {
    return this.http.post<{ id: string; courseId: string; mediaAssetId: string; title: string }>(
      `${this.baseUrl}/courses/${courseId}/videos`,
      payload
    );
  }

  deleteCourseVideo(courseVideoId: string): Observable<void> {
    return this.http.delete<void>(`${this.baseUrl}/courses/videos/${courseVideoId}`);
  }

  attachAssignmentSolutionVideo(assignmentId: string, mediaAssetId: string): Observable<MediaAsset> {
    return this.http.post<MediaAsset>(`${this.baseUrl}/assignments/${assignmentId}/solution-video`, {
      mediaAssetId
    });
  }

  deleteAssignmentSolutionVideo(assignmentId: string): Observable<void> {
    return this.http.delete<void>(`${this.baseUrl}/assignments/${assignmentId}/solution-video`);
  }

  getPlayback(mediaAssetId: string): Observable<PlaybackInfo> {
    const params = { apiBase: this.baseUrl };
    return this.http.get<PlaybackInfo>(`${this.baseUrl}/media/${mediaAssetId}/playback`, { params }).pipe(
      map((info) => ({
        ...info,
        playbackUrl: this.normalizeMediaPlaybackUrl(info)
      }))
    );
  }

  private normalizeMediaPlaybackUrl(info: PlaybackInfo): string {
    const url = info.playbackUrl;
    if (!url) return url;

    try {
      const parsed = new URL(url);
      if (parsed.pathname.endsWith('/media/stream')) {
        return `${this.baseUrl}/media/stream${parsed.search}`;
      }
    } catch {
      // Relative URLs are returned as-is.
    }

    return url;
  }

  recordWatchEvents(payload: {
    mediaAssetId: string;
    lessonId?: string | null;
    sessionId?: string | null;
    events: {
      eventType: string;
      positionSeconds: number;
      playbackRate?: number;
      fromSeconds?: number;
      toSeconds?: number;
      clientAtUtc?: string;
    }[];
  }): Observable<WatchSession> {
    return this.http.post<WatchSession>(`${this.baseUrl}/media/watch-events`, payload);
  }

  getWatchSessions(mediaAssetId: string): Observable<WatchSession[]> {
    return this.http.get<WatchSession[]>(`${this.baseUrl}/media/${mediaAssetId}/watch-sessions`);
  }
}

function normalizeSiteSettings(raw: SiteSettings | Record<string, unknown>): SiteSettings {
  const item = raw as SiteSettings & Record<string, unknown>;
  const timetableWeek =
    item.timetableWeekStartUtc ?? item['TimetableWeekStartUtc'] ?? item['timetableWeekStartUtc'];
  return {
    siteName: String(item.siteName ?? item['SiteName'] ?? 'CodeKids'),
    logoUrl: (item.logoUrl ?? item['LogoUrl'] ?? null) as string | null,
    bannerUrl: (item.bannerUrl ?? item['BannerUrl'] ?? null) as string | null,
    timetableWeekStartUtc: timetableWeek == null ? null : String(timetableWeek),
    amSessionCount: Number(item.amSessionCount ?? item['AmSessionCount'] ?? 6) || 6,
    pmSessionCount: Number(item.pmSessionCount ?? item['PmSessionCount'] ?? 6) || 6,
    pmStartMinutes: normalizePmStartMinutes(item.pmStartMinutes ?? item['PmStartMinutes']),
    updatedAtUtc: String(item.updatedAtUtc ?? item['UpdatedAtUtc'] ?? '')
  };
}

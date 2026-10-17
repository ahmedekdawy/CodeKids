import {
  HttpClient,
  HttpEventType,
  Injectable,
  Subject,
  __spreadProps,
  __spreadValues,
  filter,
  inject,
  map,
  resolveApiBaseUrl,
  setClassMetadata,
  tap,
  ɵɵdefineInjectable
} from "./chunk-EMYLRMBO.js";

// src/app/unread-changed.ts
var unreadChanged = new Subject();

// src/app/fixed-timetable.util.ts
var SESSION_MINUTES = 45;
var BREAK_MINUTES = 5;
var PERIOD_STEP_MINUTES = SESSION_MINUTES + BREAK_MINUTES;
var DEFAULT_SESSION_COUNT = 6;
var MIN_SESSION_COUNT = 1;
var MAX_SESSION_COUNT = 12;
var AM_START_MINUTES = 8 * 60;
var DEFAULT_PM_START_MINUTES = 15 * 60;
var MIN_PM_START_MINUTES = 12 * 60;
var MAX_PM_START_MINUTES = 21 * 60;
function sessionNumbers(count) {
  const n = normalizeSessionCount(count);
  return Array.from({ length: n }, (_, i) => i + 1);
}
function normalizeSessionCount(value, fallback = DEFAULT_SESSION_COUNT) {
  const n = Number(value);
  if (!Number.isInteger(n))
    return fallback;
  return Math.min(MAX_SESSION_COUNT, Math.max(MIN_SESSION_COUNT, n));
}
function buildSessionSlots(period, count = DEFAULT_SESSION_COUNT, pmStartMinutes = DEFAULT_PM_START_MINUTES, shiftMinutes = 0) {
  const base = period === "am" ? AM_START_MINUTES : normalizePmStartMinutes(pmStartMinutes);
  return sessionNumbers(count).map((sessionNumber) => {
    const startMinutes = base + (sessionNumber - 1) * PERIOD_STEP_MINUTES;
    const endMinutes = startMinutes + SESSION_MINUTES;
    const dayOffset = Math.floor((startMinutes + shiftMinutes) / MINUTES_PER_DAY);
    const dayMarker = dayOffset === 0 ? "" : ` (${dayOffset > 0 ? "+" : "-"}${Math.abs(dayOffset)})`;
    const timeLabel = `${formatClock(wrapDay(startMinutes + shiftMinutes))}-${formatClock(wrapDay(endMinutes + shiftMinutes))}${dayMarker}`;
    return {
      sessionNumber,
      period,
      key: `${period}-${sessionNumber}`,
      startMinutes,
      endMinutes,
      timeLabel,
      headerLabel: `${sessionNumber}
${timeLabel}`
    };
  });
}
function visibleSessionSlots(periodFilter, amCount = DEFAULT_SESSION_COUNT, pmCount = DEFAULT_SESSION_COUNT, pmStartMinutes = DEFAULT_PM_START_MINUTES, shiftMinutes = 0) {
  if (periodFilter === "am")
    return buildSessionSlots("am", amCount, pmStartMinutes, shiftMinutes);
  if (periodFilter === "pm")
    return buildSessionSlots("pm", pmCount, pmStartMinutes, shiftMinutes);
  return [
    ...buildSessionSlots("am", amCount, pmStartMinutes, shiftMinutes),
    ...buildSessionSlots("pm", pmCount, pmStartMinutes, shiftMinutes)
  ];
}
var MINUTES_PER_DAY = 24 * 60;
function wrapDay(totalMinutes) {
  return (totalMinutes % MINUTES_PER_DAY + MINUTES_PER_DAY) % MINUTES_PER_DAY;
}
function normalizePmStartMinutes(value, fallback = DEFAULT_PM_START_MINUTES) {
  const n = Number(value);
  if (!Number.isInteger(n) || n <= 0)
    return fallback;
  return Math.min(MAX_PM_START_MINUTES, Math.max(MIN_PM_START_MINUTES, n));
}
function minutesToTimeInput(totalMinutes) {
  const normalized = normalizePmStartMinutes(totalMinutes);
  const hour = Math.floor(normalized / 60);
  const minute = normalized % 60;
  return `${String(hour).padStart(2, "0")}:${String(minute).padStart(2, "0")}`;
}
function timeInputToMinutes(value, fallback = DEFAULT_PM_START_MINUTES) {
  const match = /^(\d{1,2}):(\d{2})$/.exec((value ?? "").trim());
  if (!match)
    return fallback;
  return normalizePmStartMinutes(Number(match[1]) * 60 + Number(match[2]), fallback);
}
function formatClock(totalMinutes) {
  const hour24 = Math.floor(totalMinutes / 60) % 24;
  const minute = (totalMinutes % 60 + 60) % 60;
  const hour12 = hour24 % 12 === 0 ? 12 : hour24 % 12;
  if (minute === 0)
    return String(hour12);
  return `${hour12}:${String(minute).padStart(2, "0")}`;
}
var WEEKDAY_INDEXES = [0, 1, 2, 3, 4, 5, 6];
function arabicWeekdayName(dayOfWeek) {
  const sunday = new Date(2026, 0, 4);
  sunday.setDate(sunday.getDate() + dayOfWeek);
  return sunday.toLocaleDateString("ar", { weekday: "long" });
}

// src/app/learning-api.service.ts
var LearningApiService = class _LearningApiService {
  constructor() {
    this.http = inject(HttpClient);
    this.baseUrl = resolveApiBaseUrl();
  }
  getCourses(includeContent = true) {
    const query = includeContent ? "" : "?includeContent=false";
    return this.http.get(`${this.baseUrl}/courses${query}`);
  }
  getCourse(courseId) {
    return this.http.get(`${this.baseUrl}/courses/${courseId}`);
  }
  getStages() {
    return this.http.get(`${this.baseUrl}/stages`);
  }
  getGrades(stageId) {
    const query = stageId == null ? "" : `?stageId=${stageId}`;
    return this.http.get(`${this.baseUrl}/grades${query}`);
  }
  getSubjects(stageId) {
    const query = stageId == null ? "" : `?stageId=${stageId}`;
    return this.http.get(`${this.baseUrl}/subjects${query}`);
  }
  getLessons(courseId) {
    const query = courseId ? `?courseId=${courseId}` : "";
    return this.http.get(`${this.baseUrl}/lessons${query}`);
  }
  getLesson(lessonId) {
    return this.http.get(`${this.baseUrl}/lessons/${lessonId}`);
  }
  setStudentAskEnabled(scope, id, enabled) {
    return this.http.put(`${this.baseUrl}/admin/student-ask`, {
      scope,
      id,
      enabled
    });
  }
  askStudentQuestion(payload) {
    return this.http.post(`${this.baseUrl}/student-ask`, payload);
  }
  listStudentAskedQuestions(filters) {
    const params = new URLSearchParams();
    if (filters.courseId)
      params.set("courseId", filters.courseId);
    if (filters.unitId)
      params.set("unitId", filters.unitId);
    if (filters.lessonId)
      params.set("lessonId", filters.lessonId);
    if (filters.fromDate)
      params.set("fromDate", filters.fromDate);
    if (filters.toDate)
      params.set("toDate", filters.toDate);
    if (filters.q)
      params.set("q", filters.q);
    const query = params.toString();
    return this.http.get(`${this.baseUrl}/student-ask/questions${query ? `?${query}` : ""}`);
  }
  answerStudentAskedQuestion(id, answer) {
    return this.http.put(`${this.baseUrl}/student-ask/questions/${id}/answer`, {
      answer
    });
  }
  updateStudentAskedQuestion(id, question) {
    return this.http.put(`${this.baseUrl}/student-ask/questions/${id}`, { question });
  }
  deleteStudentAskedQuestion(id) {
    return this.http.delete(`${this.baseUrl}/student-ask/questions/${id}`);
  }
  listChatRooms() {
    return this.http.get(`${this.baseUrl}/chat/rooms`);
  }
  getChatUnreadSummary() {
    return this.http.get(`${this.baseUrl}/chat/unread`);
  }
  listNotifications(limit = 30) {
    return this.http.get(`${this.baseUrl}/notifications`, { params: { limit } });
  }
  getNotificationUnreadSummary() {
    return this.http.get(`${this.baseUrl}/notifications/unread`);
  }
  markNotificationRead(id) {
    return this.http.post(`${this.baseUrl}/notifications/${id}/read`, {}).pipe(tap(() => unreadChanged.next()));
  }
  markAllNotificationsRead() {
    return this.http.post(`${this.baseUrl}/notifications/read-all`, {}).pipe(tap(() => unreadChanged.next()));
  }
  markChatRoomRead(roomId) {
    return this.http.put(`${this.baseUrl}/chat/rooms/${roomId}/read`, {}).pipe(tap(() => unreadChanged.next()));
  }
  createChatRoom(payload) {
    return this.http.post(`${this.baseUrl}/chat/rooms`, payload);
  }
  listChatMessages(roomId) {
    return this.http.get(`${this.baseUrl}/chat/rooms/${roomId}/messages`);
  }
  sendChatMessage(roomId, body) {
    return this.http.post(`${this.baseUrl}/chat/rooms/${roomId}/messages`, { body });
  }
  deleteChatMessage(id) {
    return this.http.delete(`${this.baseUrl}/chat/messages/${id}`);
  }
  setChatMemberBlocked(roomId, studentId, blocked) {
    return this.http.put(`${this.baseUrl}/chat/rooms/${roomId}/members/${studentId}/block`, {
      blocked
    });
  }
  getStudentSummary() {
    return this.http.get(`${this.baseUrl}/progress/me`);
  }
  completeStep(payload) {
    return this.http.post(`${this.baseUrl}/progress/complete-step`, payload);
  }
  getQuizzes(courseId) {
    const query = courseId ? `?courseId=${courseId}` : "";
    return this.http.get(`${this.baseUrl}/quizzes${query}`);
  }
  getQuiz(quizId) {
    return this.http.get(`${this.baseUrl}/quizzes/${quizId}`);
  }
  createQuiz(payload) {
    return this.http.post(`${this.baseUrl}/quizzes`, payload);
  }
  getTeacherQuizzes(filters) {
    const params = new URLSearchParams();
    if (filters?.fromDate)
      params.set("fromDate", filters.fromDate);
    if (filters?.toDate)
      params.set("toDate", filters.toDate);
    if (filters?.grade != null)
      params.set("grade", String(filters.grade));
    if (filters?.courseId)
      params.set("courseId", filters.courseId);
    const query = params.toString();
    return this.http.get(`${this.baseUrl}/teacher/quizzes${query ? `?${query}` : ""}`);
  }
  getTeacherQuiz(quizId) {
    return this.http.get(`${this.baseUrl}/teacher/quizzes/${quizId}`);
  }
  updateQuiz(quizId, payload) {
    return this.http.put(`${this.baseUrl}/teacher/quizzes/${quizId}`, payload);
  }
  deleteQuiz(quizId) {
    return this.http.delete(`${this.baseUrl}/teacher/quizzes/${quizId}`);
  }
  publishQuiz(quizId) {
    return this.http.post(`${this.baseUrl}/teacher/quizzes/${quizId}/publish`, {});
  }
  getQuizAttempts(quizId) {
    return this.http.get(`${this.baseUrl}/teacher/quizzes/${quizId}/attempts`);
  }
  submitQuiz(payload) {
    return this.http.post(`${this.baseUrl}/quizzes/submit`, payload);
  }
  getBadges() {
    return this.http.get(`${this.baseUrl}/badges/me`);
  }
  getAvatars() {
    return this.http.get(`${this.baseUrl}/avatars`);
  }
  selectAvatar(avatarId) {
    return this.http.post(`${this.baseUrl}/avatars/select`, { avatarId });
  }
  getParentDashboard() {
    return this.http.get(`${this.baseUrl}/dashboard/parent`);
  }
  getParentChildOverview(childId) {
    return this.http.get(`${this.baseUrl}/parent/children/${childId}`);
  }
  updateParentManagedAccount(userId, payload) {
    return this.http.put(`${this.baseUrl}/parent/accounts/${userId}`, payload);
  }
  getTeacherDashboard() {
    return this.http.get(`${this.baseUrl}/dashboard/teacher`);
  }
  getTeacherStudentDetail(studentId) {
    return this.http.get(`${this.baseUrl}/dashboard/teacher/students/${studentId}`);
  }
  getClassroomDiagnosis(classroomId) {
    return this.http.get(`${this.baseUrl}/dashboard/teacher/classrooms/${classroomId}/diagnosis`);
  }
  runDailyWhatsAppReports(force = true) {
    return this.http.post(`${this.baseUrl}/reports/whatsapp/daily?force=${force}`, {});
  }
  getMeetings() {
    return this.http.get(`${this.baseUrl}/meetings`);
  }
  createMeeting(payload) {
    return this.http.post(`${this.baseUrl}/meetings`, payload);
  }
  getAppointments(fromUtc, toUtc) {
    const params = new URLSearchParams();
    if (fromUtc)
      params.set("fromUtc", fromUtc);
    if (toUtc)
      params.set("toUtc", toUtc);
    const query = params.toString();
    return this.http.get(`${this.baseUrl}/admin/appointments${query ? `?${query}` : ""}`);
  }
  getMyAppointments(fromUtc, toUtc) {
    const params = new URLSearchParams();
    if (fromUtc)
      params.set("fromUtc", fromUtc);
    if (toUtc)
      params.set("toUtc", toUtc);
    const query = params.toString();
    return this.http.get(`${this.baseUrl}/appointments${query ? `?${query}` : ""}`);
  }
  createAppointment(payload) {
    return this.http.post(`${this.baseUrl}/admin/appointments`, payload);
  }
  updateAppointment(appointmentId, payload) {
    return this.http.put(`${this.baseUrl}/admin/appointments/${appointmentId}`, payload);
  }
  deleteAppointment(appointmentId) {
    return this.http.delete(`${this.baseUrl}/admin/appointments/${appointmentId}`);
  }
  getTimetableEntries(filters) {
    const params = new URLSearchParams();
    if (filters?.teacherId)
      params.set("teacherId", filters.teacherId);
    if (filters?.grade != null)
      params.set("grade", String(filters.grade));
    if (filters?.period)
      params.set("period", filters.period);
    const query = params.toString();
    return this.http.get(`${this.baseUrl}/admin/timetable-entries${query ? `?${query}` : ""}`);
  }
  getMyTimetableEntries(filters) {
    const params = new URLSearchParams();
    if (filters?.grade != null)
      params.set("grade", String(filters.grade));
    if (filters?.period)
      params.set("period", filters.period);
    const query = params.toString();
    return this.http.get(`${this.baseUrl}/timetable-entries${query ? `?${query}` : ""}`);
  }
  createTimetableEntry(payload) {
    return this.http.post(`${this.baseUrl}/admin/timetable-entries`, payload);
  }
  updateTimetableEntry(entryId, payload) {
    return this.http.put(`${this.baseUrl}/admin/timetable-entries/${entryId}`, payload);
  }
  deleteTimetableEntry(entryId) {
    return this.http.delete(`${this.baseUrl}/admin/timetable-entries/${entryId}`);
  }
  getSessionAttendance(filters) {
    const params = new URLSearchParams();
    if (filters?.teacherId)
      params.set("teacherId", filters.teacherId);
    if (filters?.grade != null)
      params.set("grade", String(filters.grade));
    if (filters?.sessionDate)
      params.set("sessionDate", filters.sessionDate);
    if (filters?.fromDate)
      params.set("fromDate", filters.fromDate);
    if (filters?.toDate)
      params.set("toDate", filters.toDate);
    const query = params.toString();
    return this.http.get(`${this.baseUrl}/admin/session-attendance${query ? `?${query}` : ""}`);
  }
  getMySessionAttendance(filters) {
    const params = new URLSearchParams();
    if (filters?.grade != null)
      params.set("grade", String(filters.grade));
    if (filters?.sessionDate)
      params.set("sessionDate", filters.sessionDate);
    if (filters?.fromDate)
      params.set("fromDate", filters.fromDate);
    if (filters?.toDate)
      params.set("toDate", filters.toDate);
    const query = params.toString();
    return this.http.get(`${this.baseUrl}/session-attendance${query ? `?${query}` : ""}`);
  }
  createSessionAttendance(payload) {
    return this.http.post(`${this.baseUrl}/admin/session-attendance`, payload);
  }
  createMySessionAttendance(payload) {
    return this.http.post(`${this.baseUrl}/session-attendance`, payload);
  }
  deleteSessionAttendance(attendanceId) {
    return this.http.delete(`${this.baseUrl}/admin/session-attendance/${attendanceId}`);
  }
  deleteMySessionAttendance(attendanceId) {
    return this.http.delete(`${this.baseUrl}/session-attendance/${attendanceId}`);
  }
  getStudentAttendance(params) {
    const query = new URLSearchParams();
    if (params.classroomId)
      query.set("classroomId", params.classroomId);
    if (params.gradeId != null)
      query.set("gradeId", String(params.gradeId));
    if (params.fromDate)
      query.set("fromDate", params.fromDate);
    if (params.toDate)
      query.set("toDate", params.toDate);
    if (params.studentSearch?.trim())
      query.set("studentSearch", params.studentSearch.trim());
    if (params.sortKey)
      query.set("sortKey", params.sortKey);
    if (params.sortDir)
      query.set("sortDir", params.sortDir);
    if (params.page)
      query.set("page", String(params.page));
    if (params.pageSize)
      query.set("pageSize", String(params.pageSize));
    const qs = query.toString();
    return this.http.get(`${this.baseUrl}/student-attendance${qs ? `?${qs}` : ""}`);
  }
  createStudentAttendance(payload) {
    return this.http.post(`${this.baseUrl}/student-attendance`, payload);
  }
  deleteStudentAttendance(attendanceId) {
    return this.http.delete(`${this.baseUrl}/student-attendance/${attendanceId}`);
  }
  getAdminStudentAttendance(params) {
    const query = new URLSearchParams();
    if (params.classroomId)
      query.set("classroomId", params.classroomId);
    if (params.gradeId != null)
      query.set("gradeId", String(params.gradeId));
    if (params.fromDate)
      query.set("fromDate", params.fromDate);
    if (params.toDate)
      query.set("toDate", params.toDate);
    if (params.studentSearch?.trim())
      query.set("studentSearch", params.studentSearch.trim());
    if (params.sortKey)
      query.set("sortKey", params.sortKey);
    if (params.sortDir)
      query.set("sortDir", params.sortDir);
    if (params.page)
      query.set("page", String(params.page));
    if (params.pageSize)
      query.set("pageSize", String(params.pageSize));
    const qs = query.toString();
    return this.http.get(`${this.baseUrl}/admin/student-attendance${qs ? `?${qs}` : ""}`);
  }
  createAdminStudentAttendance(payload) {
    return this.http.post(`${this.baseUrl}/admin/student-attendance`, payload);
  }
  deleteAdminStudentAttendance(attendanceId) {
    return this.http.delete(`${this.baseUrl}/admin/student-attendance/${attendanceId}`);
  }
  getWeeklyReportGrid(filters) {
    const params = new URLSearchParams();
    params.set("weekStart", filters.weekStart);
    if (filters.grade != null)
      params.set("grade", String(filters.grade));
    return this.http.get(`${this.baseUrl}/weekly-reports/grid?${params}`);
  }
  listWeeklyReports(filters) {
    const params = new URLSearchParams();
    if (filters?.grade != null)
      params.set("grade", String(filters.grade));
    if (filters?.fromDate)
      params.set("fromDate", filters.fromDate);
    if (filters?.toDate)
      params.set("toDate", filters.toDate);
    const query = params.toString();
    return this.http.get(`${this.baseUrl}/weekly-reports${query ? `?${query}` : ""}`);
  }
  listAdminWeeklyReports(filters) {
    const params = new URLSearchParams();
    if (filters?.teacherId)
      params.set("teacherId", filters.teacherId);
    if (filters?.grade != null)
      params.set("grade", String(filters.grade));
    if (filters?.fromDate)
      params.set("fromDate", filters.fromDate);
    if (filters?.toDate)
      params.set("toDate", filters.toDate);
    const query = params.toString();
    return this.http.get(`${this.baseUrl}/admin/weekly-reports${query ? `?${query}` : ""}`);
  }
  saveWeeklyReports(payload) {
    return this.http.put(`${this.baseUrl}/weekly-reports`, payload);
  }
  listTopWeeklyStudents(weekStart) {
    const params = new URLSearchParams();
    if (weekStart)
      params.set("weekStart", weekStart);
    const query = params.toString();
    return this.http.get(`${this.baseUrl}/weekly-reports/top-students${query ? `?${query}` : ""}`);
  }
  listStudyPlans(filters) {
    const params = new URLSearchParams();
    if (filters?.courseId)
      params.set("courseId", filters.courseId);
    if (filters?.teacherId)
      params.set("teacherId", filters.teacherId);
    if (filters?.studentId)
      params.set("studentId", filters.studentId);
    if (filters?.fromDate)
      params.set("fromDate", filters.fromDate);
    if (filters?.toDate)
      params.set("toDate", filters.toDate);
    const query = params.toString();
    return this.http.get(`${this.baseUrl}/study-plans${query ? `?${query}` : ""}`);
  }
  getAdminStudyPlans(params) {
    const query = new URLSearchParams();
    if (params.teacherId)
      query.set("teacherId", params.teacherId);
    if (params.courseId)
      query.set("courseId", params.courseId);
    if (params.fromDate)
      query.set("fromDate", params.fromDate);
    if (params.toDate)
      query.set("toDate", params.toDate);
    if (params.sortKey)
      query.set("sortKey", params.sortKey);
    if (params.sortDir)
      query.set("sortDir", params.sortDir);
    if (params.page)
      query.set("page", String(params.page));
    if (params.pageSize)
      query.set("pageSize", String(params.pageSize));
    const qs = query.toString();
    return this.http.get(`${this.baseUrl}/admin/study-plans${qs ? `?${qs}` : ""}`);
  }
  saveStudyPlan(payload) {
    return this.http.put(`${this.baseUrl}/study-plans`, payload);
  }
  generateStudyPlan(payload) {
    return this.http.post(`${this.baseUrl}/study-plans/generate`, payload);
  }
  generateAssessment(payload) {
    return this.http.post(`${this.baseUrl}/assessments/generate`, payload);
  }
  deleteStudyPlan(planId) {
    return this.http.delete(`${this.baseUrl}/study-plans/${planId}`);
  }
  getPayrollReport(filters) {
    const params = new URLSearchParams();
    params.set("fromDate", filters.fromDate);
    params.set("toDate", filters.toDate);
    if (filters.teacherId)
      params.set("teacherId", filters.teacherId);
    if (filters.stage != null)
      params.set("stage", String(filters.stage));
    if (filters.grade != null)
      params.set("grade", String(filters.grade));
    return this.http.get(`${this.baseUrl}/admin/payroll-report?${params.toString()}`);
  }
  getPayrollAdjustments(filters) {
    const params = new URLSearchParams();
    if (filters?.fromDate)
      params.set("fromDate", filters.fromDate);
    if (filters?.toDate)
      params.set("toDate", filters.toDate);
    if (filters?.teacherId)
      params.set("teacherId", filters.teacherId);
    const query = params.toString();
    return this.http.get(`${this.baseUrl}/admin/payroll-adjustments${query ? `?${query}` : ""}`);
  }
  createPayrollAdjustment(payload) {
    return this.http.post(`${this.baseUrl}/admin/payroll-adjustments`, payload);
  }
  deletePayrollAdjustment(adjustmentId) {
    return this.http.delete(`${this.baseUrl}/admin/payroll-adjustments/${adjustmentId}`);
  }
  getAccountReport(filters) {
    const params = new URLSearchParams();
    params.set("fromDate", filters.fromDate);
    params.set("toDate", filters.toDate);
    return this.http.get(`${this.baseUrl}/admin/account-report?${params.toString()}`);
  }
  getTeacherAssessmentsReport(filters) {
    const params = new URLSearchParams();
    if (filters.teacherId)
      params.set("teacherId", filters.teacherId);
    if (filters.classroomId)
      params.set("classroomId", filters.classroomId);
    if (filters.fromDate)
      params.set("fromDate", filters.fromDate);
    if (filters.toDate)
      params.set("toDate", filters.toDate);
    if (filters.status)
      params.set("status", filters.status);
    if (filters.kind)
      params.set("kind", filters.kind);
    if (filters.page)
      params.set("page", String(filters.page));
    if (filters.pageSize)
      params.set("pageSize", String(filters.pageSize));
    const query = params.toString();
    return this.http.get(`${this.baseUrl}/admin/teacher-assessments${query ? `?${query}` : ""}`);
  }
  getAdminLoginDashboard(filters) {
    const params = new URLSearchParams();
    params.set("fromDate", filters.fromDate);
    params.set("toDate", filters.toDate);
    return this.http.get(`${this.baseUrl}/admin/dashboard/logins?${params.toString()}`);
  }
  getTuitionPayments(filters) {
    const params = new URLSearchParams();
    if (filters?.parentId)
      params.set("parentId", filters.parentId);
    if (filters?.studentId)
      params.set("studentId", filters.studentId);
    if (filters?.fromDate)
      params.set("fromDate", filters.fromDate);
    if (filters?.toDate)
      params.set("toDate", filters.toDate);
    if (filters?.year != null)
      params.set("year", String(filters.year));
    if (filters?.month != null)
      params.set("month", String(filters.month));
    const query = params.toString();
    return this.http.get(`${this.baseUrl}/admin/payments${query ? `?${query}` : ""}`);
  }
  createTuitionPayment(payload) {
    return this.http.post(`${this.baseUrl}/admin/payments`, payload);
  }
  deleteTuitionPayment(paymentId) {
    return this.http.delete(`${this.baseUrl}/admin/payments/${paymentId}`);
  }
  getOtherExpenses(filters) {
    const params = new URLSearchParams();
    if (filters?.fromDate)
      params.set("fromDate", filters.fromDate);
    if (filters?.toDate)
      params.set("toDate", filters.toDate);
    if (filters?.name)
      params.set("name", filters.name);
    const query = params.toString();
    return this.http.get(`${this.baseUrl}/admin/other-expenses${query ? `?${query}` : ""}`);
  }
  getAdminTenants() {
    return this.http.get(`${this.baseUrl}/admin/tenants`);
  }
  createOtherExpense(payload) {
    return this.http.post(`${this.baseUrl}/admin/other-expenses`, payload);
  }
  deleteOtherExpense(expenseId) {
    return this.http.delete(`${this.baseUrl}/admin/other-expenses/${expenseId}`);
  }
  getAvailability(userId) {
    return this.http.get(`${this.baseUrl}/availability/${userId}`);
  }
  /** Replaces the person's whole weekly availability. */
  saveAvailability(userId, periods) {
    return this.http.put(`${this.baseUrl}/availability/${userId}`, { periods });
  }
  getTeacherVacations(teacherId) {
    const query = teacherId ? `?teacherId=${teacherId}` : "";
    return this.http.get(`${this.baseUrl}/teacher-vacations${query}`);
  }
  createTeacherVacation(payload) {
    return this.http.post(`${this.baseUrl}/teacher-vacations`, payload);
  }
  deleteTeacherVacation(vacationId) {
    return this.http.delete(`${this.baseUrl}/teacher-vacations/${vacationId}`);
  }
  getUser(userId) {
    return this.http.get(`${this.baseUrl}/admin/users/${userId}`);
  }
  getUsers(role) {
    const query = role ? `?role=${role}` : "";
    return this.http.get(`${this.baseUrl}/admin/users${query}`);
  }
  createUser(payload) {
    return this.http.post(`${this.baseUrl}/admin/users`, payload);
  }
  updateUser(userId, payload) {
    return this.http.put(`${this.baseUrl}/admin/users/${userId}`, payload);
  }
  /** @param deleteData true also deletes everything that belongs to the user; false moves it to the admin. */
  deleteUser(userId, deleteData = false) {
    return this.http.delete(`${this.baseUrl}/admin/users/${userId}?deleteData=${deleteData}`);
  }
  setUserActive(userId, isActive) {
    return this.http.post(`${this.baseUrl}/admin/users/${userId}/active`, { isActive });
  }
  getZoomStatus() {
    return this.http.get(`${this.baseUrl}/zoom/status`);
  }
  getZoomConnectUrl() {
    return this.http.get(`${this.baseUrl}/zoom/connect`);
  }
  disconnectZoom() {
    return this.http.post(`${this.baseUrl}/zoom/disconnect`, {});
  }
  getZoomOAuthSettings() {
    return this.http.get(`${this.baseUrl}/zoom/oauth-settings`);
  }
  saveZoomOAuthSettings(payload) {
    return this.http.put(`${this.baseUrl}/zoom/oauth-settings`, payload);
  }
  getAdminCourses(params) {
    const query = new URLSearchParams();
    if (params.titleSearch?.trim())
      query.set("titleSearch", params.titleSearch.trim());
    if (params.stageId != null)
      query.set("stageId", String(params.stageId));
    if (params.grade != null)
      query.set("grade", String(params.grade));
    if (params.sortKey)
      query.set("sortKey", params.sortKey);
    if (params.sortDir)
      query.set("sortDir", params.sortDir);
    if (params.page)
      query.set("page", String(params.page));
    if (params.pageSize)
      query.set("pageSize", String(params.pageSize));
    const qs = query.toString();
    return this.http.get(`${this.baseUrl}/admin/courses${qs ? `?${qs}` : ""}`);
  }
  createCourse(payload) {
    return this.http.post(`${this.baseUrl}/admin/courses`, payload);
  }
  setCoursePublished(courseId, isPublished) {
    return this.http.post(`${this.baseUrl}/admin/courses/${courseId}/published`, { isPublished });
  }
  updateCourse(courseId, payload) {
    return this.http.put(`${this.baseUrl}/admin/courses/${courseId}`, payload);
  }
  createCourseUnit(courseId, payload) {
    return this.http.post(`${this.baseUrl}/admin/courses/${courseId}/units`, payload);
  }
  updateCourseUnit(unitId, payload) {
    return this.http.put(`${this.baseUrl}/admin/units/${unitId}`, payload);
  }
  deleteCourseUnit(unitId) {
    return this.http.delete(`${this.baseUrl}/admin/units/${unitId}`);
  }
  createCourseLesson(unitId, payload) {
    return this.http.post(`${this.baseUrl}/admin/units/${unitId}/lessons`, payload);
  }
  updateCourseLesson(lessonId, payload) {
    return this.http.put(`${this.baseUrl}/admin/lessons/${lessonId}`, payload);
  }
  deleteCourseLesson(lessonId) {
    return this.http.delete(`${this.baseUrl}/admin/lessons/${lessonId}`);
  }
  generateCourseTree(courseId, payload) {
    return this.http.post(`${this.baseUrl}/admin/courses/${courseId}/tree/generate`, payload);
  }
  getSiteSettings() {
    return this.http.get(`${this.baseUrl}/site-settings`).pipe(map((settings) => normalizeSiteSettings(settings)));
  }
  updateSiteSettings(payload) {
    return this.http.put(`${this.baseUrl}/admin/site-settings`, payload).pipe(map((settings) => normalizeSiteSettings(settings)));
  }
  uploadSiteImage(kind, file) {
    const form = new FormData();
    form.append("file", file);
    form.append("kind", kind);
    return this.http.post(`${this.baseUrl}/admin/site-settings/upload`, form).pipe(map((settings) => normalizeSiteSettings(settings)));
  }
  siteAssetUrl(path) {
    if (!path)
      return null;
    if (path.startsWith("http://") || path.startsWith("https://"))
      return path;
    const root = this.baseUrl.replace(/\/api\/?$/, "");
    return `${root}${path.startsWith("/") ? path : `/${path}`}`;
  }
  deleteCourse(courseId) {
    return this.http.delete(`${this.baseUrl}/admin/courses/${courseId}`);
  }
  listGradeCertificates() {
    return this.http.get(`${this.baseUrl}/grade-certificates`);
  }
  getGradeCertificate(certificateId) {
    return this.http.get(`${this.baseUrl}/grade-certificates/${certificateId}`);
  }
  createGradeCertificate(payload) {
    return this.http.post(`${this.baseUrl}/admin/grade-certificates`, payload);
  }
  updateGradeCertificate(certificateId, payload) {
    return this.http.put(`${this.baseUrl}/admin/grade-certificates/${certificateId}`, payload);
  }
  deleteGradeCertificate(certificateId) {
    return this.http.delete(`${this.baseUrl}/admin/grade-certificates/${certificateId}`);
  }
  /** A null degree clears that student's mark for the subject. */
  saveGradeCertificateMarks(certificateId, entries) {
    return this.http.put(`${this.baseUrl}/grade-certificates/${certificateId}/marks`, { entries });
  }
  approveGradeCertificate(certificateId) {
    return this.http.post(`${this.baseUrl}/admin/grade-certificates/${certificateId}/approve`, {});
  }
  approveAllGradeCertificates() {
    return this.http.post(`${this.baseUrl}/admin/grade-certificates/approve-all`, {});
  }
  revokeGradeCertificateApproval(certificateId) {
    return this.http.post(`${this.baseUrl}/admin/grade-certificates/${certificateId}/revoke-approval`, {});
  }
  /** Approved certificates with the student's own degrees. */
  getStudentGradeCertificates(studentId) {
    return this.http.get(`${this.baseUrl}/students/${studentId}/grade-certificates`);
  }
  /** Approved certificates of a child of the signed-in parent, with the child's degrees. */
  getChildGradeCertificates(childId) {
    return this.http.get(`${this.baseUrl}/parent/children/${childId}/grade-certificates`);
  }
  getClassrooms() {
    return this.http.get(`${this.baseUrl}/classrooms`);
  }
  getClassroomEnrollments(params) {
    const query = new URLSearchParams();
    if (params.classroomId)
      query.set("classroomId", params.classroomId);
    if (params.courseId)
      query.set("courseId", params.courseId);
    if (params.studentSearch?.trim())
      query.set("studentSearch", params.studentSearch.trim());
    if (params.sortKey)
      query.set("sortKey", params.sortKey);
    if (params.sortDir)
      query.set("sortDir", params.sortDir);
    if (params.page)
      query.set("page", String(params.page));
    if (params.pageSize)
      query.set("pageSize", String(params.pageSize));
    const qs = query.toString();
    return this.http.get(`${this.baseUrl}/classrooms/enrollments${qs ? `?${qs}` : ""}`);
  }
  createClassroom(payload) {
    return this.http.post(`${this.baseUrl}/classrooms`, payload);
  }
  updateClassroom(classroomId, payload) {
    return this.http.put(`${this.baseUrl}/classrooms/${classroomId}`, payload);
  }
  deleteClassroom(classroomId) {
    return this.http.delete(`${this.baseUrl}/classrooms/${classroomId}`);
  }
  assignClassroom(classroomId, payload) {
    return this.http.put(`${this.baseUrl}/classrooms/${classroomId}/assignments`, payload);
  }
  addStudentToClassroom(classroomId, studentId, courseIds) {
    return this.http.post(`${this.baseUrl}/classrooms/${classroomId}/students`, {
      studentId,
      courseIds: courseIds?.length ? courseIds : null
    });
  }
  updateStudentClassroomEnrollment(classroomId, studentId, courseIds) {
    return this.http.put(`${this.baseUrl}/classrooms/${classroomId}/students/${studentId}`, {
      courseIds: courseIds?.length ? courseIds : null
    });
  }
  removeStudentFromClassroom(classroomId, studentId) {
    return this.http.delete(`${this.baseUrl}/classrooms/${classroomId}/students/${studentId}`);
  }
  updateClassroomWhatsApp(classroomId, payload) {
    return this.http.put(`${this.baseUrl}/classrooms/${classroomId}/whatsapp`, payload);
  }
  updateClassroomZoom(classroomId, payload) {
    return this.http.put(`${this.baseUrl}/classrooms/${classroomId}/zoom`, payload);
  }
  sendClassroomWhatsApp(classroomId, payload) {
    return this.http.post(`${this.baseUrl}/classrooms/${classroomId}/whatsapp/send`, payload);
  }
  sendAdminWhatsApp(payload) {
    return this.http.post(`${this.baseUrl}/admin/whatsapp/send`, payload);
  }
  getAssignments(classroomId) {
    const query = classroomId ? `?classroomId=${classroomId}` : "";
    return this.http.get(`${this.baseUrl}/assignments${query}`);
  }
  getAssignment(assignmentId) {
    return this.http.get(`${this.baseUrl}/assignments/${assignmentId}`);
  }
  createAssignment(payload) {
    return this.http.post(`${this.baseUrl}/assignments`, payload);
  }
  updateAssignment(assignmentId, payload) {
    return this.http.put(`${this.baseUrl}/assignments/${assignmentId}`, payload);
  }
  deleteAssignment(assignmentId) {
    return this.http.delete(`${this.baseUrl}/assignments/${assignmentId}`);
  }
  publishAssignment(assignmentId) {
    return this.http.post(`${this.baseUrl}/assignments/${assignmentId}/publish`, {});
  }
  submitAssignment(payload) {
    return this.http.post(`${this.baseUrl}/assignments/submit`, payload);
  }
  getAssignmentSubmissions(assignmentId) {
    return this.http.get(`${this.baseUrl}/assignments/${assignmentId}/submissions`);
  }
  gradeSubmission(payload) {
    return this.http.post(`${this.baseUrl}/assignments/submissions/grade`, payload);
  }
  getBankQuestions(courseId) {
    const query = courseId ? `?courseId=${courseId}` : "";
    return this.http.get(`${this.baseUrl}/question-bank${query}`);
  }
  createBankQuestion(payload) {
    return this.http.post(`${this.baseUrl}/question-bank`, payload);
  }
  deleteBankQuestion(questionId) {
    return this.http.delete(`${this.baseUrl}/question-bank/${questionId}`);
  }
  getExams(classroomId) {
    const query = classroomId ? `?classroomId=${classroomId}` : "";
    return this.http.get(`${this.baseUrl}/exams${query}`);
  }
  getExam(examId) {
    return this.http.get(`${this.baseUrl}/exams/${examId}`);
  }
  createExam(payload) {
    return this.http.post(`${this.baseUrl}/exams`, payload);
  }
  publishExam(examId) {
    return this.http.post(`${this.baseUrl}/exams/${examId}/publish`, {});
  }
  getAssessmentStudentLinks(kind, resourceId) {
    return this.http.get(`${this.baseUrl}/assessment-links`, {
      params: { kind, resourceId }
    });
  }
  submitExam(payload) {
    return this.http.post(`${this.baseUrl}/exams/submit`, payload);
  }
  startExam(examId) {
    return this.http.post(`${this.baseUrl}/exams/${examId}/start`, {});
  }
  getExamAttempts(examId) {
    return this.http.get(`${this.baseUrl}/exams/${examId}/attempts`);
  }
  gradeExamAttempt(payload) {
    return this.http.post(`${this.baseUrl}/exams/attempts/grade`, payload);
  }
  /** @param courseId the course the video is for; its files are stored in that subject's folder. */
  uploadMedia(file, durationSeconds, courseId) {
    const form = new FormData();
    form.append("file", file, file.name);
    if (courseId)
      form.append("courseId", courseId);
    if (durationSeconds && durationSeconds > 0) {
      form.append("durationSeconds", String(Math.round(durationSeconds)));
    }
    return this.http.post(`${this.baseUrl}/media/upload`, form);
  }
  uploadMediaWithProgress(file, durationSeconds, courseId) {
    const form = new FormData();
    form.append("file", file, file.name);
    if (courseId)
      form.append("courseId", courseId);
    if (durationSeconds && durationSeconds > 0) {
      form.append("durationSeconds", String(Math.round(durationSeconds)));
    }
    return this.http.post(`${this.baseUrl}/media/upload`, form, {
      reportProgress: true,
      observe: "events"
    }).pipe(map((event) => {
      if (event.type === HttpEventType.UploadProgress) {
        const total = event.total ?? 0;
        const progress = total > 0 ? Math.round(100 * event.loaded / total) : 0;
        return { progress };
      }
      if (event.type === HttpEventType.Response) {
        return { progress: 100, asset: event.body ?? void 0 };
      }
      return null;
    }), filter((update) => update !== null));
  }
  uploadQuestionImage(file) {
    const form = new FormData();
    form.append("file", file, file.name);
    return this.http.post(`${this.baseUrl}/question-images/upload`, form);
  }
  registerMediaFromUrl(payload) {
    return this.http.post(`${this.baseUrl}/media/from-url`, payload);
  }
  getVideoLibrary() {
    return this.http.get(`${this.baseUrl}/media/library`);
  }
  attachLessonVideo(lessonId, payload) {
    return this.http.post(`${this.baseUrl}/lessons/${lessonId}/videos`, payload);
  }
  deleteLessonVideo(lessonVideoId) {
    return this.http.delete(`${this.baseUrl}/lessons/videos/${lessonVideoId}`);
  }
  getCourseVideoLibrary() {
    return this.http.get(`${this.baseUrl}/media/course-videos`);
  }
  attachCourseVideo(courseId, payload) {
    return this.http.post(`${this.baseUrl}/courses/${courseId}/videos`, payload);
  }
  deleteCourseVideo(courseVideoId) {
    return this.http.delete(`${this.baseUrl}/courses/videos/${courseVideoId}`);
  }
  attachAssignmentSolutionVideo(assignmentId, mediaAssetId) {
    return this.http.post(`${this.baseUrl}/assignments/${assignmentId}/solution-video`, {
      mediaAssetId
    });
  }
  deleteAssignmentSolutionVideo(assignmentId) {
    return this.http.delete(`${this.baseUrl}/assignments/${assignmentId}/solution-video`);
  }
  uploadLearningMaterialFile(file, courseId) {
    const form = new FormData();
    form.append("file", file, file.name);
    if (courseId)
      form.append("courseId", courseId);
    return this.http.post(`${this.baseUrl}/media/learning-materials/upload`, form);
  }
  attachLearningMaterial(payload) {
    return this.http.post(`${this.baseUrl}/learning-materials`, payload);
  }
  getLearningMaterials() {
    return this.http.get(`${this.baseUrl}/learning-materials`);
  }
  getLearningMaterialPage(filters) {
    const query = new URLSearchParams();
    if (filters.courseId)
      query.set("courseId", filters.courseId);
    if (filters.unitId)
      query.set("unitId", filters.unitId);
    if (filters.lessonId)
      query.set("lessonId", filters.lessonId);
    if (filters.all)
      query.set("all", "true");
    const qs = query.toString();
    return this.http.get(`${this.baseUrl}/learning-materials/page${qs ? `?${qs}` : ""}`);
  }
  deleteLearningMaterial(materialId) {
    return this.http.delete(`${this.baseUrl}/learning-materials/${materialId}`);
  }
  learningMaterialFileUrl(mediaAssetId) {
    return `${this.baseUrl}/learning-materials/${mediaAssetId}/file`;
  }
  getPlayback(mediaAssetId) {
    const params = { apiBase: this.baseUrl };
    return this.http.get(`${this.baseUrl}/media/${mediaAssetId}/playback`, { params }).pipe(map((info) => __spreadProps(__spreadValues({}, info), {
      playbackUrl: this.normalizeMediaPlaybackUrl(info)
    })));
  }
  normalizeMediaPlaybackUrl(info) {
    const url = info.playbackUrl;
    if (!url)
      return url;
    try {
      const parsed = new URL(url);
      if (parsed.pathname.endsWith("/media/stream")) {
        return `${this.baseUrl}/media/stream${parsed.search}`;
      }
    } catch (e) {
    }
    return url;
  }
  recordWatchEvents(payload) {
    return this.http.post(`${this.baseUrl}/media/watch-events`, payload);
  }
  getWatchSessions(mediaAssetId) {
    return this.http.get(`${this.baseUrl}/media/${mediaAssetId}/watch-sessions`);
  }
  /** The current user's menu: pages from their roles, with their own order and hidden pages. */
  getMyMenu() {
    return this.http.get(`${this.baseUrl}/menu`);
  }
  /** Items are saved in the given order; `isHidden` keeps a page out of the sidebar. */
  saveMyMenu(items) {
    return this.http.put(`${this.baseUrl}/menu`, { items });
  }
  resetMyMenu() {
    return this.http.post(`${this.baseUrl}/menu/reset`, {});
  }
  getPages() {
    return this.http.get(`${this.baseUrl}/admin/pages`);
  }
  getRoles(baseRole) {
    const query = baseRole ? `?baseRole=${encodeURIComponent(baseRole)}` : "";
    return this.http.get(`${this.baseUrl}/admin/roles${query}`);
  }
  getRoleMembers(roleId) {
    return this.http.get(`${this.baseUrl}/admin/roles/${roleId}/members`);
  }
  createRole(payload) {
    return this.http.post(`${this.baseUrl}/admin/roles`, payload);
  }
  updateRole(roleId, payload) {
    return this.http.put(`${this.baseUrl}/admin/roles/${roleId}`, payload);
  }
  deleteRole(roleId) {
    return this.http.delete(`${this.baseUrl}/admin/roles/${roleId}`);
  }
  assignRoleUser(roleId, userId) {
    return this.http.post(`${this.baseUrl}/admin/roles/${roleId}/users`, { userId });
  }
  removeRoleUser(roleId, userId) {
    return this.http.delete(`${this.baseUrl}/admin/roles/${roleId}/users/${userId}`);
  }
  static {
    this.\u0275fac = function LearningApiService_Factory(__ngFactoryType__) {
      return new (__ngFactoryType__ || _LearningApiService)();
    };
  }
  static {
    this.\u0275prov = /* @__PURE__ */ \u0275\u0275defineInjectable({ token: _LearningApiService, factory: _LearningApiService.\u0275fac, providedIn: "root" });
  }
};
(() => {
  (typeof ngDevMode === "undefined" || ngDevMode) && setClassMetadata(LearningApiService, [{
    type: Injectable,
    args: [{ providedIn: "root" }]
  }], null, null);
})();
function normalizeSiteSettings(raw) {
  const item = raw;
  const timetableWeek = item.timetableWeekStartUtc ?? item["TimetableWeekStartUtc"] ?? item["timetableWeekStartUtc"];
  return {
    siteName: String(item.siteName ?? item["SiteName"] ?? "CodeKids"),
    logoUrl: item.logoUrl ?? item["LogoUrl"] ?? null,
    bannerUrl: item.bannerUrl ?? item["BannerUrl"] ?? null,
    timetableWeekStartUtc: timetableWeek == null ? null : String(timetableWeek),
    amSessionCount: Number(item.amSessionCount ?? item["AmSessionCount"] ?? 6) || 6,
    pmSessionCount: Number(item.pmSessionCount ?? item["PmSessionCount"] ?? 6) || 6,
    pmStartMinutes: normalizePmStartMinutes(item.pmStartMinutes ?? item["PmStartMinutes"]),
    updatedAtUtc: String(item.updatedAtUtc ?? item["UpdatedAtUtc"] ?? ""),
    tenantStyle: item.tenantStyle ?? item["TenantStyle"] ?? null,
    multiTimeZone: (item.multiTimeZone ?? item["MultiTimeZone"]) === true,
    timeZone: item.timeZone ?? item["TimeZone"] ?? null
  };
}

export {
  unreadChanged,
  DEFAULT_SESSION_COUNT,
  MIN_SESSION_COUNT,
  MAX_SESSION_COUNT,
  DEFAULT_PM_START_MINUTES,
  MIN_PM_START_MINUTES,
  MAX_PM_START_MINUTES,
  sessionNumbers,
  normalizeSessionCount,
  visibleSessionSlots,
  normalizePmStartMinutes,
  minutesToTimeInput,
  timeInputToMinutes,
  WEEKDAY_INDEXES,
  arabicWeekdayName,
  LearningApiService
};
//# sourceMappingURL=chunk-7YFOTRM2.js.map

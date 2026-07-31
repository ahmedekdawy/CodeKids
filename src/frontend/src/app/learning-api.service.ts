import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import {
  Assignment,
  AssignmentSubmission,
  Avatar,
  Badge,
  Classroom,
  CompleteStepResponse,
  Course,
  CreateMeetingPayload,
  Lesson,
  LiveSession,
  ManagedUser,
  ParentDashboard,
  Quiz,
  StudentSummary,
  SubmitQuizResponse,
  TeacherDashboard,
  ZoomConnectionStatus
} from './models';

@Injectable({ providedIn: 'root' })
export class LearningApiService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = 'http://localhost:5078/api';

  getCourses(): Observable<Course[]> {
    return this.http.get<Course[]>(`${this.baseUrl}/courses`);
  }

  getLessons(courseId?: string): Observable<Lesson[]> {
    const query = courseId ? `?courseId=${courseId}` : '';
    return this.http.get<Lesson[]>(`${this.baseUrl}/lessons${query}`);
  }

  getLesson(lessonId: string): Observable<Lesson> {
    return this.http.get<Lesson>(`${this.baseUrl}/lessons/${lessonId}`);
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
    questions: {
      prompt: string;
      optionA: string;
      optionB: string;
      optionC: string;
      correctOption: string;
      sortOrder: number;
    }[];
  }): Observable<Quiz> {
    return this.http.post<Quiz>(`${this.baseUrl}/quizzes`, payload);
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

  getTeacherDashboard(): Observable<TeacherDashboard> {
    return this.http.get<TeacherDashboard>(`${this.baseUrl}/dashboard/teacher`);
  }

  getMeetings(): Observable<LiveSession[]> {
    return this.http.get<LiveSession[]>(`${this.baseUrl}/meetings`);
  }

  createMeeting(payload: CreateMeetingPayload): Observable<LiveSession> {
    return this.http.post<LiveSession>(`${this.baseUrl}/meetings`, payload);
  }

  getUsers(role?: string): Observable<ManagedUser[]> {
    const query = role ? `?role=${role}` : '';
    return this.http.get<ManagedUser[]>(`${this.baseUrl}/admin/users${query}`);
  }

  createUser(payload: {
    email: string;
    displayName: string;
    password: string;
    role: string;
    parentId?: string | null;
    mobilePhone?: string | null;
  }): Observable<ManagedUser> {
    return this.http.post<ManagedUser>(`${this.baseUrl}/admin/users`, payload);
  }

  updateUser(
    userId: string,
    payload: {
      email: string;
      displayName: string;
      role: string;
      parentId?: string | null;
      password?: string | null;
      mobilePhone?: string | null;
    }
  ): Observable<ManagedUser> {
    return this.http.put<ManagedUser>(`${this.baseUrl}/admin/users/${userId}`, payload);
  }

  deleteUser(userId: string): Observable<void> {
    return this.http.delete<void>(`${this.baseUrl}/admin/users/${userId}`);
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

  createCourse(payload: {
    title: string;
    theme: string;
    description: string;
    ageMin: number;
    ageMax: number;
    sortOrder: number;
  }): Observable<Course> {
    return this.http.post<Course>(`${this.baseUrl}/admin/courses`, payload);
  }

  updateCourse(
    courseId: string,
    payload: {
      title: string;
      theme: string;
      description: string;
      ageMin: number;
      ageMax: number;
      sortOrder: number;
    }
  ): Observable<Course> {
    return this.http.put<Course>(`${this.baseUrl}/admin/courses/${courseId}`, payload);
  }

  deleteCourse(courseId: string): Observable<void> {
    return this.http.delete<void>(`${this.baseUrl}/admin/courses/${courseId}`);
  }

  getClassrooms(): Observable<Classroom[]> {
    return this.http.get<Classroom[]>(`${this.baseUrl}/classrooms`);
  }

  createClassroom(payload: {
    name: string;
    description?: string;
    teacherId?: string | null;
    courseId?: string | null;
    whatsAppGroupInviteUrl?: string;
    whatsAppNotifyPhones?: string;
  }): Observable<Classroom> {
    return this.http.post<Classroom>(`${this.baseUrl}/classrooms`, payload);
  }

  updateClassroom(
    classroomId: string,
    payload: {
      name: string;
      description?: string;
      teacherId?: string | null;
      courseId?: string | null;
      whatsAppGroupInviteUrl?: string;
      whatsAppNotifyPhones?: string;
    }
  ): Observable<Classroom> {
    return this.http.put<Classroom>(`${this.baseUrl}/classrooms/${classroomId}`, payload);
  }

  deleteClassroom(classroomId: string): Observable<void> {
    return this.http.delete<void>(`${this.baseUrl}/classrooms/${classroomId}`);
  }

  assignClassroom(classroomId: string, payload: { teacherId?: string | null; courseId?: string | null }): Observable<Classroom> {
    return this.http.put<Classroom>(`${this.baseUrl}/classrooms/${classroomId}/assignments`, payload);
  }

  addStudentToClassroom(classroomId: string, studentId: string): Observable<Classroom> {
    return this.http.post<Classroom>(`${this.baseUrl}/classrooms/${classroomId}/students`, { studentId });
  }

  removeStudentFromClassroom(classroomId: string, studentId: string): Observable<Classroom> {
    return this.http.delete<Classroom>(`${this.baseUrl}/classrooms/${classroomId}/students/${studentId}`);
  }

  updateClassroomWhatsApp(
    classroomId: string,
    payload: { whatsAppGroupInviteUrl?: string; whatsAppNotifyPhones?: string }
  ): Observable<Classroom> {
    return this.http.put<Classroom>(`${this.baseUrl}/classrooms/${classroomId}/whatsapp`, payload);
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
    questions: {
      prompt: string;
      questionType: string;
      optionA?: string | null;
      optionB?: string | null;
      optionC?: string | null;
      correctAnswer: string;
      points: number;
      sortOrder: number;
    }[];
  }): Observable<Assignment> {
    return this.http.post<Assignment>(`${this.baseUrl}/assignments`, payload);
  }

  submitAssignment(payload: {
    assignmentId: string;
    answers: { questionId: string; answerText: string }[];
  }): Observable<AssignmentSubmission> {
    return this.http.post<AssignmentSubmission>(`${this.baseUrl}/assignments/submit`, payload);
  }

  getAssignmentSubmissions(assignmentId: string): Observable<AssignmentSubmission[]> {
    return this.http.get<AssignmentSubmission[]>(`${this.baseUrl}/assignments/${assignmentId}/submissions`);
  }

  gradeSubmission(payload: {
    submissionId: string;
    teacherFeedback?: string;
    answers?: { questionId: string; isCorrect: boolean; pointsAwarded: number }[];
  }): Observable<AssignmentSubmission> {
    return this.http.post<AssignmentSubmission>(`${this.baseUrl}/assignments/submissions/grade`, payload);
  }
}

import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { resolveApiBaseUrl } from './api-base-url';

export type SmartStudyAction = 'Outline' | 'Summary' | 'Assignment' | 'Quiz';
export type SmartStudyTarget = 'tree' | 'bank' | 'quiz' | 'assignment' | 'exam';

export interface SmartStudyQuestion {
  prompt: string;
  questionType: string;
  options: string[];
  correctOption: string;
  correctAnswer: string;
  points: number;
  sortOrder: number;
}

export interface SmartStudyUnit {
  title: string;
  sortOrder: number;
  lessons: string[];
}

export interface SmartStudyResult {
  action: string;
  title: string;
  markdown: string;
  questions: SmartStudyQuestion[];
  units: SmartStudyUnit[];
}

export interface ApplySmartStudyResult {
  target: string;
  resourceId?: string | null;
  resourceUrl?: string | null;
  questionsCreated: number;
  unitsCreated: number;
  lessonsCreated: number;
  questionIds: string[];
  message: string;
}

@Injectable({ providedIn: 'root' })
export class SmartStudyAssistantService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = resolveApiBaseUrl();

  generate(payload: {
    action: SmartStudyAction;
    courseId: string;
    unitId?: string | null;
    lessonId?: string | null;
    language?: string;
    files?: File[];
  }): Observable<SmartStudyResult> {
    const form = new FormData();
    form.set('action', payload.action);
    form.set('courseId', payload.courseId);
    if (payload.unitId) form.set('unitId', payload.unitId);
    if (payload.lessonId) form.set('lessonId', payload.lessonId);
    form.set('language', payload.language ?? 'ar');
    for (const file of payload.files ?? []) {
      form.append('files', file, file.name);
    }
    return this.http.post<SmartStudyResult>(
      `${this.baseUrl}/smart-study-assistant/generate`, form);
  }

  apply(payload: {
    courseId: string;
    target: SmartStudyTarget;
    title: string;
    description?: string | null;
    unitId?: string | null;
    lessonId?: string | null;
    mode?: string | null;
    questions?: SmartStudyQuestion[] | null;
    units?: SmartStudyUnit[] | null;
  }): Observable<ApplySmartStudyResult> {
    return this.http.post<ApplySmartStudyResult>(
      `${this.baseUrl}/smart-study-assistant/apply`, payload);
  }
}

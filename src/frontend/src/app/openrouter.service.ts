import { HttpClient, HttpHeaders } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Observable, throwError } from 'rxjs';
import { map } from 'rxjs/operators';
import { environment } from '../environments/environment';

export type OpenRouterRole = 'user' | 'assistant' | 'system';

export interface OpenRouterMessage {
  role: OpenRouterRole;
  content: string;
}

export interface OpenRouterChatCompletionRequest {
  model?: string;
  messages: OpenRouterMessage[];
  temperature?: number;
  stream?: boolean;
}

export interface OpenRouterChoice {
  message?: {
    role?: string;
    content?: string;
  };
}

export interface OpenRouterChatCompletionResponse {
  choices?: OpenRouterChoice[];
  error?: {
    message?: string;
  };
}

@Injectable({ providedIn: 'root' })
export class OpenRouterService {
  private readonly baseUrl = environment.openRouterBaseUrl;
  private readonly defaultModel = environment.openRouterModel;

  constructor(private readonly http: HttpClient) {}

  generateText(prompt: string, systemPrompt?: string): Observable<string> {
    const messages: OpenRouterMessage[] = [];

    if (systemPrompt) {
      messages.push({ role: 'system', content: systemPrompt });
    }

    messages.push({ role: 'user', content: prompt });

    return this.chat(messages).pipe(map((response) => response.choices?.[0]?.message?.content ?? ''));
  }

  chat(messages: OpenRouterMessage[], options: Partial<OpenRouterChatCompletionRequest> = {}): Observable<OpenRouterChatCompletionResponse> {
    const apiKey = environment.openRouterApiKey?.trim();
    if (!apiKey) {
      return throwError(() => new Error('OpenRouter API key is not configured. Set environment.openRouterApiKey first.'));
    }

    const payload: OpenRouterChatCompletionRequest = {
      model: options.model ?? this.defaultModel,
      messages,
      temperature: options.temperature ?? 0.7,
      stream: options.stream ?? false,
      ...options
    };

    const headers = new HttpHeaders({
      Authorization: `Bearer ${apiKey}`,
      'Content-Type': 'application/json',
      'HTTP-Referer': environment.appBaseUrl,
      'X-Title': 'CodeKids'
    });

    return this.http.post<OpenRouterChatCompletionResponse>(`${this.baseUrl}/chat/completions`, payload, { headers });
  }
}

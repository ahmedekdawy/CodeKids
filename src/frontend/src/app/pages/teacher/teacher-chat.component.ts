import { Component } from '@angular/core';
import { TranslatePipe } from '../../shared/translate.pipe';
import { ChatBoardComponent } from '../../shared/chat-board/chat-board.component';

@Component({
  selector: 'app-teacher-chat',
  imports: [TranslatePipe, ChatBoardComponent],
  template: `
    <div class="panel-page">
      <h2>{{ 'chat.teacherTitle' | t }}</h2>
      <p class="meta">{{ 'chat.teacherSubtitle' | t }}</p>
      <app-chat-board [canCreate]="true" [canModerate]="true" />
    </div>
  `,
  styleUrl: './teacher-panel.css'
})
export class TeacherChatComponent {}

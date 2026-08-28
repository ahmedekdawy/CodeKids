import { DatePipe } from '@angular/common';
import { Component, OnInit, computed, inject, input, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import { AuthService } from '../../auth.service';
import { ChatRealtimeService } from '../../chat-realtime.service';
import { LearningApiService } from '../../learning-api.service';
import { Classroom, Course, ChatMember, ChatMessage, ChatRoom } from '../../models';
import { LocaleService } from '../../i18n/locale.service';
import { formatCourseLabel } from '../../grade.util';
import { TranslatePipe } from '../translate.pipe';
import { SearchableSelectComponent } from '../searchable-select/searchable-select.component';
import { SearchableMultiSelectComponent } from '../searchable-multi-select/searchable-multi-select.component';
import { PageFeedbackComponent } from '../page-feedback/page-feedback.component';
import { IconActionButtonComponent } from '../icon-action-button/icon-action-button.component';

@Component({
  selector: 'app-chat-board',
  imports: [
    FormsModule,
    DatePipe,
    TranslatePipe,
    SearchableSelectComponent,
    SearchableMultiSelectComponent,
    PageFeedbackComponent,
    IconActionButtonComponent
  ],
  templateUrl: './chat-board.component.html',
  styleUrl: './chat-board.component.css'
})
export class ChatBoardComponent implements OnInit {
  readonly canCreate = input(false);
  readonly canModerate = input(false);

  private readonly api = inject(LearningApiService);
  private readonly realtime = inject(ChatRealtimeService);
  private readonly auth = inject(AuthService);
  private readonly locale = inject(LocaleService);
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);

  readonly classrooms = signal<Classroom[]>([]);
  readonly courses = signal<Course[]>([]);
  readonly rooms = signal<ChatRoom[]>([]);
  readonly messages = signal<ChatMessage[]>([]);
  readonly selectedRoomId = signal('');
  readonly error = signal('');
  readonly info = signal('');
  readonly sending = signal(false);

  classroomId = '';
  courseId = '';
  unitId = '';
  lessonId = '';
  kind: 'Direct' | 'Group' | 'Class' = 'Class';
  studentIds: string[] = [];
  draft = '';

  readonly selectedRoom = computed(() => this.rooms().find((r) => r.id === this.selectedRoomId()) ?? null);
  readonly meBlocked = computed(() => this.selectedRoom()?.isBlocked === true);

  readonly classroomOptions = computed(() => this.classrooms().map((c) => ({ value: c.id, label: c.name })));

  readonly courseOptions = computed(() => {
    this.locale.lang();
    const room = this.classrooms().find((c) => c.id === this.classroomId);
    const ids = new Set((room?.courses ?? []).map((c) => c.courseId).filter(Boolean));
    const list = ids.size ? this.courses().filter((c) => ids.has(c.id)) : this.courses();
    return list.map((c) => ({
      value: c.id,
      label: formatCourseLabel((k, p) => this.locale.t(k, p), c.title, c.grade, 'common.allGrades', c.stageId)
    }));
  });

  readonly unitOptions = computed(() => {
    const course = this.courses().find((c) => c.id === this.courseId);
    return (course?.units ?? []).map((u) => ({ value: u.id, label: u.title }));
  });

  readonly lessonOptions = computed(() => {
    const course = this.courses().find((c) => c.id === this.courseId);
    const units = course?.units ?? [];
    const lessons = this.unitId
      ? (units.find((u) => u.id === this.unitId)?.lessons ?? [])
      : (course?.lessons ?? units.flatMap((u) => u.lessons ?? []));
    return lessons.map((l) => ({ value: l.id, label: l.title }));
  });

  readonly studentOptions = computed(() => {
    const room = this.classrooms().find((c) => c.id === this.classroomId);
    const students = (room?.students ?? []).filter((s) => {
      if (!this.courseId) return true;
      const enrolled = s.enrolledCourseIds;
      return !enrolled?.length || enrolled.includes(this.courseId);
    });
    return students.map((s) => ({ value: s.studentId, label: s.displayName }));
  });

  constructor() {
    this.realtime.messages$.pipe(takeUntilDestroyed()).subscribe((message) => this.upsertMessage(message));
    this.realtime.deleted$.pipe(takeUntilDestroyed()).subscribe((message) => this.upsertMessage(message));
    this.realtime.members$.pipe(takeUntilDestroyed()).subscribe((member) => this.applyMember(member));
    this.route.queryParamMap.pipe(takeUntilDestroyed()).subscribe((params) => {
      const roomId = params.get('room') ?? undefined;
      if (this.rooms().length) {
        if (roomId && roomId !== this.selectedRoomId()) this.selectRoom(roomId);
        return;
      }
      this.reloadRooms(roomId);
    });
  }

  ngOnInit(): void {
    if (!this.canCreate()) return;
    this.api.getClassrooms().subscribe((rooms) => this.classrooms.set(rooms));
    this.api.getCourses(true).subscribe((courses) => this.courses.set(courses));
  }

  kindLabel(kind: ChatRoom['kind']): string {
    const key =
      kind === 'Direct' || kind === 0
        ? 'chat.kindDirect'
        : kind === 'Group' || kind === 1
          ? 'chat.kindGroup'
          : 'chat.kindClass';
    return this.locale.t(key);
  }

  onClassroomChange(value: string): void {
    this.classroomId = value;
    this.courseId = '';
    this.unitId = '';
    this.lessonId = '';
    this.studentIds = [];
  }

  onCourseChange(value: string): void {
    this.courseId = value;
    this.unitId = '';
    this.lessonId = '';
    this.studentIds = [];
  }

  onUnitChange(value: string): void {
    this.unitId = value;
    this.lessonId = '';
  }

  selectRoom(roomId: string): void {
    this.selectedRoomId.set(roomId);
    this.error.set('');
    this.syncRoomQuery(roomId);
    this.api.listChatMessages(roomId).subscribe({
      next: (rows) => {
        this.messages.set(rows);
        void this.realtime.joinRoom(roomId);
        this.markRoomRead(roomId);
      },
      error: (err) => this.error.set(this.locale.fromApiError(err, 'chat.loadFailed'))
    });
  }

  createRoom(): void {
    if (!this.classroomId || !this.courseId) {
      this.error.set(this.locale.t('chat.scopeRequired'));
      return;
    }
    this.error.set('');
    this.api
      .createChatRoom({
        classroomId: this.classroomId,
        courseId: this.courseId,
        unitId: this.unitId || null,
        lessonId: this.lessonId || null,
        kind: this.kind,
        studentIds: this.kind === 'Class' ? [] : this.studentIds
      })
      .subscribe({
        next: (room) => {
          this.info.set(this.locale.t('chat.created'));
          this.reloadRooms(room.id);
        },
        error: (err) => this.error.set(this.locale.fromApiError(err, 'chat.createFailed'))
      });
  }

  send(): void {
    const room = this.selectedRoom();
    const body = this.draft.trim();
    if (!room || !body || this.meBlocked()) return;
    this.sending.set(true);
    this.error.set('');
    void this.realtime
      .sendMessage(room.id, body)
      .then(() => {
        this.draft = '';
      })
      .catch(() => {
        this.api.sendChatMessage(room.id, body).subscribe({
          next: () => {
            this.draft = '';
          },
          error: (err) => this.error.set(this.locale.fromApiError(err, 'chat.sendFailed'))
        });
      })
      .finally(() => this.sending.set(false));
  }

  deleteMessage(id: string): void {
    if (!confirm(this.locale.t('chat.confirmDelete'))) return;
    this.api.deleteChatMessage(id).subscribe({
      error: (err) => this.error.set(this.locale.fromApiError(err, 'chat.deleteFailed'))
    });
  }

  toggleBlock(member: ChatMember): void {
    const room = this.selectedRoom();
    if (!room) return;
    this.api.setChatMemberBlocked(room.id, member.userId, !member.isBlocked).subscribe({
      error: (err) => this.error.set(this.locale.fromApiError(err, 'chat.blockFailed'))
    });
  }

  isMine(message: ChatMessage): boolean {
    return message.senderId === this.auth.user()?.id;
  }

  isStudentMember(member: ChatMember): boolean {
    return member.role === 'Student';
  }

  private reloadRooms(selectId?: string): void {
    this.api.listChatRooms().subscribe({
      next: (rooms) => {
        this.rooms.set(rooms);
        const requested = selectId || this.route.snapshot.queryParamMap.get('room') || this.selectedRoomId();
        const id = rooms.some((r) => r.id === requested) ? requested : rooms[0]?.id || '';
        if (id) this.selectRoom(id);
      },
      error: (err) => this.error.set(this.locale.fromApiError(err, 'chat.loadFailed'))
    });
  }

  private syncRoomQuery(roomId: string): void {
    const current = this.route.snapshot.queryParamMap.get('room');
    if (current === roomId) return;
    void this.router.navigate([], {
      relativeTo: this.route,
      queryParams: { room: roomId },
      queryParamsHandling: 'merge',
      replaceUrl: true
    });
  }

  private upsertMessage(message: ChatMessage): void {
    if (message.roomId === this.selectedRoomId()) {
      this.messages.update((rows) => {
        const rest = rows.filter((r) => r.id !== message.id);
        return [...rest, message].sort((a, b) => a.createdAtUtc.localeCompare(b.createdAtUtc));
      });
      if (!this.isMine(message) && !message.isDeleted) {
        this.markRoomRead(message.roomId);
      }
      return;
    }

    if (!this.isMine(message) && !message.isDeleted) {
      this.bumpRoomUnread(message.roomId);
    }
  }

  private markRoomRead(roomId: string): void {
    this.setRoomUnread(roomId, 0);
    this.api.markChatRoomRead(roomId).subscribe();
  }

  private bumpRoomUnread(roomId: string): void {
    this.rooms.update((rooms) =>
      rooms.map((room) =>
        room.id === roomId ? { ...room, unreadCount: (room.unreadCount ?? 0) + 1 } : room
      )
    );
  }

  private setRoomUnread(roomId: string, count: number): void {
    this.rooms.update((rooms) =>
      rooms.map((room) => (room.id === roomId ? { ...room, unreadCount: count } : room))
    );
  }

  private applyMember(member: ChatMember): void {
    this.rooms.update((rooms) =>
      rooms.map((room) => {
        if (room.id !== this.selectedRoomId()) return room;
        const members = room.members.map((m) => (m.userId === member.userId ? member : m));
        const me = this.auth.user()?.id;
        return {
          ...room,
          members,
          isBlocked: me === member.userId ? member.isBlocked : room.isBlocked
        };
      })
    );
  }
}

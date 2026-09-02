import { Component, EventEmitter, Input, Output } from '@angular/core';

export type IconActionKind = 'edit' | 'delete' | 'play' | 'apply' | 'clear' | 'loginAs';

@Component({
  selector: 'app-icon-action-button',
  templateUrl: './icon-action-button.component.html',
  styleUrl: './icon-action-button.component.css'
})
export class IconActionButtonComponent {
  @Input({ required: true }) kind!: IconActionKind;
  @Input() label = '';
  @Input() disabled = false;
  @Input() variant: 'default' | 'danger' | 'ghost' = 'default';

  @Output() action = new EventEmitter<void>();

  ariaLabel(): string {
    if (this.label) return this.label;
    if (this.kind === 'edit') return 'Edit';
    if (this.kind === 'play') return 'Play';
    if (this.kind === 'apply') return 'Apply filters';
    if (this.kind === 'clear') return 'Clear filters';
    if (this.kind === 'loginAs') return 'Logged in as';
    return 'Delete';
  }
}

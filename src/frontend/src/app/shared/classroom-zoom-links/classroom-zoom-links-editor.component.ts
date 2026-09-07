import { Component, Input } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { IconActionButtonComponent } from '../icon-action-button/icon-action-button.component';
import { TranslatePipe } from '../translate.pipe';
import { ClassroomZoomLinkDraft, emptyZoomLinkDraft } from './classroom-zoom-links.util';

@Component({
  selector: 'app-classroom-zoom-links-editor',
  imports: [FormsModule, IconActionButtonComponent, TranslatePipe],
  templateUrl: './classroom-zoom-links-editor.component.html',
  styleUrl: './classroom-zoom-links-editor.component.css'
})
export class ClassroomZoomLinksEditorComponent {
  @Input({ required: true }) links!: ClassroomZoomLinkDraft[];

  addLink(): void {
    this.links.push(emptyZoomLinkDraft());
  }

  removeLink(index: number): void {
    this.links.splice(index, 1);
  }
}

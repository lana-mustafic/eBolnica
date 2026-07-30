import {
  Component,
  ElementRef,
  EventEmitter,
  Input,
  Output,
  ViewChild
} from '@angular/core';
import { CommonModule } from '@angular/common';
import {
  configureBlockedDragOver,
  configureFileDragOver,
  hasFileDragPayload,
  shouldClearDragOver
} from './medication-image-dropzone-drag.util';
import {
  createDropzoneFileInputId,
  resetNativeFileInput
} from './medication-image-dropzone-file-picker.util';
import {
  buildSelectionLimitedEvent,
  MEDICATION_IMAGE_MAX_FILES,
  normalizeSelectedFiles,
  SelectionLimitedEvent
} from './medication-image-dropzone-selection.util';

/** Accepted MIME types for medication image uploads. */
export const MEDICATION_IMAGE_ACCEPT = 'image/jpeg,image/png,image/webp';

export const MEDICATION_IMAGE_ACCEPTED_MIME_TYPES = [
  'image/jpeg',
  'image/png',
  'image/webp'
] as const;

/** Human-readable max file size label (matches backend MedicationImageUploadSettings). */
export const MEDICATION_IMAGE_MAX_FILE_SIZE_LABEL = '5MB';

export { MEDICATION_IMAGE_MAX_FILES } from './medication-image-dropzone-selection.util';

@Component({
  selector: 'app-medication-image-dropzone',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './medication-image-dropzone.component.html',
  styleUrl: './medication-image-dropzone.component.css'
})
export class MedicationImageDropzoneComponent {
  @Input() disabled = false;
  @Input() busy = false;
  @Input() multiple = true;
  @Input() maxFiles = MEDICATION_IMAGE_MAX_FILES;
  @Input() accept = MEDICATION_IMAGE_ACCEPT;
  @Input() title = 'Drag and drop images here';
  @Input() subtitle = 'or browse files';
  @Input() hint?: string;

  @Output() filesSelected = new EventEmitter<File[]>();
  @Output() selectionLimited = new EventEmitter<SelectionLimitedEvent>();

  readonly fileInputId = createDropzoneFileInputId();

  @ViewChild('fileInput') fileInput?: ElementRef<HTMLInputElement>;

  isDragOver = false;

  get isInteractive(): boolean {
    return !this.disabled && !this.busy;
  }

  get browseLabel(): string {
    return this.busy ? 'Uploading...' : 'Browse files';
  }

  get browseInputId(): string | null {
    return this.isInteractive ? this.fileInputId : null;
  }

  get displayHint(): string {
    if (this.hint) return this.hint;

    const fileLimit = this.multiple ? ` (max ${this.maxFiles} files)` : '';
    return `JPG, PNG, or WEBP up to ${MEDICATION_IMAGE_MAX_FILE_SIZE_LABEL} each${fileLimit}`;
  }

  onDropzoneClick(event: MouseEvent): void {
    if (!this.isInteractive) return;

    const target = event.target as HTMLElement;
    if (target.closest('.image-dropzone-browse-btn')) return;

    this.browseFiles(event);
  }

  onDropzoneKeydown(event: KeyboardEvent): void {
    if (!this.isInteractive) return;

    if (event.key === 'Enter' || event.key === ' ') {
      event.preventDefault();
      this.browseFiles();
    }
  }

  onBrowsePress(event: MouseEvent): void {
    if (!this.isInteractive) {
      event.preventDefault();
      return;
    }

    this.prepareFileInput();
  }

  onBrowseClick(event: MouseEvent): void {
    event.stopPropagation();
  }

  browseFiles(event?: Event): void {
    event?.preventDefault();
    event?.stopPropagation();

    if (!this.isInteractive) return;

    this.prepareFileInput();
    this.fileInput?.nativeElement.click();
  }

  onDragEnter(event: DragEvent): void {
    if (!hasFileDragPayload(event)) return;

    if (!this.isInteractive) {
      configureBlockedDragOver(event);
      return;
    }

    configureFileDragOver(event);
    this.isDragOver = true;
  }

  onDragOver(event: DragEvent): void {
    if (!hasFileDragPayload(event)) return;

    if (!this.isInteractive) {
      configureBlockedDragOver(event);
      return;
    }

    configureFileDragOver(event);
  }

  onDragLeave(event: DragEvent): void {
    event.preventDefault();
    event.stopPropagation();

    const dropzone = event.currentTarget as HTMLElement;
    if (!shouldClearDragOver(event, dropzone)) return;

    this.isDragOver = false;
  }

  onDrop(event: DragEvent): void {
    event.preventDefault();
    event.stopPropagation();
    this.isDragOver = false;

    if (!this.isInteractive || !hasFileDragPayload(event)) return;

    this.emitFiles(event.dataTransfer?.files);
  }

  onFileInputChange(event: Event): void {
    if (!this.isInteractive) {
      this.prepareFileInput();
      return;
    }

    const input = event.target as HTMLInputElement;
    this.emitFiles(input.files);
  }

  private prepareFileInput(): void {
    resetNativeFileInput(this.fileInput?.nativeElement);
  }

  private emitFiles(fileList: FileList | null | undefined): void {
    const selection = normalizeSelectedFiles(fileList, {
      multiple: this.multiple,
      maxFiles: this.maxFiles
    });

    if (selection.files.length === 0) return;

    this.filesSelected.emit(selection.files);

    if (selection.wasLimited) {
      this.selectionLimited.emit(
        buildSelectionLimitedEvent(selection, this.multiple ? this.maxFiles : 1)
      );
    }

    this.prepareFileInput();
  }
}

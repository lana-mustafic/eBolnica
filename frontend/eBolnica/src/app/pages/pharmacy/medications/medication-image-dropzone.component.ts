import {
  Component,
  ElementRef,
  EventEmitter,
  Input,
  Output,
  ViewChild
} from '@angular/core';
import { CommonModule } from '@angular/common';

/** Accepted MIME types for medication image uploads. */
export const MEDICATION_IMAGE_ACCEPT = 'image/jpeg,image/png,image/webp';

export const MEDICATION_IMAGE_ACCEPTED_MIME_TYPES = [
  'image/jpeg',
  'image/png',
  'image/webp'
] as const;

/** Human-readable max file size label (matches backend MedicationImageUploadSettings). */
export const MEDICATION_IMAGE_MAX_FILE_SIZE_LABEL = '5MB';

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
  @Input() accept = MEDICATION_IMAGE_ACCEPT;
  @Input() title = 'Drag and drop images here';
  @Input() subtitle = 'or browse files';
  @Input() hint = `JPG, PNG, or WEBP up to ${MEDICATION_IMAGE_MAX_FILE_SIZE_LABEL}`;

  @Output() filesSelected = new EventEmitter<File[]>();

  @ViewChild('fileInput') fileInput?: ElementRef<HTMLInputElement>;

  isDragOver = false;

  get isInteractive(): boolean {
    return !this.disabled && !this.busy;
  }

  get browseLabel(): string {
    return this.busy ? 'Uploading...' : 'Browse files';
  }

  onDropzoneClick(event: MouseEvent): void {
    if (!this.isInteractive) return;

    const target = event.target as HTMLElement;
    if (target.closest('.image-dropzone-browse-btn')) return;

    this.openFilePicker();
  }

  onDropzoneKeydown(event: KeyboardEvent): void {
    if (!this.isInteractive) return;

    if (event.key === 'Enter' || event.key === ' ') {
      event.preventDefault();
      this.openFilePicker();
    }
  }

  onBrowseClick(event: MouseEvent): void {
    event.stopPropagation();
    if (!this.isInteractive) return;
    this.openFilePicker();
  }

  onDragOver(event: DragEvent): void {
    if (!this.isInteractive) return;

    event.preventDefault();
    event.stopPropagation();
    this.isDragOver = true;
  }

  onDragLeave(event: DragEvent): void {
    event.preventDefault();
    event.stopPropagation();
    this.isDragOver = false;
  }

  onDrop(event: DragEvent): void {
    event.preventDefault();
    event.stopPropagation();
    this.isDragOver = false;

    if (!this.isInteractive) return;

    this.emitFiles(event.dataTransfer?.files);
  }

  onFileInputChange(event: Event): void {
    const input = event.target as HTMLInputElement;
    this.emitFiles(input.files);
  }

  openFilePicker(): void {
    this.fileInput?.nativeElement.click();
  }

  private emitFiles(fileList: FileList | null | undefined): void {
    const files = this.collectFiles(fileList);
    if (files.length === 0) return;

    this.filesSelected.emit(files);
    this.resetFileInput();
  }

  private collectFiles(fileList: FileList | null | undefined): File[] {
    if (!fileList?.length) return [];
    return Array.from(fileList);
  }

  private resetFileInput(): void {
    if (this.fileInput?.nativeElement) {
      this.fileInput.nativeElement.value = '';
    }
  }
}

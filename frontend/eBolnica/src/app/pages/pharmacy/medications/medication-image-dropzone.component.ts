import {
  Component,
  ElementRef,
  EventEmitter,
  Input,
  OnDestroy,
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
import {
  addFilesToPendingQueue,
  cancelPendingMedicationImageQueue,
  getUploadablePendingFiles,
  hasUploadablePendingFiles,
  PendingMedicationImage,
  removePendingMedicationImage
} from './medication-image-pending-queue.util';
import {
  MEDICATION_IMAGE_ACCEPT,
  MEDICATION_IMAGE_MAX_FILE_SIZE_LABEL,
  MedicationImageValidationError,
  partitionMedicationImageFiles
} from './medication-image-validation.util';

export {
  MEDICATION_IMAGE_ACCEPT,
  MEDICATION_IMAGE_MAX_FILE_SIZE_LABEL
} from './medication-image-validation.util';
export { MEDICATION_IMAGE_MAX_FILES } from './medication-image-dropzone-selection.util';
export type { PendingMedicationImage } from './medication-image-pending-queue.util';

@Component({
  selector: 'app-medication-image-dropzone',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './medication-image-dropzone.component.html',
  styleUrl: './medication-image-dropzone.component.css'
})
export class MedicationImageDropzoneComponent implements OnDestroy {
  @Input() disabled = false;
  @Input() busy = false;
  @Input() multiple = true;
  @Input() maxFiles = MEDICATION_IMAGE_MAX_FILES;
  @Input() accept = MEDICATION_IMAGE_ACCEPT;
  @Input() title = 'Drag and drop images here';
  @Input() subtitle = 'or browse files';
  @Input() hint?: string;
  /** When true, files queue for preview and upload starts only after Upload selected. */
  @Input() usePendingQueue = true;

  @Output() filesSelected = new EventEmitter<File[]>();
  @Output() pendingQueueChange = new EventEmitter<PendingMedicationImage[]>();
  @Output() pendingQueueCancelRequested = new EventEmitter<void>();
  @Output() selectionLimited = new EventEmitter<SelectionLimitedEvent>();
  @Output() validationErrors = new EventEmitter<MedicationImageValidationError[]>();
  @Output() uploadBlocked = new EventEmitter<void>();

  readonly fileInputId = createDropzoneFileInputId();

  @ViewChild('fileInput') fileInput?: ElementRef<HTMLInputElement>;

  isDragOver = false;
  validationMessages: MedicationImageValidationError[] = [];
  pendingQueue: PendingMedicationImage[] = [];

  get hasPendingQueue(): boolean {
    return this.pendingQueue.length > 0;
  }

  get uploadablePendingCount(): number {
    return getUploadablePendingFiles(this.pendingQueue).length;
  }

  get canUploadSelected(): boolean {
    return this.isInteractive && hasUploadablePendingFiles(this.pendingQueue);
  }

  get uploadSelectedLabel(): string {
    return `Upload selected (${this.uploadablePendingCount})`;
  }

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
    if (target.closest('.image-dropzone-pending-actions')) return;

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

    this.clearValidationMessages();
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
    this.clearValidationMessages();
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

    if (!hasFileDragPayload(event)) return;

    if (!this.isInteractive) {
      if (this.busy) {
        this.uploadBlocked.emit();
      }
      return;
    }

    this.emitFiles(event.dataTransfer?.files);
  }

  onFileInputChange(event: Event): void {
    if (!this.isInteractive) {
      if (this.busy) {
        this.uploadBlocked.emit();
      }
      this.prepareFileInput();
      return;
    }

    const input = event.target as HTMLInputElement;
    this.emitFiles(input.files);
  }

  removePendingFile(id: string): void {
    const result = removePendingMedicationImage(this.pendingQueue, id);
    this.pendingQueue = result.queue;
    this.pendingQueueChange.emit(this.pendingQueue);
  }

  onPendingRemoveClick(event: Event, id: string): void {
    event.preventDefault();
    event.stopPropagation();

    if (!this.isInteractive) return;

    this.removePendingFile(id);
  }

  onUploadSelectedClick(event: Event): void {
    event.preventDefault();
    event.stopPropagation();

    if (!this.canUploadSelected) return;

    const files = getUploadablePendingFiles(this.pendingQueue);
    if (files.length === 0) return;

    this.filesSelected.emit(files);
  }

  onClearAllClick(event: Event): void {
    event.preventDefault();
    event.stopPropagation();

    if (!this.isInteractive || this.pendingQueue.length === 0) return;

    this.pendingQueueCancelRequested.emit();
  }

  clearPendingQueue(): void {
    this.pendingQueue = cancelPendingMedicationImageQueue(this.pendingQueue);
    this.pendingQueueChange.emit(this.pendingQueue);
  }

  ngOnDestroy(): void {
    this.pendingQueue = cancelPendingMedicationImageQueue(this.pendingQueue);
  }

  private prepareFileInput(): void {
    resetNativeFileInput(this.fileInput?.nativeElement);
  }

  private emitFiles(fileList: FileList | null | undefined): void {
    const availableMax = this.usePendingQueue
      ? Math.max(0, this.maxFiles - this.pendingQueue.length)
      : this.maxFiles;

    const selection = normalizeSelectedFiles(fileList, {
      multiple: this.multiple,
      maxFiles: availableMax
    });

    if (selection.files.length === 0) return;

    if (this.usePendingQueue) {
      this.enqueuePendingFiles(selection);
      return;
    }

    const { validFiles, errors } = partitionMedicationImageFiles(selection.files);
    this.validationMessages = errors;

    if (errors.length > 0) {
      this.validationErrors.emit(errors);
    }

    if (validFiles.length === 0) {
      this.prepareFileInput();
      return;
    }

    if (!this.isInteractive) {
      if (this.busy) {
        this.uploadBlocked.emit();
      }
      this.prepareFileInput();
      return;
    }

    this.filesSelected.emit(validFiles);

    if (selection.wasLimited) {
      this.selectionLimited.emit(
        buildSelectionLimitedEvent(selection, this.multiple ? this.maxFiles : 1)
      );
    }

    this.prepareFileInput();
  }

  private enqueuePendingFiles(selection: ReturnType<typeof normalizeSelectedFiles>): void {
    this.clearValidationMessages();

    const result = addFilesToPendingQueue(
      this.pendingQueue,
      selection.files,
      this.maxFiles
    );

    this.pendingQueue = result.queue;
    this.pendingQueueChange.emit(this.pendingQueue);

    const queueErrors = result.added
      .filter(item => item.status === 'invalid')
      .map(item => ({ fileName: item.fileName, message: item.errorMessage! }));

    if (queueErrors.length > 0) {
      this.validationErrors.emit(queueErrors);
    }

    if (selection.wasLimited || result.wasLimited) {
      this.selectionLimited.emit({
        selected: result.added.length,
        provided: selection.totalProvided,
        maxFiles: this.maxFiles
      });
    }

    this.prepareFileInput();
  }

  private clearValidationMessages(): void {
    this.validationMessages = [];
  }
}

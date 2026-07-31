import { Component, EventEmitter, Input, Output, ViewChild, inject, OnChanges, SimpleChanges, OnInit, OnDestroy } from '@angular/core';
import { CommonModule } from '@angular/common';
import { CdkDragDrop, DragDropModule } from '@angular/cdk/drag-drop';
import { finalize } from 'rxjs';
import { MedicationImageDto } from '../../../models/medication-image.dto';
import { PharmacyService } from '../../../shared/services/pharmacy/pharmacy.service';
import { AuthService } from '../../../shared/services/auth.service';
import { NotificationService } from '../../../shared/services/notification.service';
import { ConfirmDialogService } from '../../../shared/services/confirm-dialog.service';
import { MedicationImageLightboxComponent } from './medication-image-lightbox.component';
import { MedicationImageDropzoneComponent } from './medication-image-dropzone.component';
import { SelectionLimitedEvent } from './medication-image-dropzone-selection.util';
import { uploadMedicationImagesSequentially, MedicationImageUploadBatchResult, MedicationImageUploadEntry } from './medication-image-upload.util';
import {
  createUploadFileStatuses,
  deriveBatchUploadProgress,
  finalizeUploadFileStatusesAfterBatch,
  formatBatchUploadProgressLabel,
  formatCompletedBatchUploadProgressLabel,
  hasUploadFileErrors,
  isSuccessfulUploadBatch,
  markUploadFileStatus,
  markUploadFileStatusesComplete,
  MedicationImageUploadFileStatus,
  shouldShowBatchUploadProgress,
  UPLOAD_PROGRESS_COMPLETE_DISPLAY_MS,
  updateUploadFileProgress
} from './medication-image-upload-status.util';
import {
  beginMedicationImageUploadBatch,
  MEDICATION_IMAGE_UPLOAD_BATCH_IN_PROGRESS_MESSAGE
} from './medication-image-upload-batch.util';
import { buildPendingQueueCancelMessage } from './medication-image-pending-queue.util';
import {
  buildMedicationImageUploadSuccessMessage,
  formatMedicationImageDimensions,
  formatMedicationImageFileSize,
  hasMedicationImageMetadata
} from './medication-image-metadata.util';
import {
  buildMedicationImageReorderPayload,
  createMedicationImageGalleryReorderSnapshot,
  getMedicationImageReorderErrorMessage,
  MedicationImageGalleryReorderSnapshot,
  moveMedicationImageInGallery,
  restoreMedicationImageGalleryReorderSnapshot
} from './medication-image-gallery-reorder.util';

const IMAGE_DELETE_ROLES = ['Pharmacist', 'Admin'] as const;

@Component({
  selector: 'app-medication-image-gallery',
  standalone: true,
  imports: [CommonModule, DragDropModule, MedicationImageLightboxComponent, MedicationImageDropzoneComponent],
  templateUrl: './medication-image-gallery.component.html',
  styleUrl: './medication-image-gallery.component.css'
})
export class MedicationImageGalleryComponent implements OnChanges, OnInit, OnDestroy {
  private pharmacyService = inject(PharmacyService);
  private authService = inject(AuthService);
  private notificationService = inject(NotificationService);
  private confirmDialog = inject(ConfirmDialogService);

  @ViewChild(MedicationImageDropzoneComponent)
  private imageDropzone?: MedicationImageDropzoneComponent;

  @Input({ required: true }) medicationId!: number;
  @Input() images: MedicationImageDto[] = [];
  @Input() medicationName = 'Medication';

  @Output() imagesChange = new EventEmitter<MedicationImageDto[]>();

  selectedIndex = 0;
  isUploading = false;
  isManaging = false;
  isDeleting = false;
  isReordering = false;
  errorMessage: string | null = null;
  successMessage: string | null = null;
  lightboxOpen = false;
  canDeleteImages = false;
  deletingImageId: number | null = null;
  uploadFileStatuses: MedicationImageUploadFileStatus[] = [];
  batchUploadProgress = 0;
  batchUploadLabel = '';

  get showBatchUploadProgress(): boolean {
    if (!shouldShowBatchUploadProgress(this.uploadFileStatuses.length)) {
      return false;
    }

    return this.isUploading || this.batchUploadProgress === 100;
  }

  get showUploadFileStatusList(): boolean {
    return this.uploadFileStatuses.length > 0;
  }

  canRetryUpload = (item: MedicationImageUploadFileStatus): boolean =>
    item.status === 'error' && !this.isUploading && !this.isDeleting;

  readonly formatImageDimensions = formatMedicationImageDimensions;
  readonly formatImageFileSize = formatMedicationImageFileSize;
  readonly hasImageMetadata = hasMedicationImageMetadata;

  private successTimeout: ReturnType<typeof setTimeout> | null = null;
  private uploadProgressHideTimeout: ReturnType<typeof setTimeout> | null = null;
  private uploadFilesByKey = new Map<string, File>();

  ngOnInit(): void {
    this.canDeleteImages = this.hasDeletePermission();
  }

  ngOnDestroy(): void {
    if (this.successTimeout) {
      clearTimeout(this.successTimeout);
    }

    if (this.uploadProgressHideTimeout) {
      clearTimeout(this.uploadProgressHideTimeout);
    }
  }

  ngOnChanges(changes: SimpleChanges): void {
    if (changes['images'] && this.images.length > 0) {
      const primaryIndex = this.images.findIndex(img => img.isPrimary);
      this.selectedIndex = primaryIndex >= 0 ? primaryIndex : 0;
    }
  }

  resolveUrl = (url: string): string => {
    return this.pharmacyService.resolveMedicationImageUrl(url) ?? '';
  };

  resolveThumbnailUrl = (image: MedicationImageDto): string => {
    const url = image.thumbnailUrl ?? image.imageUrl;
    return this.pharmacyService.resolveMedicationImageUrl(url) ?? '';
  };

  get selectedImage(): MedicationImageDto | null {
    return this.images[this.selectedIndex] ?? null;
  }

  get selectedImageUrl(): string | null {
    const image = this.selectedImage;
    return image ? this.pharmacyService.resolveMedicationImageUrl(image.imageUrl) : null;
  }

  get canReorderImages(): boolean {
    return this.images.length >= 2
      && !this.isDeleting
      && !this.isUploading
      && !this.isManaging
      && !this.isReordering;
  }

  selectImage(index: number): void {
    if (index >= 0 && index < this.images.length) {
      this.selectedIndex = index;
    }
  }

  openLightbox(): void {
    if (this.images.length > 0) {
      this.lightboxOpen = true;
    }
  }

  closeLightbox(): void {
    this.lightboxOpen = false;
  }

  onLightboxIndexChange(index: number): void {
    this.selectedIndex = index;
  }

  onThumbnailDrop(event: CdkDragDrop<MedicationImageDto[]>): void {
    if (!this.canReorderImages || event.previousIndex === event.currentIndex) {
      return;
    }

    const snapshot = createMedicationImageGalleryReorderSnapshot(this.images, this.selectedIndex);
    const result = moveMedicationImageInGallery(
      this.images,
      event.previousIndex,
      event.currentIndex,
      this.selectedIndex
    );
    const selectedImageId = this.selectedImage?.id;

    this.applyGalleryImages(result.images, result.selectedIndex);

    this.isReordering = true;
    this.clearMessages();

    this.pharmacyService.reorderMedicationImages(
      this.medicationId,
      buildMedicationImageReorderPayload(result.images)
    ).pipe(
      finalize(() => {
        this.isReordering = false;
      })
    ).subscribe({
      next: (images) => {
        this.applyGalleryImages(
          images,
          this.resolveSelectedIndex(images, selectedImageId, result.selectedIndex)
        );
      },
      error: (error) => {
        this.rollbackGalleryReorder(snapshot, error);
      }
    });
  }

  private rollbackGalleryReorder(
    snapshot: MedicationImageGalleryReorderSnapshot,
    error: { status?: number; error?: { message?: string } | string }
  ): void {
    const restored = restoreMedicationImageGalleryReorderSnapshot(snapshot);
    this.applyGalleryImages(restored.images, restored.selectedIndex);
    this.errorMessage = getMedicationImageReorderErrorMessage(error);
  }

  private applyGalleryImages(images: MedicationImageDto[], selectedIndex: number): void {
    this.images = images;
    this.selectedIndex = selectedIndex;
    this.imagesChange.emit(this.images);
  }

  private resolveSelectedIndex(
    images: MedicationImageDto[],
    selectedImageId: number | undefined,
    fallbackIndex: number
  ): number {
    if (selectedImageId == null) {
      return fallbackIndex;
    }

    const index = images.findIndex(image => image.id === selectedImageId);
    return index >= 0 ? index : fallbackIndex;
  }

  /** Starts upload for files confirmed via Upload selected (not on drop/browse). */
  onDropzoneFilesSelected(entries: MedicationImageUploadEntry[]): void {
    if (entries.length === 0 || this.isDeleting) return;

    const batch = beginMedicationImageUploadBatch(this.isUploading);
    if (!batch.started) {
      this.errorMessage = batch.message ?? MEDICATION_IMAGE_UPLOAD_BATCH_IN_PROGRESS_MESSAGE;
      return;
    }

    this.isUploading = true;
    this.uploadFilesSequentially(entries);
  }

  onUploadBlocked(): void {
    this.errorMessage = MEDICATION_IMAGE_UPLOAD_BATCH_IN_PROGRESS_MESSAGE;
  }

  retryFailedUpload(uploadKey: string): void {
    const file = this.uploadFilesByKey.get(uploadKey);
    if (!file || this.isUploading || this.isDeleting) {
      return;
    }

    const batch = beginMedicationImageUploadBatch(this.isUploading);
    if (!batch.started) {
      this.errorMessage = batch.message ?? MEDICATION_IMAGE_UPLOAD_BATCH_IN_PROGRESS_MESSAGE;
      return;
    }

    this.isUploading = true;
    this.clearMessages();
    this.uploadFilesSequentially([{ file, uploadKey }], { preserveExistingStatuses: true });
  }

  onDropzoneRetryUpload(uploadKey: string): void {
    this.retryFailedUpload(uploadKey);
  }

  onDropzoneSelectionLimited(event: SelectionLimitedEvent): void {
    this.errorMessage =
      `Only ${event.maxFiles} files can be uploaded at once. ${event.provided} files were provided.`;
  }

  onPendingQueueCancelRequested(): void {
    const pendingCount = this.imageDropzone?.pendingQueue.length ?? 0;
    if (pendingCount === 0 || this.isUploading || this.isDeleting) return;

    this.confirmDialog.confirm({
      title: 'Clear pending images',
      message: buildPendingQueueCancelMessage(pendingCount),
      confirmText: 'Clear all',
      cancelText: 'Keep images',
      confirmColor: 'warn'
    }).subscribe(confirmed => {
      if (confirmed) {
        this.imageDropzone?.clearPendingQueue();
      }
    });
  }

  private uploadFilesSequentially(
    entries: MedicationImageUploadEntry[],
    options?: { preserveExistingStatuses?: boolean }
  ): void {
    entries.forEach(entry => this.uploadFilesByKey.set(entry.uploadKey, entry.file));

    if (!options?.preserveExistingStatuses) {
      this.uploadFileStatuses = createUploadFileStatuses(entries);
    } else {
      this.uploadFileStatuses = entries.reduce(
        (statuses, entry) => markUploadFileStatus(statuses, entry.uploadKey, 'pending'),
        this.uploadFileStatuses
      );
    }

    this.syncBatchUploadProgress();
    this.clearMessages();

    uploadMedicationImagesSequentially(
      this.medicationId,
      entries,
      (medicationId, file) => this.pharmacyService.uploadMedicationImage(medicationId, file),
      {
        onFileStart: (uploadKey) => {
          this.uploadFileStatuses = markUploadFileStatus(this.uploadFileStatuses, uploadKey, 'uploading');
          this.syncBatchUploadProgress();
        },
        onFileProgress: (uploadKey, _fileName, progressPercent) => {
          this.uploadFileStatuses = updateUploadFileProgress(
            this.uploadFileStatuses,
            uploadKey,
            progressPercent
          );
          this.syncBatchUploadProgress();
        },
        onFileComplete: (uploadKey) => {
          this.uploadFileStatuses = markUploadFileStatus(this.uploadFileStatuses, uploadKey, 'done');
          this.syncBatchUploadProgress();
        },
        onFileError: (uploadKey, _fileName, message) => {
          this.uploadFileStatuses = markUploadFileStatus(
            this.uploadFileStatuses,
            uploadKey,
            'error',
            message
          );
          this.syncBatchUploadProgress();
        }
      }
    ).pipe(
      finalize(() => {
        this.isUploading = false;

        if (hasUploadFileErrors(this.uploadFileStatuses)) {
          this.uploadFileStatuses = finalizeUploadFileStatusesAfterBatch(this.uploadFileStatuses);
          this.syncBatchUploadProgress();
        }
      })
    ).subscribe({
      next: (result) => this.handleUploadBatchResult(result),
      error: () => {
        this.errorMessage = 'Failed to upload images. Please try again.';
      }
    });
  }

  private handleUploadBatchResult(result: MedicationImageUploadBatchResult): void {
    if (result.errors.length > 0) {
      this.errorMessage = result.errors.length === 1
        ? 'One upload failed. Review the file below and use Retry to try again.'
        : `${result.errors.length} uploads failed. Review each file below and use Retry as needed.`;
    }

    if (result.uploaded.length > 0) {
      this.removeUploadedPendingFiles();
      this.refreshGalleryImages();
      this.showUploadSuccess(result.uploaded);
    }

    if (isSuccessfulUploadBatch(result)) {
      this.completeSuccessfulUploadProgress();
    }
  }

  private completeSuccessfulUploadProgress(): void {
    this.uploadFileStatuses = markUploadFileStatusesComplete(this.uploadFileStatuses);
    this.batchUploadProgress = 100;
    this.batchUploadLabel = formatCompletedBatchUploadProgressLabel(this.uploadFileStatuses.length);

    if (this.uploadProgressHideTimeout) {
      clearTimeout(this.uploadProgressHideTimeout);
    }

    this.uploadProgressHideTimeout = setTimeout(() => {
      this.clearUploadProgressState();
      this.uploadProgressHideTimeout = null;
    }, UPLOAD_PROGRESS_COMPLETE_DISPLAY_MS);
  }

  private clearUploadProgressState(): void {
    this.uploadFileStatuses = [];
    this.batchUploadProgress = 0;
    this.batchUploadLabel = '';
  }

  private syncBatchUploadProgress(): void {
    const batch = deriveBatchUploadProgress(this.uploadFileStatuses);
    this.batchUploadProgress = batch.overallPercent;
    this.batchUploadLabel = formatBatchUploadProgressLabel(batch);
  }

  private showUploadSuccess(uploaded: MedicationImageDto[]): void {
    this.successMessage = buildMedicationImageUploadSuccessMessage(uploaded);

    if (this.successTimeout) {
      clearTimeout(this.successTimeout);
    }

    this.successTimeout = setTimeout(() => {
      this.successMessage = null;
    }, 4000);
  }

  private removeUploadedPendingFiles(): void {
    const completedKeys = this.uploadFileStatuses
      .filter(status => status.status === 'done')
      .map(status => status.uploadKey);

    for (const uploadKey of completedKeys) {
      this.imageDropzone?.removePendingFile(uploadKey);
    }
  }

  private refreshGalleryImages(): void {
    this.pharmacyService.getMedicationImages(this.medicationId).subscribe({
      next: (images) => {
        const dedupedImages = this.dedupeImagesById(images);
        const selectedImageId = this.selectedImage?.id;
        const selectedIndex = selectedImageId == null
          ? Math.max(0, dedupedImages.length - 1)
          : dedupedImages.findIndex(image => image.id === selectedImageId);

        this.applyGalleryImages(
          dedupedImages,
          selectedIndex >= 0 ? selectedIndex : Math.max(0, dedupedImages.length - 1)
        );
      },
      error: () => {
        this.errorMessage = 'Images uploaded, but the gallery could not be refreshed. Reload the page if thumbnails look out of date.';
      }
    });
  }

  private dedupeImagesById(images: MedicationImageDto[]): MedicationImageDto[] {
    const seen = new Set<number>();

    return images.filter(image => {
      if (seen.has(image.id)) {
        return false;
      }

      seen.add(image.id);
      return true;
    });
  }

  setPrimary(): void {
    const image = this.selectedImage;
    if (!image || image.isPrimary) return;

    this.isManaging = true;
    this.clearMessages();

    this.pharmacyService.setPrimaryMedicationImage(this.medicationId, image.id).pipe(
      finalize(() => this.isManaging = false)
    ).subscribe({
      next: () => {
        this.images = this.images.map(img => ({
          ...img,
          isPrimary: img.id === image.id
        }));
        this.imagesChange.emit(this.images);
      },
      error: () => {
        this.errorMessage = 'Failed to set primary image.';
      }
    });
  }

  confirmDelete(image?: MedicationImageDto): void {
    if (!this.canDeleteImages) {
      this.errorMessage = 'You do not have permission to delete medication images.';
      return;
    }

    const target = image ?? this.selectedImage;
    if (!target) return;

    const label = this.getImageLabel(target);
    const primaryNote = target.isPrimary
      ? ' This is the primary image — another image will be promoted automatically.'
      : '';
    const message = `Are you sure you want to delete "${label}"? This action cannot be undone.${primaryNote}`;

    this.confirmDialog.confirm({
      title: 'Delete image',
      message,
      confirmText: 'Delete',
      cancelText: 'Cancel',
      confirmColor: 'warn'
    }).subscribe(confirmed => {
      if (confirmed) {
        this.executeDelete(target);
      }
    });
  }

  private executeDelete(image: MedicationImageDto): void {
    if (!this.canDeleteImages) return;

    this.isDeleting = true;
    this.deletingImageId = image.id;
    this.clearMessages();

    this.pharmacyService.deleteMedicationImage(this.medicationId, image.id).pipe(
      finalize(() => {
        this.isDeleting = false;
        this.deletingImageId = null;
      })
    ).subscribe({
      next: () => {
        this.applyDeletedImage(image);
        this.showDeleteSuccess(image);
      },
      error: (error) => {
        this.handleDeleteError(error);
      }
    });
  }

  isImageDeleting(imageId: number): boolean {
    return this.deletingImageId === imageId;
  }

  private applyDeletedImage(image: MedicationImageDto): void {
    const deletedIndex = this.images.findIndex(img => img.id === image.id);
    this.images = this.images.filter(img => img.id !== image.id);

    if (this.images.length === 0) {
      this.selectedIndex = 0;
      this.lightboxOpen = false;
    } else if (deletedIndex >= 0 && deletedIndex < this.images.length) {
      this.selectedIndex = deletedIndex;
    } else if (deletedIndex >= this.images.length) {
      this.selectedIndex = this.images.length - 1;
    }

    this.imagesChange.emit(this.images);
  }

  private showDeleteSuccess(image: MedicationImageDto): void {
    const label = this.getImageLabel(image);
    this.successMessage = `Image "${label}" was deleted successfully.`;

    this.notificationService.success(
      'Image Deleted',
      `"${label}" has been removed from ${this.medicationName}.`
    );

    if (this.successTimeout) {
      clearTimeout(this.successTimeout);
    }

    this.successTimeout = setTimeout(() => {
      this.successMessage = null;
    }, 4000);
  }

  private handleDeleteError(error: { status?: number }): void {
    if (error?.status === 403) {
      this.errorMessage = 'You do not have permission to delete medication images.';
      this.canDeleteImages = false;
      return;
    }

    if (error?.status === 404) {
      this.errorMessage = 'This image no longer exists. Refreshing the gallery is recommended.';
      return;
    }

    this.errorMessage = 'Failed to delete image. Please try again.';
  }

  private hasDeletePermission(): boolean {
    const role = this.authService.getUserType();
    return !!role && IMAGE_DELETE_ROLES.includes(role as typeof IMAGE_DELETE_ROLES[number]);
  }

  private getImageLabel(image: MedicationImageDto): string {
    if (image.fileName?.trim()) {
      return image.fileName.trim();
    }

    const parts = image.imageUrl.split('/');
    return parts[parts.length - 1] || 'medication image';
  }

  private clearMessages(): void {
    this.errorMessage = null;
    this.successMessage = null;
  }
}

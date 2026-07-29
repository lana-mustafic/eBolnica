import { Component, EventEmitter, Input, Output, inject, OnChanges, SimpleChanges, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { finalize } from 'rxjs';
import { MedicationImageDto } from '../../../models/medication-image.dto';
import { PharmacyService } from '../../../shared/services/pharmacy/pharmacy.service';
import { AuthService } from '../../../shared/services/auth.service';
import { NotificationService } from '../../../shared/services/notification.service';
import { ConfirmDialogService } from '../../../shared/services/confirm-dialog.service';
import { MedicationImageLightboxComponent } from './medication-image-lightbox.component';

const IMAGE_DELETE_ROLES = ['Pharmacist', 'Admin'] as const;

@Component({
  selector: 'app-medication-image-gallery',
  standalone: true,
  imports: [CommonModule, MedicationImageLightboxComponent],
  templateUrl: './medication-image-gallery.component.html',
  styleUrl: './medication-image-gallery.component.css'
})
export class MedicationImageGalleryComponent implements OnChanges, OnInit {
  private pharmacyService = inject(PharmacyService);
  private authService = inject(AuthService);
  private notificationService = inject(NotificationService);
  private confirmDialog = inject(ConfirmDialogService);

  @Input({ required: true }) medicationId!: number;
  @Input() images: MedicationImageDto[] = [];
  @Input() medicationName = 'Medication';

  @Output() imagesChange = new EventEmitter<MedicationImageDto[]>();

  selectedIndex = 0;
  isUploading = false;
  isManaging = false;
  isDeleting = false;
  errorMessage: string | null = null;
  successMessage: string | null = null;
  lightboxOpen = false;
  canDeleteImages = false;
  deletingImageId: number | null = null;

  private successTimeout: ReturnType<typeof setTimeout> | null = null;

  ngOnInit(): void {
    this.canDeleteImages = this.hasDeletePermission();
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

  onUpload(event: Event): void {
    const input = event.target as HTMLInputElement;
    const file = input.files?.[0];
    if (!file) return;

    this.isUploading = true;
    this.clearMessages();

    this.pharmacyService.uploadMedicationImage(this.medicationId, file).pipe(
      finalize(() => {
        this.isUploading = false;
        input.value = '';
      })
    ).subscribe({
      next: (newImage) => {
        this.images = [...this.images, newImage];
        this.selectedIndex = this.images.length - 1;
        this.imagesChange.emit(this.images);
      },
      error: (error) => {
        if (error?.status === 403) {
          this.errorMessage = error.error?.message || error.error || 'File failed security scan and was rejected.';
        } else if (error?.error?.message) {
          this.errorMessage = error.error.message;
        } else {
          this.errorMessage = 'Failed to upload image. Please try again.';
        }
      }
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

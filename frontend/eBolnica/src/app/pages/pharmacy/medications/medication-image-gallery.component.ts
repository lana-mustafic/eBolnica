import { Component, EventEmitter, Input, Output, inject, OnChanges, SimpleChanges } from '@angular/core';
import { CommonModule } from '@angular/common';
import { finalize } from 'rxjs';
import { MedicationImageDto } from '../../../models/medication-image.dto';
import { PharmacyService } from '../../../shared/services/pharmacy/pharmacy.service';
import { ConfirmModalComponent } from '../../../shared/components/confirm-modal/confirm-modal.component';
import { MedicationImageLightboxComponent } from './medication-image-lightbox.component';

@Component({
  selector: 'app-medication-image-gallery',
  standalone: true,
  imports: [CommonModule, ConfirmModalComponent, MedicationImageLightboxComponent],
  templateUrl: './medication-image-gallery.component.html',
  styleUrl: './medication-image-gallery.component.css'
})
export class MedicationImageGalleryComponent implements OnChanges {
  private pharmacyService = inject(PharmacyService);

  @Input({ required: true }) medicationId!: number;
  @Input() images: MedicationImageDto[] = [];
  @Input() medicationName = 'Medication';

  @Output() imagesChange = new EventEmitter<MedicationImageDto[]>();

  selectedIndex = 0;
  isUploading = false;
  isManaging = false;
  errorMessage: string | null = null;
  showDeleteConfirm = false;
  lightboxOpen = false;

  ngOnChanges(changes: SimpleChanges): void {
    if (changes['images'] && this.images.length > 0) {
      const primaryIndex = this.images.findIndex(img => img.isPrimary);
      this.selectedIndex = primaryIndex >= 0 ? primaryIndex : 0;
    }
  }

  resolveUrl = (url: string): string => {
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
    this.errorMessage = null;

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
      error: () => {
        this.errorMessage = 'Failed to upload image. Please try again.';
      }
    });
  }

  setPrimary(): void {
    const image = this.selectedImage;
    if (!image || image.isPrimary) return;

    this.isManaging = true;
    this.errorMessage = null;

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

  confirmDelete(): void {
    if (!this.selectedImage) return;
    this.showDeleteConfirm = true;
  }

  cancelDelete(): void {
    this.showDeleteConfirm = false;
  }

  deleteSelected(): void {
    const image = this.selectedImage;
    if (!image) return;

    this.showDeleteConfirm = false;
    this.isManaging = true;
    this.errorMessage = null;

    this.pharmacyService.deleteMedicationImage(this.medicationId, image.id).pipe(
      finalize(() => this.isManaging = false)
    ).subscribe({
      next: () => {
        const deletedIndex = this.selectedIndex;
        this.images = this.images.filter(img => img.id !== image.id);

        if (this.images.length === 0) {
          this.selectedIndex = 0;
          this.lightboxOpen = false;
        } else if (deletedIndex >= this.images.length) {
          this.selectedIndex = this.images.length - 1;
        }

        this.imagesChange.emit(this.images);
      },
      error: () => {
        this.errorMessage = 'Failed to delete image.';
      }
    });
  }
}

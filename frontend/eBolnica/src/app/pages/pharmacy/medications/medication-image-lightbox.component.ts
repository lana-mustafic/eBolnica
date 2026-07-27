import { Component, EventEmitter, HostListener, Input, Output } from '@angular/core';
import { CommonModule } from '@angular/common';
import { MedicationImageDto } from '../../../models/medication-image.dto';

@Component({
  selector: 'app-medication-image-lightbox',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './medication-image-lightbox.component.html',
  styleUrl: './medication-image-lightbox.component.css'
})
export class MedicationImageLightboxComponent {
  @Input() isOpen = false;
  @Input() images: MedicationImageDto[] = [];
  @Input() currentIndex = 0;
  @Input() resolveUrl!: (url: string) => string;
  @Input() canDelete = false;
  @Input() isDeleting = false;

  @Output() closed = new EventEmitter<void>();
  @Output() indexChange = new EventEmitter<number>();
  @Output() deleteRequested = new EventEmitter<void>();

  @HostListener('document:keydown', ['$event'])
  onKeydown(event: KeyboardEvent): void {
    if (!this.isOpen) return;

    switch (event.key) {
      case 'Escape':
        event.preventDefault();
        this.close();
        break;
      case 'ArrowLeft':
        event.preventDefault();
        this.previous();
        break;
      case 'ArrowRight':
        event.preventDefault();
        this.next();
        break;
    }
  }

  get currentImage(): MedicationImageDto | null {
    return this.images[this.currentIndex] ?? null;
  }

  close(): void {
    this.closed.emit();
  }

  previous(): void {
    if (this.images.length === 0) return;
    const nextIndex = (this.currentIndex - 1 + this.images.length) % this.images.length;
    this.indexChange.emit(nextIndex);
  }

  next(): void {
    if (this.images.length === 0) return;
    const nextIndex = (this.currentIndex + 1) % this.images.length;
    this.indexChange.emit(nextIndex);
  }

  goTo(index: number): void {
    this.indexChange.emit(index);
  }
}

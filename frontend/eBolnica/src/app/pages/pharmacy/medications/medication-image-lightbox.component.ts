import {
  Component,
  EventEmitter,
  HostListener,
  Input,
  OnChanges,
  Output,
  SimpleChanges
} from '@angular/core';
import { CommonModule } from '@angular/common';
import { MedicationImageDto } from '../../../models/medication-image.dto';

/** Minimum zoom level (100%). */
export const LIGHTBOX_MIN_ZOOM = 1;

/** Maximum zoom level (300%). */
export const LIGHTBOX_MAX_ZOOM = 3;

/** Zoom increment per step (25%). */
export const LIGHTBOX_ZOOM_STEP = 0.25;

export interface LightboxZoomState {
  scale: number;
  translateX: number;
  translateY: number;
}

export const LIGHTBOX_DEFAULT_ZOOM_STATE: LightboxZoomState = {
  scale: LIGHTBOX_MIN_ZOOM,
  translateX: 0,
  translateY: 0
};

@Component({
  selector: 'app-medication-image-lightbox',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './medication-image-lightbox.component.html',
  styleUrl: './medication-image-lightbox.component.css'
})
export class MedicationImageLightboxComponent implements OnChanges {
  @Input() isOpen = false;
  @Input() images: MedicationImageDto[] = [];
  @Input() currentIndex = 0;
  @Input() resolveUrl!: (url: string) => string;
  @Input() resolveThumbnailUrl?: (image: MedicationImageDto) => string;
  @Input() canDelete = false;
  @Input() isDeleting = false;

  @Output() closed = new EventEmitter<void>();
  @Output() indexChange = new EventEmitter<number>();
  @Output() deleteRequested = new EventEmitter<void>();

  zoomScale = LIGHTBOX_DEFAULT_ZOOM_STATE.scale;
  zoomTranslateX = LIGHTBOX_DEFAULT_ZOOM_STATE.translateX;
  zoomTranslateY = LIGHTBOX_DEFAULT_ZOOM_STATE.translateY;

  ngOnChanges(changes: SimpleChanges): void {
    if (changes['currentIndex'] && !changes['currentIndex'].firstChange) {
      this.resetZoom();
    }

    if (changes['isOpen']?.currentValue === false) {
      this.resetZoom();
    }
  }

  get zoomState(): LightboxZoomState {
    return {
      scale: this.zoomScale,
      translateX: this.zoomTranslateX,
      translateY: this.zoomTranslateY
    };
  }

  get isZoomed(): boolean {
    return (
      this.zoomScale > LIGHTBOX_MIN_ZOOM ||
      this.zoomTranslateX !== 0 ||
      this.zoomTranslateY !== 0
    );
  }

  get canZoomIn(): boolean {
    return this.zoomScale < LIGHTBOX_MAX_ZOOM;
  }

  get canZoomOut(): boolean {
    return this.zoomScale > LIGHTBOX_MIN_ZOOM;
  }

  get zoomPercentLabel(): string {
    return `${Math.round(this.zoomScale * 100)}%`;
  }

  /** CSS transform for the main lightbox image. */
  get imageTransform(): string {
    return `translate(${this.zoomTranslateX}px, ${this.zoomTranslateY}px) scale(${this.zoomScale})`;
  }

  resetZoom(): void {
    this.zoomScale = LIGHTBOX_DEFAULT_ZOOM_STATE.scale;
    this.zoomTranslateX = LIGHTBOX_DEFAULT_ZOOM_STATE.translateX;
    this.zoomTranslateY = LIGHTBOX_DEFAULT_ZOOM_STATE.translateY;
  }

  zoomIn(): void {
    if (!this.canZoomIn) return;

    this.zoomScale = Math.min(
      LIGHTBOX_MAX_ZOOM,
      this.roundZoom(this.zoomScale + LIGHTBOX_ZOOM_STEP)
    );
  }

  zoomOut(): void {
    if (!this.canZoomOut) return;

    this.zoomScale = Math.max(
      LIGHTBOX_MIN_ZOOM,
      this.roundZoom(this.zoomScale - LIGHTBOX_ZOOM_STEP)
    );

    if (this.zoomScale <= LIGHTBOX_MIN_ZOOM) {
      this.zoomTranslateX = LIGHTBOX_DEFAULT_ZOOM_STATE.translateX;
      this.zoomTranslateY = LIGHTBOX_DEFAULT_ZOOM_STATE.translateY;
    }
  }

  private roundZoom(scale: number): number {
    return Math.round(scale * 100) / 100;
  }

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
    this.resetZoom();
    this.indexChange.emit(nextIndex);
  }

  next(): void {
    if (this.images.length === 0) return;
    const nextIndex = (this.currentIndex + 1) % this.images.length;
    this.resetZoom();
    this.indexChange.emit(nextIndex);
  }

  goTo(index: number): void {
    if (index !== this.currentIndex) {
      this.resetZoom();
    }
    this.indexChange.emit(index);
  }

  getThumbnailUrl(image: MedicationImageDto): string {
    if (this.resolveThumbnailUrl) {
      return this.resolveThumbnailUrl(image);
    }

    const url = image.thumbnailUrl ?? image.imageUrl;
    return this.resolveUrl(url);
  }
}

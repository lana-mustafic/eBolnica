import {
  Component,
  ElementRef,
  EventEmitter,
  HostListener,
  Input,
  OnChanges,
  Output,
  SimpleChanges,
  ViewChild
} from '@angular/core';
import { CommonModule } from '@angular/common';
import { MedicationImageDto } from '../../../models/medication-image.dto';
import { clampLightboxPan } from './medication-image-lightbox-pan.util';

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

  @ViewChild('imageViewport') imageViewport?: ElementRef<HTMLElement>;
  @ViewChild('lightboxImage') lightboxImage?: ElementRef<HTMLImageElement>;

  zoomScale = LIGHTBOX_DEFAULT_ZOOM_STATE.scale;
  zoomTranslateX = LIGHTBOX_DEFAULT_ZOOM_STATE.translateX;
  zoomTranslateY = LIGHTBOX_DEFAULT_ZOOM_STATE.translateY;
  isPanning = false;

  private panStartX = 0;
  private panStartY = 0;
  private panOriginTranslateX = 0;
  private panOriginTranslateY = 0;

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

  get canPan(): boolean {
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
    this.isPanning = false;
    this.applyZoomScale(LIGHTBOX_DEFAULT_ZOOM_STATE.scale);
    this.zoomTranslateX = LIGHTBOX_DEFAULT_ZOOM_STATE.translateX;
    this.zoomTranslateY = LIGHTBOX_DEFAULT_ZOOM_STATE.translateY;
  }

  zoomIn(): void {
    if (!this.canZoomIn) return;
    this.applyZoomScale(this.zoomScale + LIGHTBOX_ZOOM_STEP);
  }

  zoomOut(): void {
    if (!this.canZoomOut) return;
    this.applyZoomScale(this.zoomScale - LIGHTBOX_ZOOM_STEP);
  }

  onWheel(event: WheelEvent): void {
    if (!this.isOpen) return;

    event.preventDefault();

    if (event.deltaY < 0) {
      this.zoomIn();
    } else if (event.deltaY > 0) {
      this.zoomOut();
    }
  }

  onPointerDown(event: PointerEvent): void {
    if (!this.isOpen || !this.canPan || event.button !== 0) return;

    event.preventDefault();
    this.isPanning = true;
    this.panStartX = event.clientX;
    this.panStartY = event.clientY;
    this.panOriginTranslateX = this.zoomTranslateX;
    this.panOriginTranslateY = this.zoomTranslateY;
    (event.currentTarget as HTMLElement).setPointerCapture(event.pointerId);
  }

  onPointerMove(event: PointerEvent): void {
    if (!this.isPanning) return;

    event.preventDefault();
    this.setPanOffset(
      this.panOriginTranslateX + (event.clientX - this.panStartX),
      this.panOriginTranslateY + (event.clientY - this.panStartY)
    );
  }

  onPointerUp(event: PointerEvent): void {
    if (!this.isPanning) return;

    this.isPanning = false;

    if (event.currentTarget instanceof HTMLElement && event.currentTarget.hasPointerCapture(event.pointerId)) {
      event.currentTarget.releasePointerCapture(event.pointerId);
    }
  }

  private setPanOffset(translateX: number, translateY: number): void {
    if (!this.canPan) return;

    this.zoomTranslateX = translateX;
    this.zoomTranslateY = translateY;
    this.clampPanToBounds();
  }

  private clampPanToBounds(): void {
    const viewport = this.imageViewport?.nativeElement;
    const image = this.lightboxImage?.nativeElement;

    if (!viewport || !image || !this.canPan) {
      if (!this.canPan) {
        this.zoomTranslateX = LIGHTBOX_DEFAULT_ZOOM_STATE.translateX;
        this.zoomTranslateY = LIGHTBOX_DEFAULT_ZOOM_STATE.translateY;
      }
      return;
    }

    const clamped = clampLightboxPan(this.zoomTranslateX, this.zoomTranslateY, {
      viewportWidth: viewport.clientWidth,
      viewportHeight: viewport.clientHeight,
      imageWidth: image.offsetWidth,
      imageHeight: image.offsetHeight,
      scale: this.zoomScale
    });

    this.zoomTranslateX = clamped.translateX;
    this.zoomTranslateY = clamped.translateY;
  }

  private applyZoomScale(scale: number): void {
    this.zoomScale = this.roundZoom(
      Math.min(LIGHTBOX_MAX_ZOOM, Math.max(LIGHTBOX_MIN_ZOOM, scale))
    );

    if (this.zoomScale <= LIGHTBOX_MIN_ZOOM) {
      this.zoomTranslateX = LIGHTBOX_DEFAULT_ZOOM_STATE.translateX;
      this.zoomTranslateY = LIGHTBOX_DEFAULT_ZOOM_STATE.translateY;
    } else {
      this.clampPanToBounds();
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
    this.resetZoom();
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

import { Component, HostListener, inject } from '@angular/core';
import { MAT_DIALOG_DATA, MatDialogRef } from '@angular/material/dialog';

export interface MedicationImageLightboxData {
  imageUrl: string;
  fileName: string;
}

@Component({
  selector: 'app-medication-image-lightbox',
  standalone: false,
  templateUrl: './medication-image-lightbox.component.html',
  styleUrl: './medication-image-lightbox.component.scss',
})
export class MedicationImageLightboxComponent {
  data = inject<MedicationImageLightboxData>(MAT_DIALOG_DATA);
  private dialogRef = inject(MatDialogRef<MedicationImageLightboxComponent>);

  scale = 1;
  translateX = 0;
  translateY = 0;
  private isPanning = false;
  private panStartX = 0;
  private panStartY = 0;
  private panOriginX = 0;
  private panOriginY = 0;

  get imageTransform(): string {
    return `translate(${this.translateX}px, ${this.translateY}px) scale(${this.scale})`;
  }

  close(): void {
    this.dialogRef.close();
  }

  resetZoom(): void {
    this.scale = 1;
    this.translateX = 0;
    this.translateY = 0;
  }

  zoomIn(): void {
    this.scale = Math.min(this.scale + 0.25, 4);
  }

  zoomOut(): void {
    this.scale = Math.max(this.scale - 0.25, 0.5);
    if (this.scale <= 1) {
      this.translateX = 0;
      this.translateY = 0;
    }
  }

  onWheel(event: WheelEvent): void {
    event.preventDefault();
    const delta = event.deltaY > 0 ? -0.15 : 0.15;
    this.scale = Math.min(Math.max(this.scale + delta, 0.5), 4);
    if (this.scale <= 1) {
      this.translateX = 0;
      this.translateY = 0;
    }
  }

  onPointerDown(event: PointerEvent): void {
    if (this.scale <= 1) return;
    this.isPanning = true;
    this.panStartX = event.clientX;
    this.panStartY = event.clientY;
    this.panOriginX = this.translateX;
    this.panOriginY = this.translateY;
    (event.target as HTMLElement).setPointerCapture?.(event.pointerId);
  }

  onPointerMove(event: PointerEvent): void {
    if (!this.isPanning) return;
    this.translateX = this.panOriginX + (event.clientX - this.panStartX);
    this.translateY = this.panOriginY + (event.clientY - this.panStartY);
  }

  onPointerUp(event: PointerEvent): void {
    this.isPanning = false;
    (event.target as HTMLElement).releasePointerCapture?.(event.pointerId);
  }

  @HostListener('document:keydown.escape')
  onEscape(): void {
    this.close();
  }
}

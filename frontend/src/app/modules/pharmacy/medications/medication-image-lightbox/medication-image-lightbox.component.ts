import {
  ChangeDetectionStrategy,
  Component,
  computed,
  HostListener,
  inject,
  signal,
} from '@angular/core';
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
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class MedicationImageLightboxComponent {
  data = inject<MedicationImageLightboxData>(MAT_DIALOG_DATA);
  private dialogRef = inject(MatDialogRef<MedicationImageLightboxComponent>);

  scale = signal(1);
  translateX = signal(0);
  translateY = signal(0);
  private isPanning = false;
  private panStartX = 0;
  private panStartY = 0;
  private panOriginX = 0;
  private panOriginY = 0;

  imageTransform = computed(
    () => `translate(${this.translateX()}px, ${this.translateY()}px) scale(${this.scale()})`
  );

  close(): void {
    this.dialogRef.close();
  }

  resetZoom(): void {
    this.scale.set(1);
    this.translateX.set(0);
    this.translateY.set(0);
  }

  zoomIn(): void {
    this.scale.update((value) => Math.min(value + 0.25, 4));
  }

  zoomOut(): void {
    this.scale.update((value) => {
      const next = Math.max(value - 0.25, 0.5);
      if (next <= 1) {
        this.translateX.set(0);
        this.translateY.set(0);
      }
      return next;
    });
  }

  onWheel(event: WheelEvent): void {
    event.preventDefault();
    const delta = event.deltaY > 0 ? -0.15 : 0.15;
    this.scale.update((value) => {
      const next = Math.min(Math.max(value + delta, 0.5), 4);
      if (next <= 1) {
        this.translateX.set(0);
        this.translateY.set(0);
      }
      return next;
    });
  }

  onPointerDown(event: PointerEvent): void {
    if (this.scale() <= 1) return;
    this.isPanning = true;
    this.panStartX = event.clientX;
    this.panStartY = event.clientY;
    this.panOriginX = this.translateX();
    this.panOriginY = this.translateY();
    (event.target as HTMLElement).setPointerCapture?.(event.pointerId);
  }

  onPointerMove(event: PointerEvent): void {
    if (!this.isPanning) return;
    this.translateX.set(this.panOriginX + (event.clientX - this.panStartX));
    this.translateY.set(this.panOriginY + (event.clientY - this.panStartY));
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

/** Returns true when the drag payload includes local files. */
export function hasFileDragPayload(event: DragEvent): boolean {
  const types = event.dataTransfer?.types;
  if (!types) return false;
  return Array.from(types).includes('Files');
}

/** Enables dropping files and shows a copy cursor. */
export function configureFileDragOver(event: DragEvent): void {
  event.preventDefault();
  event.stopPropagation();

  if (event.dataTransfer) {
    event.dataTransfer.dropEffect = 'copy';
  }
}

/** Marks the dropzone as unavailable while uploads are blocked. */
export function configureBlockedDragOver(event: DragEvent): void {
  event.preventDefault();
  event.stopPropagation();

  if (event.dataTransfer) {
    event.dataTransfer.dropEffect = 'none';
  }
}

/**
 * Returns true when the pointer leaves the dropzone entirely
 * (ignores dragleave events fired when moving between child elements).
 */
export function shouldClearDragOver(event: DragEvent, dropzone: HTMLElement): boolean {
  const relatedTarget = event.relatedTarget as Node | null;
  return !relatedTarget || !dropzone.contains(relatedTarget);
}

/** Tracks nested dragenter/dragleave events without flicker on child elements. */
export class DropzoneDragDepthTracker {
  private depth = 0;

  get isActive(): boolean {
    return this.depth > 0;
  }

  enter(): void {
    this.depth++;
  }

  leave(): void {
    this.depth = Math.max(0, this.depth - 1);
  }

  reset(): void {
    this.depth = 0;
  }
}

import {
  configureBlockedDragOver,
  configureFileDragOver,
  DropzoneDragDepthTracker,
  hasFileDragPayload,
  shouldClearDragOver
} from './medication-image-dropzone-drag.util';

describe('medication-image-dropzone-drag.util', () => {
  function dragEvent(type: string, init: Partial<DragEventInit> & { types?: string[] } = {}): DragEvent {
    const { types, ...rest } = init;
    const event = new DragEvent(type, { bubbles: true, cancelable: true, ...rest });
    if (types) {
      Object.defineProperty(event, 'dataTransfer', {
        configurable: true,
        value: { types, dropEffect: 'none' }
      });
    }
    return event;
  }

  it('detects file drag payloads', () => {
    expect(hasFileDragPayload(dragEvent('dragenter', { types: ['Files'] }))).toBeTrue();
    expect(hasFileDragPayload(dragEvent('dragenter', { types: ['text/plain'] }))).toBeFalse();
  });

  it('configures copy drop effect on dragover', () => {
    const dataTransfer = { types: ['Files'], dropEffect: 'none' };
    const event = dragEvent('dragover');
    Object.defineProperty(event, 'dataTransfer', { configurable: true, value: dataTransfer });
    spyOn(event, 'preventDefault');
    spyOn(event, 'stopPropagation');

    configureFileDragOver(event);

    expect(event.preventDefault).toHaveBeenCalled();
    expect(event.stopPropagation).toHaveBeenCalled();
    expect(dataTransfer.dropEffect).toBe('copy');
  });

  it('configures blocked drop effect when inactive', () => {
    const dataTransfer = { types: ['Files'], dropEffect: 'copy' };
    const event = dragEvent('dragover');
    Object.defineProperty(event, 'dataTransfer', { configurable: true, value: dataTransfer });

    configureBlockedDragOver(event);

    expect(dataTransfer.dropEffect).toBe('none');
  });

  it('ignores dragleave when moving to a child element', () => {
    const dropzone = document.createElement('div');
    const child = document.createElement('button');
    dropzone.appendChild(child);

    const event = dragEvent('dragleave');
    Object.defineProperty(event, 'relatedTarget', { configurable: true, value: child });

    expect(shouldClearDragOver(event, dropzone)).toBeFalse();
  });

  it('clears drag state when leaving the dropzone entirely', () => {
    const dropzone = document.createElement('div');
    const outside = document.createElement('div');
    const event = dragEvent('dragleave');
    Object.defineProperty(event, 'relatedTarget', { configurable: true, value: outside });

    expect(shouldClearDragOver(event, dropzone)).toBeTrue();
  });

  it('tracks nested drag depth without going negative', () => {
    const tracker = new DropzoneDragDepthTracker();

    tracker.enter();
    tracker.enter();
    expect(tracker.isActive).toBeTrue();

    tracker.leave();
    expect(tracker.isActive).toBeTrue();

    tracker.leave();
    expect(tracker.isActive).toBeFalse();

    tracker.leave();
    expect(tracker.isActive).toBeFalse();
  });
});

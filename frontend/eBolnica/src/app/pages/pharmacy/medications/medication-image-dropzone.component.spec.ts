import { ComponentFixture, TestBed } from '@angular/core/testing';
import {
  MEDICATION_IMAGE_ACCEPT,
  MEDICATION_IMAGE_MAX_FILES,
  MedicationImageDropzoneComponent
} from './medication-image-dropzone.component';
import { MEDICATION_IMAGE_MAX_FILE_SIZE_BYTES } from './medication-image-validation.util';

describe('MedicationImageDropzoneComponent', () => {
  let component: MedicationImageDropzoneComponent;
  let fixture: ComponentFixture<MedicationImageDropzoneComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [MedicationImageDropzoneComponent]
    }).compileComponents();

    fixture = TestBed.createComponent(MedicationImageDropzoneComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  function createFile(
    name: string,
    options: { type?: string; size?: number } = {}
  ): File {
    const size = options.size ?? 11;
    return new File([new Uint8Array(size)], name, {
      type: options.type ?? 'image/jpeg'
    });
  }

  function dragEvent(
    type: string,
    target: HTMLElement,
    options: { types?: string[]; relatedTarget?: Node | null; files?: File[] } = {}
  ): DragEvent {
    const event = new DragEvent(type, { bubbles: true, cancelable: true });
    const dataTransfer: Partial<DataTransfer> = {
      types: options.types ?? ['Files'],
      dropEffect: 'none',
      files: options.files as unknown as FileList
    };
    Object.defineProperty(event, 'dataTransfer', { configurable: true, value: dataTransfer });
    if ('relatedTarget' in options) {
      Object.defineProperty(event, 'relatedTarget', {
        configurable: true,
        value: options.relatedTarget
      });
    }
    Object.defineProperty(event, 'currentTarget', { configurable: true, value: target });
    return event;
  }

  it('creates with default medication image accept types', () => {
    const input = fixture.nativeElement.querySelector('.image-dropzone-input') as HTMLInputElement;
    expect(input.accept).toBe(MEDICATION_IMAGE_ACCEPT);
    expect(input.multiple).toBeTrue();
    expect(component.maxFiles).toBe(MEDICATION_IMAGE_MAX_FILES);
    expect(fixture.nativeElement.querySelector('.image-dropzone-hint')?.textContent)
      .toContain(`max ${MEDICATION_IMAGE_MAX_FILES} files`);
  });

  it('emits filesSelected when files are chosen via input', () => {
    spyOn(component.filesSelected, 'emit');
    const file = createFile('photo.jpg');
    const input = fixture.nativeElement.querySelector('.image-dropzone-input') as HTMLInputElement;

    Object.defineProperty(input, 'files', {
      configurable: true,
      value: [file]
    });
    input.dispatchEvent(new Event('change'));

    expect(component.filesSelected.emit).toHaveBeenCalledWith([file]);
    expect(input.value).toBe('');
  });

  it('emits multiple files from file picker when multiple is enabled', () => {
    spyOn(component.filesSelected, 'emit');
    const files = [createFile('a.jpg'), createFile('b.jpg')];
    const input = fixture.nativeElement.querySelector('.image-dropzone-input') as HTMLInputElement;

    Object.defineProperty(input, 'files', {
      configurable: true,
      value: files
    });
    input.dispatchEvent(new Event('change'));

    expect(component.filesSelected.emit).toHaveBeenCalledWith(files);
  });

  it('limits multiple dropped files to maxFiles and emits selectionLimited', () => {
    spyOn(component.filesSelected, 'emit');
    spyOn(component.selectionLimited, 'emit');
    const files = Array.from({ length: 7 }, (_, index) => createFile(`photo-${index + 1}.jpg`));
    const dropzone = fixture.nativeElement.querySelector('.image-dropzone') as HTMLElement;

    component.onDrop(dragEvent('drop', dropzone, { files }));

    expect(component.filesSelected.emit).toHaveBeenCalledWith(files.slice(0, MEDICATION_IMAGE_MAX_FILES));
    expect(component.selectionLimited.emit).toHaveBeenCalledWith({
      selected: MEDICATION_IMAGE_MAX_FILES,
      provided: 7,
      maxFiles: MEDICATION_IMAGE_MAX_FILES
    });
  });

  it('selects only one file when multiple input is disabled', () => {
    component.multiple = false;
    fixture.detectChanges();
    spyOn(component.filesSelected, 'emit');
    spyOn(component.selectionLimited, 'emit');

    const files = [createFile('a.jpg'), createFile('b.jpg')];
    const input = fixture.nativeElement.querySelector('.image-dropzone-input') as HTMLInputElement;
    expect(input.multiple).toBeFalse();

    Object.defineProperty(input, 'files', { configurable: true, value: files });
    input.dispatchEvent(new Event('change'));

    expect(component.filesSelected.emit).toHaveBeenCalledWith([files[0]]);
    expect(component.selectionLimited.emit).toHaveBeenCalledWith({
      selected: 1,
      provided: 2,
      maxFiles: 1
    });
  });

  it('does not emit when file picker is cancelled', () => {
    spyOn(component.filesSelected, 'emit');
    const input = fixture.nativeElement.querySelector('.image-dropzone-input') as HTMLInputElement;

    Object.defineProperty(input, 'files', {
      configurable: true,
      value: []
    });
    input.dispatchEvent(new Event('change'));

    expect(component.filesSelected.emit).not.toHaveBeenCalled();
  });

  it('emits filesSelected when files are dropped', () => {
    spyOn(component.filesSelected, 'emit');
    const file = createFile('dropped.png');
    const dropzone = fixture.nativeElement.querySelector('.image-dropzone') as HTMLElement;

    component.onDrop(dragEvent('drop', dropzone, { files: [file] }));

    expect(component.filesSelected.emit).toHaveBeenCalledWith([file]);
    expect(component.isDragOver).toBeFalse();
  });

  it('handles dragenter and dragover for file drags', () => {
    const dropzone = fixture.nativeElement.querySelector('.image-dropzone') as HTMLElement;
    const enter = dragEvent('dragenter', dropzone);
    spyOn(enter, 'preventDefault');
    spyOn(enter, 'stopPropagation');

    component.onDragEnter(enter);
    fixture.detectChanges();

    expect(enter.preventDefault).toHaveBeenCalled();
    expect(component.isDragOver).toBeTrue();
    expect(dropzone.classList.contains('drag-over')).toBeTrue();

    const over = dragEvent('dragover', dropzone);
    spyOn(over, 'preventDefault');
    component.onDragOver(over);
    expect(over.preventDefault).toHaveBeenCalled();
  });

  it('clears drag-over only when leaving the dropzone entirely', () => {
    const dropzone = fixture.nativeElement.querySelector('.image-dropzone') as HTMLElement;
    const child = dropzone.querySelector('.image-dropzone-browse-btn') as HTMLElement;

    component.onDragEnter(dragEvent('dragenter', dropzone));
    expect(component.isDragOver).toBeTrue();

    component.onDragLeave(dragEvent('dragleave', dropzone, { relatedTarget: child }));
    expect(component.isDragOver).toBeTrue();

    component.onDragLeave(dragEvent('dragleave', dropzone, { relatedTarget: document.body }));
    expect(component.isDragOver).toBeFalse();
  });

  it('ignores non-file drag payloads', () => {
    const dropzone = fixture.nativeElement.querySelector('.image-dropzone') as HTMLElement;

    component.onDragEnter(dragEvent('dragenter', dropzone, { types: ['text/plain'] }));
    expect(component.isDragOver).toBeFalse();

    spyOn(component.filesSelected, 'emit');
    component.onDrop(dragEvent('drop', dropzone, { types: ['text/plain'], files: [createFile('skip.jpg')] }));
    expect(component.filesSelected.emit).not.toHaveBeenCalled();
  });

  it('blocks drag-over styling while busy or disabled', () => {
    const dropzone = fixture.nativeElement.querySelector('.image-dropzone') as HTMLElement;
    component.busy = true;
    fixture.detectChanges();

    const enter = dragEvent('dragenter', dropzone);
    component.onDragEnter(enter);

    expect(component.isDragOver).toBeFalse();
    expect(enter.dataTransfer?.dropEffect).toBe('none');
  });

  it('sets drag-over state while dragging over', () => {
    const dropzone = fixture.nativeElement.querySelector('.image-dropzone') as HTMLElement;

    component.onDragEnter(dragEvent('dragenter', dropzone));
    fixture.detectChanges();

    expect(component.isDragOver).toBeTrue();
    expect(dropzone.classList.contains('drag-over')).toBeTrue();

    component.onDragLeave(dragEvent('dragleave', dropzone, { relatedTarget: document.body }));
    fixture.detectChanges();

    expect(component.isDragOver).toBeFalse();
  });

  it('does not emit files when disabled', () => {
    component.disabled = true;
    fixture.detectChanges();
    spyOn(component.filesSelected, 'emit');

    const dropzone = fixture.nativeElement.querySelector('.image-dropzone') as HTMLElement;
    component.onDrop(dragEvent('drop', dropzone, { files: [createFile('blocked.jpg')] }));

    expect(component.filesSelected.emit).not.toHaveBeenCalled();
  });

  it('does not emit files while busy', () => {
    component.busy = true;
    fixture.detectChanges();
    spyOn(component.filesSelected, 'emit');
    spyOn(component.uploadBlocked, 'emit');

    const dropzone = fixture.nativeElement.querySelector('.image-dropzone') as HTMLElement;
    component.onDrop(dragEvent('drop', dropzone, { files: [createFile('busy.jpg')] }));

    expect(component.filesSelected.emit).not.toHaveBeenCalled();
    expect(component.uploadBlocked.emit).toHaveBeenCalled();
    expect(fixture.nativeElement.querySelector('.image-dropzone-browse-btn').textContent.trim()).toBe('Uploading...');
  });

  it('opens file picker from browse label via native for attribute', () => {
    const input = fixture.nativeElement.querySelector('.image-dropzone-input') as HTMLInputElement;
    const browseLabel = fixture.nativeElement.querySelector('.image-dropzone-browse-btn') as HTMLLabelElement;

    expect(browseLabel.getAttribute('for')).toBe(component.fileInputId);
    expect(input.id).toBe(component.fileInputId);
  });

  it('browseFiles resets input and programmatically opens picker', () => {
    const input = fixture.nativeElement.querySelector('.image-dropzone-input') as HTMLInputElement;
    input.value = 'C:\\fakepath\\old.jpg';
    spyOn(input, 'click');

    component.browseFiles();

    expect(input.value).toBe('');
    expect(input.click).toHaveBeenCalled();
  });

  it('opens file picker when dropzone is clicked', () => {
    spyOn(component, 'browseFiles');
    const dropzone = fixture.nativeElement.querySelector('.image-dropzone') as HTMLElement;

    dropzone.click();

    expect(component.browseFiles).toHaveBeenCalled();
  });

  it('opens file picker from keyboard on dropzone', () => {
    spyOn(component, 'browseFiles');
    const event = new KeyboardEvent('keydown', { key: 'Enter', bubbles: true, cancelable: true });
    spyOn(event, 'preventDefault');

    component.onDropzoneKeydown(event);

    expect(event.preventDefault).toHaveBeenCalled();
    expect(component.browseFiles).toHaveBeenCalled();
  });

  it('does not open file picker while disabled or busy', () => {
    const input = fixture.nativeElement.querySelector('.image-dropzone-input') as HTMLInputElement;
    spyOn(input, 'click');

    component.disabled = true;
    component.browseFiles();
    expect(input.click).not.toHaveBeenCalled();

    component.disabled = false;
    component.busy = true;
    fixture.detectChanges();
    component.browseFiles();
    expect(input.click).not.toHaveBeenCalled();
    expect(fixture.nativeElement.querySelector('.image-dropzone-browse-btn').getAttribute('for')).toBeNull();
  });

  it('allows selecting the same file twice via browse fallback', () => {
    spyOn(component.filesSelected, 'emit');
    const input = fixture.nativeElement.querySelector('.image-dropzone-input') as HTMLInputElement;
    const file = createFile('repeat.jpg');

    Object.defineProperty(input, 'files', { configurable: true, value: [file] });
    input.dispatchEvent(new Event('change'));
    Object.defineProperty(input, 'files', { configurable: true, value: [file] });
    input.dispatchEvent(new Event('change'));

    expect(component.filesSelected.emit).toHaveBeenCalledTimes(2);
  });

  it('rejects invalid files client-side and still emits valid ones', () => {
    spyOn(component.filesSelected, 'emit');
    spyOn(component.validationErrors, 'emit');
    const dropzone = fixture.nativeElement.querySelector('.image-dropzone') as HTMLElement;
    const valid = createFile('ok.jpg');
    const invalid = createFile('bad.pdf', { type: 'application/pdf' });

    component.onDrop(dragEvent('drop', dropzone, { files: [valid, invalid] }));
    fixture.detectChanges();

    expect(component.filesSelected.emit).toHaveBeenCalledWith([valid]);
    expect(component.validationErrors.emit).toHaveBeenCalledWith([
      { fileName: 'bad.pdf', message: 'Invalid file type. Allowed formats: JPG, PNG, WEBP.' }
    ]);
    expect(fixture.nativeElement.querySelector('.image-dropzone-errors')?.textContent)
      .toContain('bad.pdf');
  });

  it('rejects files larger than 5MB with a clear message', () => {
    spyOn(component.filesSelected, 'emit');
    spyOn(component.validationErrors, 'emit');
    const input = fixture.nativeElement.querySelector('.image-dropzone-input') as HTMLInputElement;
    const tooLarge = createFile('large.jpg', { size: MEDICATION_IMAGE_MAX_FILE_SIZE_BYTES + 1 });

    Object.defineProperty(input, 'files', { configurable: true, value: [tooLarge] });
    input.dispatchEvent(new Event('change'));
    fixture.detectChanges();

    expect(component.filesSelected.emit).not.toHaveBeenCalled();
    expect(component.validationErrors.emit).toHaveBeenCalledWith([
      { fileName: 'large.jpg', message: 'File is too large. Maximum size is 5MB.' }
    ]);
  });

  describe('pending queue mode', () => {
    beforeEach(() => {
      component.usePendingQueue = true;
      fixture.detectChanges();
      spyOn(URL, 'createObjectURL').and.callFake((blob: Blob) => `blob:${(blob as File).name}`);
    });

    it('adds validated files to pending queue instead of emitting filesSelected', () => {
      spyOn(component.filesSelected, 'emit');
      spyOn(component.pendingQueueChange, 'emit');
      const files = [createFile('a.jpg'), createFile('b.jpg')];
      const dropzone = fixture.nativeElement.querySelector('.image-dropzone') as HTMLElement;

      component.onDrop(dragEvent('drop', dropzone, { files }));
      fixture.detectChanges();

      expect(component.filesSelected.emit).not.toHaveBeenCalled();
      expect(component.pendingQueue).toHaveSize(2);
      expect(component.pendingQueue.every(item => item.status === 'valid')).toBeTrue();
      expect(component.pendingQueueChange.emit).toHaveBeenCalledWith(component.pendingQueue);

      const previews = fixture.nativeElement.querySelectorAll('.image-dropzone-pending-thumb') as NodeListOf<HTMLImageElement>;
      expect(previews.length).toBe(2);
      expect(previews[0].src).toContain('blob:a.jpg');
      expect(previews[1].src).toContain('blob:b.jpg');
    });

    it('keeps invalid files in pending queue with error state', () => {
      spyOn(component.validationErrors, 'emit');
      const valid = createFile('ok.jpg');
      const invalid = createFile('bad.pdf', { type: 'application/pdf' });
      const dropzone = fixture.nativeElement.querySelector('.image-dropzone') as HTMLElement;

      component.onDrop(dragEvent('drop', dropzone, { files: [valid, invalid] }));
      fixture.detectChanges();

      expect(component.pendingQueue).toHaveSize(2);
      expect(component.pendingQueue[0].status).toBe('valid');
      expect(component.pendingQueue[1].status).toBe('invalid');
      expect(component.pendingQueue[1].previewUrl).toBeNull();
      expect(component.validationErrors.emit).toHaveBeenCalledWith([
        { fileName: 'bad.pdf', message: 'Invalid file type. Allowed formats: JPG, PNG, WEBP.' }
      ]);

      const previews = fixture.nativeElement.querySelectorAll('.image-dropzone-pending-thumb');
      const placeholders = fixture.nativeElement.querySelectorAll('.image-dropzone-pending-placeholder');
      expect(previews.length).toBe(1);
      expect(placeholders.length).toBe(1);
    });

    it('respects remaining queue capacity when adding more files', () => {
      spyOn(component.selectionLimited, 'emit');
      const dropzone = fixture.nativeElement.querySelector('.image-dropzone') as HTMLElement;
      const firstBatch = Array.from({ length: 4 }, (_, index) => createFile(`first-${index + 1}.jpg`));
      const secondBatch = Array.from({ length: 3 }, (_, index) => createFile(`second-${index + 1}.jpg`));

      component.onDrop(dragEvent('drop', dropzone, { files: firstBatch }));
      component.onDrop(dragEvent('drop', dropzone, { files: secondBatch }));

      expect(component.pendingQueue).toHaveSize(MEDICATION_IMAGE_MAX_FILES);
      expect(component.selectionLimited.emit).toHaveBeenCalledWith({
        selected: 1,
        provided: 3,
        maxFiles: MEDICATION_IMAGE_MAX_FILES
      });
    });

    it('removes pending file and revokes preview URL', () => {
      spyOn(URL, 'revokeObjectURL');
      const dropzone = fixture.nativeElement.querySelector('.image-dropzone') as HTMLElement;
      component.onDrop(dragEvent('drop', dropzone, { files: [createFile('remove-me.jpg')] }));
      const previewUrl = component.pendingQueue[0].previewUrl;

      component.removePendingFile(component.pendingQueue[0].id);

      expect(component.pendingQueue).toEqual([]);
      expect(URL.revokeObjectURL).toHaveBeenCalledWith(previewUrl);
    });

    it('removes pending preview via remove button click', () => {
      spyOn(URL, 'revokeObjectURL');
      spyOn(component.pendingQueueChange, 'emit');
      const dropzone = fixture.nativeElement.querySelector('.image-dropzone') as HTMLElement;
      component.onDrop(dragEvent('drop', dropzone, {
        files: [createFile('keep.jpg'), createFile('remove.jpg')]
      }));
      fixture.detectChanges();

      const removeButtons = fixture.nativeElement.querySelectorAll(
        '.image-dropzone-pending-remove-btn'
      ) as NodeListOf<HTMLButtonElement>;
      expect(removeButtons.length).toBe(2);

      removeButtons[1].click();
      fixture.detectChanges();

      expect(component.pendingQueue).toHaveSize(1);
      expect(component.pendingQueue[0].fileName).toBe('keep.jpg');
      expect(fixture.nativeElement.querySelectorAll('.image-dropzone-pending-item').length).toBe(1);
      expect(component.pendingQueueChange.emit).toHaveBeenCalled();
      expect(URL.revokeObjectURL).toHaveBeenCalledWith('blob:remove.jpg');
    });

    it('does not remove pending preview while busy', () => {
      const dropzone = fixture.nativeElement.querySelector('.image-dropzone') as HTMLElement;
      component.onDrop(dragEvent('drop', dropzone, { files: [createFile('busy.jpg')] }));
      component.busy = true;
      fixture.detectChanges();

      const removeButton = fixture.nativeElement.querySelector(
        '.image-dropzone-pending-remove-btn'
      ) as HTMLButtonElement;
      expect(removeButton.disabled).toBeTrue();

      removeButton.click();
      fixture.detectChanges();

      expect(component.pendingQueue).toHaveSize(1);
    });

    it('clears pending queue and revokes all preview URLs', () => {
      spyOn(URL, 'revokeObjectURL');
      const dropzone = fixture.nativeElement.querySelector('.image-dropzone') as HTMLElement;
      component.onDrop(dragEvent('drop', dropzone, {
        files: [createFile('a.jpg'), createFile('b.jpg')]
      }));
      const previewUrls = component.pendingQueue.map(item => item.previewUrl);

      component.clearPendingQueue();

      expect(component.pendingQueue).toEqual([]);
      previewUrls.forEach(url => expect(URL.revokeObjectURL).toHaveBeenCalledWith(url));
    });
  });
});

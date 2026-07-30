import { ComponentFixture, TestBed } from '@angular/core/testing';
import {
  MEDICATION_IMAGE_ACCEPT,
  MedicationImageDropzoneComponent
} from './medication-image-dropzone.component';

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

  function createFile(name: string): File {
    return new File(['image-bytes'], name, { type: 'image/jpeg' });
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

    const dropzone = fixture.nativeElement.querySelector('.image-dropzone') as HTMLElement;
    component.onDrop(dragEvent('drop', dropzone, { files: [createFile('busy.jpg')] }));

    expect(component.filesSelected.emit).not.toHaveBeenCalled();
    expect(fixture.nativeElement.querySelector('.image-dropzone-browse-btn').textContent.trim()).toBe('Uploading...');
  });

  it('opens file picker from browse button', () => {
    const input = fixture.nativeElement.querySelector('.image-dropzone-input') as HTMLInputElement;
    spyOn(input, 'click');

    const browseButton = fixture.nativeElement.querySelector('.image-dropzone-browse-btn') as HTMLButtonElement;
    browseButton.click();

    expect(input.click).toHaveBeenCalled();
  });
});

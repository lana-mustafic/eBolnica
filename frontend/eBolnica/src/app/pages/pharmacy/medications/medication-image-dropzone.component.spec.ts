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

    dropzone.dispatchEvent(new DragEvent('drop', {
      bubbles: true,
      cancelable: true,
      dataTransfer: { files: [file] } as unknown as DataTransfer
    }));

    expect(component.filesSelected.emit).toHaveBeenCalledWith([file]);
  });

  it('sets drag-over state while dragging over', () => {
    const dropzone = fixture.nativeElement.querySelector('.image-dropzone') as HTMLElement;

    dropzone.dispatchEvent(new DragEvent('dragover', { bubbles: true, cancelable: true }));
    fixture.detectChanges();

    expect(component.isDragOver).toBeTrue();
    expect(dropzone.classList.contains('drag-over')).toBeTrue();

    dropzone.dispatchEvent(new DragEvent('dragleave', { bubbles: true, cancelable: true }));
    fixture.detectChanges();

    expect(component.isDragOver).toBeFalse();
  });

  it('does not emit files when disabled', () => {
    component.disabled = true;
    fixture.detectChanges();
    spyOn(component.filesSelected, 'emit');

    const dropzone = fixture.nativeElement.querySelector('.image-dropzone') as HTMLElement;
    dropzone.dispatchEvent(new DragEvent('drop', {
      bubbles: true,
      cancelable: true,
      dataTransfer: { files: [createFile('blocked.jpg')] } as unknown as DataTransfer
    }));

    expect(component.filesSelected.emit).not.toHaveBeenCalled();
  });

  it('does not emit files while busy', () => {
    component.busy = true;
    fixture.detectChanges();
    spyOn(component.filesSelected, 'emit');

    const dropzone = fixture.nativeElement.querySelector('.image-dropzone') as HTMLElement;
    dropzone.dispatchEvent(new DragEvent('drop', {
      bubbles: true,
      cancelable: true,
      dataTransfer: { files: [createFile('busy.jpg')] } as unknown as DataTransfer
    }));

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

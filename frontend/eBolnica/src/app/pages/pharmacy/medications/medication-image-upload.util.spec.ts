import { HttpEvent, HttpEventType, HttpResponse } from '@angular/common/http';
import { of, throwError } from 'rxjs';
import { MedicationImageDto } from '../../../models/medication-image.dto';
import {
  getMedicationImageUploadErrorMessage,
  uploadMedicationImagesSequentially
} from './medication-image-upload.util';

describe('medication-image-upload.util', () => {
  const medicationId = 42;

  function createImage(name: string, id: number): MedicationImageDto {
    return {
      id,
      medicationId,
      fileName: name,
      imageUrl: `/images/${name}`,
      isPrimary: false,
      sortOrder: id,
      uploadedAt: '2026-01-01T00:00:00Z'
    };
  }

  function createFile(name: string): File {
    return new File(['image'], name, { type: 'image/jpeg' });
  }

  function createUploadResponse(name: string, id: number) {
    return of(new HttpResponse({
      body: createImage(name, id)
    }));
  }

  it('uploads each file sequentially via uploadMedicationImage', (done) => {
    const files = [createFile('a.jpg'), createFile('b.jpg')];
    const calls: string[] = [];

    uploadMedicationImagesSequentially(medicationId, files, (_id, file) => {
      calls.push(file.name);
      return createUploadResponse(file.name, calls.length);
    }).subscribe(result => {
      expect(calls).toEqual(['a.jpg', 'b.jpg']);
      expect(result.uploaded.map(image => image.fileName)).toEqual(['a.jpg', 'b.jpg']);
      expect(result.errors).toEqual([]);
      done();
    });
  });

  it('reports upload progress from HttpEvent stream', (done) => {
    const files = [createFile('a.jpg')];
    const progressUpdates: number[] = [];
    const batchUpdates: number[] = [];

    uploadMedicationImagesSequentially(
      medicationId,
      files,
      () => of(
        { type: HttpEventType.UploadProgress, loaded: 40, total: 100 } as HttpEvent<MedicationImageDto>,
        new HttpResponse({ body: createImage('a.jpg', 1) })
      ),
      {
        onFileProgress: (_fileName, progressPercent) => progressUpdates.push(progressPercent),
        onBatchProgress: (overallPercent) => batchUpdates.push(overallPercent)
      }
    ).subscribe(() => {
      expect(progressUpdates).toEqual([40, 100]);
      expect(batchUpdates).toEqual([0, 40, 100, 100]);
      done();
    });
  });

  it('reports overall batch progress while uploading multiple files', (done) => {
    const files = [createFile('a.jpg'), createFile('b.jpg'), createFile('c.jpg')];
    const batchUpdates: number[] = [];

    uploadMedicationImagesSequentially(
      medicationId,
      files,
      (_id, file) => {
        if (file.name === 'a.jpg') {
          return of(
            { type: HttpEventType.UploadProgress, loaded: 100, total: 100 } as HttpEvent<MedicationImageDto>,
            new HttpResponse({ body: createImage('a.jpg', 1) })
          );
        }

        if (file.name === 'b.jpg') {
          return of(
            { type: HttpEventType.UploadProgress, loaded: 50, total: 100 } as HttpEvent<MedicationImageDto>,
            new HttpResponse({ body: createImage('b.jpg', 2) })
          );
        }

        return createUploadResponse('c.jpg', 3);
      },
      {
        onBatchProgress: (overallPercent) => batchUpdates.push(overallPercent)
      }
    ).subscribe(() => {
      expect(batchUpdates).toContain(33);
      expect(batchUpdates).toContain(50);
      expect(batchUpdates).toContain(100);
      done();
    });
  });

  it('continues uploading remaining files after a per-file failure', (done) => {
    const files = [createFile('bad.jpg'), createFile('good.jpg')];
    const started: string[] = [];
    const completed: string[] = [];

    uploadMedicationImagesSequentially(
      medicationId,
      files,
      (_id, file) => {
      if (file.name === 'bad.jpg') {
        return throwError(() => ({ status: 400, error: { message: 'Invalid image content.' } }));
      }
      return createUploadResponse(file.name, 2);
    },
      {
        onFileStart: (fileName) => started.push(fileName),
        onFileComplete: (fileName) => completed.push(fileName),
        onFileError: (fileName) => completed.push(`error:${fileName}`)
      }
    ).subscribe(result => {
      expect(started).toEqual(['bad.jpg', 'good.jpg']);
      expect(completed).toEqual(['error:bad.jpg', 'good.jpg']);
      expect(result.uploaded).toEqual([createImage('good.jpg', 2)]);
      expect(result.errors).toEqual([
        { fileName: 'bad.jpg', message: 'Invalid image content.' }
      ]);
      done();
    });
  });

  it('builds clear upload error messages', () => {
    expect(getMedicationImageUploadErrorMessage({ status: 403, error: 'Rejected' }, 'scan.jpg'))
      .toBe('Rejected');
    expect(getMedicationImageUploadErrorMessage({ status: 400, error: 'Uploaded image content is corrupted or unsupported.' }, 'bad.jpg'))
      .toBe('Uploaded image content is corrupted or unsupported.');
    expect(getMedicationImageUploadErrorMessage({ status: 500 }, 'x.jpg'))
      .toBe('Failed to upload "x.jpg". Please try again.');
  });
});

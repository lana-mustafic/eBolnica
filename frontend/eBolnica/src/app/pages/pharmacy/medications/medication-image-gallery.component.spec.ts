import { ComponentFixture, TestBed } from '@angular/core/testing';
import { NoopAnimationsModule } from '@angular/platform-browser/animations';
import { CdkDragDrop } from '@angular/cdk/drag-drop';
import { HttpEvent, HttpEventType, HttpResponse } from '@angular/common/http';
import { of, Subject, throwError } from 'rxjs';
import { MedicationImageGalleryComponent } from './medication-image-gallery.component';
import { PharmacyService } from '../../../shared/services/pharmacy/pharmacy.service';
import { AuthService } from '../../../shared/services/auth.service';
import { NotificationService } from '../../../shared/services/notification.service';
import { ConfirmDialogService } from '../../../shared/services/confirm-dialog.service';
import { MedicationImageDto } from '../../../models/medication-image.dto';

describe('MedicationImageGalleryComponent metadata', () => {
  let fixture: ComponentFixture<MedicationImageGalleryComponent>;
  let component: MedicationImageGalleryComponent;

  const imageWithMetadata: MedicationImageDto = {
    id: 1,
    medicationId: 10,
    fileName: 'optimized.jpg',
    imageUrl: '/uploads/medications/10/optimized.jpg',
    thumbnailUrl: '/uploads/medications/10/thumbnails/optimized.jpg',
    isPrimary: true,
    sortOrder: 0,
    uploadedAt: '2026-07-30T10:00:00.000Z',
    fileSizeBytes: 262144,
    width: 1920,
    height: 1080
  };

  beforeEach(async () => {
    const pharmacyService = jasmine.createSpyObj<PharmacyService>('PharmacyService', [
      'resolveMedicationImageUrl',
      'uploadMedicationImage',
      'setPrimaryMedicationImage',
      'deleteMedicationImage'
    ]);
    pharmacyService.resolveMedicationImageUrl.and.callFake((url: string) => `http://localhost:5000${url}`);

    const authService = jasmine.createSpyObj<AuthService>('AuthService', ['getUserType']);
    authService.getUserType.and.returnValue('Pharmacist');

    const notificationService = jasmine.createSpyObj<NotificationService>('NotificationService', ['success']);
    const confirmDialog = jasmine.createSpyObj<ConfirmDialogService>('ConfirmDialogService', ['confirm']);
    confirmDialog.confirm.and.returnValue(of(false));

    await TestBed.configureTestingModule({
      imports: [MedicationImageGalleryComponent, NoopAnimationsModule],
      providers: [
        { provide: PharmacyService, useValue: pharmacyService },
        { provide: AuthService, useValue: authService },
        { provide: NotificationService, useValue: notificationService },
        { provide: ConfirmDialogService, useValue: confirmDialog }
      ]
    }).compileComponents();

    fixture = TestBed.createComponent(MedicationImageGalleryComponent);
    component = fixture.componentInstance;
    component.medicationId = 10;
    component.images = [imageWithMetadata];
  });

  it('displays file size and dimensions from DTO for selected image', () => {
    fixture.detectChanges();

    const metadata = fixture.nativeElement.querySelector('.image-metadata');
    expect(metadata).toBeTruthy();
    expect(metadata.textContent).toContain('Stored dimensions');
    expect(metadata.textContent).toContain('1920 × 1080 px');
    expect(metadata.textContent).toContain('Stored file size');
    expect(metadata.textContent).toContain('256.0 KB');
    expect(metadata.textContent).toContain('Stored optimized image');
  });

  it('hides metadata panel when DTO has no metadata fields', () => {
    component.images = [{
      ...imageWithMetadata,
      fileSizeBytes: undefined,
      width: undefined,
      height: undefined
    }];

    fixture.detectChanges();

    expect(fixture.nativeElement.querySelector('.image-metadata')).toBeFalsy();
  });

  it('updates metadata when another image is selected', () => {
    component.images = [
      imageWithMetadata,
      {
        ...imageWithMetadata,
        id: 2,
        isPrimary: false,
        fileSizeBytes: 1024,
        width: 128,
        height: 128
      }
    ];

    fixture.detectChanges();
    component.selectImage(1);
    fixture.detectChanges();

    const metadata = fixture.nativeElement.querySelector('.image-metadata');
    expect(metadata.textContent).toContain('128 × 128 px');
    expect(metadata.textContent).toContain('1.0 KB');
  });

  it('shows upload optimization helper text', () => {
    fixture.detectChanges();

    const helper = fixture.nativeElement.querySelector('.gallery-upload-helper');
    expect(helper?.textContent?.trim()).toBe('Images are automatically optimized on upload (max 1920×1920).');
  });
});

describe('MedicationImageGalleryComponent reorder', () => {
  let fixture: ComponentFixture<MedicationImageGalleryComponent>;
  let component: MedicationImageGalleryComponent;
  let pharmacyService: jasmine.SpyObj<PharmacyService>;

  function createImage(id: number, sortOrder: number, isPrimary = false): MedicationImageDto {
    return {
      id,
      medicationId: 10,
      fileName: `image-${id}.jpg`,
      imageUrl: `/uploads/medications/10/${id}.jpg`,
      thumbnailUrl: `/uploads/medications/10/thumbnails/${id}.jpg`,
      isPrimary,
      sortOrder,
      uploadedAt: '2026-07-30T10:00:00.000Z'
    };
  }

  beforeEach(async () => {
    pharmacyService = jasmine.createSpyObj<PharmacyService>('PharmacyService', [
      'resolveMedicationImageUrl',
      'uploadMedicationImage',
      'setPrimaryMedicationImage',
      'deleteMedicationImage',
      'reorderMedicationImages'
    ]);
    pharmacyService.resolveMedicationImageUrl.and.callFake((url: string) => `http://localhost:5000${url}`);
    pharmacyService.reorderMedicationImages.and.returnValue(of([]));

    const authService = jasmine.createSpyObj<AuthService>('AuthService', ['getUserType']);
    authService.getUserType.and.returnValue('Pharmacist');

    const notificationService = jasmine.createSpyObj<NotificationService>('NotificationService', ['success']);
    const confirmDialog = jasmine.createSpyObj<ConfirmDialogService>('ConfirmDialogService', ['confirm']);
    confirmDialog.confirm.and.returnValue(of(false));

    await TestBed.configureTestingModule({
      imports: [MedicationImageGalleryComponent, NoopAnimationsModule],
      providers: [
        { provide: PharmacyService, useValue: pharmacyService },
        { provide: AuthService, useValue: authService },
        { provide: NotificationService, useValue: notificationService },
        { provide: ConfirmDialogService, useValue: confirmDialog }
      ]
    }).compileComponents();

    fixture = TestBed.createComponent(MedicationImageGalleryComponent);
    component = fixture.componentInstance;
    component.medicationId = 10;
    component.images = [
      createImage(1, 0, true),
      createImage(2, 1),
      createImage(3, 2)
    ];
  });

  function createDropEvent(previousIndex: number, currentIndex: number): CdkDragDrop<MedicationImageDto[]> {
    return {
      previousIndex,
      currentIndex,
      item: {} as CdkDragDrop<MedicationImageDto[]>['item'],
      container: {} as CdkDragDrop<MedicationImageDto[]>['container'],
      previousContainer: {} as CdkDragDrop<MedicationImageDto[]>['previousContainer'],
      isPointerOverContainer: true,
      distance: { x: 0, y: 0 },
      dropPoint: { x: 0, y: 0 },
      event: {} as MouseEvent
    };
  }

  it('shows reorder helper text when gallery has multiple images', () => {
    fixture.detectChanges();

    const helper = fixture.nativeElement.querySelector('.thumbnail-strip-helper');
    expect(helper?.textContent?.trim()).toBe('Drag thumbnails to reorder.');
  });

  it('reorders thumbnails optimistically and persists via API', () => {
    const serverImages = [
      createImage(3, 0),
      createImage(1, 1, true),
      createImage(2, 2)
    ];
    pharmacyService.reorderMedicationImages.and.returnValue(of(serverImages));
    const emitted: MedicationImageDto[][] = [];
    component.imagesChange.subscribe(images => emitted.push(images));

    component.onThumbnailDrop(createDropEvent(2, 0));

    expect(component.images.map(image => image.id)).toEqual([3, 1, 2]);
    expect(pharmacyService.reorderMedicationImages).toHaveBeenCalledWith(10, [3, 1, 2]);
    expect(emitted[0].map(image => image.id)).toEqual([3, 1, 2]);
    expect(emitted[emitted.length - 1].map(image => image.id)).toEqual([3, 1, 2]);
    expect(component.isReordering).toBeFalse();
  });

  it('reverts gallery order and shows error when reorder API fails', () => {
    pharmacyService.reorderMedicationImages.and.returnValue(
      throwError(() => ({ status: 500 }))
    );
    const emitted: MedicationImageDto[][] = [];
    component.imagesChange.subscribe(images => emitted.push(images));

    component.onThumbnailDrop(createDropEvent(2, 0));

    expect(component.images.map(image => image.id)).toEqual([1, 2, 3]);
    expect(component.images.map(image => image.sortOrder)).toEqual([0, 1, 2]);
    expect(component.selectedIndex).toBe(0);
    expect(component.images.find(image => image.id === 1)?.isPrimary).toBeTrue();
    expect(emitted[0].map(image => image.id)).toEqual([3, 1, 2]);
    expect(emitted[emitted.length - 1].map(image => image.id)).toEqual([1, 2, 3]);
    expect(component.errorMessage).toContain('restored to its previous order');
    expect(component.isReordering).toBeFalse();
  });

  it('restores previous selection when reorder API fails after moving selected thumbnail', () => {
    pharmacyService.reorderMedicationImages.and.returnValue(
      throwError(() => ({ status: 400, error: 'Invalid image order.' }))
    );

    component.selectedIndex = 1;
    component.onThumbnailDrop(createDropEvent(1, 0));

    expect(component.images.map(image => image.id)).toEqual([1, 2, 3]);
    expect(component.selectedIndex).toBe(1);
    expect(component.errorMessage).toContain('Invalid image order.');
  });

  it('does not reorder when drag ends at the same position', () => {
    const originalOrder = component.images.map(image => image.id);

    component.onThumbnailDrop(createDropEvent(1, 1));

    expect(component.images.map(image => image.id)).toEqual(originalOrder);
  });
});

describe('MedicationImageGalleryComponent upload progress', () => {
  let fixture: ComponentFixture<MedicationImageGalleryComponent>;
  let component: MedicationImageGalleryComponent;
  let pharmacyService: jasmine.SpyObj<PharmacyService>;

  function createImage(id: number): MedicationImageDto {
    return {
      id,
      medicationId: 10,
      fileName: `image-${id}.jpg`,
      imageUrl: `/uploads/medications/10/${id}.jpg`,
      isPrimary: id === 1,
      sortOrder: id - 1,
      uploadedAt: '2026-07-30T10:00:00.000Z'
    };
  }

  function createFile(name: string): File {
    return new File(['image'], name, { type: 'image/jpeg' });
  }

  beforeEach(async () => {
    pharmacyService = jasmine.createSpyObj<PharmacyService>('PharmacyService', [
      'resolveMedicationImageUrl',
      'uploadMedicationImage',
      'getMedicationImages',
      'setPrimaryMedicationImage',
      'deleteMedicationImage',
      'reorderMedicationImages'
    ]);
    pharmacyService.resolveMedicationImageUrl.and.callFake((url: string) => `http://localhost:5000${url}`);
    pharmacyService.getMedicationImages.and.returnValue(of([]));

    const authService = jasmine.createSpyObj<AuthService>('AuthService', ['getUserType']);
    authService.getUserType.and.returnValue('Pharmacist');

    const notificationService = jasmine.createSpyObj<NotificationService>('NotificationService', ['success']);
    const confirmDialog = jasmine.createSpyObj<ConfirmDialogService>('ConfirmDialogService', ['confirm']);
    confirmDialog.confirm.and.returnValue(of(false));

    await TestBed.configureTestingModule({
      imports: [MedicationImageGalleryComponent, NoopAnimationsModule],
      providers: [
        { provide: PharmacyService, useValue: pharmacyService },
        { provide: AuthService, useValue: authService },
        { provide: NotificationService, useValue: notificationService },
        { provide: ConfirmDialogService, useValue: confirmDialog }
      ]
    }).compileComponents();

    fixture = TestBed.createComponent(MedicationImageGalleryComponent);
    component = fixture.componentInstance;
    component.medicationId = 10;
    component.images = [createImage(1)];
  });

  it('shows batch and per-file progress bars during upload', () => {
    const upload$ = new Subject<HttpEvent<MedicationImageDto>>();
    pharmacyService.uploadMedicationImage.and.returnValue(upload$.asObservable());

    component.onDropzoneFilesSelected([createFile('new.jpg')]);
    upload$.next({ type: HttpEventType.UploadProgress, loaded: 50, total: 100 } as HttpEvent<MedicationImageDto>);
    fixture.detectChanges();

    expect(component.isUploading).toBeTrue();
    expect(component.batchUploadProgress).toBe(50);
    expect(fixture.nativeElement.querySelector('.gallery-upload-batch-progress')).toBeTruthy();
    expect(fixture.nativeElement.querySelector('.gallery-upload-status-list')).toBeTruthy();

    upload$.next(new HttpResponse({ body: createImage(2) }));
    upload$.complete();
    pharmacyService.getMedicationImages.and.returnValue(of([createImage(1), createImage(2)]));
    fixture.detectChanges();
  });

  it('hides progress UI and refreshes gallery after successful upload', () => {
    const refreshedImages = [createImage(1), createImage(2)];
    pharmacyService.uploadMedicationImage.and.returnValue(of(
      { type: HttpEventType.UploadProgress, loaded: 100, total: 100 } as HttpEvent<MedicationImageDto>,
      new HttpResponse({ body: createImage(2) })
    ));
    pharmacyService.getMedicationImages.and.returnValue(of(refreshedImages));

    component.onDropzoneFilesSelected([createFile('new.jpg')]);
    fixture.detectChanges();
    fixture.detectChanges();

    expect(component.isUploading).toBeFalse();
    expect(component.uploadFileStatuses).toEqual([]);
    expect(component.batchUploadProgress).toBe(0);
    expect(pharmacyService.getMedicationImages).toHaveBeenCalledWith(10);
    expect(component.images).toEqual(refreshedImages);
    expect(fixture.nativeElement.querySelector('.gallery-upload-batch-progress')).toBeFalsy();
  });

  it('keeps error progress visible and allows retry', () => {
    pharmacyService.uploadMedicationImage.and.returnValues(
      throwError(() => ({ status: 500 })),
      of(
        { type: HttpEventType.UploadProgress, loaded: 100, total: 100 } as HttpEvent<MedicationImageDto>,
        new HttpResponse({ body: createImage(2) })
      )
    );
    pharmacyService.getMedicationImages.and.returnValue(of([createImage(1), createImage(2)]));

    component.onDropzoneFilesSelected([createFile('retry-me.jpg')]);
    fixture.detectChanges();

    expect(component.uploadFileStatuses.length).toBe(1);
    expect(component.uploadFileStatuses[0].status).toBe('error');
    expect(fixture.nativeElement.querySelector('.gallery-upload-retry-btn')).toBeTruthy();

    component.retryFailedUpload('retry-me.jpg');
    fixture.detectChanges();
    fixture.detectChanges();

    expect(pharmacyService.uploadMedicationImage).toHaveBeenCalledTimes(2);
    expect(component.uploadFileStatuses).toEqual([]);
  });
});

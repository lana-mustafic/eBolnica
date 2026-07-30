import { ComponentFixture, TestBed } from '@angular/core/testing';
import { NoopAnimationsModule } from '@angular/platform-browser/animations';
import { CdkDragDrop } from '@angular/cdk/drag-drop';
import { of, throwError } from 'rxjs';
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

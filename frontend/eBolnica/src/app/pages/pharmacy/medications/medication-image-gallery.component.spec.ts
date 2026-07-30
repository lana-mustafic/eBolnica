import { ComponentFixture, TestBed } from '@angular/core/testing';
import { NoopAnimationsModule } from '@angular/platform-browser/animations';
import { of } from 'rxjs';
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

import { ComponentFixture, TestBed } from '@angular/core/testing';
import {
  LIGHTBOX_DEFAULT_ZOOM_STATE,
  LIGHTBOX_MAX_ZOOM,
  LIGHTBOX_MIN_ZOOM,
  MedicationImageLightboxComponent
} from './medication-image-lightbox.component';
import { MedicationImageDto } from '../../../models/medication-image.dto';

describe('MedicationImageLightboxComponent zoom state', () => {
  let component: MedicationImageLightboxComponent;
  let fixture: ComponentFixture<MedicationImageLightboxComponent>;

  const images: MedicationImageDto[] = [
    {
      id: 1,
      medicationId: 10,
      imageUrl: '/a.jpg',
      isPrimary: true,
      sortOrder: 0,
      uploadedAt: '2026-01-01T00:00:00Z'
    },
    {
      id: 2,
      medicationId: 10,
      imageUrl: '/b.jpg',
      isPrimary: false,
      sortOrder: 1,
      uploadedAt: '2026-01-02T00:00:00Z'
    }
  ];

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [MedicationImageLightboxComponent]
    }).compileComponents();

    fixture = TestBed.createComponent(MedicationImageLightboxComponent);
    component = fixture.componentInstance;
    component.isOpen = true;
    component.images = images;
    component.currentIndex = 0;
    component.resolveUrl = url => url;
  });

  it('starts at default 100% zoom with no pan offset', () => {
    expect(component.zoomState).toEqual(LIGHTBOX_DEFAULT_ZOOM_STATE);
    expect(component.isZoomed).toBeFalse();
    expect(component.imageTransform).toBe('translate(0px, 0px) scale(1)');
  });

  it('exposes configured min and max zoom constants', () => {
    expect(LIGHTBOX_MIN_ZOOM).toBe(1);
    expect(LIGHTBOX_MAX_ZOOM).toBe(3);
  });

  it('resetZoom restores default scale and translate', () => {
    component.zoomScale = 2;
    component.zoomTranslateX = 40;
    component.zoomTranslateY = -20;

    component.resetZoom();

    expect(component.zoomState).toEqual(LIGHTBOX_DEFAULT_ZOOM_STATE);
    expect(component.isZoomed).toBeFalse();
  });

  it('resets zoom when navigating to another image', () => {
    component.zoomScale = LIGHTBOX_MAX_ZOOM;
    component.zoomTranslateX = 10;
    spyOn(component.indexChange, 'emit');

    component.next();

    expect(component.zoomState).toEqual(LIGHTBOX_DEFAULT_ZOOM_STATE);
    expect(component.indexChange.emit).toHaveBeenCalledWith(1);
  });

  it('resets zoom when currentIndex input changes', () => {
    fixture.detectChanges();
    component.zoomScale = 2.5;

    component.currentIndex = 1;
    component.ngOnChanges({
      currentIndex: {
        previousValue: 0,
        currentValue: 1,
        firstChange: false,
        isFirstChange: () => false
      }
    });

    expect(component.zoomScale).toBe(LIGHTBOX_MIN_ZOOM);
  });

  it('resets zoom when lightbox closes', () => {
    component.zoomScale = 1.5;

    component.isOpen = false;
    component.ngOnChanges({
      isOpen: {
        previousValue: true,
        currentValue: false,
        firstChange: false,
        isFirstChange: () => false
      }
    });

    expect(component.zoomState).toEqual(LIGHTBOX_DEFAULT_ZOOM_STATE);
  });
});

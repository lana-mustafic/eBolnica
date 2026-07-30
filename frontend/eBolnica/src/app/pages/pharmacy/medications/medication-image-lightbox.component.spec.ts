import { ComponentFixture, TestBed } from '@angular/core/testing';
import {
  LIGHTBOX_DEFAULT_ZOOM_STATE,
  LIGHTBOX_MAX_ZOOM,
  LIGHTBOX_MIN_ZOOM,
  LIGHTBOX_ZOOM_STEP,
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

describe('MedicationImageLightboxComponent zoom controls', () => {
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
    fixture.detectChanges();
  });

  it('renders zoom toolbar buttons and current zoom level', () => {
    const element = fixture.nativeElement as HTMLElement;

    expect(element.querySelector('.lightbox-toolbar')).toBeTruthy();
    expect(element.querySelector('[aria-label="Zoom in"]')).toBeTruthy();
    expect(element.querySelector('[aria-label="Zoom out"]')).toBeTruthy();
    expect(element.querySelector('[aria-label="Reset zoom"]')).toBeTruthy();
    expect(element.querySelector('.lightbox-zoom-level')?.textContent?.trim()).toBe('100%');
  });

  it('zoomIn increases scale by step up to max', () => {
    component.zoomIn();
    expect(component.zoomScale).toBe(1 + LIGHTBOX_ZOOM_STEP);
    expect(component.zoomPercentLabel).toBe('125%');
    expect(component.canZoomOut).toBeTrue();
    expect(component.isZoomed).toBeTrue();

    component.zoomScale = LIGHTBOX_MAX_ZOOM - 0.1;
    component.zoomIn();
    expect(component.zoomScale).toBe(LIGHTBOX_MAX_ZOOM);
    expect(component.canZoomIn).toBeFalse();
  });

  it('zoomOut decreases scale by step and clears pan at 100%', () => {
    component.zoomScale = 1.5;
    component.zoomTranslateX = 20;
    component.zoomTranslateY = 10;

    component.zoomOut();
    expect(component.zoomScale).toBe(1.25);
    expect(component.zoomTranslateX).toBe(20);

    component.zoomOut();
    expect(component.zoomScale).toBe(LIGHTBOX_MIN_ZOOM);
    expect(component.zoomTranslateX).toBe(0);
    expect(component.zoomTranslateY).toBe(0);
    expect(component.canZoomOut).toBeFalse();
  });

  it('resetZoom via toolbar restores default zoom state', () => {
    component.zoomScale = 2;
    component.zoomTranslateX = 30;

    component.resetZoom();

    expect(component.zoomState).toEqual(LIGHTBOX_DEFAULT_ZOOM_STATE);
    expect(component.isZoomed).toBeFalse();
  });

  it('applies transform style to the lightbox image', () => {
    component.zoomScale = 1.5;
    fixture.detectChanges();

    const image = fixture.nativeElement.querySelector('.lightbox-image') as HTMLElement;
    expect(image.style.transform).toBe('translate(0px, 0px) scale(1.5)');
  });
});

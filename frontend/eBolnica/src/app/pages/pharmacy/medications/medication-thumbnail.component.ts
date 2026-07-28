import {
  Component,
  Input,
  ElementRef,
  AfterViewInit,
  OnDestroy,
  OnChanges,
  SimpleChanges,
  inject
} from '@angular/core';
import { CommonModule } from '@angular/common';
import { PharmacyService } from '../../../shared/services/pharmacy/pharmacy.service';

@Component({
  selector: 'app-medication-thumbnail',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './medication-thumbnail.component.html',
  styleUrl: './medication-thumbnail.component.css'
})
export class MedicationThumbnailComponent implements AfterViewInit, OnDestroy, OnChanges {
  private elementRef = inject(ElementRef);
  private pharmacyService = inject(PharmacyService);

  @Input({ required: true }) medicationId!: number;
  @Input() imageUrl?: string;
  @Input() alt = 'Medication';

  resolvedUrl: string | null = null;
  shouldLoad = false;
  isLoaded = false;
  hasError = false;

  private observer: IntersectionObserver | null = null;

  ngOnChanges(changes: SimpleChanges): void {
    if (changes['imageUrl'] || changes['medicationId']) {
      this.resetState();
      this.resolvedUrl = this.pharmacyService.resolveMedicationImageUrl(this.imageUrl);
      if (this.resolvedUrl) {
        this.setupObserver();
      }
    }
  }

  ngAfterViewInit(): void {
    this.resolvedUrl = this.pharmacyService.resolveMedicationImageUrl(this.imageUrl);
    if (this.resolvedUrl) {
      this.setupObserver();
    }
  }

  ngOnDestroy(): void {
    this.disconnectObserver();
  }

  onLoad(): void {
    this.isLoaded = true;
  }

  onError(): void {
    this.hasError = true;
    this.isLoaded = false;
  }

  private resetState(): void {
    this.shouldLoad = false;
    this.isLoaded = false;
    this.hasError = false;
    this.disconnectObserver();
  }

  private setupObserver(): void {
    this.disconnectObserver();

    if (typeof IntersectionObserver === 'undefined') {
      this.shouldLoad = true;
      return;
    }

    this.observer = new IntersectionObserver(
      ([entry]) => {
        if (entry.isIntersecting) {
          this.shouldLoad = true;
          this.disconnectObserver();
        }
      },
      { rootMargin: '100px' }
    );

    this.observer.observe(this.elementRef.nativeElement);
  }

  private disconnectObserver(): void {
    this.observer?.disconnect();
    this.observer = null;
  }
}

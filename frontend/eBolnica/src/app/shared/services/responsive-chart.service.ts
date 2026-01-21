import { Injectable, inject, PLATFORM_ID } from '@angular/core';
import { isPlatformBrowser } from '@angular/common';
import { BehaviorSubject, Observable, fromEvent, debounceTime, map, startWith } from 'rxjs';

/**
 * Breakpoint definitions for responsive design
 */
export const BREAKPOINTS = {
  mobile: 768,    // < 768px
  tablet: 1024,   // 768px - 1024px
  desktop: 1025   // > 1024px
} as const;

export type BreakpointType = 'mobile' | 'tablet' | 'desktop';

/**
 * Responsive Chart Service
 * 
 * Provides breakpoint detection and responsive utilities for chart components.
 * Handles window resize events with debouncing and provides reactive breakpoint updates.
 * 
 * Features:
 * - Breakpoint detection (mobile, tablet, desktop)
 * - Window resize handling with debouncing
 * - Orientation change detection
 * - Touch device detection
 * - Performance optimizations
 */
@Injectable({
  providedIn: 'root'
})
export class ResponsiveChartService {
  private platformId = inject(PLATFORM_ID);
  private isBrowser = isPlatformBrowser(this.platformId);
  
  private currentBreakpoint$ = new BehaviorSubject<BreakpointType>('desktop');
  private isMobile$ = new BehaviorSubject<boolean>(false);
  private isTablet$ = new BehaviorSubject<boolean>(false);
  private isDesktop$ = new BehaviorSubject<boolean>(true);
  private isTouchDevice$ = new BehaviorSubject<boolean>(false);
  private isPortrait$ = new BehaviorSubject<boolean>(true);

  constructor() {
    if (this.isBrowser) {
      this.initialize();
    }
  }

  /**
   * Initialize responsive service
   */
  private initialize(): void {
    // Detect initial state
    this.updateBreakpoints();
    this.detectTouchDevice();
    this.detectOrientation();

    // Listen to window resize events with debouncing
    fromEvent(window, 'resize')
      .pipe(
        debounceTime(200),
        startWith(null)
      )
      .subscribe(() => {
        this.updateBreakpoints();
        this.detectOrientation();
      });

    // Listen to orientation changes
    fromEvent(window, 'orientationchange')
      .pipe(debounceTime(300))
      .subscribe(() => {
        setTimeout(() => {
          this.updateBreakpoints();
          this.detectOrientation();
        }, 100);
      });
  }

  /**
   * Update breakpoint states based on current window size
   */
  private updateBreakpoints(): void {
    if (!this.isBrowser) return;

    const width = window.innerWidth;
    const isMobile = width < BREAKPOINTS.mobile;
    const isTablet = width >= BREAKPOINTS.mobile && width < BREAKPOINTS.desktop;
    const isDesktop = width >= BREAKPOINTS.desktop;

    let breakpoint: BreakpointType = 'desktop';
    if (isMobile) {
      breakpoint = 'mobile';
    } else if (isTablet) {
      breakpoint = 'tablet';
    }

    this.currentBreakpoint$.next(breakpoint);
    this.isMobile$.next(isMobile);
    this.isTablet$.next(isTablet);
    this.isDesktop$.next(isDesktop);
  }

  /**
   * Detect if device supports touch
   */
  private detectTouchDevice(): void {
    if (!this.isBrowser) {
      this.isTouchDevice$.next(false);
      return;
    }

    const isTouch = 'ontouchstart' in window || 
                   navigator.maxTouchPoints > 0 ||
                   (navigator as any).msMaxTouchPoints > 0;
    
    this.isTouchDevice$.next(isTouch);
  }

  /**
   * Detect device orientation
   */
  private detectOrientation(): void {
    if (!this.isBrowser) {
      this.isPortrait$.next(true);
      return;
    }

    const isPortrait = window.innerHeight > window.innerWidth;
    this.isPortrait$.next(isPortrait);
  }

  /**
   * Get current breakpoint as Observable
   */
  getBreakpoint(): Observable<BreakpointType> {
    return this.currentBreakpoint$.asObservable();
  }

  /**
   * Get current breakpoint value
   */
  getCurrentBreakpoint(): BreakpointType {
    return this.currentBreakpoint$.value;
  }

  /**
   * Check if current viewport is mobile
   */
  isMobile(): Observable<boolean> {
    return this.isMobile$.asObservable();
  }

  /**
   * Check if current viewport is mobile (synchronous)
   */
  isMobileSync(): boolean {
    return this.isMobile$.value;
  }

  /**
   * Check if current viewport is tablet
   */
  isTablet(): Observable<boolean> {
    return this.isTablet$.asObservable();
  }

  /**
   * Check if current viewport is tablet (synchronous)
   */
  isTabletSync(): boolean {
    return this.isTablet$.value;
  }

  /**
   * Check if current viewport is desktop
   */
  isDesktop(): Observable<boolean> {
    return this.isDesktop$.asObservable();
  }

  /**
   * Check if current viewport is desktop (synchronous)
   */
  isDesktopSync(): boolean {
    return this.isDesktop$.value;
  }

  /**
   * Check if device supports touch
   */
  isTouchDevice(): Observable<boolean> {
    return this.isTouchDevice$.asObservable();
  }

  /**
   * Check if device supports touch (synchronous)
   */
  isTouchDeviceSync(): boolean {
    return this.isTouchDevice$.value;
  }

  /**
   * Check if device is in portrait orientation
   */
  isPortrait(): Observable<boolean> {
    return this.isPortrait$.asObservable();
  }

  /**
   * Check if device is in portrait orientation (synchronous)
   */
  isPortraitSync(): boolean {
    return this.isPortrait$.value;
  }

  /**
   * Get responsive font size multiplier
   * Returns smaller multiplier for mobile devices
   */
  getFontSizeMultiplier(): number {
    if (this.isMobileSync()) {
      return 0.75; // 25% reduction
    } else if (this.isTabletSync()) {
      return 0.9; // 10% reduction
    }
    return 1.0; // Full size
  }

  /**
   * Get responsive chart height
   * Adjusts height based on viewport
   */
  getResponsiveHeight(defaultHeight: number): number {
    if (this.isMobileSync()) {
      return Math.min(defaultHeight * 0.7, 300); // Max 300px on mobile
    } else if (this.isTabletSync()) {
      return defaultHeight * 0.85;
    }
    return defaultHeight;
  }

  /**
   * Get responsive padding
   * Reduces padding on mobile devices
   */
  getResponsivePadding(defaultPadding: number): number {
    if (this.isMobileSync()) {
      return defaultPadding * 0.5;
    } else if (this.isTabletSync()) {
      return defaultPadding * 0.75;
    }
    return defaultPadding;
  }

  /**
   * Get optimal number of data points for mobile
   * Reduces data points for better performance on small screens
   */
  getOptimalDataPoints(totalPoints: number): number {
    if (this.isMobileSync()) {
      return Math.min(totalPoints, 12); // Max 12 points on mobile
    } else if (this.isTabletSync()) {
      return Math.min(totalPoints, 20); // Max 20 points on tablet
    }
    return totalPoints;
  }

  /**
   * Check if animations should be reduced
   * Disables complex animations on low-end devices
   */
  shouldReduceAnimations(): boolean {
    if (!this.isBrowser) return false;
    
    // Check for low-end device indicators
    const isLowEnd = this.isMobileSync() && 
                     (navigator.hardwareConcurrency || 4) < 4;
    
    return isLowEnd;
  }
}

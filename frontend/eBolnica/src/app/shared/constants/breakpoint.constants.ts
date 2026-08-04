/**
 * Breakpoint Constants
 * 
 * Shared breakpoint definitions for responsive design across the application.
 * Used by chart components and other responsive features.
 */

export const BREAKPOINTS = {
  /** Mobile breakpoint: screens smaller than 768px */
  mobile: 768,
  
  /** Tablet breakpoint: screens between 768px and 1024px */
  tablet: 1024,
  
  /** Desktop breakpoint: screens larger than 1024px */
  desktop: 1025
} as const;

export type BreakpointType = 'mobile' | 'tablet' | 'desktop';

/**
 * Media query strings for CSS
 */
export const MEDIA_QUERIES = {
  mobile: `(max-width: ${BREAKPOINTS.mobile - 1}px)`,
  tablet: `(min-width: ${BREAKPOINTS.mobile}px) and (max-width: ${BREAKPOINTS.tablet}px)`,
  desktop: `(min-width: ${BREAKPOINTS.desktop}px)`,
  tabletAndUp: `(min-width: ${BREAKPOINTS.mobile}px)`,
  tabletAndDown: `(max-width: ${BREAKPOINTS.tablet}px)`
} as const;

/**
 * Touch target minimum sizes (in pixels)
 * Based on WCAG accessibility guidelines
 */
export const TOUCH_TARGETS = {
  minimum: 44,      // Minimum touch target size
  recommended: 48,  // Recommended touch target size
  spacing: 8        // Minimum spacing between touch targets
} as const;

/**
 * Responsive font size multipliers
 */
export const FONT_MULTIPLIERS = {
  mobile: 0.75,    // 25% reduction on mobile
  tablet: 0.9,     // 10% reduction on tablet
  desktop: 1.0     // Full size on desktop
} as const;

/**
 * Chart-specific responsive settings
 */
export const CHART_RESPONSIVE = {
  mobile: {
    maxDataPoints: 12,
    minHeight: 250,
    maxHeight: 300,
    fontSizeMultiplier: 0.75,
    paddingMultiplier: 0.5,
    animationDuration: 0 // Disable animations
  },
  tablet: {
    maxDataPoints: 20,
    minHeight: 300,
    maxHeight: 400,
    fontSizeMultiplier: 0.9,
    paddingMultiplier: 0.75,
    animationDuration: 500
  },
  desktop: {
    maxDataPoints: Infinity,
    minHeight: 400,
    maxHeight: Infinity,
    fontSizeMultiplier: 1.0,
    paddingMultiplier: 1.0,
    animationDuration: 1000
  }
} as const;

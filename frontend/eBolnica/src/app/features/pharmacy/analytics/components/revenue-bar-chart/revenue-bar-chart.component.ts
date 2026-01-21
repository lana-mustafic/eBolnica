import { Component, OnInit, OnChanges, SimpleChanges, Input, Output, EventEmitter, inject, OnDestroy, ViewChild } from '@angular/core';
import { CommonModule } from '@angular/common';
import { BaseChartDirective } from 'ng2-charts';
import { ChartConfiguration, ChartData, ChartType } from 'chart.js';
import { PharmacyService } from '../../../../../shared/services/pharmacy/pharmacy.service';
import { ResponsiveChartService, BreakpointType } from '../../../../../shared/services/responsive-chart.service';
import { MonthlyRevenueData, AnalyticsPeriod } from '../../../../../models/analytics.dto';
import { Subscription } from 'rxjs';

/**
 * Revenue Bar Chart Component
 * 
 * A reusable bar chart component for displaying monthly revenue data.
 * Integrates with PharmacyService analytics API to fetch and display revenue trends.
 * 
 * Features:
 * - Fetches data from analytics service on initialization
 * - Displays loading, error, and empty states
 * - Responsive design with customizable height
 * - Currency formatting on y-axis and tooltips
 * - Accessible with ARIA labels
 * - Follows pharmacy design system
 * 
 * @example
 * ```html
 * <app-revenue-bar-chart 
 *   [title]="'Monthly Revenue Overview'"
 *   [height]="300"
 *   [period]="'last12months'">
 * </app-revenue-bar-chart>
 * ```
 */
@Component({
  selector: 'app-revenue-bar-chart',
  standalone: true,
  imports: [CommonModule, BaseChartDirective],
  templateUrl: './revenue-bar-chart.component.html',
  styleUrl: './revenue-bar-chart.component.scss'
})
export class RevenueBarChartComponent implements OnInit, OnChanges, OnDestroy {
  @ViewChild(BaseChartDirective) chart?: BaseChartDirective;
  
  private pharmacyService = inject(PharmacyService);
  private responsiveService = inject(ResponsiveChartService);
  private subscriptions = new Subscription();
  
  currentBreakpoint: BreakpointType = 'desktop';
  responsiveHeight: number = 400;

  // Input properties
  /** Chart title displayed above the chart */
  @Input() title: string = 'Monthly Revenue';
  
  /** Chart container height in pixels */
  @Input() height: number = 400;
  
  /** Period for revenue data (default: last12months) */
  @Input() period: AnalyticsPeriod = 'last12months';
  
  /** Custom date range (optional, overrides period if provided) */
  @Input() dateRange?: { startDate: Date; endDate: Date };
  
  /** Whether to use cached data (default: true) */
  @Input() useCache: boolean = true;
  
  /** Custom bar color (default: pharmacy blue #3b82f6) */
  @Input() barColor: string = '#3b82f6';
  
  /** Custom bar hover color */
  @Input() barHoverColor: string = '#2563eb';

  // Output events
  /** Emitted when chart data is loaded successfully */
  @Output() dataLoaded = new EventEmitter<MonthlyRevenueData[]>();
  
  /** Emitted when an error occurs */
  @Output() errorOccurred = new EventEmitter<Error>();
  
  /** Emitted when a bar is clicked */
  @Output() barClicked = new EventEmitter<{ month: string; revenue: number }>();

  // Component state
  isLoading: boolean = false;
  errorMessage: string | null = null;
  hasData: boolean = false;
  
  // Chart data
  monthlyRevenue: MonthlyRevenueData[] = [];
  
  // Chart configuration
  chartType: ChartType = 'bar';
  
  chartData: ChartData<'bar'> = {
    labels: [],
    datasets: []
  };

  chartOptions: ChartConfiguration<'bar'>['options'] = {
    responsive: true,
    maintainAspectRatio: false,
    animation: {
      duration: 1000,
      easing: 'easeInOutQuart'
    },
    plugins: {
      legend: {
        display: true,
        position: 'top',
        labels: {
          usePointStyle: true,
          padding: 15,
          font: {
            size: 12,
            family: "'Inter', 'Segoe UI', sans-serif"
          }
        }
      },
      tooltip: {
        enabled: true,
        backgroundColor: 'rgba(0, 0, 0, 0.8)',
        padding: 12,
        titleFont: {
          size: 14,
          weight: '600',
          family: "'Inter', 'Segoe UI', sans-serif"
        },
        bodyFont: {
          size: 13,
          family: "'Inter', 'Segoe UI', sans-serif"
        },
        callbacks: {
          label: (context) => {
            const value = context.parsed.y;
            return `Revenue: $${value.toLocaleString('en-US', { 
              minimumFractionDigits: 2, 
              maximumFractionDigits: 2 
            })}`;
          },
          title: (context) => {
            return context[0].label || 'Unknown Month';
          }
        }
      },
      title: {
        display: false // We'll show title in template
      }
    },
    scales: {
      x: {
        grid: {
          display: true,
          color: 'rgba(0, 0, 0, 0.05)',
          drawBorder: false
        },
        ticks: {
          font: {
            size: 11,
            family: "'Inter', 'Segoe UI', sans-serif"
          },
          color: '#718096'
        }
      },
      y: {
        beginAtZero: true,
        grid: {
          display: true,
          color: 'rgba(0, 0, 0, 0.05)',
          drawBorder: false
        },
        ticks: {
          font: {
            size: 11,
            family: "'Inter', 'Segoe UI', sans-serif"
          },
          color: '#718096',
          callback: (value) => {
            if (typeof value === 'number') {
              return '$' + this.formatCurrency(value);
            }
            return value;
          }
        }
      }
    },
    interaction: {
      intersect: false,
      mode: 'index'
    },
    onHover: (event, activeElements) => {
      const chart = event.native?.target as HTMLElement;
      if (chart) {
        chart.style.cursor = activeElements.length > 0 ? 'pointer' : 'default';
      }
    },
    onClick: (event, activeElements) => {
      if (activeElements.length > 0) {
        const element = activeElements[0];
        const datasetIndex = element.datasetIndex;
        const index = element.index;
        
        if (this.monthlyRevenue[index]) {
          const data = this.monthlyRevenue[index];
          this.barClicked.emit({
            month: data.month,
            revenue: data.revenue
          });
        }
      }
    }
  };

  ngOnInit(): void {
    // Subscribe to breakpoint changes
    this.subscriptions.add(
      this.responsiveService.getBreakpoint().subscribe(breakpoint => {
        this.currentBreakpoint = breakpoint;
        this.updateResponsiveHeight();
        this.updateChartOptions();
        if (this.hasData) {
          this.updateChart();
        }
      })
    );
    
    // Initialize responsive height
    this.updateResponsiveHeight();
    this.updateChartOptions();
    
    this.loadRevenueData();
  }

  ngOnDestroy(): void {
    this.subscriptions.unsubscribe();
  }

  ngOnChanges(changes: SimpleChanges): void {
    // Reload data if period or dateRange changes
    if ((changes['period'] && !changes['period'].firstChange) ||
        (changes['dateRange'] && !changes['dateRange'].firstChange)) {
      this.loadRevenueData();
    }
    
    // Update chart colors if barColor changes
    if (changes['barColor'] || changes['barHoverColor']) {
      if (this.hasData) {
        this.updateChart();
      }
    }
  }

  /**
   * Load revenue data from analytics service
   */
  loadRevenueData(): void {
    this.isLoading = true;
    this.errorMessage = null;
    this.hasData = false;

    const dateRange = this.dateRange ? {
      startDate: this.dateRange.startDate,
      endDate: this.dateRange.endDate
    } : undefined;

    this.pharmacyService.getMonthlyRevenue(
      this.period,
      dateRange,
      this.useCache
    ).subscribe({
      next: (data) => {
        this.monthlyRevenue = data;
        this.hasData = data && data.length > 0;
        
        if (this.hasData) {
          this.updateChart();
          this.dataLoaded.emit(data);
        }
        
        this.isLoading = false;
      },
      error: (error) => {
        console.error('[RevenueBarChart] Error loading revenue data:', error);
        this.errorMessage = error.message || 'Failed to load revenue data. Please try again.';
        this.isLoading = false;
        this.hasData = false;
        this.errorOccurred.emit(error);
      }
    });
  }

  /**
   * Update responsive height based on breakpoint
   */
  private updateResponsiveHeight(): void {
    this.responsiveHeight = this.responsiveService.getResponsiveHeight(this.height);
  }

  /**
   * Update chart options based on current breakpoint
   */
  private updateChartOptions(): void {
    const isMobile = this.currentBreakpoint === 'mobile';
    const isTablet = this.currentBreakpoint === 'tablet';
    const isTouch = this.responsiveService.isTouchDeviceSync();
    const fontSizeMultiplier = this.responsiveService.getFontSizeMultiplier();
    const reduceAnimations = this.responsiveService.shouldReduceAnimations();

    // Update legend position
    if (this.chartOptions.plugins?.legend) {
      this.chartOptions.plugins.legend.position = isMobile ? 'bottom' : 'top';
      if (this.chartOptions.plugins.legend.labels) {
        this.chartOptions.plugins.legend.labels.font = {
          size: Math.round(12 * fontSizeMultiplier),
          family: "'Inter', 'Segoe UI', sans-serif"
        };
        this.chartOptions.plugins.legend.labels.padding = isMobile ? 10 : 15;
      }
    }

    // Update tooltip
    if (this.chartOptions.plugins?.tooltip) {
      this.chartOptions.plugins.tooltip.padding = isMobile ? 8 : 12;
      if (this.chartOptions.plugins.tooltip.titleFont) {
        this.chartOptions.plugins.tooltip.titleFont.size = Math.round(14 * fontSizeMultiplier);
      }
      if (this.chartOptions.plugins.tooltip.bodyFont) {
        this.chartOptions.plugins.tooltip.bodyFont.size = Math.round(13 * fontSizeMultiplier);
      }
      // Longer display time for touch devices
      if (isTouch) {
        this.chartOptions.plugins.tooltip.displayColors = true;
      }
    }

    // Update X-axis (simplify labels on mobile)
    if (this.chartOptions.scales?.x) {
      if (this.chartOptions.scales.x.ticks) {
        this.chartOptions.scales.x.ticks.font = {
          size: Math.round(11 * fontSizeMultiplier),
          family: "'Inter', 'Segoe UI', sans-serif"
        };
        this.chartOptions.scales.x.ticks.maxRotation = isMobile ? 45 : 0;
        this.chartOptions.scales.x.ticks.minRotation = isMobile ? 45 : 0;
      }
    }

    // Update Y-axis
    if (this.chartOptions.scales?.y) {
      if (this.chartOptions.scales.y.ticks) {
        this.chartOptions.scales.y.ticks.font = {
          size: Math.round(11 * fontSizeMultiplier),
          family: "'Inter', 'Segoe UI', sans-serif"
        };
      }
    }

    // Update animations
    if (this.chartOptions.animation) {
      this.chartOptions.animation.duration = reduceAnimations ? 0 : (isMobile ? 500 : 1000);
    }

    // Update bar thickness for mobile
    if (this.chartData.datasets && this.chartData.datasets.length > 0) {
      this.chartData.datasets.forEach(dataset => {
        if (isMobile) {
          (dataset as any).barThickness = 'flex';
          (dataset as any).maxBarThickness = 30;
        } else if (isTablet) {
          (dataset as any).barThickness = 'flex';
          (dataset as any).maxBarThickness = 40;
        } else {
          (dataset as any).barThickness = undefined;
          (dataset as any).maxBarThickness = undefined;
        }
      });
    }
  }

  /**
   * Update chart data and configuration
   */
  private updateChart(): void {
    if (!this.monthlyRevenue || this.monthlyRevenue.length === 0) {
      return;
    }

    // Extract labels (months) - simplify on mobile
    const isMobile = this.currentBreakpoint === 'mobile';
    const labels = this.monthlyRevenue.map(item => {
      if (isMobile) {
        // Use abbreviated month on mobile
        return item.monthAbbr || item.month.substring(0, 3);
      }
      return item.monthAbbr || item.month;
    });

    // Extract revenue values
    const revenueValues = this.monthlyRevenue.map(item => item.revenue);

    // Calculate max value for better scaling
    const maxRevenue = Math.max(...revenueValues);
    const suggestedMax = Math.ceil(maxRevenue * 1.1); // Add 10% padding

    // Update chart data
    const isMobile = this.currentBreakpoint === 'mobile';
    const isTablet = this.currentBreakpoint === 'tablet';
    
    this.chartData = {
      labels,
      datasets: [{
        label: 'Revenue',
        data: revenueValues,
        backgroundColor: this.barColor + '80', // Add transparency
        borderColor: this.barColor,
        borderWidth: isMobile ? 1.5 : 2,
        borderRadius: isMobile ? 4 : 6,
        borderSkipped: false,
        hoverBackgroundColor: this.barHoverColor + '80',
        hoverBorderColor: this.barHoverColor,
        hoverBorderWidth: isMobile ? 2 : 3,
        // Responsive bar thickness
        barThickness: isMobile ? 'flex' : undefined,
        maxBarThickness: isMobile ? 30 : (isTablet ? 40 : undefined)
      }]
    };

    // Update chart options after data update
    this.updateChartOptions();
    
    // Trigger chart update
    setTimeout(() => {
      this.chart?.update();
    }, 0);

    // Update y-axis max if needed
    if (this.chartOptions?.scales?.y) {
      this.chartOptions.scales.y.max = suggestedMax;
    }
  }

  /**
   * Format currency value for display
   * @param value Currency value
   * @returns Formatted string (e.g., "1,234" for 1234)
   */
  private formatCurrency(value: number): string {
    if (value >= 1000000) {
      return (value / 1000000).toFixed(1) + 'M';
    } else if (value >= 1000) {
      return (value / 1000).toFixed(1) + 'K';
    }
    return value.toLocaleString('en-US');
  }

  /**
   * Retry loading data after error
   */
  retry(): void {
    this.loadRevenueData();
  }

  /**
   * Refresh data (clears cache and reloads)
   */
  refresh(): void {
    this.pharmacyService.clearAnalyticsCache();
    this.loadRevenueData();
  }

  /**
   * Get total revenue for display
   */
  getTotalRevenue(): number {
    return this.monthlyRevenue.reduce((sum, item) => sum + item.revenue, 0);
  }

  /**
   * Get average monthly revenue
   */
  getAverageRevenue(): number {
    if (this.monthlyRevenue.length === 0) return 0;
    return this.getTotalRevenue() / this.monthlyRevenue.length;
  }
}

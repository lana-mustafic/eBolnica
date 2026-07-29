import { Component, OnInit, OnChanges, SimpleChanges, Input, Output, EventEmitter, inject, OnDestroy, ViewChild } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { BaseChartDirective } from 'ng2-charts';
import { ChartConfiguration, ChartData, ChartType } from 'chart.js';
import { PharmacyService } from '../../../../../shared/services/pharmacy/pharmacy.service';
import { ResponsiveChartService, BreakpointType } from '../../../../../shared/services/responsive-chart.service';
import { StockTrendData } from '../../../../../models/analytics.dto';
import { Subscription } from 'rxjs';

/**
 * Current stock levels bar chart component.
 *
 * Displays one bar per selected medication representing current stock as a
 * percentage of estimated capacity. The API returns a point-in-time snapshot
 * (no inventory history timeline).
 */
@Component({
  selector: 'app-stock-trends-line-chart',
  standalone: true,
  imports: [CommonModule, FormsModule, BaseChartDirective],
  templateUrl: './stock-trends-line-chart.component.html',
  styleUrl: './stock-trends-line-chart.component.scss'
})
export class StockTrendsLineChartComponent implements OnInit, OnChanges, OnDestroy {
  @ViewChild(BaseChartDirective) chart?: BaseChartDirective;
  
  private pharmacyService = inject(PharmacyService);
  private responsiveService = inject(ResponsiveChartService);
  private subscriptions = new Subscription();
  
  currentBreakpoint: BreakpointType = 'desktop';
  responsiveHeight: number = 400;

  // Input properties
  /** Chart title displayed above the chart */
  @Input() title: string = 'Current Stock Levels by Medication';

  /** Explains what the chart measures (shown under the title) */
  @Input() description: string =
    'Current stock level for each medication as a percentage of estimated capacity (minimum stock × 3).';
  
  /** Chart container height in pixels */
  @Input() height: number = 400;
  
  /** Array of medication IDs to display (max 5) */
  @Input() medicationIds?: number[];
  
  /** Number of days to look back (default: 30) */
  @Input() days: number = 30;
  
  /** Whether to use cached data (default: true) */
  @Input() useCache: boolean = true;
  
  /** Show threshold lines at critical levels */
  @Input() showThresholds: boolean = true;
  
  /** Enable comparison mode (multiple medications) */
  @Input() compareMode: boolean = true;
  
  /** Low stock threshold (default: 20%) */
  @Input() lowThreshold: number = 20;
  
  /** Critical stock threshold (default: 50%) */
  @Input() criticalThreshold: number = 50;
  
  /** Ideal stock threshold (default: 80%) */
  @Input() idealThreshold: number = 80;
  
  /** Show fill area under lines */
  @Input() showFill: boolean = false;
  
  /** Line tension (0 = straight, 0.4 = curved) */
  @Input() lineTension: number = 0.4;

  // Output events
  /** Emitted when chart data is loaded successfully */
  @Output() dataLoaded = new EventEmitter<StockTrendData[]>();
  
  /** Emitted when an error occurs */
  @Output() errorOccurred = new EventEmitter<Error>();
  
  /** Emitted when a data point is clicked */
  @Output() pointClicked = new EventEmitter<StockTrendData>();
  
  /** Emitted when medication selection changes */
  @Output() medicationsChanged = new EventEmitter<number[]>();

  // Component state
  isLoading: boolean = true;
  errorMessage: string | null = null;
  hasData: boolean = false;
  
  // Chart data
  stockTrendData: StockTrendData[] = [];
  groupedData: Map<number, StockTrendData[]> = new Map();
  availableMedications: { id: number; name: string }[] = [];
  selectedMedicationIds: number[] = [];
  
  // Chart configuration (bar chart — one bar per medication snapshot)
  chartType: 'bar' = 'bar';

  chartData: ChartData<'bar'> = {
    labels: [],
    datasets: []
  };

  chartOptions: ChartConfiguration<'bar'>['options'] = {
    responsive: true,
    maintainAspectRatio: false,
    interaction: {
      intersect: false,
      mode: 'index'
    },
    plugins: {
      legend: {
        display: false
      },
      tooltip: {
        enabled: true,
        backgroundColor: 'rgba(0, 0, 0, 0.8)',
        padding: 12,
        titleFont: {
          size: 14,
          weight: 600,
          family: "'Inter', 'Segoe UI', sans-serif"
        },
        bodyFont: {
          size: 13,
          family: "'Inter', 'Segoe UI', sans-serif"
        },
        callbacks: {
          label: (context) => {
            const value = context.parsed.y;
            const medicationId = this.selectedMedicationIds[context.dataIndex];
            const point = medicationId ? this.groupedData.get(medicationId)?.[0] : undefined;
            const lines = [`Stock level: ${value ?? 0}%`];

            if (point?.quantity != null) {
              lines.push(`Quantity: ${point.quantity}`);
            }
            if (point?.status) {
              lines.push(`Status: ${point.status}`);
            }

            return lines;
          }
        }
      },
      title: {
        display: false
      }
    },
    scales: {
      x: {
        grid: {
          display: false
        },
        ticks: {
          font: {
            size: 11,
            family: "'Inter', 'Segoe UI', sans-serif"
          },
          color: '#718096',
          maxRotation: 45,
          minRotation: 0
        }
      },
      y: {
        beginAtZero: true,
        max: 100,
        grid: {
          display: true,
          color: 'rgba(0, 0, 0, 0.05)'
        },
        ticks: {
          font: {
            size: 11,
            family: "'Inter', 'Segoe UI', sans-serif"
          },
          color: '#718096',
          callback: (value) => `${value}%`
        }
      }
    },
    onClick: (event, activeElements) => {
      if (activeElements.length > 0) {
        const index = activeElements[0].index;
        const medicationId = this.selectedMedicationIds[index];
        const medicationData = this.groupedData.get(medicationId);

        if (medicationData?.[0]) {
          this.pointClicked.emit(medicationData[0]);
        }
      }
    },
    onHover: (event, activeElements) => {
      const chart = event.native?.target as HTMLElement;
      if (chart) {
        chart.style.cursor = activeElements.length > 0 ? 'pointer' : 'default';
      }
    }
  };

  // Color palette for medications
  private readonly colorPalette: string[] = [
    '#3b82f6', // Pharmacy blue
    '#10b981', // Green
    '#f59e0b', // Amber
    '#ef4444', // Red
    '#8b5cf6'  // Purple
  ];

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
    
    this.initializeMedications();
    this.loadStockTrendData();
  }

  ngOnDestroy(): void {
    this.subscriptions.unsubscribe();
  }

  ngOnChanges(changes: SimpleChanges): void {
    // Reload data if medicationIds or days change
    if ((changes['medicationIds'] && !changes['medicationIds'].firstChange) ||
        (changes['days'] && !changes['days'].firstChange)) {
      this.initializeMedications();
      this.loadStockTrendData();
    }
    
    // Update thresholds if changed
    if (changes['showThresholds'] || changes['lowThreshold'] || 
        changes['criticalThreshold'] || changes['idealThreshold']) {
      if (this.hasData) {
        this.updateChart();
      }
    }
  }

  /**
   * Initialize medication selection
   */
  private initializeMedications(): void {
    if (this.medicationIds && this.medicationIds.length > 0) {
      // Limit to 5 medications for comparison
      this.selectedMedicationIds = this.medicationIds.slice(0, 5);
    } else {
      // Default: empty array, will be populated after data loads
      this.selectedMedicationIds = [];
    }
  }

  /**
   * Load stock trend data from analytics service
   */
  loadStockTrendData(): void {
    this.isLoading = true;
    this.errorMessage = null;
    this.hasData = false;

    this.pharmacyService.getStockTrends(
      this.selectedMedicationIds.length > 0 ? this.selectedMedicationIds : undefined,
      this.days,
      this.useCache
    ).subscribe({
      next: (data) => {
        this.stockTrendData = data;
        this.groupDataByMedication(data);
        this.extractAvailableMedications(data);
        
        // Auto-select medications if none selected
        if (this.selectedMedicationIds.length === 0 && this.availableMedications.length > 0) {
          const topMedications = this.availableMedications.slice(0, Math.min(3, this.availableMedications.length));
          this.selectedMedicationIds = topMedications.map(m => m.id);
        }
        
        this.hasData = data.length > 0;
        
        if (this.hasData) {
          this.updateChart();
          this.dataLoaded.emit(data);
        }
        
        this.isLoading = false;
      },
      error: (error) => {
        console.error('[StockTrendsLineChart] Error loading stock trends:', error);
        this.errorMessage = error.message || 'Failed to load stock level data. Please try again.';
        this.isLoading = false;
        this.hasData = false;
        this.errorOccurred.emit(error);
      }
    });
  }

  /**
   * Group stock data by medication ID
   */
  private groupDataByMedication(data: StockTrendData[]): void {
    this.groupedData.clear();
    
    data.forEach(item => {
      if (!this.groupedData.has(item.medicationId)) {
        this.groupedData.set(item.medicationId, []);
      }
      this.groupedData.get(item.medicationId)!.push(item);
    });
    
    // Sort each medication's data by date
    this.groupedData.forEach((values, key) => {
      values.sort((a, b) => new Date(a.date).getTime() - new Date(b.date).getTime());
    });
  }

  /**
   * Extract unique medications from data
   */
  private extractAvailableMedications(data: StockTrendData[]): void {
    const medicationMap = new Map<number, string>();
    
    data.forEach(item => {
      if (!medicationMap.has(item.medicationId)) {
        medicationMap.set(item.medicationId, item.medicationName);
      }
    });
    
    this.availableMedications = Array.from(medicationMap.entries()).map(([id, name]) => ({
      id,
      name
    }));
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
    if (!this.chartOptions) {
      return;
    }

    const isMobile = this.currentBreakpoint === 'mobile';
    const isTablet = this.currentBreakpoint === 'tablet';
    const isTouch = this.responsiveService.isTouchDeviceSync();
    const fontSizeMultiplier = this.responsiveService.getFontSizeMultiplier();
    const reduceAnimations = this.responsiveService.shouldReduceAnimations();

    // Update legend position
    if (this.chartOptions.plugins?.legend) {
      this.chartOptions.plugins.legend.position = isMobile ? 'bottom' : 'top';
      if (this.chartOptions.plugins.legend.labels) {
        const font = typeof this.chartOptions.plugins.legend.labels.font === 'object'
          ? this.chartOptions.plugins.legend.labels.font
          : {};
        this.chartOptions.plugins.legend.labels.font = {
          ...font,
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
        const titleFont = typeof this.chartOptions.plugins.tooltip.titleFont === 'object'
          ? this.chartOptions.plugins.tooltip.titleFont
          : {};
        this.chartOptions.plugins.tooltip.titleFont = {
          ...titleFont,
          size: Math.round(14 * fontSizeMultiplier)
        };
      }
      if (this.chartOptions.plugins.tooltip.bodyFont) {
        const bodyFont = typeof this.chartOptions.plugins.tooltip.bodyFont === 'object'
          ? this.chartOptions.plugins.tooltip.bodyFont
          : {};
        this.chartOptions.plugins.tooltip.bodyFont = {
          ...bodyFont,
          size: Math.round(13 * fontSizeMultiplier)
        };
      }
      // Longer display time for touch devices
      if (isTouch) {
        this.chartOptions.plugins.tooltip.displayColors = true;
      }
    }

    // Update X-axis (simplify date labels on mobile)
    if (this.chartOptions.scales?.['x']) {
      if (this.chartOptions.scales['x'].ticks) {
        const ticksFont = typeof this.chartOptions.scales['x'].ticks.font === 'object'
          ? this.chartOptions.scales['x'].ticks.font
          : {};
        this.chartOptions.scales['x'].ticks.font = {
          ...ticksFont,
          size: Math.round(11 * fontSizeMultiplier),
          family: "'Inter', 'Segoe UI', sans-serif"
        };
        this.chartOptions.scales['x'].ticks.maxRotation = (isMobile || isTablet) ? 45 : 0;
        this.chartOptions.scales['x'].ticks.minRotation = (isMobile || isTablet) ? 45 : 0;
      }
    }

    // Update Y-axis
    if (this.chartOptions.scales?.['y']) {
      if (this.chartOptions.scales['y'].ticks) {
        const ticksFont = typeof this.chartOptions.scales['y'].ticks.font === 'object'
          ? this.chartOptions.scales['y'].ticks.font
          : {};
        this.chartOptions.scales['y'].ticks.font = {
          ...ticksFont,
          size: Math.round(11 * fontSizeMultiplier),
          family: "'Inter', 'Segoe UI', sans-serif"
        };
      }
    }

    // Update animations
    if (this.chartOptions.animation !== undefined) {
      this.chartOptions.animation = reduceAnimations ? false : {
        duration: isMobile ? 500 : (isTablet ? 750 : 1000)
      };
    }
  }

  /**
   * Sample data points for mobile devices
   */
  private sampleDataPoints<T>(data: T[], maxPoints: number): T[] {
    if (data.length <= maxPoints) return data;
    
    const step = Math.ceil(data.length / maxPoints);
    const sampled: T[] = [];
    
    for (let i = 0; i < data.length; i += step) {
      sampled.push(data[i]);
    }
    
    // Always include last point
    if (sampled[sampled.length - 1] !== data[data.length - 1]) {
      sampled.push(data[data.length - 1]);
    }
    
    return sampled;
  }

  /**
   * Update chart data and configuration (current-stock snapshot bars)
   */
  private updateChart(): void {
    if (this.selectedMedicationIds.length === 0 || this.groupedData.size === 0) {
      return;
    }

    const labels: string[] = [];
    const values: number[] = [];
    const backgroundColors: string[] = [];
    const borderColors: string[] = [];
    const isMobile = this.currentBreakpoint === 'mobile';
    const isCompact = this.currentBreakpoint !== 'desktop';

    this.selectedMedicationIds.forEach((medicationId, index) => {
      const medicationData = this.groupedData.get(medicationId);
      const point = medicationData?.[0];
      if (!point) {
        return;
      }

      labels.push(isCompact ? this.truncateLabel(point.medicationName, 14) : point.medicationName);
      values.push(point.stockLevel);
      const color = this.colorPalette[index % this.colorPalette.length];
      backgroundColors.push(color + 'CC');
      borderColors.push(color);
    });

    if (labels.length === 0) {
      return;
    }

    this.chartData = {
      labels,
      datasets: [{
        label: 'Stock level (% of capacity)',
        data: values,
        backgroundColor: backgroundColors,
        borderColor: borderColors,
        borderWidth: isMobile ? 1.5 : 2,
        borderRadius: isMobile ? 4 : 6,
        maxBarThickness: isMobile ? 48 : (this.currentBreakpoint === 'tablet' ? 56 : 64)
      }]
    };

    this.updateChartOptions();

    setTimeout(() => {
      this.chart?.update();
    }, 0);
  }

  get snapshotLabel(): string {
    const firstPoint = this.stockTrendData[0];
    if (!firstPoint?.date) {
      return 'Current';
    }

    return new Date(firstPoint.date).toLocaleString('en-US', {
      month: 'short',
      day: 'numeric',
      year: 'numeric',
      hour: '2-digit',
      minute: '2-digit'
    });
  }

  /**
   * Toggle medication selection
   */
  toggleMedication(medicationId: number): void {
    const index = this.selectedMedicationIds.indexOf(medicationId);
    
    if (index > -1) {
      // Remove medication
      this.selectedMedicationIds.splice(index, 1);
    } else {
      // Add medication (max 5)
      if (this.selectedMedicationIds.length < 5) {
        this.selectedMedicationIds.push(medicationId);
      }
    }
    
    this.medicationsChanged.emit([...this.selectedMedicationIds]);
    this.loadStockTrendData();
  }

  /**
   * Select all available medications
   */
  selectAllMedications(): void {
    const maxMedications = Math.min(5, this.availableMedications.length);
    this.selectedMedicationIds = this.availableMedications
      .slice(0, maxMedications)
      .map(m => m.id);
    this.medicationsChanged.emit([...this.selectedMedicationIds]);
    this.loadStockTrendData();
  }

  /**
   * Clear all medication selections
   */
  clearAllMedications(): void {
    this.selectedMedicationIds = [];
    this.medicationsChanged.emit([]);
    this.loadStockTrendData();
  }

  /**
   * Retry loading data after error
   */
  retry(): void {
    this.loadStockTrendData();
  }

  /**
   * Refresh data (clears cache and reloads)
   */
  refresh(): void {
    this.pharmacyService.clearAnalyticsCache();
    this.loadStockTrendData();
  }

  /**
   * Check if medication is selected
   */
  isMedicationSelected(medicationId: number): boolean {
    return this.selectedMedicationIds.includes(medicationId);
  }

  /**
   * Get medication name by ID
   */
  getMedicationName(medicationId: number): string {
    const medication = this.availableMedications.find(m => m.id === medicationId);
    return medication?.name || `Medication ${medicationId}`;
  }

  get showChart(): boolean {
    return this.hasData
      && this.selectedMedicationIds.length > 0
      && !this.isLoading
      && !this.errorMessage;
  }

  get showEmptyState(): boolean {
    return !this.isLoading && !this.errorMessage && !this.showChart;
  }

  get emptyStateMessage(): string {
    if (this.availableMedications.length === 0) {
      return 'No medications available to display stock levels.';
    }
    if (this.selectedMedicationIds.length === 0) {
      return 'Select at least one medication to view stock levels.';
    }
    return 'No stock level data available for the selected medications.';
  }

  private truncateLabel(text: string, maxLength: number): string {
    if (text.length <= maxLength) {
      return text;
    }

    return `${text.substring(0, maxLength - 1)}…`;
  }
}

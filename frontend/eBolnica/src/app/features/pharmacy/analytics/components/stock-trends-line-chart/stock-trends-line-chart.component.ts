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
 * Current stock levels line chart component.
 *
 * Displays each selected medication's current stock as a percentage of estimated
 * capacity. The API has no inventory history yet, so values are flat across the
 * selected date window (not a historical trend).
 *
 * @example
 * ```html
 * <app-stock-trends-line-chart
 *   [days]="30"
 *   [showThresholds]="true">
 * </app-stock-trends-line-chart>
 * ```
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
    'Current stock shown as % of estimated capacity. No inventory history is stored yet, so levels stay flat across the selected period—not a historical trend.';
  
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
  
  // Chart configuration
  chartType: 'line' = 'line';
  
  chartData: ChartData<'line'> = {
    labels: [],
    datasets: []
  };

  chartOptions: ChartConfiguration<'line'>['options'] = {
    responsive: true,
    maintainAspectRatio: false,
    interaction: {
      intersect: false,
      mode: 'index'
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
        },
        onClick: (e, legendItem, legend) => {
          const index = legendItem.datasetIndex;
          if (index !== undefined) {
            const meta = legend.chart.getDatasetMeta(index);
            meta.hidden = !meta.hidden;
            legend.chart.update();
          }
        }
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
          title: (context) => {
            const index = context[0].dataIndex;
            const date = this.chartData.labels?.[index] as string;
            return this.formatDateLabel(date);
          },
          label: (context) => {
            const datasetLabel = context.dataset.label || '';
            const value = context.parsed.y;
            const index = context.dataIndex;
            
            // Calculate percentage change
            const change = this.calculatePercentageChange(context.datasetIndex, index);
            const changeText = change !== null 
              ? (change >= 0 ? '+' : '') + change.toFixed(1) + '%'
              : '';
            
            if (value === null || value === undefined) {
              return [`${datasetLabel}: 0`, changeText ? `Change: ${changeText}` : ''].filter(Boolean);
            }
            return [
              `${datasetLabel}: ${value.toLocaleString()}`,
              changeText ? `Change: ${changeText}` : ''
            ].filter(Boolean);
          }
        }
      },
      title: {
        display: false
      }
    },
    scales: {
      x: {
        type: 'category',
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
          maxRotation: 45,
          minRotation: 0,
          callback: (value, index) => {
            const label = this.chartData.labels?.[index] as string;
            return this.formatDateLabel(label);
          }
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
          callback: (value) => {
            return value + '%';
          }
        }
      }
    },
    onClick: (event, activeElements) => {
      if (activeElements.length > 0) {
        const element = activeElements[0];
        const datasetIndex = element.datasetIndex;
        const index = element.index;
        
        const medicationId = this.selectedMedicationIds[datasetIndex];
        const medicationData = this.groupedData.get(medicationId);
        
        if (medicationData && medicationData[index]) {
          this.pointClicked.emit(medicationData[index]);
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

  // Line styles for differentiation
  private readonly lineStyles: ('solid' | 'dashed' | 'dotted')[] = [
    'solid',
    'dashed',
    'dotted',
    'solid',
    'dashed'
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
        this.chartOptions.scales['x'].ticks.maxRotation = isMobile ? 45 : 0;
        this.chartOptions.scales['x'].ticks.minRotation = isMobile ? 45 : 0;
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
   * Update chart data and configuration
   */
  private updateChart(): void {
    if (this.selectedMedicationIds.length === 0 || this.groupedData.size === 0) {
      return;
    }

    // Get all unique dates across selected medications
    const allDates = new Set<string>();
    this.selectedMedicationIds.forEach(id => {
      const medicationData = this.groupedData.get(id);
      if (medicationData) {
        medicationData.forEach(item => allDates.add(item.date));
      }
    });
    
    let sortedDates = Array.from(allDates).sort((a, b) => 
      new Date(a).getTime() - new Date(b).getTime()
    );

    // Sample dates for mobile devices
    const isMobile = this.currentBreakpoint === 'mobile';
    if (isMobile) {
      const maxPoints = this.responsiveService.getOptimalDataPoints(sortedDates.length);
      sortedDates = this.sampleDataPoints(sortedDates, maxPoints);
    }

    // Create datasets for each selected medication
    const datasets = this.selectedMedicationIds.map((medicationId, index) => {
      const medicationData = this.groupedData.get(medicationId) || [];
      const medicationName = medicationData[0]?.medicationName || `Medication ${medicationId}`;
      
      // Map data points to dates
      const dataPoints = sortedDates.map(date => {
        const dataPoint = medicationData.find(d => d.date === date);
        return dataPoint ? dataPoint.stockLevel : null;
      });

      const color = this.colorPalette[index % this.colorPalette.length];
      const lineStyle = this.lineStyles[index % this.lineStyles.length];
      
      const borderDash = lineStyle === 'dashed' ? [10, 5] : 
                        lineStyle === 'dotted' ? [2, 2] : [];

      const isMobile = this.currentBreakpoint === 'mobile';
      const isTablet = this.currentBreakpoint === 'tablet';

      return {
        label: medicationName,
        data: dataPoints,
        borderColor: color,
        backgroundColor: this.showFill ? color + '20' : 'transparent',
        borderWidth: isMobile ? 1.5 : (isTablet ? 1.75 : 2),
        borderDash,
        pointRadius: isMobile ? 3 : 4,
        pointHoverRadius: isMobile ? 5 : 6,
        pointBackgroundColor: color,
        pointBorderColor: '#fff',
        pointBorderWidth: isMobile ? 1.5 : 2,
        fill: this.showFill,
        tension: this.lineTension,
        spanGaps: false
      };
    });

    // Add threshold lines if enabled
    if (this.showThresholds) {
      const thresholdLines = this.createThresholdLines(sortedDates.length);
      datasets.push(...thresholdLines);
    }

    this.chartData = {
      labels: sortedDates,
      datasets
    };

    // Update chart options after data update
    this.updateChartOptions();
    
    // Trigger chart update
    setTimeout(() => {
      this.chart?.update();
    }, 0);
  }

  /**
   * Create threshold lines for critical stock levels
   */
  private createThresholdLines(dataLength: number): any[] {
    const thresholds = [
      { value: this.lowThreshold, label: 'Low Stock', color: '#ef4444', style: 'dashed' },
      { value: this.criticalThreshold, label: 'Critical', color: '#f59e0b', style: 'dashed' },
      { value: this.idealThreshold, label: 'Ideal', color: '#10b981', style: 'dashed' }
    ];

    return thresholds.map(threshold => ({
      label: threshold.label,
      data: new Array(dataLength).fill(threshold.value),
      borderColor: threshold.color,
      backgroundColor: 'transparent',
      borderWidth: 1.5,
      borderDash: [5, 5],
      pointRadius: 0,
      pointHoverRadius: 0,
      fill: false,
      tension: 0,
      order: 100, // Draw behind main lines
      tooltip: {
        enabled: false
      }
    }));
  }

  /**
   * Calculate percentage change from previous point
   */
  private calculatePercentageChange(datasetIndex: number, pointIndex: number): number | null {
    if (pointIndex === 0) return null;
    
    const dataset = this.chartData.datasets[datasetIndex];
    const currentValue = dataset.data[pointIndex] as number;
    const previousValue = dataset.data[pointIndex - 1] as number;
    
    if (currentValue === null || previousValue === null) return null;
    if (previousValue === 0) return currentValue > 0 ? 100 : -100;
    
    return ((currentValue - previousValue) / previousValue) * 100;
  }

  /**
   * Format date label for display
   */
  private formatDateLabel(dateString: string): string {
    if (!dateString) return '';
    
    const isMobile = this.currentBreakpoint === 'mobile';
    const date = new Date(dateString);
    const now = new Date();
    const diffDays = Math.floor((now.getTime() - date.getTime()) / (1000 * 60 * 60 * 24));
    
    if (diffDays === 0) return 'Today';
    if (diffDays === 1) return 'Yesterday';
    if (diffDays < 7) return `${diffDays}d`;
    
    // Simplified format for mobile
    if (isMobile) {
      return `${date.getMonth() + 1}/${date.getDate()}`;
    }
    
    return date.toLocaleDateString('en-US', { 
      month: 'short', 
      day: 'numeric',
      year: date.getFullYear() !== now.getFullYear() ? 'numeric' : undefined
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
}

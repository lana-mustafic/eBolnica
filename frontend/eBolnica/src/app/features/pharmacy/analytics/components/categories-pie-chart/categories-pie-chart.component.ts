import { Component, OnInit, OnChanges, SimpleChanges, Input, Output, EventEmitter, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { BaseChartDirective } from 'ng2-charts';
import { ChartConfiguration, ChartData, ChartType } from 'chart.js';
import { PharmacyService } from '../../../../../shared/services/pharmacy/pharmacy.service';
import { MedicationCategoryData } from '../../../../../models/analytics.dto';

/**
 * Categories Pie Chart Component
 * 
 * A reusable pie/doughnut chart component for displaying medication category distribution.
 * Integrates with PharmacyService analytics API to fetch and display category data.
 * 
 * Features:
 * - Toggle between pie and doughnut chart types
 * - Percentage labels on segments
 * - Interactive legend (click to hide/show categories)
 * - Color palette using pharmacy design system
 * - Responsive design with mobile optimization
 * - Accessible with ARIA labels and keyboard navigation
 * 
 * @example
 * ```html
 * <app-categories-pie-chart 
 *   [title]="'Medication Categories Distribution'"
 *   [chartType]="'doughnut'"
 *   [maxCategories]="8">
 * </app-categories-pie-chart>
 * ```
 */
@Component({
  selector: 'app-categories-pie-chart',
  standalone: true,
  imports: [CommonModule, BaseChartDirective],
  templateUrl: './categories-pie-chart.component.html',
  styleUrl: './categories-pie-chart.component.scss'
})
export class CategoriesPieChartComponent implements OnInit, OnChanges {
  private pharmacyService = inject(PharmacyService);

  // Input properties
  /** Chart title displayed above the chart */
  @Input() title: string = 'Medication Categories Distribution';
  
  /** Chart container height in pixels */
  @Input() height: number = 400;
  
  /** Chart type: 'pie' or 'doughnut' */
  @Input() chartType: 'pie' | 'doughnut' = 'doughnut';
  
  /** Maximum number of categories to display */
  @Input() maxCategories: number = 10;
  
  /** Whether to use cached data (default: true) */
  @Input() useCache: boolean = true;
  
  /** Show percentage labels on chart segments */
  @Input() showLabels: boolean = true;
  
  /** Show center label with total (only for doughnut) */
  @Input() showCenterLabel: boolean = true;

  // Output events
  /** Emitted when chart data is loaded successfully */
  @Output() dataLoaded = new EventEmitter<MedicationCategoryData[]>();
  
  /** Emitted when an error occurs */
  @Output() errorOccurred = new EventEmitter<Error>();
  
  /** Emitted when a segment is clicked */
  @Output() segmentClicked = new EventEmitter<MedicationCategoryData>();

  // Component state
  isLoading: boolean = false;
  errorMessage: string | null = null;
  hasData: boolean = false;
  hasInsufficientData: boolean = false;
  
  // Chart data
  categoryData: MedicationCategoryData[] = [];
  
  // Chart configuration
  chartTypeValue: ChartType = 'doughnut';
  
  chartData: ChartData<'pie' | 'doughnut'> = {
    labels: [],
    datasets: []
  };

  chartOptions: ChartConfiguration<'pie' | 'doughnut'>['options'] = {
    responsive: true,
    maintainAspectRatio: false,
    plugins: {
      legend: {
        display: true,
        position: 'right',
        labels: {
          usePointStyle: true,
          padding: 15,
          font: {
            size: 12,
            family: "'Inter', 'Segoe UI', sans-serif"
          },
          generateLabels: (chart) => {
            const data = chart.data;
            if (data.labels && data.datasets && data.datasets[0]) {
              return data.labels.map((label, i) => {
                const value = data.datasets[0].data[i] as number;
                const total = (data.datasets[0].data as number[]).reduce((a, b) => a + b, 0);
                const percentage = total > 0 ? ((value / total) * 100).toFixed(1) : '0';
                
                return {
                  text: `${label} (${percentage}%)`,
                  fillStyle: (data.datasets[0].backgroundColor as string[])[i],
                  hidden: chart.getDatasetMeta(0).data[i].hidden || false,
                  index: i
                };
              });
            }
            return [];
          }
        },
        onClick: (e, legendItem, legend) => {
          const index = legendItem.index;
          const chart = legend.chart;
          const meta = chart.getDatasetMeta(0);
          
          // Toggle visibility
          meta.data[index].hidden = !meta.data[index].hidden;
          chart.update();
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
            const label = context.label || '';
            const value = context.parsed || 0;
            const total = context.dataset.data.reduce((a: number, b: number) => a + b, 0) as number;
            const percentage = total > 0 ? ((value / total) * 100).toFixed(1) : '0';
            
            // Find category data for additional info
            const categoryInfo = this.categoryData.find(cat => cat.category === label);
            const count = categoryInfo?.count || value;
            
            return [
              `${label}`,
              `Medications: ${count}`,
              `Percentage: ${percentage}%`
            ];
          }
        }
      },
      title: {
        display: false // We'll show title in template
      }
    },
    interaction: {
      intersect: false
    },
    onClick: (event, activeElements) => {
      if (activeElements.length > 0) {
        const element = activeElements[0];
        const index = element.index;
        
        if (this.categoryData[index]) {
          this.segmentClicked.emit(this.categoryData[index]);
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

  // Pharmacy design system color palette (public for template access)
  readonly colorPalette: string[] = [
    '#3b82f6', // Pharmacy blue
    '#10b981', // Green
    '#f59e0b', // Amber
    '#ef4444', // Red
    '#8b5cf6', // Purple
    '#ec4899', // Pink
    '#06b6d4', // Cyan
    '#84cc16', // Lime
    '#f97316', // Orange
    '#6366f1', // Indigo
    '#14b8a6', // Teal
    '#a855f7'  // Violet
  ];

  ngOnInit(): void {
    this.updateChartType();
    this.loadCategoryData();
  }

  ngOnChanges(changes: SimpleChanges): void {
    // Reload data if maxCategories changes
    if (changes['maxCategories'] && !changes['maxCategories'].firstChange) {
      this.loadCategoryData();
    }
    
    // Update chart type if changed
    if (changes['chartType']) {
      this.updateChartType();
      if (this.hasData) {
        this.updateChart();
      }
    }
  }

  /**
   * Update chart type based on input
   */
  private updateChartType(): void {
    this.chartTypeValue = this.chartType === 'pie' ? 'pie' : 'doughnut';
    
    // Update cutout for doughnut
    if (this.chartTypeValue === 'doughnut') {
      this.chartOptions = {
        ...this.chartOptions,
        cutout: '60%' // 60% cutout for center label space
      };
    } else {
      this.chartOptions = {
        ...this.chartOptions,
        cutout: 0 // No cutout for pie chart
      };
    }
  }

  /**
   * Load category data from analytics service
   */
  loadCategoryData(): void {
    this.isLoading = true;
    this.errorMessage = null;
    this.hasData = false;
    this.hasInsufficientData = false;

    this.pharmacyService.getTopMedicationCategories(
      this.maxCategories,
      this.useCache
    ).subscribe({
      next: (data) => {
        this.categoryData = data;
        this.hasInsufficientData = data.length < 2;
        this.hasData = data.length >= 2;
        
        if (this.hasData) {
          this.updateChart();
          this.dataLoaded.emit(data);
        }
        
        this.isLoading = false;
      },
      error: (error) => {
        console.error('[CategoriesPieChart] Error loading category data:', error);
        this.errorMessage = error.message || 'Failed to load category data. Please try again.';
        this.isLoading = false;
        this.hasData = false;
        this.hasInsufficientData = false;
        this.errorOccurred.emit(error);
      }
    });
  }

  /**
   * Update chart data and configuration
   */
  private updateChart(): void {
    if (!this.categoryData || this.categoryData.length === 0) {
      return;
    }

    // Extract labels and values
    const labels = this.categoryData.map(item => item.category);
    const values = this.categoryData.map(item => item.count);
    
    // Generate colors for categories
    const colors = this.generateColors(this.categoryData.length);
    const backgroundColors = colors.map(c => c + 'CC'); // Add transparency
    const borderColors = colors;
    const hoverColors = colors.map(c => this.adjustBrightness(c, 10));

    // Update chart data
    this.chartData = {
      labels,
      datasets: [{
        label: 'Categories',
        data: values,
        backgroundColor: backgroundColors,
        borderColor: borderColors,
        borderWidth: 2,
        hoverBackgroundColor: hoverColors,
        hoverBorderColor: borderColors,
        hoverBorderWidth: 3
      }]
    };
  }

  /**
   * Generate color palette for categories
   * @param count Number of categories
   * @returns Array of color hex codes
   */
  private generateColors(count: number): string[] {
    const colors: string[] = [];
    
    // Use predefined palette and repeat if needed
    for (let i = 0; i < count; i++) {
      const colorIndex = i % this.colorPalette.length;
      colors.push(this.colorPalette[colorIndex]);
    }
    
    return colors;
  }

  /**
   * Adjust color brightness
   * @param hex Hex color code
   * @param percent Brightness adjustment percentage
   * @returns Adjusted hex color
   */
  private adjustBrightness(hex: string, percent: number): string {
    const num = parseInt(hex.replace('#', ''), 16);
    const amt = Math.round(2.55 * percent);
    const R = (num >> 16) + amt;
    const G = (num >> 8 & 0x00FF) + amt;
    const B = (num & 0x0000FF) + amt;
    
    return '#' + (0x1000000 + (R < 255 ? R < 1 ? 0 : R : 255) * 0x10000 +
      (G < 255 ? G < 1 ? 0 : G : 255) * 0x100 +
      (B < 255 ? B < 1 ? 0 : B : 255)).toString(16).slice(1);
  }

  /**
   * Retry loading data after error
   */
  retry(): void {
    this.loadCategoryData();
  }

  /**
   * Refresh data (clears cache and reloads)
   */
  refresh(): void {
    this.pharmacyService.clearAnalyticsCache();
    this.loadCategoryData();
  }

  /**
   * Toggle between pie and doughnut chart
   */
  toggleChartType(): void {
    this.chartType = this.chartType === 'pie' ? 'doughnut' : 'pie';
    this.updateChartType();
    if (this.hasData) {
      this.updateChart();
    }
  }

  /**
   * Get total number of medications across all categories
   */
  getTotalMedications(): number {
    return this.categoryData.reduce((sum, item) => sum + item.count, 0);
  }

  /**
   * Get total number of categories
   */
  getTotalCategories(): number {
    return this.categoryData.length;
  }

  /**
   * Get center label text for doughnut chart
   */
  getCenterLabel(): string {
    if (this.chartType !== 'doughnut' || !this.showCenterLabel) {
      return '';
    }
    return `${this.getTotalCategories()}\nCategories`;
  }

  /**
   * Get color for category item (for accessible list)
   */
  getCategoryColor(index: number): string {
    return this.colorPalette[index % this.colorPalette.length];
  }
}

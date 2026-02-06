import { Component, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { PharmacyService } from '../../../shared/services/pharmacy/pharmacy.service';
import { 
  MonthlyRevenueData, 
  MedicationCategoryData, 
  StockTrendData, 
  DashboardStats,
  AnalyticsPeriod 
} from '../../../models/analytics.dto';
import { BaseChartDirective } from 'ng2-charts';
import { ChartConfiguration, ChartData, ChartType } from 'chart.js';

/**
 * Example component demonstrating how to use PharmacyService analytics methods
 * This component shows:
 * - How to fetch analytics data
 * - How to handle loading states
 * - How to handle errors
 * - How to display data in charts
 * 
 * Usage:
 * Import this component in your module/route and use it as a reference
 * for implementing analytics in your pharmacy dashboard.
 */
@Component({
  selector: 'app-analytics-example',
  standalone: true,
  imports: [CommonModule, FormsModule, BaseChartDirective],
  template: `
    <div class="analytics-container">
      <h2>Pharmacy Analytics Example</h2>

      <!-- Loading State -->
      <div *ngIf="isLoading" class="loading">
        <p>Loading analytics data...</p>
      </div>

      <!-- Error State -->
      <div *ngIf="errorMessage" class="error">
        <p>{{ errorMessage }}</p>
        <button (click)="loadData()">Retry</button>
      </div>

      <!-- Charts Section -->
      <div *ngIf="!isLoading && !errorMessage" class="charts-grid">
        
        <!-- Monthly Revenue Bar Chart -->
        <div class="chart-card">
          <h3>Monthly Revenue</h3>
          <canvas baseChart
            [data]="revenueChartData"
            [type]="revenueChartType"
            [options]="revenueChartOptions">
          </canvas>
        </div>

        <!-- Top Categories Pie Chart -->
        <div class="chart-card">
          <h3>Top Medication Categories</h3>
          <canvas baseChart
            [data]="categoriesChartData"
            [type]="categoriesChartType"
            [options]="categoriesChartOptions">
          </canvas>
        </div>

        <!-- Stock Trends Line Chart -->
        <div class="chart-card">
          <h3>Stock Trends</h3>
          <canvas baseChart
            [data]="stockTrendsChartData"
            [type]="stockTrendsChartType"
            [options]="stockTrendsChartOptions">
          </canvas>
        </div>
      </div>

      <!-- Summary Statistics -->
      <div *ngIf="dashboardStats?.summary" class="summary-stats">
        <h3>Summary Statistics</h3>
        <div class="stats-grid">
          <div class="stat-item">
            <span class="stat-label">Total Revenue:</span>
            <span class="stat-value">${{ dashboardStats.summary.totalRevenue?.toLocaleString() }}</span>
          </div>
          <div class="stat-item">
            <span class="stat-label">Total Medications:</span>
            <span class="stat-value">{{ dashboardStats.summary.totalMedications }}</span>
          </div>
          <div class="stat-item">
            <span class="stat-label">Categories:</span>
            <span class="stat-value">{{ dashboardStats.summary.totalCategories }}</span>
          </div>
          <div class="stat-item">
            <span class="stat-label">Avg Stock Level:</span>
            <span class="stat-value">{{ dashboardStats.summary.averageStockLevel?.toFixed(0) }}</span>
          </div>
        </div>
      </div>

      <!-- Controls -->
      <div class="controls">
        <button (click)="loadData()">Refresh Data</button>
        <button (click)="clearCache()">Clear Cache</button>
        <select [(ngModel)]="selectedPeriod" (change)="onPeriodChange()">
          <option value="last7days">Last 7 Days</option>
          <option value="last30days">Last 30 Days</option>
          <option value="last3months">Last 3 Months</option>
          <option value="last6months">Last 6 Months</option>
          <option value="last12months">Last 12 Months</option>
        </select>
      </div>
    </div>
  `,
  styles: [`
    .analytics-container {
      padding: 20px;
      max-width: 1400px;
      margin: 0 auto;
    }

    h2 {
      color: #333;
      margin-bottom: 20px;
    }

    .loading, .error {
      padding: 20px;
      text-align: center;
      background: #f5f5f5;
      border-radius: 4px;
      margin-bottom: 20px;
    }

    .error {
      background: #fee;
      color: #c33;
    }

    .charts-grid {
      display: grid;
      grid-template-columns: repeat(auto-fit, minmax(400px, 1fr));
      gap: 20px;
      margin-bottom: 30px;
    }

    .chart-card {
      background: white;
      padding: 20px;
      border-radius: 8px;
      box-shadow: 0 2px 4px rgba(0,0,0,0.1);
    }

    .chart-card h3 {
      margin-top: 0;
      color: #555;
    }

    .summary-stats {
      background: white;
      padding: 20px;
      border-radius: 8px;
      box-shadow: 0 2px 4px rgba(0,0,0,0.1);
      margin-bottom: 20px;
    }

    .stats-grid {
      display: grid;
      grid-template-columns: repeat(auto-fit, minmax(200px, 1fr));
      gap: 15px;
    }

    .stat-item {
      display: flex;
      flex-direction: column;
      padding: 10px;
      background: #f9f9f9;
      border-radius: 4px;
    }

    .stat-label {
      font-size: 0.9em;
      color: #666;
      margin-bottom: 5px;
    }

    .stat-value {
      font-size: 1.2em;
      font-weight: bold;
      color: #333;
    }

    .controls {
      display: flex;
      gap: 10px;
      align-items: center;
      margin-top: 20px;
    }

    button {
      padding: 8px 16px;
      background: #007bff;
      color: white;
      border: none;
      border-radius: 4px;
      cursor: pointer;
    }

    button:hover {
      background: #0056b3;
    }

    select {
      padding: 8px;
      border: 1px solid #ddd;
      border-radius: 4px;
    }
  `]
})
export class AnalyticsExampleComponent implements OnInit {
  private pharmacyService = inject(PharmacyService);

  // Loading and error states
  isLoading: boolean = false;
  errorMessage: string | null = null;

  // Data
  monthlyRevenue: MonthlyRevenueData[] = [];
  topCategories: MedicationCategoryData[] = [];
  stockTrends: StockTrendData[] = [];
  dashboardStats: DashboardStats | null = null;

  // Period selection
  selectedPeriod: AnalyticsPeriod = 'last12months';

  // Chart configurations
  revenueChartType: ChartType = 'bar';
  categoriesChartType: ChartType = 'pie';
  stockTrendsChartType: ChartType = 'line';

  revenueChartData: ChartData<'bar'> = {
    labels: [],
    datasets: []
  };

  categoriesChartData: ChartData<'pie'> = {
    labels: [],
    datasets: []
  };

  stockTrendsChartData: ChartData<'line'> = {
    labels: [],
    datasets: []
  };

  revenueChartOptions: ChartConfiguration<'bar'>['options'] = {
    responsive: true,
    plugins: {
      legend: {
        display: true,
        position: 'top'
      },
      title: {
        display: true,
        text: 'Monthly Revenue'
      }
    },
    scales: {
      y: {
        beginAtZero: true,
        ticks: {
          callback: function(value) {
            return '$' + value.toLocaleString();
          }
        }
      }
    }
  };

  categoriesChartOptions: ChartConfiguration<'pie'>['options'] = {
    responsive: true,
    plugins: {
      legend: {
        display: true,
        position: 'right'
      },
      title: {
        display: true,
        text: 'Top Medication Categories'
      }
    }
  };

  stockTrendsChartOptions: ChartConfiguration<'line'>['options'] = {
    responsive: true,
    plugins: {
      legend: {
        display: true,
        position: 'top'
      },
      title: {
        display: true,
        text: 'Stock Level Trends'
      }
    },
    scales: {
      y: {
        beginAtZero: true
      }
    }
  };

  ngOnInit(): void {
    this.loadData();
  }

  /**
   * Load all analytics data
   * Demonstrates using getDashboardStats for fetching all data at once
   */
  loadData(): void {
    this.isLoading = true;
    this.errorMessage = null;

    this.pharmacyService.getDashboardStats(
      this.selectedPeriod,
      undefined,
      10,
      30,
      true // Use cache
    ).subscribe({
      next: (stats) => {
        this.dashboardStats = stats;
        this.monthlyRevenue = stats.monthlyRevenue;
        this.topCategories = stats.topCategories;
        this.stockTrends = stats.stockTrends;
        
        this.updateCharts();
        this.isLoading = false;
      },
      error: (error) => {
        console.error('Error loading analytics:', error);
        this.errorMessage = error.message || 'Failed to load analytics data';
        this.isLoading = false;
      }
    });
  }

  /**
   * Load individual analytics methods (alternative approach)
   * Demonstrates fetching each chart data separately
   */
  loadDataSeparately(): void {
    this.isLoading = true;
    this.errorMessage = null;

    // Example: Load monthly revenue
    this.pharmacyService.getMonthlyRevenue(this.selectedPeriod).subscribe({
      next: (data) => {
        this.monthlyRevenue = data;
        this.updateRevenueChart();
      },
      error: (error) => {
        console.error('Error loading monthly revenue:', error);
      }
    });

    // Example: Load top categories
    this.pharmacyService.getTopMedicationCategories(10).subscribe({
      next: (data) => {
        this.topCategories = data;
        this.updateCategoriesChart();
      },
      error: (error) => {
        console.error('Error loading top categories:', error);
      }
    });

    // Example: Load stock trends
    this.pharmacyService.getStockTrends(undefined, 30).subscribe({
      next: (data) => {
        this.stockTrends = data;
        this.updateStockTrendsChart();
        this.isLoading = false;
      },
      error: (error) => {
        console.error('Error loading stock trends:', error);
        this.isLoading = false;
      }
    });
  }

  /**
   * Update all charts with current data
   */
  updateCharts(): void {
    this.updateRevenueChart();
    this.updateCategoriesChart();
    this.updateStockTrendsChart();
  }

  /**
   * Update revenue bar chart
   */
  updateRevenueChart(): void {
    this.revenueChartData = {
      labels: this.monthlyRevenue.map(item => item.monthAbbr || item.month),
      datasets: [{
        label: 'Revenue',
        data: this.monthlyRevenue.map(item => item.revenue),
        backgroundColor: 'rgba(54, 162, 235, 0.6)',
        borderColor: 'rgba(54, 162, 235, 1)',
        borderWidth: 1
      }]
    };
  }

  /**
   * Update categories pie chart
   */
  updateCategoriesChart(): void {
    this.categoriesChartData = {
      labels: this.topCategories.map(item => item.category),
      datasets: [{
        label: 'Medications',
        data: this.topCategories.map(item => item.count),
        backgroundColor: [
          'rgba(255, 99, 132, 0.6)',
          'rgba(54, 162, 235, 0.6)',
          'rgba(255, 206, 86, 0.6)',
          'rgba(75, 192, 192, 0.6)',
          'rgba(153, 102, 255, 0.6)',
          'rgba(255, 159, 64, 0.6)',
          'rgba(199, 199, 199, 0.6)',
          'rgba(83, 102, 255, 0.6)'
        ]
      }]
    };
  }

  /**
   * Update stock trends line chart
   */
  updateStockTrendsChart(): void {
    // Group by medication for multiple lines
    const medicationMap = new Map<number, { name: string; data: { date: string; stock: number }[] }>();
    
    this.stockTrends.forEach(trend => {
      if (!medicationMap.has(trend.medicationId)) {
        medicationMap.set(trend.medicationId, {
          name: trend.medicationName,
          data: []
        });
      }
      medicationMap.get(trend.medicationId)!.data.push({
        date: trend.date,
        stock: trend.stockLevel
      });
    });

    // Get unique dates for labels
    const dates = [...new Set(this.stockTrends.map(t => t.date))].sort();
    
    // Create datasets for each medication (limit to top 5 for readability)
    const datasets = Array.from(medicationMap.entries())
      .slice(0, 5)
      .map(([id, med], index) => {
        const colors = [
          'rgba(255, 99, 132, 1)',
          'rgba(54, 162, 235, 1)',
          'rgba(255, 206, 86, 1)',
          'rgba(75, 192, 192, 1)',
          'rgba(153, 102, 255, 1)'
        ];
        
        const data = dates.map(date => {
          const point = med.data.find(d => d.date === date);
          return point ? point.stock : null;
        });

        return {
          label: med.name,
          data,
          borderColor: colors[index],
          backgroundColor: colors[index].replace('1)', '0.1)'),
          tension: 0.4
        };
      });

    this.stockTrendsChartData = {
      labels: dates,
      datasets
    };
  }

  /**
   * Handle period change
   */
  onPeriodChange(): void {
    this.loadData();
  }

  /**
   * Clear analytics cache
   */
  clearCache(): void {
    this.pharmacyService.clearAnalyticsCache();
    this.loadData();
  }
}

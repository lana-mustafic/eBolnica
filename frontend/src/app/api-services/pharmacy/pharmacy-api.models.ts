export interface MedicationDto {
  id: number;
  name: string;
  genericName?: string | null;
  description?: string | null;
  manufacturer?: string | null;
  price: number;
  stockQuantity: number;
  minimumStockLevel: number;
  expiryDate?: string | null;
  batchNumber?: string | null;
  isActive: boolean;
  requiresPrescription: boolean;
  category?: string | null;
  dosageForm?: string | null;
  strength?: string | null;
  createdAt: string;
  updatedAt?: string | null;
  rowVersion?: string | null;
  primaryImageUrl?: string | null;
  primaryImageId?: number | null;
}

export interface MedicationStockHistoryDto {
  id: number;
  occurredAt: string;
  changeQuantity: number;
  stockAfter: number;
  reason: string;
  referenceLabel?: string | null;
}

export interface MedicationUpsertCommand {
  name: string;
  genericName?: string | null;
  description?: string | null;
  manufacturer?: string | null;
  price: number;
  stockQuantity: number;
  minimumStockLevel: number;
  expiryDate: string;
  batchNumber?: string | null;
  isActive: boolean;
  requiresPrescription: boolean;
  category: string;
  dosageForm?: string | null;
  strength?: string | null;
  rowVersion?: string | null;
}

export interface ListMedicationsRequest {
  search?: string;
  category?: string;
  isActive?: boolean;
  includeInactive?: boolean;
  stockStatus?: string;
  requiresPrescription?: boolean;
  pageNumber?: number;
  pageSize?: number;
  sortBy?: string;
  sortOrder?: 'asc' | 'desc';
}

export interface ListMedicationsResponse {
  items: MedicationDto[];
  totalCount: number;
  currentPage: number;
  pageSize: number;
  totalPages: number;
}

export interface MedicationNameAvailabilityDto {
  isAvailable: boolean;
}

export interface MedicationAutocompleteSuggestion {
  id: number;
  name: string;
  category?: string | null;
  manufacturer?: string | null;
}

export interface MedicationImportResult {
  successCount: number;
  failureCount: number;
  totalRows: number;
  committed: boolean;
  importedMedicationIds: number[];
  errors: { rowNumber: number; reason: string; field?: string | null; value?: string | null }[];
  batchError?: string | null;
}

export interface InventoryResponse {
  items: MedicationDto[];
  lowStockAlerts: MedicationDto[];
  expiryAlerts: MedicationDto[];
  lowStockAlertCount: number;
  expiryAlertCount: number;
  totalCount: number;
  currentPage: number;
  pageSize: number;
  totalPages: number;
}

export interface MedicationImageDto {
  id: number;
  medicationId: number;
  fileName: string;
  relativeUrl: string;
  isPrimary: boolean;
  sortOrder: number;
  fileSizeBytes?: number | null;
}

export interface PrescriptionPatientDto {
  id: number;
  firstName: string;
  lastName: string;
  phoneNumber?: string | null;
}

export interface PrescriptionDoctorDto {
  firstName: string;
  lastName: string;
  phoneNumber?: string | null;
  specialization?: string | null;
  licenseNumber: string;
  birthDate?: string | null;
  address?: string | null;
  email?: string | null;
}

export interface PrescriptionPharmacistDto {
  id: number;
  firstName: string;
  lastName: string;
  licenseNumber: string;
  phoneNumber?: string | null;
  address?: string | null;
  hireDate?: string | null;
  email?: string | null;
}

export interface PrescriptionItemDto {
  id: number;
  prescriptionId: number;
  medicationId: number;
  medicationName: string;
  quantity: number;
  instructions?: string | null;
  unitPrice: number;
  totalPrice: number;
  stockQuantity?: number | null;
  minimumStockLevel?: number | null;
}

export interface PrescriptionDto {
  id: number;
  prescriptionNumber: string;
  medicalReportId: number;
  patientId: number;
  patient: PrescriptionPatientDto;
  doctorId: number;
  doctor: PrescriptionDoctorDto;
  pharmacistId?: number | null;
  pharmacist?: PrescriptionPharmacistDto | null;
  status: string;
  prescribedDate: string;
  dispensedDate?: string | null;
  totalAmount: number;
  notes?: string | null;
  createdAt: string;
  updatedAt?: string | null;
  prescriptionItems: PrescriptionItemDto[];
}

export interface ListPrescriptionsRequest {
  status?: string;
  search?: string;
  patientSearch?: string;
  doctorSearch?: string;
  prescribedFrom?: string;
  prescribedTo?: string;
  pageNumber?: number;
  pageSize?: number;
  sortBy?: string;
  sortOrder?: 'asc' | 'desc';
}

export interface ListPrescriptionsResponse {
  items: PrescriptionDto[];
  totalCount: number;
  currentPage: number;
  pageSize: number;
  totalPages: number;
}

export interface DispensePrescriptionRequest {
  dispensedDate?: string | null;
}

export interface CreatePrescriptionItemRequest {
  medicationId: number;
  quantity: number;
  instructions?: string | null;
}

export interface CreatePrescriptionRequest {
  medicalReportId: number;
  patientId: number;
  notes?: string | null;
  prescriptionItems: CreatePrescriptionItemRequest[];
}

export interface PrescriptionFormPatientDto {
  id: number;
  firstName: string;
  lastName: string;
  phoneNumber?: string | null;
}

export interface PrescriptionFormMedicalReportDto {
  id: number;
  createdAt: string;
  diagnosis?: string | null;
  doctorFirstName: string;
  doctorLastName: string;
  doctorSpecialization?: string | null;
}

export interface MonthlyRevenueItemDto {
  month: string;
  monthShort: string;
  yearMonth: string;
  revenue: number;
  prescriptionCount: number;
}

export interface RevenueDataDto {
  data: MonthlyRevenueItemDto[];
  totalRevenue: number;
  averageMonthlyRevenue: number;
  revenueChangePercentage: number;
}

export interface CategoryItemDto {
  category: string;
  medicationCount: number;
  percentage: number;
  totalValue: number;
}

export interface CategoriesDataDto {
  data: CategoryItemDto[];
  totalCategories: number;
  totalMedications: number;
}

export interface StockTrendItemDto {
  date: string;
  medicationId: number;
  medicationName: string;
  stockLevel: number;
  quantity: number;
  status: string;
}

export interface StockTrendsDataDto {
  data: StockTrendItemDto[];
  medications: {
    id: number;
    name: string;
    color: string;
    currentStock: number;
    trendDirection?: number;
  }[];
  timeline?: string[];
  metricType: string;
  snapshotAt: string;
  note?: string | null;
}

export interface StatisticsSummaryDto {
  totalPrescriptions: number;
  totalMedications: number;
  totalCategories: number;
  totalRevenue: number;
  pendingPrescriptions: number;
  lowStockAlerts: number;
  expiringSoon: number;
  expiredMedications: number;
  inventoryValue: number;
}

export interface DashboardStatsResponseDto {
  monthlyRevenue: RevenueDataDto;
  topCategories: CategoriesDataDto;
  stockTrends: StockTrendsDataDto;
  metadata: { generatedAt: string; summary: StatisticsSummaryDto };
}

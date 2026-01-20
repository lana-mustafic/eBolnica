using eBolnicaAPI.Data;
using eBolnicaAPI.Models.DTOs;
using eBolnicaAPI.Models.Entities;
using eBolnicaAPI.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace eBolnicaAPI.Controllers
{
    [Route("api/pharmacy")]
    [ApiController]
    public class PharmacyController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly UserManager<AppUser> _userManager;
        private readonly IPharmacyService _pharmacyService;

        public PharmacyController(AppDbContext context, UserManager<AppUser> userManager, IPharmacyService pharmacyService)
        {
            _context = context;
            _userManager = userManager;
            _pharmacyService = pharmacyService;
        }

        #region Medications CRUD

        [HttpGet("medications")]
        [Authorize(Roles = "Pharmacist")]
        public async Task<IActionResult> GetMedications(
            [FromQuery] string? category = null,
            [FromQuery] string? search = null,
            [FromQuery] string? stockStatus = null,
            [FromQuery] bool? requiresPrescription = null,
            [FromQuery] bool? isActive = null,
            [FromQuery] int page = 1, // Backward compatibility: keep 'page' parameter
            [FromQuery] int pageNumber = 1, // New parameter name
            [FromQuery] int pageSize = 10,
            [FromQuery] string? sortBy = null,
            [FromQuery] string? sortOrder = "desc")
        {
            // Start with base query
            var query = _context.Medications.AsQueryable();

            // Filter 1: Active Status
            // Default behavior: show only active medications (isActive=null means use default)
            // If isActive has a value (true/false), apply that filter
            // To show all medications, frontend should explicitly pass isActive=null or handle it differently
            // For backwards compatibility and default behavior, we show active only when not specified
            if (isActive.HasValue)
            {
                query = query.Where(m => m.IsActive == isActive.Value);
            }
            else
            {
                // Default: show only active medications when isActive is not provided
                query = query.Where(m => m.IsActive);
            }

            // Filter 2: Category (exact match, case-insensitive)
            if (!string.IsNullOrEmpty(category))
            {
                query = query.Where(m => m.Category != null && m.Category.ToLower() == category.ToLower());
            }

            // Filter 3: Search (across Name, GenericName, and Manufacturer - case-insensitive)
            if (!string.IsNullOrEmpty(search))
            {
                var searchTerm = search.ToLower();
                query = query.Where(m =>
                    m.Name.ToLower().Contains(searchTerm) ||
                    (m.GenericName != null && m.GenericName.ToLower().Contains(searchTerm)) ||
                    (m.Manufacturer != null && m.Manufacturer.ToLower().Contains(searchTerm))
                );
            }

            // Filter 4: Stock Status
            if (!string.IsNullOrEmpty(stockStatus))
            {
                var status = stockStatus.ToLower();
                switch (status)
                {
                    case "low stock":
                        query = query.Where(m => m.StockQuantity < m.MinimumStockLevel && m.StockQuantity > 0);
                        break;
                    case "out of stock":
                        query = query.Where(m => m.StockQuantity == 0);
                        break;
                    case "normal stock":
                        query = query.Where(m => m.StockQuantity >= m.MinimumStockLevel);
                        break;
                    // If invalid stockStatus, ignore the filter
                }
            }

            // Filter 5: Requires Prescription
            if (requiresPrescription.HasValue)
            {
                query = query.Where(m => m.RequiresPrescription == requiresPrescription.Value);
            }

            // NEW: Apply dynamic filters from query parameters using PharmacyService
            // Supports filters like: minPrice=10, maxPrice=100, category=antibiotics, status=active
            query = _pharmacyService.GetFilteredMedications(query, Request.Query);

            // Get total count BEFORE pagination (for performance optimization)
            // Use AsNoTracking() for read-only count query
            var totalCount = await query.AsNoTracking().CountAsync();

            // NEW: Parameter validation and normalization
            // Use pageNumber if provided, otherwise fall back to 'page' for backward compatibility
            var currentPage = pageNumber != 1 ? pageNumber : (page != 1 ? page : 1);
            
            // Edge case: Validate pageNumber > 0
            if (currentPage < 1) currentPage = 1;

            // Validate and clamp pageSize (1-100 range, default: 10)
            pageSize = Math.Clamp(pageSize, 1, 100);

            // Edge case: Handle empty results
            if (totalCount == 0)
            {
                return Ok(new PaginatedResponse<MedicationDto>(
                    new List<MedicationDto>(),
                    totalCount: 0,
                    pageNumber: currentPage,
                    pageSize: pageSize
                ));
            }

            // Edge case: Handle pageNumber out of range - adjust to last valid page
            var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);
            if (currentPage > totalPages && totalPages > 0)
            {
                currentPage = totalPages;
            }

            // NEW: Apply sorting using PharmacyService
            query = _pharmacyService.ApplySorting(query, sortBy, sortOrder);

            // Calculate Skip and Take values for server-side pagination
            // Skip = (pageNumber - 1) * pageSize
            var skipValue = (currentPage - 1) * pageSize;
            var takeValue = pageSize;

            // Apply pagination at database level using Entity Framework
            // Use AsNoTracking() for read-only queries to improve performance
            var medications = await query
                .AsNoTracking()
                .Skip(skipValue)
                .Take(takeValue)
                .ToListAsync();

            // Map to DTOs
            var dtoList = medications.Select(m => new MedicationDto
            {
                Id = m.Id,
                Name = m.Name,
                GenericName = m.GenericName,
                Description = m.Description,
                Manufacturer = m.Manufacturer,
                Price = m.Price,
                StockQuantity = m.StockQuantity,
                MinimumStockLevel = m.MinimumStockLevel,
                ExpiryDate = m.ExpiryDate,
                BatchNumber = m.BatchNumber,
                IsActive = m.IsActive,
                RequiresPrescription = m.RequiresPrescription,
                Category = m.Category,
                DosageForm = m.DosageForm,
                Strength = m.Strength,
                CreatedAt = m.CreatedAt,
                UpdatedAt = m.UpdatedAt
            }).ToList();

            // Return paginated response using PaginatedResponse<T> class
            var response = new PaginatedResponse<MedicationDto>(
                dtoList,
                totalCount,
                currentPage,
                pageSize
            );

            return Ok(response);
        }

        [HttpGet("medications/{id}")]
        [Authorize(Roles = "Pharmacist")]
        public async Task<IActionResult> GetMedication(int id)
        {
            var medication = await _context.Medications.FindAsync(id);

            if (medication == null || !medication.IsActive)
            {
                return NotFound("Medication not found");
            }

            var dto = new MedicationDto
            {
                Id = medication.Id,
                Name = medication.Name,
                GenericName = medication.GenericName,
                Description = medication.Description,
                Manufacturer = medication.Manufacturer,
                Price = medication.Price,
                StockQuantity = medication.StockQuantity,
                MinimumStockLevel = medication.MinimumStockLevel,
                ExpiryDate = medication.ExpiryDate,
                BatchNumber = medication.BatchNumber,
                IsActive = medication.IsActive,
                RequiresPrescription = medication.RequiresPrescription,
                Category = medication.Category,
                DosageForm = medication.DosageForm,
                Strength = medication.Strength,
                CreatedAt = medication.CreatedAt,
                UpdatedAt = medication.UpdatedAt
            };

            return Ok(dto);
        }

        [HttpPost("medications")]
        [Authorize(Roles = "Pharmacist")]
        public async Task<IActionResult> CreateMedication([FromBody] MedicationCreateDto dto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            if (dto.ExpiryDate <= DateTime.Now)
            {
                return BadRequest("Expiry date must be in the future");
            }

            var medication = new Medication
            {
                Name = dto.Name,
                GenericName = dto.GenericName,
                Description = dto.Description,
                Manufacturer = dto.Manufacturer,
                Price = dto.Price,
                StockQuantity = dto.StockQuantity,
                MinimumStockLevel = dto.MinimumStockLevel,
                ExpiryDate = dto.ExpiryDate,
                BatchNumber = dto.BatchNumber,
                IsActive = dto.IsActive,
                RequiresPrescription = dto.RequiresPrescription,
                Category = dto.Category,
                DosageForm = dto.DosageForm,
                Strength = dto.Strength,
                CreatedAt = DateTime.Now
            };

            _context.Medications.Add(medication);
            await _context.SaveChangesAsync();

            var resultDto = new MedicationDto
            {
                Id = medication.Id,
                Name = medication.Name,
                GenericName = medication.GenericName,
                Description = medication.Description,
                Manufacturer = medication.Manufacturer,
                Price = medication.Price,
                StockQuantity = medication.StockQuantity,
                MinimumStockLevel = medication.MinimumStockLevel,
                ExpiryDate = medication.ExpiryDate,
                BatchNumber = medication.BatchNumber,
                IsActive = medication.IsActive,
                RequiresPrescription = medication.RequiresPrescription,
                Category = medication.Category,
                DosageForm = medication.DosageForm,
                Strength = medication.Strength,
                CreatedAt = medication.CreatedAt,
                UpdatedAt = medication.UpdatedAt
            };

            return CreatedAtAction(nameof(GetMedication), new { id = medication.Id }, resultDto);
        }

        [HttpPut("medications/{id}")]
        [Authorize(Roles = "Pharmacist")]
        public async Task<IActionResult> UpdateMedication(int id, [FromBody] MedicationCreateDto dto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var medication = await _context.Medications.FindAsync(id);

            if (medication == null || !medication.IsActive)
            {
                return NotFound("Medication not found");
            }

            if (dto.ExpiryDate <= DateTime.Now)
            {
                return BadRequest("Expiry date must be in the future");
            }

            medication.Name = dto.Name;
            medication.GenericName = dto.GenericName;
            medication.Description = dto.Description;
            medication.Manufacturer = dto.Manufacturer;
            medication.Price = dto.Price;
            medication.StockQuantity = dto.StockQuantity;
            medication.MinimumStockLevel = dto.MinimumStockLevel;
            medication.ExpiryDate = dto.ExpiryDate;
            medication.BatchNumber = dto.BatchNumber;
            medication.IsActive = dto.IsActive;
            medication.RequiresPrescription = dto.RequiresPrescription;
            medication.Category = dto.Category;
            medication.DosageForm = dto.DosageForm;
            medication.Strength = dto.Strength;
            medication.UpdatedAt = DateTime.Now;

            await _context.SaveChangesAsync();

            var resultDto = new MedicationDto
            {
                Id = medication.Id,
                Name = medication.Name,
                GenericName = medication.GenericName,
                Description = medication.Description,
                Manufacturer = medication.Manufacturer,
                Price = medication.Price,
                StockQuantity = medication.StockQuantity,
                MinimumStockLevel = medication.MinimumStockLevel,
                ExpiryDate = medication.ExpiryDate,
                BatchNumber = medication.BatchNumber,
                IsActive = medication.IsActive,
                RequiresPrescription = medication.RequiresPrescription,
                Category = medication.Category,
                DosageForm = medication.DosageForm,
                Strength = medication.Strength,
                CreatedAt = medication.CreatedAt,
                UpdatedAt = medication.UpdatedAt
            };

            return Ok(resultDto);
        }

        [HttpDelete("medications/{id}")]
        [Authorize(Roles = "Pharmacist")]
        public async Task<IActionResult> DeleteMedication(int id)
        {
            var medication = await _context.Medications.FindAsync(id);

            if (medication == null)
            {
                return NotFound("Medication not found");
            }

            medication.IsActive = false;
            medication.UpdatedAt = DateTime.Now;

            await _context.SaveChangesAsync();

            return NoContent();
        }

        #endregion

        #region Prescriptions Management

        [HttpGet("prescriptions")]
        [Authorize(Roles = "Pharmacist")]
        public async Task<IActionResult> GetPrescriptions(
            [FromQuery] string? status = null,
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 10,
            [FromQuery] string? sortBy = null,
            [FromQuery] string? sortOrder = "desc")
        {
            var query = _context.Prescriptions
                .Include(p => p.Patient)
                    .ThenInclude(pat => pat.AppUser)
                .Include(p => p.Doctor)
                    .ThenInclude(d => d.AppUser)
                .Include(p => p.Pharmacist)
                    .ThenInclude(ph => ph.AppUser)
                .Include(p => p.PrescriptionItems)
                    .ThenInclude(pi => pi.Medication)
                .AsQueryable();

            // Existing filter: Status
            if (!string.IsNullOrEmpty(status))
            {
                query = query.Where(p => p.Status == status);
            }

            // NEW: Apply dynamic filters from query parameters using PharmacyService
            // Supports filters like: patientId=1, doctorId=2, minAmount=100, maxAmount=500
            query = _pharmacyService.GetFilteredPrescriptions(query, Request.Query);

            // Get total count BEFORE pagination (for performance optimization)
            // Use AsNoTracking() for read-only count query
            var totalCount = await query.AsNoTracking().CountAsync();

            // Parameter validation: pageNumber > 0
            if (pageNumber < 1) pageNumber = 1;
            
            // Validate and clamp pageSize (1-100 range, default: 10)
            pageSize = Math.Clamp(pageSize, 1, 100);

            // Edge case: Handle empty results
            if (totalCount == 0)
            {
                return Ok(new PaginatedResponse<PrescriptionDto>(
                    new List<PrescriptionDto>(),
                    totalCount: 0,
                    pageNumber: pageNumber,
                    pageSize: pageSize
                ));
            }

            // Edge case: Handle pageNumber out of range - adjust to last valid page
            var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);
            if (pageNumber > totalPages && totalPages > 0)
            {
                pageNumber = totalPages;
            }

            // Apply sorting using PharmacyService
            query = _pharmacyService.ApplySorting(query, sortBy, sortOrder);

            // Calculate Skip and Take values for server-side pagination
            // Skip = (pageNumber - 1) * pageSize
            var skipValue = (pageNumber - 1) * pageSize;
            var takeValue = pageSize;

            // Apply pagination at database level using Entity Framework
            var prescriptions = await query
                .Skip(skipValue)
                .Take(takeValue)
                .ToListAsync();

            var dtoList = prescriptions.Select(p => new PrescriptionDto
            {
                Id = p.Id,
                PrescriptionNumber = p.PrescriptionNumber,
                MedicalReportId = p.MedicalReportId,
                PatientId = p.PatientId,
                Patient = new PatientDataDto
                {
                    Id = p.Patient.Id,
                    FirstName = p.Patient.FirstName,
                    LastName = p.Patient.LastName
                },
                DoctorId = p.DoctorId,
                Doctor = new DoctorDataDto
                {
                    FirstName = p.Doctor.FirstName,
                    LastName = p.Doctor.LastName,
                    PhoneNumber = p.Doctor.PhoneNumber,
                    Specialization = p.Doctor.Specialization,
                    LicenseNumber = p.Doctor.LicenseNumber,
                    BirthDate = p.Doctor.BirthDate ?? DateTime.MinValue,
                    Address = p.Doctor.Address,
                    Email = p.Doctor.AppUser?.Email ?? ""
                },
                PharmacistId = p.PharmacistId,
                Pharmacist = p.Pharmacist != null ? new PharmacistDataDto
                {
                    Id = p.Pharmacist.Id,
                    FirstName = p.Pharmacist.FirstName,
                    LastName = p.Pharmacist.LastName,
                    LicenseNumber = p.Pharmacist.LicenseNumber,
                    PhoneNumber = p.Pharmacist.PhoneNumber,
                    Address = p.Pharmacist.Address,
                    HireDate = p.Pharmacist.HireDate,
                    Email = p.Pharmacist.AppUser?.Email ?? "",
                    UserName = p.Pharmacist.AppUser?.UserName ?? ""
                } : null,
                Status = p.Status,
                PrescribedDate = p.PrescribedDate,
                DispensedDate = p.DispensedDate,
                TotalAmount = p.TotalAmount,
                Notes = p.Notes,
                CreatedAt = p.CreatedAt,
                UpdatedAt = p.UpdatedAt,
                PrescriptionItems = p.PrescriptionItems.Select(pi => new PrescriptionItemDto
                {
                    Id = pi.Id,
                    PrescriptionId = pi.PrescriptionId,
                    MedicationId = pi.MedicationId,
                    MedicationName = pi.Medication.Name,
                    Quantity = pi.Quantity,
                    Instructions = pi.Instructions,
                    UnitPrice = pi.UnitPrice
                }).ToList()
            }).ToList();

            // Return paginated response using PaginatedResponse<T> class
            var response = new PaginatedResponse<PrescriptionDto>(
                dtoList,
                totalCount,
                pageNumber,
                pageSize
            );

            return Ok(response);
        }

        [HttpGet("prescriptions/{id}")]
        [Authorize(Roles = "Pharmacist")]
        public async Task<IActionResult> GetPrescription(int id)
        {
            var prescription = await _context.Prescriptions
                .Include(p => p.Patient)
                    .ThenInclude(pat => pat.AppUser)
                .Include(p => p.Doctor)
                    .ThenInclude(d => d.AppUser)
                .Include(p => p.Pharmacist)
                    .ThenInclude(ph => ph.AppUser)
                .Include(p => p.PrescriptionItems)
                    .ThenInclude(pi => pi.Medication)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (prescription == null)
            {
                return NotFound("Prescription not found");
            }

            var dto = new PrescriptionDto
            {
                Id = prescription.Id,
                PrescriptionNumber = prescription.PrescriptionNumber,
                MedicalReportId = prescription.MedicalReportId,
                PatientId = prescription.PatientId,
                Patient = new PatientDataDto
                {
                    Id = prescription.Patient.Id,
                    FirstName = prescription.Patient.FirstName,
                    LastName = prescription.Patient.LastName
                },
                DoctorId = prescription.DoctorId,
                Doctor = new DoctorDataDto
                {
                    FirstName = prescription.Doctor.FirstName,
                    LastName = prescription.Doctor.LastName,
                    PhoneNumber = prescription.Doctor.PhoneNumber,
                    Specialization = prescription.Doctor.Specialization,
                    LicenseNumber = prescription.Doctor.LicenseNumber,
                    BirthDate = prescription.Doctor.BirthDate ?? DateTime.MinValue,
                    Address = prescription.Doctor.Address,
                    Email = prescription.Doctor.AppUser?.Email ?? ""
                },
                PharmacistId = prescription.PharmacistId,
                Pharmacist = prescription.Pharmacist != null ? new PharmacistDataDto
                {
                    Id = prescription.Pharmacist.Id,
                    FirstName = prescription.Pharmacist.FirstName,
                    LastName = prescription.Pharmacist.LastName,
                    LicenseNumber = prescription.Pharmacist.LicenseNumber,
                    PhoneNumber = prescription.Pharmacist.PhoneNumber,
                    Address = prescription.Pharmacist.Address,
                    HireDate = prescription.Pharmacist.HireDate,
                    Email = prescription.Pharmacist.AppUser?.Email ?? "",
                    UserName = prescription.Pharmacist.AppUser?.UserName ?? ""
                } : null,
                Status = prescription.Status,
                PrescribedDate = prescription.PrescribedDate,
                DispensedDate = prescription.DispensedDate,
                TotalAmount = prescription.TotalAmount,
                Notes = prescription.Notes,
                CreatedAt = prescription.CreatedAt,
                UpdatedAt = prescription.UpdatedAt,
                PrescriptionItems = prescription.PrescriptionItems.Select(pi => new PrescriptionItemDto
                {
                    Id = pi.Id,
                    PrescriptionId = pi.PrescriptionId,
                    MedicationId = pi.MedicationId,
                    MedicationName = pi.Medication.Name,
                    Quantity = pi.Quantity,
                    Instructions = pi.Instructions,
                    UnitPrice = pi.UnitPrice
                }).ToList()
            };

            return Ok(dto);
        }

        [HttpPost("prescriptions")]
        [Authorize(Roles = "Doctor")]
        public async Task<IActionResult> CreatePrescription([FromBody] PrescriptionCreateDto dto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var doctorId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (doctorId == null)
            {
                return Unauthorized();
            }

            var doctor = await _context.Doctors
                .FirstOrDefaultAsync(d => d.AppUserId == doctorId);

            if (doctor == null)
            {
                return NotFound("Doctor not found");
            }

            if (dto.DoctorId != doctor.Id)
            {
                return Forbid("You can only create prescriptions for your own patients");
            }

            var medicalReport = await _context.MedicalReports
                .Include(mr => mr.Doctor)
                .FirstOrDefaultAsync(mr => mr.Id == dto.MedicalReportId);

            if (medicalReport == null)
            {
                return NotFound("Medical report not found");
            }

            if (medicalReport.DoctorId != doctor.Id)
            {
                return Forbid("Medical report does not belong to you");
            }

            var patient = await _context.Patients.FindAsync(dto.PatientId);

            if (patient == null)
            {
                return NotFound("Patient not found");
            }

            if (patient.DoctorId != doctor.Id)
            {
                return Forbid("Patient is not assigned to you");
            }

            if (dto.PrescriptionItems == null || !dto.PrescriptionItems.Any())
            {
                return BadRequest("At least one prescription item is required");
            }

            var prescriptionNumber = await GeneratePrescriptionNumberAsync();

            var prescription = new Prescription
            {
                PrescriptionNumber = prescriptionNumber,
                MedicalReportId = dto.MedicalReportId,
                PatientId = dto.PatientId,
                DoctorId = dto.DoctorId,
                Status = "Pending",
                PrescribedDate = DateTime.Now,
                Notes = dto.Notes,
                CreatedAt = DateTime.Now
            };

            _context.Prescriptions.Add(prescription);
            await _context.SaveChangesAsync();

            decimal totalAmount = 0;

            foreach (var itemDto in dto.PrescriptionItems)
            {
                var medication = await _context.Medications.FindAsync(itemDto.MedicationId);

                if (medication == null || !medication.IsActive)
                {
                    return BadRequest($"Medication with ID {itemDto.MedicationId} not found or inactive");
                }

                var unitPrice = medication.Price;
                var itemTotalPrice = unitPrice * itemDto.Quantity;
                totalAmount += itemTotalPrice;

                var prescriptionItem = new PrescriptionItem
                {
                    PrescriptionId = prescription.Id,
                    MedicationId = itemDto.MedicationId,
                    Quantity = itemDto.Quantity,
                    Instructions = itemDto.Instructions,
                    UnitPrice = unitPrice,
                    TotalPrice = itemTotalPrice,
                    CreatedAt = DateTime.Now
                };

                _context.PrescriptionItems.Add(prescriptionItem);
            }

            prescription.TotalAmount = totalAmount;
            await _context.SaveChangesAsync();

            var result = await _context.Prescriptions
                .Include(p => p.Patient)
                .Include(p => p.Doctor)
                    .ThenInclude(d => d.AppUser)
                .Include(p => p.PrescriptionItems)
                    .ThenInclude(pi => pi.Medication)
                .FirstOrDefaultAsync(p => p.Id == prescription.Id);

            var resultDto = new PrescriptionDto
            {
                Id = result.Id,
                PrescriptionNumber = result.PrescriptionNumber,
                MedicalReportId = result.MedicalReportId,
                PatientId = result.PatientId,
                Patient = new PatientDataDto
                {
                    Id = result.Patient.Id,
                    FirstName = result.Patient.FirstName,
                    LastName = result.Patient.LastName
                },
                DoctorId = result.DoctorId,
                Doctor = new DoctorDataDto
                {
                    FirstName = result.Doctor.FirstName,
                    LastName = result.Doctor.LastName,
                    PhoneNumber = result.Doctor.PhoneNumber,
                    Specialization = result.Doctor.Specialization,
                    LicenseNumber = result.Doctor.LicenseNumber,
                    BirthDate = result.Doctor.BirthDate ?? DateTime.MinValue,
                    Address = result.Doctor.Address,
                    Email = result.Doctor.AppUser?.Email ?? ""
                },
                Status = result.Status,
                PrescribedDate = result.PrescribedDate,
                TotalAmount = result.TotalAmount,
                Notes = result.Notes,
                CreatedAt = result.CreatedAt,
                PrescriptionItems = result.PrescriptionItems.Select(pi => new PrescriptionItemDto
                {
                    Id = pi.Id,
                    PrescriptionId = pi.PrescriptionId,
                    MedicationId = pi.MedicationId,
                    MedicationName = pi.Medication.Name,
                    Quantity = pi.Quantity,
                    Instructions = pi.Instructions,
                    UnitPrice = pi.UnitPrice
                }).ToList()
            };

            return CreatedAtAction(nameof(GetPrescription), new { id = prescription.Id }, resultDto);
        }

        [HttpPost("prescriptions/{id}/dispense")]
        [Authorize(Roles = "Pharmacist")]
        public async Task<IActionResult> DispensePrescription(int id, [FromBody] PrescriptionDispenseDto dto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var prescription = await _context.Prescriptions
                .Include(p => p.PrescriptionItems)
                    .ThenInclude(pi => pi.Medication)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (prescription == null)
            {
                return NotFound("Prescription not found");
            }

            if (prescription.Status != "Pending")
            {
                return BadRequest($"Prescription is already {prescription.Status}. Only pending prescriptions can be dispensed.");
            }

            var pharmacist = await _context.Pharmacists.FindAsync(dto.PharmacistId);

            if (pharmacist == null)
            {
                return NotFound("Pharmacist not found");
            }

            foreach (var item in prescription.PrescriptionItems)
            {
                if (item.Medication.StockQuantity < item.Quantity)
                {
                    return BadRequest($"Insufficient stock for medication {item.Medication.Name}. Available: {item.Medication.StockQuantity}, Required: {item.Quantity}");
                }
            }

            foreach (var item in prescription.PrescriptionItems)
            {
                item.Medication.StockQuantity -= item.Quantity;
                item.Medication.UpdatedAt = DateTime.Now;
            }

            prescription.Status = "Dispensed";
            prescription.PharmacistId = dto.PharmacistId;
            prescription.DispensedDate = dto.DispensedDate ?? DateTime.Now;
            prescription.UpdatedAt = DateTime.Now;

            await _context.SaveChangesAsync();

            var result = await _context.Prescriptions
                .Include(p => p.Patient)
                .Include(p => p.Doctor)
                    .ThenInclude(d => d.AppUser)
                .Include(p => p.Pharmacist)
                    .ThenInclude(ph => ph.AppUser)
                .Include(p => p.PrescriptionItems)
                    .ThenInclude(pi => pi.Medication)
                .FirstOrDefaultAsync(p => p.Id == id);

            var resultDto = new PrescriptionDto
            {
                Id = result.Id,
                PrescriptionNumber = result.PrescriptionNumber,
                MedicalReportId = result.MedicalReportId,
                PatientId = result.PatientId,
                Patient = new PatientDataDto
                {
                    Id = result.Patient.Id,
                    FirstName = result.Patient.FirstName,
                    LastName = result.Patient.LastName
                },
                DoctorId = result.DoctorId,
                Doctor = new DoctorDataDto
                {
                    FirstName = result.Doctor.FirstName,
                    LastName = result.Doctor.LastName,
                    PhoneNumber = result.Doctor.PhoneNumber,
                    Specialization = result.Doctor.Specialization,
                    LicenseNumber = result.Doctor.LicenseNumber,
                    BirthDate = result.Doctor.BirthDate ?? DateTime.MinValue,
                    Address = result.Doctor.Address,
                    Email = result.Doctor.AppUser?.Email ?? ""
                },
                PharmacistId = result.PharmacistId,
                Pharmacist = new PharmacistDataDto
                {
                    Id = result.Pharmacist.Id,
                    FirstName = result.Pharmacist.FirstName,
                    LastName = result.Pharmacist.LastName,
                    LicenseNumber = result.Pharmacist.LicenseNumber,
                    PhoneNumber = result.Pharmacist.PhoneNumber,
                    Address = result.Pharmacist.Address,
                    HireDate = result.Pharmacist.HireDate,
                    Email = result.Pharmacist.AppUser?.Email ?? "",
                    UserName = result.Pharmacist.AppUser?.UserName ?? ""
                },
                Status = result.Status,
                PrescribedDate = result.PrescribedDate,
                DispensedDate = result.DispensedDate,
                TotalAmount = result.TotalAmount,
                Notes = result.Notes,
                CreatedAt = result.CreatedAt,
                UpdatedAt = result.UpdatedAt,
                PrescriptionItems = result.PrescriptionItems.Select(pi => new PrescriptionItemDto
                {
                    Id = pi.Id,
                    PrescriptionId = pi.PrescriptionId,
                    MedicationId = pi.MedicationId,
                    MedicationName = pi.Medication.Name,
                    Quantity = pi.Quantity,
                    Instructions = pi.Instructions,
                    UnitPrice = pi.UnitPrice
                }).ToList()
            };

            return Ok(resultDto);
        }

        #endregion

        #region Inventory & Pharmacist Data

        [HttpGet("inventory")]
        [Authorize(Roles = "Pharmacist")]
        public async Task<IActionResult> GetInventory(
            [FromQuery] string? category = null,
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 10,
            [FromQuery] string? sortBy = null,
            [FromQuery] string? sortOrder = "desc")
        {
            var query = _context.Medications.Where(m => m.IsActive).AsQueryable();

            // Existing filter: Category
            if (!string.IsNullOrEmpty(category))
            {
                query = query.Where(m => m.Category == category);
            }

            // NEW: Apply dynamic filters from query parameters using PharmacyService
            // Supports filters like: minPrice=10, maxPrice=100, minStock=5, requiresPrescription=true
            query = _pharmacyService.GetFilteredInventory(query, Request.Query);

            // Get total count BEFORE pagination (for performance optimization)
            // Use AsNoTracking() for read-only count query
            var totalCount = await query.AsNoTracking().CountAsync();

            // Parameter validation: pageNumber > 0
            if (pageNumber < 1) pageNumber = 1;
            
            // Validate and clamp pageSize (1-100 range, default: 10)
            pageSize = Math.Clamp(pageSize, 1, 100);

            // Edge case: Handle empty results
            if (totalCount == 0)
            {
                return Ok(new
                {
                    items = new List<MedicationDto>(),
                    totalCount = 0,
                    totalPages = 0,
                    hasNext = false,
                    hasPrevious = false,
                    currentPage = pageNumber,
                    pageSize = pageSize,
                    LowStockAlerts = new List<MedicationDto>(),
                    ExpiryAlerts = new List<MedicationDto>()
                });
            }

            // Edge case: Handle pageNumber out of range - adjust to last valid page
            var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);
            if (pageNumber > totalPages && totalPages > 0)
            {
                pageNumber = totalPages;
            }

            // Apply sorting using PharmacyService
            query = _pharmacyService.ApplySorting(query, sortBy, sortOrder);

            // Calculate Skip and Take values for server-side pagination
            // Skip = (pageNumber - 1) * pageSize
            var skipValue = (pageNumber - 1) * pageSize;
            var takeValue = pageSize;

            // For GetInventory, we need alerts from ALL matching items (not just current page)
            // So we execute the query twice: once for all items (alerts), once with pagination (main result)
            // Note: This is necessary because alerts must reflect all matching items regardless of pagination
            
            // First: Get all matching items for alerts calculation
            var allMatchingMedications = await query.ToListAsync();
            var allDtoList = allMatchingMedications.Select(m => new MedicationDto
            {
                Id = m.Id,
                Name = m.Name,
                GenericName = m.GenericName,
                Description = m.Description,
                Manufacturer = m.Manufacturer,
                Price = m.Price,
                StockQuantity = m.StockQuantity,
                MinimumStockLevel = m.MinimumStockLevel,
                ExpiryDate = m.ExpiryDate,
                BatchNumber = m.BatchNumber,
                IsActive = m.IsActive,
                RequiresPrescription = m.RequiresPrescription,
                Category = m.Category,
                DosageForm = m.DosageForm,
                Strength = m.Strength,
                CreatedAt = m.CreatedAt,
                UpdatedAt = m.UpdatedAt
            }).ToList();

            // Second: Apply pagination at database level using Entity Framework
            // Rebuild query with same filters and sorting, then apply Skip/Take
            var paginatedQuery = _context.Medications.Where(m => m.IsActive).AsQueryable();
            if (!string.IsNullOrEmpty(category))
            {
                paginatedQuery = paginatedQuery.Where(m => m.Category == category);
            }
            paginatedQuery = _pharmacyService.GetFilteredInventory(paginatedQuery, Request.Query);
            paginatedQuery = _pharmacyService.ApplySorting(paginatedQuery, sortBy, sortOrder);
            
            var medications = await paginatedQuery
                .Skip(skipValue)
                .Take(takeValue)
                .ToListAsync();

            var dtoList = medications.Select(m => new MedicationDto
            {
                Id = m.Id,
                Name = m.Name,
                GenericName = m.GenericName,
                Description = m.Description,
                Manufacturer = m.Manufacturer,
                Price = m.Price,
                StockQuantity = m.StockQuantity,
                MinimumStockLevel = m.MinimumStockLevel,
                ExpiryDate = m.ExpiryDate,
                BatchNumber = m.BatchNumber,
                IsActive = m.IsActive,
                RequiresPrescription = m.RequiresPrescription,
                Category = m.Category,
                DosageForm = m.DosageForm,
                Strength = m.Strength,
                CreatedAt = m.CreatedAt,
                UpdatedAt = m.UpdatedAt
            }).ToList();

            // Create paginated response using PaginatedResponse<T> structure
            var paginatedResponse = new PaginatedResponse<MedicationDto>(
                dtoList,
                totalCount,
                pageNumber,
                pageSize
            );

            // Return response with pagination metadata and alerts
            // Note: Alerts are calculated from ALL matching items, not just current page
            return Ok(new
            {
                items = paginatedResponse.Items,
                totalCount = paginatedResponse.TotalCount,
                totalPages = paginatedResponse.TotalPages,
                hasNext = paginatedResponse.HasNext,
                hasPrevious = paginatedResponse.HasPrevious,
                currentPage = paginatedResponse.CurrentPage,
                pageSize = paginatedResponse.PageSize,
                LowStockAlerts = allDtoList.Where(m => m.IsLowStock).ToList(),
                ExpiryAlerts = allDtoList.Where(m => m.ExpiryDate.HasValue && m.ExpiryDate.Value <= DateTime.Now.AddDays(30) && m.ExpiryDate.Value > DateTime.Now).ToList()
            });
        }

        [HttpGet("pharmacist-data")]
        [Authorize(Roles = "Pharmacist")]
        public async Task<IActionResult> GetPharmacistData()
        {
            var pharmacistId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (pharmacistId == null)
            {
                return Unauthorized();
            }

            var pharmacist = await _context.Pharmacists
                .Include(p => p.AppUser)
                .FirstOrDefaultAsync(p => p.AppUserId == pharmacistId);

            if (pharmacist == null)
            {
                return NotFound("Pharmacist not found");
            }

            var dto = new PharmacistDataDto
            {
                Id = pharmacist.Id,
                FirstName = pharmacist.FirstName,
                LastName = pharmacist.LastName,
                LicenseNumber = pharmacist.LicenseNumber,
                PhoneNumber = pharmacist.PhoneNumber,
                Address = pharmacist.Address,
                HireDate = pharmacist.HireDate,
                Email = pharmacist.AppUser?.Email ?? "",
                UserName = pharmacist.AppUser?.UserName ?? ""
            };

            return Ok(dto);
        }

        #endregion

        #region Helper Methods

        /// <summary>
        /// Builds PharmacyQueryParameters from individual query parameters for backward compatibility
        /// </summary>
        private PharmacyQueryParameters BuildQueryParameters(
            string? category = null,
            string? search = null,
            string? stockStatus = null,
            bool? requiresPrescription = null,
            bool? isActive = null,
            int page = 1,
            int pageNumber = 1,
            int pageSize = 10,
            string? sortBy = null,
            string? sortOrder = "desc")
        {
            return new PharmacyQueryParameters
            {
                PageNumber = pageNumber != 1 ? pageNumber : (page != 1 ? page : 1),
                PageSize = pageSize,
                SortBy = sortBy,
                SortOrder = sortOrder,
                SearchTerm = search,
                Category = category,
                StockStatus = stockStatus,
                RequiresPrescription = requiresPrescription,
                IsActive = isActive
            };
        }

        private async Task<string> GeneratePrescriptionNumberAsync()
        {
            var year = DateTime.Now.Year;
            var lastPrescription = await _context.Prescriptions
                .Where(p => p.PrescriptionNumber.StartsWith($"RX-{year}-"))
                .OrderByDescending(p => p.PrescriptionNumber)
                .FirstOrDefaultAsync();

            int sequence = 1;
            if (lastPrescription != null)
            {
                var parts = lastPrescription.PrescriptionNumber.Split('-');
                if (parts.Length == 3 && int.TryParse(parts[2], out int lastSeq))
                {
                    sequence = lastSeq + 1;
                }
            }

            return $"RX-{year}-{sequence.ToString("D4")}";
        }


        #endregion
    }
}

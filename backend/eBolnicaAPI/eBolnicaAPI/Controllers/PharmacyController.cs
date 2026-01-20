using eBolnicaAPI.Data;
using eBolnicaAPI.Models.DTOs;
using eBolnicaAPI.Models.Entities;
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

        public PharmacyController(AppDbContext context, UserManager<AppUser> userManager)
        {
            _context = context;
            _userManager = userManager;
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

            // NEW: Apply dynamic filters from query parameters
            // Supports filters like: minPrice=10, maxPrice=100, category=antibiotics, status=active
            query = ApplyDynamicFilters(query, Request.Query);

            // Get total count before pagination
            var totalCount = await query.CountAsync();

            // NEW: Parameter validation and normalization
            // Use pageNumber if provided, otherwise fall back to 'page' for backward compatibility
            var currentPage = pageNumber != 1 ? pageNumber : (page != 1 ? page : 1);
            if (currentPage < 1) currentPage = 1;

            // Validate and clamp pageSize (1-100 range, default: 10)
            pageSize = Math.Clamp(pageSize, 1, 100);

            // NEW: Apply sorting
            query = ApplySorting(query, sortBy, sortOrder);

            // Calculate skip amount
            var skipAmount = (currentPage - 1) * pageSize;

            // Apply pagination
            var medications = await query
                .Skip(skipAmount)
                .Take(pageSize)
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

            // Calculate pagination metadata
            var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);
            var hasNext = currentPage < totalPages;
            var hasPrevious = currentPage > 1;

            // Return paginated response with enhanced metadata
            return Ok(new
            {
                data = dtoList,
                totalCount = totalCount,
                pageNumber = currentPage, // Use pageNumber in response
                pageSize = pageSize,
                totalPages = totalPages,
                hasNext = hasNext,
                hasPrevious = hasPrevious
            });
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

            // NEW: Apply dynamic filters from query parameters
            // Supports filters like: patientId=1, doctorId=2, minAmount=100, maxAmount=500
            query = ApplyDynamicFilters(query, Request.Query);

            // Get total count before pagination
            var totalCount = await query.CountAsync();

            // NEW: Parameter validation
            if (pageNumber < 1) pageNumber = 1;
            pageSize = Math.Clamp(pageSize, 1, 100);

            // NEW: Apply sorting
            query = ApplySorting(query, sortBy, sortOrder);

            // Calculate skip amount
            var skipAmount = (pageNumber - 1) * pageSize;

            // Apply pagination
            var prescriptions = await query
                .Skip(skipAmount)
                .Take(pageSize)
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

            // Calculate pagination metadata
            var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);
            var hasNext = pageNumber < totalPages;
            var hasPrevious = pageNumber > 1;

            // Return paginated response with metadata
            return Ok(new
            {
                data = dtoList,
                totalCount = totalCount,
                pageNumber = pageNumber,
                pageSize = pageSize,
                totalPages = totalPages,
                hasNext = hasNext,
                hasPrevious = hasPrevious
            });
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

            // NEW: Apply dynamic filters from query parameters
            // Supports filters like: minPrice=10, maxPrice=100, minStock=5, requiresPrescription=true
            query = ApplyDynamicFilters(query, Request.Query);

            // Get total count before pagination
            var totalCount = await query.CountAsync();

            // NEW: Parameter validation
            if (pageNumber < 1) pageNumber = 1;
            pageSize = Math.Clamp(pageSize, 1, 100);

            // NEW: Apply sorting
            query = ApplySorting(query, sortBy, sortOrder);

            // Calculate skip amount
            var skipAmount = (pageNumber - 1) * pageSize;

            // Calculate alerts from all matching items (not just current page)
            // This ensures alerts are accurate even when paginated
            // Create a separate query for alerts (before pagination is applied)
            var alertsQuery = query;
            var allMatchingMedications = await alertsQuery.ToListAsync();
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

            // Apply pagination for the main result set
            var medications = await query
                .Skip(skipAmount)
                .Take(pageSize)
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

            // Calculate pagination metadata
            var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);
            var hasNext = pageNumber < totalPages;
            var hasPrevious = pageNumber > 1;

            return Ok(new
            {
                Medications = dtoList,
                LowStockAlerts = allDtoList.Where(m => m.IsLowStock).ToList(),
                ExpiryAlerts = allDtoList.Where(m => m.ExpiryDate.HasValue && m.ExpiryDate.Value <= DateTime.Now.AddDays(30) && m.ExpiryDate.Value > DateTime.Now).ToList(),
                totalCount = totalCount,
                pageNumber = pageNumber,
                pageSize = pageSize,
                totalPages = totalPages,
                hasNext = hasNext,
                hasPrevious = hasPrevious
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

        /// <summary>
        /// NEW: Applies dynamic filters from query parameters to Medication queries
        /// Supports: minPrice, maxPrice, minStock, maxStock, requiresPrescription, isActive, category, status
        /// </summary>
        private IQueryable<Medication> ApplyDynamicFilters(IQueryable<Medication> query, Microsoft.AspNetCore.Http.IQueryCollection queryParams)
        {
            // Price filters
            if (queryParams.ContainsKey("minPrice") && decimal.TryParse(queryParams["minPrice"], out decimal minPrice))
            {
                query = query.Where(m => m.Price >= minPrice);
            }
            if (queryParams.ContainsKey("maxPrice") && decimal.TryParse(queryParams["maxPrice"], out decimal maxPrice))
            {
                query = query.Where(m => m.Price <= maxPrice);
            }

            // Stock quantity filters
            if (queryParams.ContainsKey("minStock") && int.TryParse(queryParams["minStock"], out int minStock))
            {
                query = query.Where(m => m.StockQuantity >= minStock);
            }
            if (queryParams.ContainsKey("maxStock") && int.TryParse(queryParams["maxStock"], out int maxStock))
            {
                query = query.Where(m => m.StockQuantity <= maxStock);
            }

            // Boolean filters
            if (queryParams.ContainsKey("requiresPrescription") && bool.TryParse(queryParams["requiresPrescription"], out bool requiresPrescription))
            {
                query = query.Where(m => m.RequiresPrescription == requiresPrescription);
            }
            if (queryParams.ContainsKey("isActive") && bool.TryParse(queryParams["isActive"], out bool isActive))
            {
                query = query.Where(m => m.IsActive == isActive);
            }

            // String filters (case-insensitive)
            if (queryParams.ContainsKey("category") && !string.IsNullOrEmpty(queryParams["category"]))
            {
                var categoryValue = queryParams["category"].ToString().ToLower();
                query = query.Where(m => m.Category != null && m.Category.ToLower() == categoryValue);
            }
            if (queryParams.ContainsKey("status") && !string.IsNullOrEmpty(queryParams["status"]))
            {
                var statusValue = queryParams["status"].ToString().ToLower();
                if (statusValue == "active")
                {
                    query = query.Where(m => m.IsActive);
                }
                else if (statusValue == "inactive")
                {
                    query = query.Where(m => !m.IsActive);
                }
            }

            return query;
        }

        /// <summary>
        /// NEW: Applies dynamic filters from query parameters to Prescription queries
        /// Supports: patientId, doctorId, pharmacistId, minAmount, maxAmount, status
        /// </summary>
        private IQueryable<Prescription> ApplyDynamicFilters(IQueryable<Prescription> query, Microsoft.AspNetCore.Http.IQueryCollection queryParams)
        {
            // ID filters
            if (queryParams.ContainsKey("patientId") && int.TryParse(queryParams["patientId"], out int patientId))
            {
                query = query.Where(p => p.PatientId == patientId);
            }
            if (queryParams.ContainsKey("doctorId") && int.TryParse(queryParams["doctorId"], out int doctorId))
            {
                query = query.Where(p => p.DoctorId == doctorId);
            }
            if (queryParams.ContainsKey("pharmacistId") && int.TryParse(queryParams["pharmacistId"], out int pharmacistId))
            {
                query = query.Where(p => p.PharmacistId == pharmacistId);
            }

            // Amount filters
            if (queryParams.ContainsKey("minAmount") && decimal.TryParse(queryParams["minAmount"], out decimal minAmount))
            {
                query = query.Where(p => p.TotalAmount >= minAmount);
            }
            if (queryParams.ContainsKey("maxAmount") && decimal.TryParse(queryParams["maxAmount"], out decimal maxAmount))
            {
                query = query.Where(p => p.TotalAmount <= maxAmount);
            }

            // Status filter (case-insensitive)
            if (queryParams.ContainsKey("status") && !string.IsNullOrEmpty(queryParams["status"]))
            {
                var statusValue = queryParams["status"].ToString();
                query = query.Where(p => p.Status == statusValue);
            }

            return query;
        }

        /// <summary>
        /// NEW: Applies sorting to Medication queries
        /// Supported sortBy values: name, price, dateCreated, stockQuantity, category
        /// </summary>
        private IQueryable<Medication> ApplySorting(IQueryable<Medication> query, string? sortBy, string? sortOrder)
        {
            // Normalize sortOrder
            var isAscending = string.IsNullOrEmpty(sortOrder) || sortOrder.ToLower() == "asc";
            var isDescending = !string.IsNullOrEmpty(sortOrder) && sortOrder.ToLower() == "desc";

            // Default sorting if sortBy is not provided
            if (string.IsNullOrEmpty(sortBy))
            {
                return isDescending ? query.OrderByDescending(m => m.CreatedAt) : query.OrderBy(m => m.CreatedAt);
            }

            // Apply sorting based on sortBy parameter
            switch (sortBy.ToLower())
            {
                case "name":
                    return isAscending ? query.OrderBy(m => m.Name) : query.OrderByDescending(m => m.Name);
                case "price":
                    return isAscending ? query.OrderBy(m => m.Price) : query.OrderByDescending(m => m.Price);
                case "datecreated":
                case "createdat":
                    return isAscending ? query.OrderBy(m => m.CreatedAt) : query.OrderByDescending(m => m.CreatedAt);
                case "stockquantity":
                case "stock":
                    return isAscending ? query.OrderBy(m => m.StockQuantity) : query.OrderByDescending(m => m.StockQuantity);
                case "category":
                    return isAscending ? query.OrderBy(m => m.Category ?? "") : query.OrderByDescending(m => m.Category ?? "");
                default:
                    // Default to CreatedAt if invalid sortBy
                    return isDescending ? query.OrderByDescending(m => m.CreatedAt) : query.OrderBy(m => m.CreatedAt);
            }
        }

        /// <summary>
        /// NEW: Applies sorting to Prescription queries
        /// Supported sortBy values: dateCreated, totalAmount, prescriptionNumber, status, prescribedDate
        /// </summary>
        private IQueryable<Prescription> ApplySorting(IQueryable<Prescription> query, string? sortBy, string? sortOrder)
        {
            // Normalize sortOrder
            var isAscending = string.IsNullOrEmpty(sortOrder) || sortOrder.ToLower() == "asc";
            var isDescending = !string.IsNullOrEmpty(sortOrder) && sortOrder.ToLower() == "desc";

            // Default sorting if sortBy is not provided
            if (string.IsNullOrEmpty(sortBy))
            {
                return isDescending ? query.OrderByDescending(p => p.CreatedAt) : query.OrderBy(p => p.CreatedAt);
            }

            // Apply sorting based on sortBy parameter
            switch (sortBy.ToLower())
            {
                case "datecreated":
                case "createdat":
                    return isAscending ? query.OrderBy(p => p.CreatedAt) : query.OrderByDescending(p => p.CreatedAt);
                case "totalamount":
                case "amount":
                    return isAscending ? query.OrderBy(p => p.TotalAmount) : query.OrderByDescending(p => p.TotalAmount);
                case "prescriptionnumber":
                case "number":
                    return isAscending ? query.OrderBy(p => p.PrescriptionNumber) : query.OrderByDescending(p => p.PrescriptionNumber);
                case "status":
                    return isAscending ? query.OrderBy(p => p.Status) : query.OrderByDescending(p => p.Status);
                case "prescribeddate":
                    return isAscending ? query.OrderBy(p => p.PrescribedDate) : query.OrderByDescending(p => p.PrescribedDate);
                default:
                    // Default to CreatedAt if invalid sortBy
                    return isDescending ? query.OrderByDescending(p => p.CreatedAt) : query.OrderBy(p => p.CreatedAt);
            }
        }

        #endregion
    }
}

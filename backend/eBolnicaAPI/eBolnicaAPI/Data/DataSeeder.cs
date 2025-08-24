using eBolnicaAPI.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace eBolnicaAPI.Data
{
    public static class DataSeeder
    {
        public static void SeedData(AppDbContext context)
        {
            // Ensure the database is created
            context.Database.EnsureCreated();

            // Check if data already exists
            if (context.Departments.Any())
            {
                return; // Database has been seeded already
            }

            // Seed in proper order to maintain relationships
            var departments = SeedDepartments(context);
            var doctors = SeedDoctors(context, departments);
            var patients = SeedPatients(context);
            var appointments = SeedAppointments(context, doctors, patients);
            var medicalRecords = SeedMedicalRecords(context, patients);
            var prescriptions = SeedPrescriptions(context, doctors, patients);

            // Save all changes
            context.SaveChanges();
        }

        private static List<Department> SeedDepartments(AppDbContext context)
        {
            var departments = new List<Department>
            {
                new Department { Name = "Cardiology", Description = "Heart and cardiovascular diseases department", Location = "Building A, Floor 2", PhoneNumber = "+1-555-111-1111", Email = "cardiology@hospital.com" },
                new Department { Name = "Pediatrics", Description = "Children's health and diseases department", Location = "Building B, Floor 1", PhoneNumber = "+1-555-111-2222", Email = "pediatrics@hospital.com" },
                new Department { Name = "Orthopedics", Description = "Bone and joint diseases department", Location = "Building C, Floor 3", PhoneNumber = "+1-555-111-3333", Email = "orthopedics@hospital.com" },
                new Department { Name = "Neurology", Description = "Nervous system diseases department", Location = "Building A, Floor 3", PhoneNumber = "+1-555-111-4444", Email = "neurology@hospital.com" },
                new Department { Name = "Emergency", Description = "Emergency medical services department", Location = "Main Building, Ground Floor", PhoneNumber = "+1-555-111-5555", Email = "emergency@hospital.com" }
            };

            context.Departments.AddRange(departments);
            context.SaveChanges();
            return departments;
        }

        private static List<Doctor> SeedDoctors(AppDbContext context, List<Department> departments)
        {
            var doctors = new List<Doctor>
            {
                new Doctor { FirstName = "Michael", LastName = "Johnson", Email = "michael.johnson@hospital.com", PhoneNumber = "+1-555-123-4567", Specialization = "Cardiologist", LicenseNumber = "CARD-001", YearsOfExperience = 15, IsActive = true, DepartmentId = departments[0].Id },
                new Doctor { FirstName = "Sarah", LastName = "Williams", Email = "sarah.williams@hospital.com", PhoneNumber = "+1-555-234-5678", Specialization = "Pediatrician", LicenseNumber = "PED-001", YearsOfExperience = 12, IsActive = true, DepartmentId = departments[1].Id },
                new Doctor { FirstName = "David", LastName = "Brown", Email = "david.brown@hospital.com", PhoneNumber = "+1-555-345-6789", Specialization = "Orthopedic Surgeon", LicenseNumber = "ORT-001", YearsOfExperience = 8, IsActive = true, DepartmentId = departments[2].Id },
                new Doctor { FirstName = "Jennifer", LastName = "Davis", Email = "jennifer.davis@hospital.com", PhoneNumber = "+1-555-456-7890", Specialization = "Neurologist", LicenseNumber = "NEU-001", YearsOfExperience = 10, IsActive = true, DepartmentId = departments[3].Id },
                new Doctor { FirstName = "Robert", LastName = "Miller", Email = "robert.miller@hospital.com", PhoneNumber = "+1-555-567-8901", Specialization = "Emergency Medicine", LicenseNumber = "EMER-001", YearsOfExperience = 7, IsActive = true, DepartmentId = departments[4].Id }
            };

            context.Doctors.AddRange(doctors);
            context.SaveChanges();
            return doctors;
        }

        private static List<Patient> SeedPatients(AppDbContext context)
        {
            var patients = new List<Patient>
            {
                new Patient { FirstName = "John", LastName = "Smith", DateOfBirth = new DateTime(1985, 3, 15), Gender = "Male", PhoneNumber = "+1-555-111-2222", Email = "john.smith@email.com", Address = "123 Main St, New York, NY", BloodType = "A+", MedicalRecordId = "MR-001", Allergies = "Penicillin", IsAdmitted = false },
                new Patient { FirstName = "Emily", LastName = "Taylor", DateOfBirth = new DateTime(1992, 7, 22), Gender = "Female", PhoneNumber = "+1-555-222-3333", Email = "emily.taylor@email.com", Address = "456 Oak Ave, Los Angeles, CA", BloodType = "O-", MedicalRecordId = "MR-002", Allergies = "None", IsAdmitted = true },
                new Patient { FirstName = "James", LastName = "Wilson", DateOfBirth = new DateTime(1978, 11, 5), Gender = "Male", PhoneNumber = "+1-555-333-4444", Email = "james.wilson@email.com", Address = "789 Pine Rd, Chicago, IL", BloodType = "B+", MedicalRecordId = "MR-003", Allergies = "Shellfish, Dust", IsAdmitted = false },
                new Patient { FirstName = "Jessica", LastName = "Anderson", DateOfBirth = new DateTime(1995, 12, 30), Gender = "Female", PhoneNumber = "+1-555-444-5555", Email = "jessica.anderson@email.com", Address = "321 Elm St, Houston, TX", BloodType = "AB+", MedicalRecordId = "MR-004", Allergies = "Peanuts", IsAdmitted = true },
                new Patient { FirstName = "Daniel", LastName = "Martinez", DateOfBirth = new DateTime(1988, 5, 18), Gender = "Male", PhoneNumber = "+1-555-555-6666", Email = "daniel.martinez@email.com", Address = "654 Maple Dr, Phoenix, AZ", BloodType = "O+", MedicalRecordId = "MR-005", Allergies = "Latex", IsAdmitted = false }
            };

            context.Patients.AddRange(patients);
            context.SaveChanges();
            return patients;
        }

        private static List<Appointment> SeedAppointments(AppDbContext context, List<Doctor> doctors, List<Patient> patients)
        {
            var appointments = new List<Appointment>
            {
                new Appointment { DateTime = DateTime.Now.AddDays(1), Status = "Scheduled", Reason = "Heart checkup", PatientId = patients[0].Id, DoctorId = doctors[0].Id },
                new Appointment { DateTime = DateTime.Now.AddDays(2), Status = "Scheduled", Reason = "Child vaccination", PatientId = patients[1].Id, DoctorId = doctors[1].Id },
                new Appointment { DateTime = DateTime.Now.AddDays(3), Status = "Completed", Reason = "Knee pain consultation", PatientId = patients[2].Id, DoctorId = doctors[2].Id },
                new Appointment { DateTime = DateTime.Now.AddDays(-1), Status = "Completed", Reason = "Headache examination", PatientId = patients[3].Id, DoctorId = doctors[3].Id },
                new Appointment { DateTime = DateTime.Now.AddHours(2), Status = "Scheduled", Reason = "Emergency consultation", PatientId = patients[4].Id, DoctorId = doctors[4].Id }
            };

            context.Appointments.AddRange(appointments);
            context.SaveChanges();
            return appointments;
        }

        private static List<MedicalRecord> SeedMedicalRecords(AppDbContext context, List<Patient> patients)
        {
            var medicalRecords = new List<MedicalRecord>
            {
                new MedicalRecord { PatientId = patients[0].Id, Diagnosis = "Hypertension", Treatment = "Medication and lifestyle changes", Notes = "Patient needs regular BP monitoring", RecordDate = DateTime.Now.AddMonths(-1) },
                new MedicalRecord { PatientId = patients[1].Id, Diagnosis = "Common cold", Treatment = "Rest and hydration", Notes = "Follow up in 1 week", RecordDate = DateTime.Now.AddDays(-5) },
                new MedicalRecord { PatientId = patients[2].Id, Diagnosis = "Arthritis", Treatment = "Physical therapy", Notes = "Consider surgery if no improvement", RecordDate = DateTime.Now.AddMonths(-2) },
                new MedicalRecord { PatientId = patients[3].Id, Diagnosis = "Migraine", Treatment = "Prescription medication", Notes = "Avoid triggers", RecordDate = DateTime.Now.AddDays(-10) },
                new MedicalRecord { PatientId = patients[4].Id, Diagnosis = "Sprained ankle", Treatment = "RICE method", Notes = "Use crutches for 1 week", RecordDate = DateTime.Now.AddDays(-3) }
            };

            context.MedicalRecords.AddRange(medicalRecords);
            context.SaveChanges();
            return medicalRecords;
        }

        private static List<Prescription> SeedPrescriptions(AppDbContext context, List<Doctor> doctors, List<Patient> patients)
        {
            var prescriptions = new List<Prescription>
            {
                new Prescription { PatientId = patients[0].Id, DoctorId = doctors[0].Id, Medication = "Lisinopril", Dosage = "10mg daily", Instructions = "Take in the morning", ExpirationDate = DateTime.Now.AddMonths(3), RefillsRemaining = 2 },
                new Prescription { PatientId = patients[1].Id, DoctorId = doctors[1].Id, Medication = "Amoxicillin", Dosage = "250mg three times daily", Instructions = "Take with food", ExpirationDate = DateTime.Now.AddMonths(1), RefillsRemaining = 0 },
                new Prescription { PatientId = patients[2].Id, DoctorId = doctors[2].Id, Medication = "Ibuprofen", Dosage = "400mg as needed", Instructions = "Take for pain", ExpirationDate = DateTime.Now.AddMonths(6), RefillsRemaining = 3 },
                new Prescription { PatientId = patients[3].Id, DoctorId = doctors[3].Id, Medication = "Sumatriptan", Dosage = "50mg as needed", Instructions = "Take at onset of migraine", ExpirationDate = DateTime.Now.AddMonths(2), RefillsRemaining = 1 },
                new Prescription { PatientId = patients[4].Id, DoctorId = doctors[4].Id, Medication = "Acetaminophen", Dosage = "500mg every 6 hours", Instructions = "Take for pain and inflammation", ExpirationDate = DateTime.Now.AddMonths(4), RefillsRemaining = 2 }
            };

            context.Prescriptions.AddRange(prescriptions);
            context.SaveChanges();
            return prescriptions;
        }
    }
}
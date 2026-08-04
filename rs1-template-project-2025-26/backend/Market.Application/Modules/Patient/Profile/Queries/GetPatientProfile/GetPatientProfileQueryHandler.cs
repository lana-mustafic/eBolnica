using Market.Application.Modules.Patient.Profile.Queries.GetPatientProfile;



public sealed class GetPatientProfileQueryHandler(IAppDbContext ctx, IAppCurrentUser currentUser)

    : IRequestHandler<GetPatientProfileQuery, GetPatientProfileQueryDto>

{

    public async Task<GetPatientProfileQueryDto> Handle(GetPatientProfileQuery request, CancellationToken ct)

    {

        if (!currentUser.UserId.HasValue)

            throw new MarketBusinessRuleException("auth.not_authenticated", "User is not authenticated.");



        var patient = await ctx.Patients

            .Include(p => p.User)

            .Include(p => p.Doctor)

            .Include(p => p.MedicalRecord)

            .FirstOrDefaultAsync(p => p.UserId == currentUser.UserId.Value, ct);



        if (patient is null)

            throw new MarketNotFoundException("Patient profile not found.");



        return new GetPatientProfileQueryDto

        {

            Id = patient.Id,

            FirstName = patient.FirstName,

            LastName = patient.LastName,

            Email = patient.User.Email,

            DateOfBirth = patient.DateOfBirth,

            Gender = patient.Gender,

            PhoneNumber = patient.PhoneNumber,

            Address = patient.Address,

            BloodType = patient.BloodType,

            RegistrationStatus = patient.RegistrationStatus,

            IsAdmitted = patient.IsAdmitted,

            RecordNumber = patient.MedicalRecord?.RecordNumber,

            DoctorFirstName = patient.Doctor?.FirstName,

            DoctorLastName = patient.Doctor?.LastName,

            DoctorSpecialization = patient.Doctor?.Specialization

        };

    }

}



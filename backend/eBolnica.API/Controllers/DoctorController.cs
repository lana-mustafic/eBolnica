using eBolnica.Application.Modules.Doctor.Patients.Queries.ListDoctorPatients;
using eBolnica.Application.Modules.Doctor.Prescriptions.Queries.GetDoctorPrescriptionById;
using eBolnica.Application.Modules.Doctor.Prescriptions.Queries.ListDoctorPrescriptions;
using eBolnica.Application.Modules.Doctor.Profile.Commands.UpdateDoctorProfile;
using eBolnica.Application.Modules.Doctor.Profile.Queries.GetDoctorProfile;
using eBolnica.Application.Modules.Doctor.Stats.Queries.GetDoctorStats;
using eBolnica.Application.Modules.Pharmacy.Medications.Queries.GetMedicationAutocomplete;
using eBolnica.Application.Modules.Pharmacy.Prescriptions;
using eBolnica.Application.Modules.Pharmacy.Prescriptions.Commands.CreatePrescription;
using eBolnica.Application.Modules.Pharmacy.Prescriptions.Queries.ListPrescriptions;

[ApiController]
[Route("api/doctor")]
[Authorize(Policy = "DoctorOnly")]
public sealed class DoctorController(IMediator mediator) : ControllerBase
{
    [HttpGet("doctor-data")]
    public async Task<ActionResult<GetDoctorProfileQueryDto>> GetDoctorData(CancellationToken ct)
        => Ok(await mediator.Send(new GetDoctorProfileQuery(), ct));

    [HttpPut("edit-doctor")]
    public async Task<ActionResult<UpdateDoctorProfileCommandDto>> EditDoctor(
        [FromBody] UpdateDoctorProfileCommand command,
        CancellationToken ct)
        => Ok(await mediator.Send(command, ct));

    [HttpGet("list-patients")]
    public async Task<ActionResult<ListDoctorPatientsQueryDto>> ListPatients(
        [FromQuery] ListDoctorPatientsQuery query,
        CancellationToken ct)
        => Ok(await mediator.Send(query, ct));

    [HttpGet("doctor-stats")]
    public async Task<ActionResult<GetDoctorStatsQueryDto>> GetDoctorStats(CancellationToken ct)
        => Ok(await mediator.Send(new GetDoctorStatsQuery(), ct));

    [HttpPost("prescriptions")]
    public async Task<ActionResult<PrescriptionDto>> CreatePrescription(
        [FromBody] CreatePrescriptionCommand command,
        CancellationToken ct)
    {
        var result = await mediator.Send(command, ct);
        return Created($"/api/doctor/prescriptions/{result.Id}", result);
    }

    [HttpGet("prescriptions")]
    public async Task<ActionResult<ListPrescriptionsQueryDto>> ListPrescriptions(
        [FromQuery] ListDoctorPrescriptionsQuery query,
        CancellationToken ct)
        => Ok(await mediator.Send(query, ct));

    [HttpGet("prescriptions/{id:int}")]
    public async Task<ActionResult<PrescriptionDto>> GetPrescription(int id, CancellationToken ct)
        => Ok(await mediator.Send(new GetDoctorPrescriptionByIdQuery { Id = id }, ct));

    [HttpGet("medications/autocomplete")]
    public async Task<ActionResult<IReadOnlyList<MedicationAutocompleteSuggestionDto>>> GetMedicationAutocomplete(
        [FromQuery(Name = "q")] string query,
        [FromQuery] int limit = 10,
        CancellationToken ct = default)
        => Ok(await mediator.Send(new GetMedicationAutocompleteQuery
        {
            Query = query,
            Limit = limit,
            RequiresPrescription = true
        }, ct));
}

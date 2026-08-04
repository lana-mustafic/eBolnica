using Market.Application.Modules.Pharmacy.Medications;

using Market.Application.Modules.Pharmacy.Medications.Commands.CreateMedication;

using Market.Application.Modules.Pharmacy.Medications.Commands.DeleteMedication;

using Market.Application.Modules.Pharmacy.Medications.Commands.DeleteMedicationImage;

using Market.Application.Modules.Pharmacy.Medications.Commands.ImportMedicationsCsv;

using Market.Application.Modules.Pharmacy.Medications.Commands.SetPrimaryMedicationImage;

using Market.Application.Modules.Pharmacy.Medications.Commands.UpdateMedication;

using Market.Application.Modules.Pharmacy.Medications.Commands.UploadMedicationImage;

using Market.Application.Modules.Pharmacy.Medications.Images;

using Market.Application.Modules.Pharmacy.Medications.Queries.CheckMedicationName;

using Market.Application.Modules.Pharmacy.Medications.Queries.ExportMedicationsCsv;

using Market.Application.Modules.Pharmacy.Medications.Queries.GetInventory;

using Market.Application.Modules.Pharmacy.Medications.Queries.GetMedicationAutocomplete;

using Market.Application.Modules.Pharmacy.Medications.Queries.GetMedicationById;

using Market.Application.Modules.Pharmacy.Medications.Queries.GetMedicationImportTemplate;

using Market.Application.Modules.Pharmacy.Medications.Queries.ListMedicationImages;

using Market.Application.Modules.Pharmacy.Medications.Queries.ListMedications;

using Market.Application.Modules.Pharmacy.Prescriptions;
using Market.Application.Modules.Pharmacy.Prescriptions.Commands.CreatePrescription;
using Market.Application.Modules.Pharmacy.Prescriptions.Commands.DispensePrescription;
using Market.Application.Modules.Pharmacy.Prescriptions.Queries.GetPrescriptionById;
using Market.Application.Modules.Pharmacy.Prescriptions.Queries.ListPrescriptions;

using Market.Application.Modules.Pharmacy.Analytics;
using Market.Application.Modules.Pharmacy.Analytics.Queries.ExportInventoryPdf;
using Market.Application.Modules.Pharmacy.Analytics.Queries.ExportPrescriptionsPdf;
using Market.Application.Modules.Pharmacy.Analytics.Queries.GetDashboardStats;
using Market.Application.Modules.Pharmacy.Analytics.Queries.GetMonthlyRevenue;
using Market.Application.Modules.Pharmacy.Analytics.Queries.GetStockTrends;
using Market.Application.Modules.Pharmacy.Analytics.Queries.GetTopCategories;

using System.Text;



[ApiController]

[Route("api/pharmacy")]

[Authorize(Policy = "PharmacistOnly")]

public sealed class PharmacyController(IMediator mediator) : ControllerBase

{

    [HttpGet("medications")]

    public async Task<ActionResult<ListMedicationsQueryDto>> GetMedications(

        [FromQuery] ListMedicationsQuery query,

        CancellationToken ct)

        => Ok(await mediator.Send(query, ct));



    [HttpGet("medications/autocomplete")]

    [Authorize(Policy = "PharmacyStaff")]

    public async Task<ActionResult<IReadOnlyList<MedicationAutocompleteSuggestionDto>>> GetAutocomplete(

        [FromQuery(Name = "q")] string query,

        [FromQuery] int limit = 10,

        CancellationToken ct = default)

        => Ok(await mediator.Send(new GetMedicationAutocompleteQuery { Query = query, Limit = limit }, ct));



    [HttpGet("medications/check-name")]

    [Authorize(Policy = "PharmacyStaff")]

    public async Task<ActionResult<CheckMedicationNameQueryDto>> CheckMedicationName(

        [FromQuery] CheckMedicationNameQuery query,

        CancellationToken ct)

        => Ok(await mediator.Send(query, ct));



    [HttpGet("medications/export/csv")]

    public async Task<IActionResult> ExportMedicationsCsv(

        [FromQuery] ExportMedicationsCsvQuery query,

        CancellationToken ct)

    {

        var result = await mediator.Send(query, ct);

        return File(Encoding.UTF8.GetBytes(result.CsvContent), "text/csv", result.FileName);

    }



    [HttpGet("medications/import/template")]

    public async Task<IActionResult> DownloadImportTemplate(CancellationToken ct)

    {

        var result = await mediator.Send(new GetMedicationImportTemplateQuery(), ct);

        return File(Encoding.UTF8.GetBytes(result.CsvContent), "text/csv", result.FileName);

    }



    [HttpPost("medications/import/csv")]

    [Consumes("multipart/form-data")]

    public async Task<ActionResult<MedicationImportResultDto>> ImportMedicationsCsv(

        IFormFile? file,

        CancellationToken ct)

    {

        if (file is null || file.Length == 0)

            return BadRequest(new { error = "No file uploaded." });



        if (file.Length > 5 * 1024 * 1024)

            return BadRequest(new { error = "File exceeds 5 MB limit." });



        await using var stream = file.OpenReadStream();

        using var reader = new StreamReader(stream, Encoding.UTF8);

        var content = await reader.ReadToEndAsync(ct);



        return Ok(await mediator.Send(new ImportMedicationsCsvCommand { CsvContent = content }, ct));

    }



    [HttpGet("inventory")]

    public async Task<ActionResult<GetInventoryQueryDto>> GetInventory(

        [FromQuery] GetInventoryQuery query,

        CancellationToken ct)

        => Ok(await mediator.Send(query, ct));



    [HttpGet("medications/{id:int}")]

    public async Task<ActionResult<MedicationDto>> GetMedication(int id, CancellationToken ct)

        => Ok(await mediator.Send(new GetMedicationByIdQuery { Id = id }, ct));



    [HttpGet("medications/{id:int}/images")]

    public async Task<ActionResult<IReadOnlyList<MedicationImageDto>>> GetMedicationImages(

        int id,

        CancellationToken ct)

        => Ok(await mediator.Send(new ListMedicationImagesQuery { MedicationId = id }, ct));



    [HttpPost("medications/{id:int}/images")]

    [Authorize(Policy = "PharmacyStaff")]

    [RequestSizeLimit(5 * 1024 * 1024)]

    public async Task<ActionResult<MedicationImageDto>> UploadMedicationImage(

        int id,

        IFormFile file,

        CancellationToken ct)

    {

        if (file is null || file.Length == 0)

            return BadRequest(new { error = "No file uploaded." });



        await using var stream = file.OpenReadStream();

        var result = await mediator.Send(new UploadMedicationImageCommand

        {

            MedicationId = id,

            FileName = file.FileName,

            Content = stream

        }, ct);



        return CreatedAtAction(nameof(GetMedicationImages), new { id }, result);

    }



    [HttpPut("medications/{id:int}/images/{imageId:int}/primary")]

    [Authorize(Policy = "PharmacyStaff")]

    public async Task<IActionResult> SetPrimaryImage(int id, int imageId, CancellationToken ct)

    {

        await mediator.Send(new SetPrimaryMedicationImageCommand { MedicationId = id, ImageId = imageId }, ct);

        return NoContent();

    }



    [HttpDelete("medications/{id:int}/images/{imageId:int}")]

    [Authorize(Policy = "PharmacyStaff")]

    public async Task<IActionResult> DeleteMedicationImage(int id, int imageId, CancellationToken ct)

    {

        await mediator.Send(new DeleteMedicationImageCommand { MedicationId = id, ImageId = imageId }, ct);

        return NoContent();

    }



    [HttpPost("medications")]

    [Authorize(Policy = "PharmacyStaff")]

    public async Task<ActionResult<MedicationDto>> CreateMedication(

        [FromBody] CreateMedicationCommand command,

        CancellationToken ct)

    {

        var result = await mediator.Send(command, ct);

        return CreatedAtAction(nameof(GetMedication), new { id = result.Id }, result);

    }



    [HttpPut("medications/{id:int}")]

    [Authorize(Policy = "PharmacyStaff")]

    public async Task<ActionResult<MedicationDto>> UpdateMedication(

        int id,

        [FromBody] UpdateMedicationCommand command,

        CancellationToken ct)

    {

        command.Id = id;

        return Ok(await mediator.Send(command, ct));

    }



    [HttpDelete("medications/{id:int}")]

    public async Task<IActionResult> DeleteMedication(int id, CancellationToken ct)

    {

        await mediator.Send(new DeleteMedicationCommand { Id = id }, ct);

        return NoContent();

    }



    [HttpGet("prescriptions")]

    public async Task<ActionResult<ListPrescriptionsQueryDto>> GetPrescriptions(

        [FromQuery] ListPrescriptionsQuery query,

        CancellationToken ct)

        => Ok(await mediator.Send(query, ct));



    [HttpGet("prescriptions/{id:int}")]

    public async Task<ActionResult<PrescriptionDto>> GetPrescription(int id, CancellationToken ct)

        => Ok(await mediator.Send(new GetPrescriptionByIdQuery { Id = id }, ct));



    [HttpPost("prescriptions")]

    [Authorize(Policy = "DoctorOnly")]

    public async Task<ActionResult<PrescriptionDto>> CreatePrescription(

        [FromBody] CreatePrescriptionCommand command,

        CancellationToken ct)

    {

        var result = await mediator.Send(command, ct);

        return CreatedAtAction(nameof(GetPrescription), new { id = result.Id }, result);

    }



    [HttpPost("prescriptions/{id:int}/dispense")]

    public async Task<ActionResult<PrescriptionDto>> DispensePrescription(

        int id,

        [FromBody] DispensePrescriptionCommand? command,

        CancellationToken ct)

    {

        command ??= new DispensePrescriptionCommand();

        command.PrescriptionId = id;

        return Ok(await mediator.Send(command, ct));

    }



    [HttpGet("analytics/dashboard-stats")]

    public async Task<ActionResult<DashboardStatsResponseDto>> GetDashboardStats(

        [FromQuery] GetDashboardStatsQuery query,

        CancellationToken ct)

        => Ok(await mediator.Send(query, ct));



    [HttpGet("analytics/monthly-revenue")]

    public async Task<ActionResult<RevenueDataDto>> GetMonthlyRevenue(

        [FromQuery] GetMonthlyRevenueQuery query,

        CancellationToken ct)

        => Ok(await mediator.Send(query, ct));



    [HttpGet("analytics/top-categories")]

    public async Task<ActionResult<CategoriesDataDto>> GetTopCategories(

        [FromQuery] GetTopCategoriesQuery query,

        CancellationToken ct)

        => Ok(await mediator.Send(query, ct));



    [HttpGet("analytics/stock-trends")]

    public async Task<ActionResult<StockTrendsDataDto>> GetStockTrends(

        [FromQuery] GetStockTrendsQuery query,

        CancellationToken ct)

        => Ok(await mediator.Send(query, ct));



    [HttpGet("reports/inventory/pdf")]

    public async Task<IActionResult> ExportInventoryPdf(

        [FromQuery] ExportInventoryPdfQuery query,

        CancellationToken ct)

    {

        var result = await mediator.Send(query, ct);

        Response.Headers["X-Export-Row-Count"] = result.RowCount.ToString();

        return File(result.Content, "application/pdf", result.FileName);

    }



    [HttpGet("reports/prescriptions/pdf")]

    public async Task<IActionResult> ExportPrescriptionsPdf(

        [FromQuery] ExportPrescriptionsPdfQuery query,

        CancellationToken ct)

    {

        var result = await mediator.Send(query, ct);

        Response.Headers["X-Export-Row-Count"] = result.RowCount.ToString();

        return File(result.Content, "application/pdf", result.FileName);

    }

}



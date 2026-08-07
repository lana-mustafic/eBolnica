namespace eBolnica.Application.Modules.Pharmacy;

public static class PharmacyExportLimits
{
    public const int MaxCsvExportRows = 10_000;
    public const int MaxPdfExportRows = 2_000;
    public const int PdfFetchBatchSize = 200;
}

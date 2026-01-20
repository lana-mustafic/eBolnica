using eBolnicaAPI.Models.DTOs;
using eBolnicaAPI.Models.Entities;

namespace eBolnicaAPI.Services
{
    /// <summary>
    /// Interface for PDF report generation service
    /// </summary>
    public interface IPdfReportService
    {
        /// <summary>
        /// Generates PDF report for inventory items
        /// </summary>
        /// <param name="medications">List of medications to include in the report</param>
        /// <param name="request">PDF report request with filtering and formatting options</param>
        /// <returns>PDF file as byte array</returns>
        Task<byte[]> GenerateInventoryPdfAsync(List<Medication> medications, PharmacyPdfReportRequest request);

        /// <summary>
        /// Generates PDF report for prescriptions
        /// </summary>
        /// <param name="prescriptions">List of prescriptions to include in the report</param>
        /// <param name="request">PDF report request with filtering and formatting options</param>
        /// <returns>PDF file as byte array</returns>
        Task<byte[]> GeneratePrescriptionsPdfAsync(List<Prescription> prescriptions, PharmacyPdfReportRequest request);

        /// <summary>
        /// Generates a simple PDF for testing purposes
        /// </summary>
        /// <param name="title">Title of the PDF</param>
        /// <param name="content">Content to display</param>
        /// <returns>PDF file as byte array</returns>
        byte[] GenerateSimplePdf(string title, string content);
    }
}

namespace eBolnicaAPI.Models.Exceptions
{
    /// <summary>
    /// Exception thrown when PDF generation fails
    /// </summary>
    public class PdfGenerationException : Exception
    {
        public PdfGenerationException(string message) : base(message)
        {
        }

        public PdfGenerationException(string message, Exception innerException) 
            : base(message, innerException)
        {
        }
    }

    /// <summary>
    /// Exception thrown when PDF configuration is invalid
    /// </summary>
    public class PdfConfigurationException : Exception
    {
        public PdfConfigurationException(string message) : base(message)
        {
        }
    }
}

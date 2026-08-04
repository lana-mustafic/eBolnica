namespace eBolnicaAPI.Services.Pharmacy.MedicationImages
{
    public class MedicationImageUploadException : Exception
    {
        public MedicationImageUploadException(string message) : base(message)
        {
        }
    }

    public class MedicationImageValidationException : MedicationImageUploadException
    {
        public MedicationImageValidationException(string message) : base(message)
        {
        }
    }

    public class MedicationImageSecurityException : MedicationImageUploadException
    {
        public MedicationImageSecurityException(string message) : base(message)
        {
        }
    }
}

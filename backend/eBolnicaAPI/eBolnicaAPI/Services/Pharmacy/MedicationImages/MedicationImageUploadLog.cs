namespace eBolnicaAPI.Services.Pharmacy.MedicationImages
{
    public static class MedicationImageUploadLog
    {
        public static long CalculateBytesSaved(long originalBytes, long optimizedBytes)
        {
            return Math.Max(0, originalBytes - optimizedBytes);
        }
    }
}

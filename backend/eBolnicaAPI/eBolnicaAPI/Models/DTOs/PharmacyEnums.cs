namespace eBolnicaAPI.Models.DTOs
{
    /// <summary>
    /// Sort order enumeration for query parameters
    /// </summary>
    public enum SortOrder
    {
        /// <summary>
        /// Ascending order
        /// </summary>
        Asc,

        /// <summary>
        /// Descending order
        /// </summary>
        Desc
    }

    /// <summary>
    /// Stock status enumeration for medication filtering
    /// </summary>
    public enum StockStatus
    {
        /// <summary>
        /// Medication is in stock (quantity >= minimum level)
        /// </summary>
        InStock,

        /// <summary>
        /// Medication stock is low (quantity < minimum level but > 0)
        /// </summary>
        LowStock,

        /// <summary>
        /// Medication is out of stock (quantity = 0)
        /// </summary>
        OutOfStock
    }

    /// <summary>
    /// Medication status enumeration
    /// </summary>
    public enum MedicationStatus
    {
        /// <summary>
        /// Medication is active and available
        /// </summary>
        Active,

        /// <summary>
        /// Medication is discontinued
        /// </summary>
        Discontinued
    }

    /// <summary>
    /// Prescription status enumeration
    /// </summary>
    public enum PrescriptionStatus
    {
        /// <summary>
        /// Prescription is pending dispensation
        /// </summary>
        Pending,

        /// <summary>
        /// Prescription has been dispensed
        /// </summary>
        Dispensed,

        /// <summary>
        /// Prescription has been cancelled
        /// </summary>
        Cancelled
    }
}

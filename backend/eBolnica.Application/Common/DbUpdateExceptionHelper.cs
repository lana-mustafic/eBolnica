using Microsoft.EntityFrameworkCore;

namespace eBolnica.Application.Common;

internal static class DbUpdateExceptionHelper
{
    public static bool IsUniqueConstraintViolation(DbUpdateException exception)
    {
        for (var inner = exception.InnerException; inner != null; inner = inner.InnerException)
        {
            var message = inner.Message;
            if (message.Contains("duplicate key", StringComparison.OrdinalIgnoreCase)
                || message.Contains("UNIQUE KEY", StringComparison.OrdinalIgnoreCase)
                || message.Contains("unique index", StringComparison.OrdinalIgnoreCase)
                || message.Contains("IX_Medications_NormalizedName", StringComparison.OrdinalIgnoreCase)
                || message.Contains("PrescriptionNumber", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }
}

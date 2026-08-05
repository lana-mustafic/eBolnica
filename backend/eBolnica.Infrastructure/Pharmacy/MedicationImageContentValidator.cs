namespace eBolnica.Infrastructure.Pharmacy;

internal static class MedicationImageContentValidator
{
    public static bool IsSupportedImageContent(ReadOnlySpan<byte> header, string extension)
    {
        var ext = extension.ToLowerInvariant();
        if (ext is ".jpg" or ".jpeg")
            return header.Length >= 3 && header[0] == 0xFF && header[1] == 0xD8 && header[2] == 0xFF;

        if (ext == ".png")
            return header.Length >= 8
                && header[0] == 0x89 && header[1] == 0x50 && header[2] == 0x4E && header[3] == 0x47
                && header[4] == 0x0D && header[5] == 0x0A && header[6] == 0x1A && header[7] == 0x0A;

        if (ext == ".webp")
            return header.Length >= 12
                && header[0] == 0x52 && header[1] == 0x49 && header[2] == 0x46 && header[3] == 0x46
                && header[8] == 0x57 && header[9] == 0x45 && header[10] == 0x42 && header[11] == 0x50;

        return false;
    }
}

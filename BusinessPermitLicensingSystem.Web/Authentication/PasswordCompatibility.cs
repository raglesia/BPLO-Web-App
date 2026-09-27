using System.Security.Cryptography;
using System.Text;

namespace BusinessPermitLicensingSystem.Web.Authentication;

internal static class PasswordCompatibility
{
    private const int Pbkdf2Iterations = 260_000;
    private const int SaltBytes = 16;
    private const int HashBytes = 32;

    public static bool Verify(string password, string stored, out bool isLegacy)
    {
        isLegacy = !stored.StartsWith("pbkdf2:", StringComparison.Ordinal);
        if (isLegacy)
        {
            byte[] sha256 = SHA256.HashData(Encoding.UTF8.GetBytes(password));
            string encoded = Convert.ToBase64String(sha256);
            return FixedTimeStringEquals(encoded, stored);
        }

        string[] parts = stored.Split(':');
        if (parts.Length != 4 || !int.TryParse(parts[1], out int iterations) || iterations <= 0)
            return false;

        try
        {
            byte[] salt = Convert.FromBase64String(parts[2]);
            byte[] expected = Convert.FromBase64String(parts[3]);
            if (salt.Length == 0 || expected.Length == 0) return false;
            byte[] actual = Rfc2898DeriveBytes.Pbkdf2(
                password, salt, iterations, HashAlgorithmName.SHA256, expected.Length);
            return CryptographicOperations.FixedTimeEquals(actual, expected);
        }
        catch (FormatException)
        {
            return false;
        }
    }

    public static string HashPbkdf2(string password)
    {
        byte[] salt = RandomNumberGenerator.GetBytes(SaltBytes);
        byte[] hash = Rfc2898DeriveBytes.Pbkdf2(
            password, salt, Pbkdf2Iterations, HashAlgorithmName.SHA256, HashBytes);
        return $"pbkdf2:{Pbkdf2Iterations}:{Convert.ToBase64String(salt)}:{Convert.ToBase64String(hash)}";
    }

    private static bool FixedTimeStringEquals(string left, string right)
    {
        byte[] leftBytes = Encoding.UTF8.GetBytes(left);
        byte[] rightBytes = Encoding.UTF8.GetBytes(right);
        return CryptographicOperations.FixedTimeEquals(leftBytes, rightBytes);
    }
}

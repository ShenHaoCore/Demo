using System.Security.Cryptography;
using System.Text;

namespace Demo.Payment.Api.Services;

public static class PaymentSecurity
{
    public static string ComputeHmac(string payload, string secret)
    {
        var key = Encoding.UTF8.GetBytes(secret);
        var data = Encoding.UTF8.GetBytes(payload);
        var hash = HMACSHA256.HashData(key, data);
        return Convert.ToHexString(hash);
    }

    public static bool FixedTimeEqualsHex(string expectedHex, string? actualHex)
    {
        if (string.IsNullOrWhiteSpace(actualHex))
        {
            return false;
        }

        byte[] expected;
        byte[] actual;
        try
        {
            expected = Convert.FromHexString(expectedHex);
            actual = Convert.FromHexString(actualHex.Trim());
        }
        catch (FormatException)
        {
            return false;
        }

        if (expected.Length != actual.Length)
        {
            return false;
        }

        return CryptographicOperations.FixedTimeEquals(expected, actual);
    }
}

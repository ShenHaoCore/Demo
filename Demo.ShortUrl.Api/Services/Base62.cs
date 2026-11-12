using System.Diagnostics;

namespace Demo.ShortUrl.Api.Services;

/// <summary>发号机整数 → Base62（0-9a-zA-Z）。</summary>
public static class Base62
{
    private const string Alphabet = "0123456789abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ";

    static Base62()
    {
        Debug.Assert(Encode(0, 1) == "0");
        Debug.Assert(Encode(61, 1) == "Z");
        Debug.Assert(Encode(62, 1) == "10");
        Debug.Assert(Encode(1, 6) == "000001");
    }

    public static string Encode(long value, int minLength = 6)
    {
        if (value < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(value));
        }

        if (minLength < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(minLength));
        }

        if (value == 0)
        {
            return new string(Alphabet[0], minLength);
        }

        Span<char> buffer = stackalloc char[16];
        var index = buffer.Length;
        var remaining = value;
        while (remaining > 0)
        {
            buffer[--index] = Alphabet[(int)(remaining % 62)];
            remaining /= 62;
        }

        var significant = buffer.Length - index;
        var pad = Math.Max(0, minLength - significant);
        return pad == 0
            ? new string(buffer[index..])
            : new string(Alphabet[0], pad) + new string(buffer[index..]);
    }
}

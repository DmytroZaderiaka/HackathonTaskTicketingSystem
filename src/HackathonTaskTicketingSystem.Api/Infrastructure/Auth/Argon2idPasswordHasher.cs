using System.Security.Cryptography;
using System.Text;
using HackathonTaskTicketingSystem.Common.Abstractions;
using Konscious.Security.Cryptography;

namespace HackathonTaskTicketingSystem.Infrastructure.Auth;

/// <summary>
/// Argon2id password hasher. The stored value is "{saltBase64}.{hashBase64}";
/// the fixed cost parameters below are baked into <see cref="ComputeHash"/>.
/// </summary>
public sealed class Argon2idPasswordHasher : IPasswordHasher
{
    private const int SaltSize = 16;
    private const int HashSize = 32;
    private const int DegreeOfParallelism = 4;
    private const int Iterations = 3;
    private const int MemorySizeKb = 65536; // 64 MB

    public string Hash(string password)
    {
        var salt = RandomNumberGenerator.GetBytes(SaltSize);
        var hash = ComputeHash(password, salt);
        return $"{Convert.ToBase64String(salt)}.{Convert.ToBase64String(hash)}";
    }

    public bool Verify(string password, string hash)
    {
        var parts = hash.Split('.');
        if (parts.Length != 2)
        {
            return false;
        }

        byte[] salt;
        byte[] expected;
        try
        {
            salt = Convert.FromBase64String(parts[0]);
            expected = Convert.FromBase64String(parts[1]);
        }
        catch (FormatException)
        {
            return false;
        }

        var actual = ComputeHash(password, salt);
        return CryptographicOperations.FixedTimeEquals(actual, expected);
    }

    private static byte[] ComputeHash(string password, byte[] salt)
    {
        using var argon2 = new Argon2id(Encoding.UTF8.GetBytes(password))
        {
            Salt = salt,
            DegreeOfParallelism = DegreeOfParallelism,
            Iterations = Iterations,
            MemorySize = MemorySizeKb,
        };

        return argon2.GetBytes(HashSize);
    }
}

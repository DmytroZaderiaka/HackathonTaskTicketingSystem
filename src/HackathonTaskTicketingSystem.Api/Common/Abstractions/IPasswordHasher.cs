namespace HackathonTaskTicketingSystem.Common.Abstractions;

/// <summary>
/// Hashes and verifies passwords using an established algorithm (Argon2id).
/// </summary>
public interface IPasswordHasher
{
    string Hash(string password);

    bool Verify(string password, string hash);
}

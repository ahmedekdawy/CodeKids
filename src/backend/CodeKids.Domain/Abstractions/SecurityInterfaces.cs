using CodeKids.Domain.Entities;

namespace CodeKids.Domain.Abstractions;

public interface IPasswordHasher
{
    string Hash(string password);
    bool Verify(string password, string passwordHash);
}

public interface IJwtTokenService
{
    string CreateToken(User user);
}

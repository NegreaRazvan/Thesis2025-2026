using Microsoft.AspNetCore.Identity;

namespace Repository;

public class FastPasswordHasher<TUser> : PasswordHasher<TUser> where TUser : class
{
    public FastPasswordHasher()
        : base(Microsoft.Extensions.Options.Options.Create(
            new PasswordHasherOptions { IterationCount = 10_000 }))
    {
    }
}
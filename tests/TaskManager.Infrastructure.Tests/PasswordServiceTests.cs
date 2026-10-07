using TaskManager.Infrastructure.Security;

namespace TaskManager.Infrastructure.Tests;

public class PasswordServiceTests
{
    [Fact]
    public void Hash_uses_unique_salt_and_verifies_only_correct_password()
    {
        var service = new PasswordService();
        var first = service.Hash("StrongPassword!");
        var second = service.Hash("StrongPassword!");
        Assert.NotEqual(first, second);
        Assert.NotEqual("StrongPassword!", first);
        Assert.True(service.Verify("StrongPassword!", first));
        Assert.False(service.Verify("IncorrectPassword!", first));
        Assert.False(service.Verify("StrongPassword!", null));
    }
}

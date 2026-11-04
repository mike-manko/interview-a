using SupportTriage.Application.Common.Tenancy;

namespace SupportTriage.Application.Tests.Common.Tenancy;

[TestFixture]
public sealed class TenantIdFormatTests
{
    [TestCase("contoso")]
    [TestCase("fabrikam")]
    [TestCase("tenant-42")]
    [TestCase("a")]
    public void IsValid_LowercaseId_ReturnsTrue(string tenantId)
    {
        Assert.That(TenantIdFormat.IsValid(tenantId), Is.True);
    }

    // Partition keys are case-sensitive, so "Contoso" would silently be a different tenant.
    [TestCase("Contoso")]
    [TestCase("")]
    [TestCase(" contoso")]
    [TestCase("-contoso")]
    [TestCase("contoso-")]
    [TestCase("con toso")]
    [TestCase("contoso/fabrikam")]
    [TestCase(null)]
    public void IsValid_OtherValue_ReturnsFalse(string? tenantId)
    {
        Assert.That(TenantIdFormat.IsValid(tenantId), Is.False);
    }

    [Test]
    public void IsValid_LongerThanMax_ReturnsFalse()
    {
        Assert.That(TenantIdFormat.IsValid(new string('a', TenantIdFormat.MaxLength + 1)), Is.False);
    }
}

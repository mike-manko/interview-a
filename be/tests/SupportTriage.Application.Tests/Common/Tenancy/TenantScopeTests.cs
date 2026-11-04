using SupportTriage.Application.Common.Exceptions;
using SupportTriage.Application.Common.Tenancy;
using SupportTriage.Domain.SharedKernel;

namespace SupportTriage.Application.Tests.Common.Tenancy;

[TestFixture]
public sealed class TenantScopeTests
{
    private TenantScope _sut = null!;

    [SetUp]
    public void SetUp()
    {
        _sut = new TenantScope();
    }

    [Test]
    public void RequireTenantId_AssignedScope_ReturnsTenant()
    {
        _sut.AssignScope("contoso");

        Assert.That(_sut.RequireTenantId(), Is.EqualTo("contoso"));
    }

    [Test]
    public void RequireTenantId_NoScope_ThrowsTenantScopeException()
    {
        Assert.Throws<TenantScopeException>(() => _sut.RequireTenantId());
    }

    [Test]
    public void RequireTenantId_UnrestrictedScope_ThrowsTenantScopeException()
    {
        _sut.AssignUnrestrictedScope();

        Assert.Throws<TenantScopeException>(() => _sut.RequireTenantId());
    }

    [Test]
    public void AssignScope_SameTenantTwice_KeepsScope()
    {
        _sut.AssignScope("contoso");

        Assert.DoesNotThrow(() => _sut.AssignScope("contoso"));
    }

    // Once assigned, a scope can never move to another tenant's partition.
    [Test]
    public void AssignScope_DifferentTenant_ThrowsTenantScopeException()
    {
        _sut.AssignScope("contoso");

        Assert.Throws<TenantScopeException>(() => _sut.AssignScope("fabrikam"));
    }

    [Test]
    public void AssignUnrestrictedScope_AfterTenantScope_ThrowsTenantScopeException()
    {
        _sut.AssignScope("contoso");

        Assert.Throws<TenantScopeException>(() => _sut.AssignUnrestrictedScope());
    }

    [Test]
    public void AssignScope_InvalidTenantId_ThrowsTenantScopeException()
    {
        Assert.Throws<TenantScopeException>(() => _sut.AssignScope("Contoso"));
    }

    [Test]
    public void EnsureCanAccessTenant_OtherTenant_ThrowsForbidden()
    {
        _sut.AssignScope("contoso");

        var exception = Assert.Throws<TenantScopeException>(() => _sut.EnsureCanAccessTenant("fabrikam"));

        Assert.That(exception!.ErrorType, Is.EqualTo(ErrorType.Forbidden));
    }

    [Test]
    public void EnsureCanAccessTenant_UnrestrictedScope_AllowsEveryTenant()
    {
        _sut.AssignUnrestrictedScope();

        Assert.DoesNotThrow(() => _sut.EnsureCanAccessTenant("fabrikam"));
    }
}

using SupportTriage.Application.Tickets.Dto;
using SupportTriage.Domain.TicketAggregate.Enums;
using SupportTriage.Persistence.QuerySources;

namespace SupportTriage.Persistence.Tests.QuerySources;

[TestFixture]
public sealed class TicketListQueryBuilderTests
{
    [Test]
    public void Build_NoFilters_HidesFailedTicketsOnly()
    {
        var query = TicketListQueryBuilder.Build(Filter(false, [], []));

        Assert.Multiple(() =>
        {
            Assert.That(query.QueryText, Does.EndWith("WHERE c.isDeleted = false AND c.status != @hidden ORDER BY c.createdAt DESC"));
            Assert.That(Parameters(query), Is.EqualTo(new[] { "@hidden=Failed" }));
        });
    }

    [Test]
    public void Build_IncludeHidden_DropsTheStatusCondition()
    {
        var query = TicketListQueryBuilder.Build(Filter(true, [], []));

        Assert.Multiple(() =>
        {
            Assert.That(query.QueryText, Does.Not.Contain("c.status !="));
            Assert.That(Parameters(query), Is.Empty);
        });
    }

    [Test]
    public void Build_CategoriesPrioritiesAndHidden_CombinesEveryCondition()
    {
        var query = TicketListQueryBuilder.Build(Filter(true, [TicketCategory.Technical, TicketCategory.Billing], [TicketPriority.Critical]));

        Assert.Multiple(() =>
        {
            Assert.That(query.QueryText, Does.EndWith("WHERE c.isDeleted = false AND c.category IN (@category0, @category1) AND c.priority IN (@priority0) ORDER BY c.createdAt DESC"));
            Assert.That(Parameters(query), Is.EqualTo(new[] { "@category0=Billing", "@category1=Technical", "@priority0=Critical" }));
        });
    }

    // A continuation token only works with the query that produced it, so the same set must give the same text.
    [Test]
    public void Build_SameValuesInAnotherOrder_GivesTheSameQuery()
    {
        var first = TicketListQueryBuilder.Build(Filter(false, [TicketCategory.Shipping, TicketCategory.Account], []));
        var second = TicketListQueryBuilder.Build(Filter(false, [TicketCategory.Account, TicketCategory.Shipping], []));

        Assert.Multiple(() =>
        {
            Assert.That(second.QueryText, Is.EqualTo(first.QueryText));
            Assert.That(Parameters(second), Is.EqualTo(Parameters(first)));
        });
    }

    [Test]
    public void Build_AnyFilter_NeverPutsValuesIntoTheQueryText()
    {
        var query = TicketListQueryBuilder.Build(Filter(false, [TicketCategory.Billing], [TicketPriority.High]));

        Assert.Multiple(() =>
        {
            Assert.That(query.QueryText, Does.Not.Contain("Billing"));
            Assert.That(query.QueryText, Does.Not.Contain("High"));
        });
    }

    private static TicketListFilter Filter(bool includeHidden, TicketCategory[] categories, TicketPriority[] priorities)
    {
        return new TicketListFilter(includeHidden, new HashSet<TicketCategory>(categories), new HashSet<TicketPriority>(priorities));
    }

    private static List<string> Parameters(Microsoft.Azure.Cosmos.QueryDefinition query)
    {
        var parameters = new List<string>();
        foreach (var (name, value) in query.GetQueryParameters())
            parameters.Add(name + "=" + value);

        return parameters;
    }
}

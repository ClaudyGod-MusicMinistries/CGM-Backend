using ClaudyGod.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace ClaudyGod.PostgresIntegration.Tests;

[Collection(PostgresCollection.Name)]
public sealed class ProductionSchemaTests(PostgresFixture fixture)
{
    [Fact]
    public async Task Migrations_create_jsonb_outbox_and_commit_business_data_atomically()
    {
        await using var db = fixture.CreateContext();
        await using var transaction = await db.Database.BeginTransactionAsync();
        var subscriber = Subscriber.Create("Integration Test", $"integration-{Guid.NewGuid():N}@example.test");
        db.Subscribers.Add(subscriber);
        db.OutboxMessages.Add(new OutboxMessage
        {
            Kind = "email",
            Type = "template",
            Payload = "{\"template\":\"welcome\"}",
            OccurredAt = DateTime.UtcNow,
            AvailableAt = DateTime.UtcNow
        });
        await db.SaveChangesAsync();
        await transaction.CommitAsync();

        await using var verification = fixture.CreateContext();
        Assert.True(await verification.Subscribers.AnyAsync(x => x.Id == subscriber.Id));
        Assert.True(await verification.OutboxMessages.AnyAsync(x => x.Kind == "email"));
    }

    [Fact]
    public async Task PostgreSql_enforces_subscriber_email_uniqueness()
    {
        await using var db = fixture.CreateContext();
        var email = $"duplicate-{Guid.NewGuid():N}@example.test";
        db.Subscribers.Add(Subscriber.Create("First", email));
        db.Subscribers.Add(Subscriber.Create("Second", email));
        await Assert.ThrowsAsync<DbUpdateException>(() => db.SaveChangesAsync());
    }
}

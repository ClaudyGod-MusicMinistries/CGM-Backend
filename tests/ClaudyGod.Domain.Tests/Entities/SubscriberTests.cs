using ClaudyGod.Domain.Entities;
using FluentAssertions;

namespace ClaudyGod.Domain.Tests.Entities;

public class SubscriberTests
{
    [Fact]
    public void Create_SetsEmailToLowerCase()
    {
        var sub = Subscriber.Create("Peter", "PETER@CLAUDYGOD.ORG");
        sub.Email.Should().Be("peter@claudygod.org");
    }

    [Fact]
    public void Create_GeneratesUnsubscribeToken()
    {
        var sub = Subscriber.Create("Peter", "peter@claudygod.org");
        sub.UnsubscribeToken.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public void Unsubscribe_SetsIsActiveFalse()
    {
        var sub = Subscriber.Create("Peter", "peter@claudygod.org");
        sub.Unsubscribe();
        sub.IsActive.Should().BeFalse();
        sub.UnsubscribedAt.Should().NotBeNull();
    }

    [Fact]
    public void Resubscribe_SetsIsActiveTrue_AndRotatesToken()
    {
        var sub = Subscriber.Create("Peter", "peter@claudygod.org");
        var originalToken = sub.UnsubscribeToken;

        sub.Unsubscribe();
        sub.Resubscribe();

        sub.IsActive.Should().BeTrue();
        sub.UnsubscribeToken.Should().NotBe(originalToken);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_ThrowsOnBlankName(string name)
    {
        var act = () => Subscriber.Create(name, "test@example.com");
        act.Should().Throw<ArgumentException>();
    }
}

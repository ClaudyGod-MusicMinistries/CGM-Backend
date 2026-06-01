using ClaudyGod.Domain.Entities;
using ClaudyGod.Domain.Enums;
using FluentAssertions;

namespace ClaudyGod.Domain.Tests.Entities;

public class PrayerRequestTests
{
    [Fact]
    public void Create_SetsStatusReceived()
    {
        var req = PrayerRequest.Create("Peter", "peter@example.com", "Healing", "Please pray for me.");
        req.Status.Should().Be(PrayerRequestStatus.Received);
    }

    [Fact]
    public void Create_NormalisesEmail()
    {
        var req = PrayerRequest.Create("Peter", "PETER@EXAMPLE.COM", "Subject", "Body");
        req.Email.Should().Be("peter@example.com");
    }

    [Fact]
    public void Respond_SetsStatusRespondedAndResponse()
    {
        var req = PrayerRequest.Create("Peter", "peter@example.com", "Healing", "Body");
        req.Respond("God bless you!", "admin@claudygod.org");

        req.Status.Should().Be(PrayerRequestStatus.Responded);
        req.AdminResponse.Should().Be("God bless you!");
        req.RespondedBy.Should().Be("admin@claudygod.org");
        req.RespondedAt.Should().NotBeNull();
    }

    [Fact]
    public void MarkPrayedFor_SetsCorrectStatus()
    {
        var req = PrayerRequest.Create("Peter", "peter@example.com", "Healing", "Body");
        req.MarkPrayedFor();
        req.Status.Should().Be(PrayerRequestStatus.PrayedFor);
    }
}

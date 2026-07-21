using ClaudyGod.Application.Features.AI.Queries;
using FluentAssertions;
using FluentValidation.TestHelper;

namespace ClaudyGod.Application.Tests.Features.AI;

public class ChatWithAssistantQueryValidatorTests
{
    private readonly ChatWithAssistantQueryValidator _validator = new();

    [Fact]
    public void Fails_WhenMessageIsEmpty() =>
        _validator.TestValidate(new ChatWithAssistantQuery("", null))
            .ShouldHaveValidationErrorFor(x => x.Message);

    [Fact]
    public void Fails_WhenMessageExceedsMaxLength() =>
        _validator.TestValidate(new ChatWithAssistantQuery(new string('a', 1001), null))
            .ShouldHaveValidationErrorFor(x => x.Message);

    [Fact]
    public void Passes_ForValidMessage() =>
        _validator.TestValidate(new ChatWithAssistantQuery("Hello there", null))
            .ShouldNotHaveValidationErrorFor(x => x.Message);
}

public class PrayerChatQueryValidatorTests
{
    private readonly PrayerChatQueryValidator _validator = new();

    [Fact]
    public void Fails_WhenMessageExceedsMaxLength() =>
        _validator.TestValidate(new PrayerChatQuery(new string('a', 2001)))
            .ShouldHaveValidationErrorFor(x => x.Message);

    [Fact]
    public void Passes_ForMessageUpToTwoThousandChars() =>
        _validator.TestValidate(new PrayerChatQuery(new string('a', 2000)))
            .ShouldNotHaveValidationErrorFor(x => x.Message);
}

public class BookingHelpChatQueryValidatorTests
{
    private readonly BookingHelpChatQueryValidator _validator = new();

    [Fact]
    public void Fails_WhenMessageIsEmpty() =>
        _validator.TestValidate(new BookingHelpChatQuery(""))
            .ShouldHaveValidationErrorFor(x => x.Message);
}

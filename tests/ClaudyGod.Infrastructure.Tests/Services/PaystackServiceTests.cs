using System.Net;
using System.Text;
using ClaudyGod.Domain.Exceptions;
using ClaudyGod.Infrastructure.Services;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;

namespace ClaudyGod.Infrastructure.Tests.Services;

public class PaystackServiceTests
{
    private sealed class FakeHandler : HttpMessageHandler
    {
        private readonly Func<HttpRequestMessage, HttpResponseMessage> _respond;
        public HttpRequestMessage? LastRequest { get; private set; }

        public FakeHandler(Func<HttpRequestMessage, HttpResponseMessage> respond) => _respond = respond;

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request, CancellationToken cancellationToken)
        {
            LastRequest = request;
            return Task.FromResult(_respond(request));
        }
    }

    private static HttpResponseMessage JsonResponse(HttpStatusCode status, string json) => new(status)
    {
        Content = new StringContent(json, Encoding.UTF8, "application/json"),
    };

    private static PaystackService NewService(
        Func<HttpRequestMessage, HttpResponseMessage> respond, string? secretKey = "sk_test_real_key")
    {
        var handler = new FakeHandler(respond);
        var http = new HttpClient(handler) { BaseAddress = new Uri("https://api.paystack.co") };
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { ["Paystack:SecretKey"] = secretKey })
            .Build();

        return new PaystackService(http, config, NullLogger<PaystackService>.Instance);
    }

    [Fact]
    public void IsConfigured_FalseWhenSecretKeyMissing()
    {
        var service = NewService(_ => throw new InvalidOperationException("should not be called"), secretKey: null);
        service.IsConfigured.Should().BeFalse();
    }

    [Fact]
    public void IsConfigured_FalseWhenSecretKeyIsPlaceholder()
    {
        var service = NewService(_ => throw new InvalidOperationException("should not be called"),
            secretKey: "CHANGE-ME-paystack-secret-key");
        service.IsConfigured.Should().BeFalse();
    }

    [Fact]
    public async Task VerifyTransactionAsync_WhenNotConfigured_ThrowsServiceUnavailable()
    {
        var service = NewService(_ => throw new InvalidOperationException("should not be called"), secretKey: null);

        var act = () => service.VerifyTransactionAsync("ref-123");

        await act.Should().ThrowAsync<ServiceUnavailableException>();
    }

    [Fact]
    public async Task VerifyTransactionAsync_OnSuccessfulPaystackResponse_ReturnsSuccessResult()
    {
        var service = NewService(_ => JsonResponse(HttpStatusCode.OK, """
            { "status": true, "data": { "status": "success", "amount": 10000, "currency": "NGN", "reference": "ref-123" } }
            """));

        var result = await service.VerifyTransactionAsync("ref-123");

        result.Success.Should().BeTrue();
        result.AmountInMinorUnits.Should().Be(10000);
        result.Currency.Should().Be("NGN");
    }

    [Fact]
    public async Task VerifyTransactionAsync_OnFailedPaystackStatus_ReturnsUnsuccessfulResult()
    {
        var service = NewService(_ => JsonResponse(HttpStatusCode.OK, """
            { "status": true, "data": { "status": "failed", "amount": 10000, "currency": "NGN", "reference": "ref-123" } }
            """));

        var result = await service.VerifyTransactionAsync("ref-123");

        result.Success.Should().BeFalse();
    }

    [Fact]
    public async Task VerifyTransactionAsync_OnHttpErrorStatus_ReturnsUnsuccessfulResult()
    {
        var service = NewService(_ => JsonResponse(HttpStatusCode.NotFound, """{ "status": false, "message": "not found" }"""));

        var result = await service.VerifyTransactionAsync("bad-ref");

        result.Success.Should().BeFalse();
    }

    [Fact]
    public async Task VerifyTransactionAsync_OnNetworkFailure_ThrowsServiceUnavailable()
    {
        var handler = new FakeHandler(_ => throw new HttpRequestException("network down"));
        var http = new HttpClient(handler);
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { ["Paystack:SecretKey"] = "sk_test_real_key" })
            .Build();
        var service = new PaystackService(http, config, NullLogger<PaystackService>.Instance);

        var act = () => service.VerifyTransactionAsync("ref-123");

        await act.Should().ThrowAsync<ServiceUnavailableException>();
    }

    [Fact]
    public async Task VerifyTransactionAsync_SendsBearerAuthorizationHeaderWithConfiguredSecret()
    {
        var handler = new FakeHandler(_ => JsonResponse(HttpStatusCode.OK, """
            { "status": true, "data": { "status": "success", "amount": 100, "currency": "NGN", "reference": "r" } }
            """));
        var http = new HttpClient(handler);
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { ["Paystack:SecretKey"] = "sk_test_abc123" })
            .Build();
        var service = new PaystackService(http, config, NullLogger<PaystackService>.Instance);

        await service.VerifyTransactionAsync("r");

        handler.LastRequest.Should().NotBeNull();
        handler.LastRequest!.Headers.Authorization!.Scheme.Should().Be("Bearer");
        handler.LastRequest.Headers.Authorization!.Parameter.Should().Be("sk_test_abc123");
    }
}

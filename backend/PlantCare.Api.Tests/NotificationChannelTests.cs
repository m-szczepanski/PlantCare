using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using PlantCare.Api.Services;
using Xunit;

namespace PlantCare.Api.Tests;

public class NotificationChannelTests
{
    private sealed class RecordingHandler : HttpMessageHandler
    {
        public HttpRequestMessage? Request { get; private set; }

        public string? Body { get; private set; }

        public HttpStatusCode Status { get; set; } = HttpStatusCode.OK;

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Request = request;
            Body = request.Content is null ? null : await request.Content.ReadAsStringAsync(cancellationToken);
            return new HttpResponseMessage(Status);
        }
    }

    private sealed class FakePublisher : INtfyPublisher
    {
        public (string Title, string Message, int Priority, string? Click, string? ButtonUrl)? Last { get; private set; }

        public Task PublishAsync(string title, string message, int priority = 3, string? clickUrl = null, string? buttonLabel = null, string? buttonUrl = null, CancellationToken cancellationToken = default)
        {
            Last = (title, message, priority, clickUrl, buttonUrl);
            return Task.CompletedTask;
        }
    }

    [Fact]
    public async Task NtfyChannel_MapsDigestOntoPublisher()
    {
        var publisher = new FakePublisher();
        var channel = new NtfyChannel(publisher);

        await channel.SendAsync(new NotificationMessage("T", "B", 5, "http://app", "http://water"));

        Assert.Equal(("T", "B", 5, "http://app", "http://water"), publisher.Last);
    }

    [Fact]
    public async Task TelegramChannel_PostsMarkdownMessageToBotApi()
    {
        var handler = new RecordingHandler();
        var channel = new TelegramChannel(
            new HttpClient(handler),
            new TelegramOptions("123:ABC", "-100999"),
            NullLogger<TelegramChannel>.Instance);

        await channel.SendAsync(new NotificationMessage("Title", "Body line", 3, null, "http://water"));

        Assert.Equal("https://api.telegram.org/bot123:ABC/sendMessage", handler.Request!.RequestUri!.ToString());
        Assert.Contains("\"chat_id\":\"-100999\"", handler.Body);
        Assert.Contains("*Title*", handler.Body);
        Assert.Contains("[Water now](http://water)", handler.Body);
        Assert.Contains("Markdown", handler.Body);
    }

    [Fact]
    public async Task TelegramChannel_NonSuccess_Throws()
    {
        var handler = new RecordingHandler { Status = HttpStatusCode.BadRequest };
        var channel = new TelegramChannel(
            new HttpClient(handler),
            new TelegramOptions("t", "c"),
            NullLogger<TelegramChannel>.Instance);

        await Assert.ThrowsAsync<HttpRequestException>(() =>
            channel.SendAsync(new NotificationMessage("t", "b", 3)));
    }

    [Fact]
    public async Task DigestRecordsEvenWhenOneChannelFails()
    {
        using var database = new TempDatabase();
        var publisher = new FakePublisher();
        using var factory = database.CreateFactory(services =>
        {
            services.Remove(services.Single(d => d.ServiceType == typeof(INtfyPublisher)));
            services.AddSingleton<INtfyPublisher>(publisher);
            services.AddSingleton<INotificationChannel>(new BrokenChannel());
        });
        var client = factory.CreateClient();

        await client.PostAsJsonAsync("/api/plants", new
        {
            nickName = "Halfway",
            customWateringIntervalDays = 7,
            lastWateredAt = DateTime.UtcNow.Date.AddDays(-10),
        });

        await using var scope = factory.Services.CreateAsyncScope();
        var result = await scope.ServiceProvider.GetRequiredService<IWateringCheckService>().RunAsync();

        Assert.Equal(1, result.SentDigests);
        Assert.NotNull(publisher.Last);
    }

    private sealed class BrokenChannel : INotificationChannel
    {
        public string Name => "broken";

        public Task SendAsync(NotificationMessage message, CancellationToken cancellationToken = default)
            => throw new HttpRequestException("channel down");
    }
}



using System.Net;
using Microsoft.Extensions.Logging.Abstractions;
using PlantCare.Api.Services;
using Xunit;

namespace PlantCare.Api.Tests;

public class NtfyPublisherTests
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

    private static NtfyPublisher Create(RecordingHandler handler, string baseUrl = "http://ntfy.example:8080/")
        => new(new HttpClient(handler), new NtfyOptions(baseUrl, "plant-care"), NullLogger<NtfyPublisher>.Instance);

    [Fact]
    public async Task Publish_PostsMessageToTopicUrlWithTitleHeader()
    {
        var handler = new RecordingHandler();

        await Create(handler).PublishAsync("Watering due", "Thirsty (Desk) — 3 days overdue");

        Assert.NotNull(handler.Request);
        Assert.Equal(HttpMethod.Post, handler.Request.Method);
        Assert.Equal("http://ntfy.example:8080/plant-care", handler.Request.RequestUri!.ToString());
        Assert.Equal("Thirsty (Desk) — 3 days overdue", handler.Body);
        Assert.True(handler.Request.Headers.TryGetValues("Title", out var titles));
        Assert.Equal(["Watering due"], titles!);
    }

    [Fact]
    public async Task Publish_ExplicitPriority_SentAsHeader()
    {
        var handler = new RecordingHandler();

        await Create(handler).PublishAsync("Overdue", "Thirsty", 5);

        Assert.True(handler.Request!.Headers.TryGetValues("Priority", out var values));
        Assert.Equal(["5"], values!);
    }

    [Fact]
    public async Task Publish_NonSuccessStatus_Throws()
    {
        var handler = new RecordingHandler { Status = HttpStatusCode.NotFound };

        await Assert.ThrowsAsync<HttpRequestException>(() => Create(handler).PublishAsync("t", "m"));
    }
}

using System.Net;
using Microsoft.Extensions.Logging;
using WatchTracker.Api.Services;

namespace WatchTracker.Api.Tests;

public class OllamaChatTests
{
    [Fact]
    public async Task An_unreachable_provider_becomes_a_request_failure_that_names_the_setting()
    {
        var logger = new CollectingLogger(LogLevel.Information);

        // Unhandled, this reached the client as a 500 with nothing to act on.
        var error = await Assert.ThrowsAsync<InvalidOperationException>(() => Send(
            new ThrowingHandler(new HttpRequestException("Connection refused")),
            logger));

        Assert.Contains("could not reach Ollama", error.Message);
        Assert.Contains("Admin", error.Message);
        Assert.Contains(logger.Messages, m => m.Contains("could not reach the configured model provider"));
    }

    [Fact]
    public async Task A_connection_failure_keeps_the_url_and_the_exception_for_debug()
    {
        var atInformation = new CollectingLogger(LogLevel.Information);
        var atDebug = new CollectingLogger(LogLevel.Debug);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            Send(new ThrowingHandler(new HttpRequestException("Connection refused")), atInformation));
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            Send(new ThrowingHandler(new HttpRequestException("Connection refused")), atDebug));

        Assert.DoesNotContain(atInformation.Messages, m => m.Contains("http://ollama.test"));
        Assert.Contains(atDebug.Messages, m => m.Contains("http://ollama.test"));
    }

    [Fact]
    public async Task A_rejected_request_returns_its_body_and_logs_the_status()
    {
        var logger = new CollectingLogger(LogLevel.Information);

        var result = await Send(
            new StubHandler(HttpStatusCode.NotFound, "model 'missing' not found"),
            logger);

        // The caller keeps its own wording for a rejection, so the body comes back.
        Assert.False(result.IsSuccess);
        Assert.Equal(HttpStatusCode.NotFound, result.StatusCode);
        Assert.Contains("model 'missing' not found", result.Body);
        Assert.Contains(logger.Messages, m => m.Contains("HTTP 404"));
        // The provider's body is not part of a redacted log.
        Assert.DoesNotContain(logger.Messages, m => m.Contains("model 'missing' not found"));
    }

    [Fact]
    public async Task Debug_logging_carries_the_prompt_the_reply_and_the_provider_body()
    {
        var rejected = new CollectingLogger(LogLevel.Debug);
        var answered = new CollectingLogger(LogLevel.Debug);

        await Send(new StubHandler(HttpStatusCode.BadRequest, "PROVIDER_DETAIL"), rejected);
        await Send(new StubHandler(HttpStatusCode.OK, "MODEL_REPLY"), answered);

        Assert.Contains(rejected.Messages, m => m.Contains("PROVIDER_DETAIL"));
        Assert.Contains(rejected.Messages, m => m.Contains("PROMPT_TEXT"));
        Assert.Contains(answered.Messages, m => m.Contains("MODEL_REPLY"));
    }

    [Fact]
    public async Task A_cancelled_request_stays_a_cancellation_for_the_caller_to_explain()
    {
        using var cancelled = new CancellationTokenSource();
        await cancelled.CancelAsync();

        // The caller knows whether this was its own timeout or a caller hanging up.
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => Send(
            new ThrowingHandler(new TaskCanceledException()),
            new CollectingLogger(LogLevel.Debug),
            cancelled.Token));
    }

    [Fact]
    public void Logged_payloads_are_bounded_and_single_line()
    {
        var forged = LogText.Bounded("first line\nwarn: forged second line");
        Assert.DoesNotContain("\n", forged);

        var long_ = LogText.Bounded(new string('x', LogText.DefaultMaxLength + 500));
        Assert.True(long_.Length < LogText.DefaultMaxLength + 100);
        Assert.Contains("characters", long_);
    }

    private static Task<OllamaChatResult> Send(
        HttpMessageHandler handler,
        ILogger logger,
        CancellationToken ct = default) =>
        OllamaChat.SendAsync(
            new HttpClient(handler),
            new HttpRequestMessage(HttpMethod.Post, "http://ollama.test/api/chat"),
            logger,
            "test feature",
            "http://ollama.test",
            "PROMPT_TEXT",
            ct);

    private sealed class StubHandler(HttpStatusCode status, string body) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken) =>
            Task.FromResult(new HttpResponseMessage(status) { Content = new StringContent(body) });
    }

    private sealed class ThrowingHandler(Exception failure) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken) =>
            throw failure;
    }

    private sealed class CollectingLogger(LogLevel minimum) : ILogger
    {
        public List<string> Messages { get; } = [];

        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;
        public bool IsEnabled(LogLevel logLevel) => logLevel >= minimum;

        public void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter)
        {
            if (!IsEnabled(logLevel)) return;
            Messages.Add($"{formatter(state, exception)} {exception?.Message}");
        }
    }
}

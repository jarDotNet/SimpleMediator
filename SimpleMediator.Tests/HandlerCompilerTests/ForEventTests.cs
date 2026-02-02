using FluentAssertions;
using SimpleMediator.Internals;

namespace SimpleMediator.Tests.HandlerCompilerTests;

/// <summary>
/// Tests for HandlerCompiler.ForEvent method.
/// </summary>
public class ForEventTests
{
    public record TestEvent(string Message);

    public class EventTaskHandler
    {
        public string? ReceivedMessage { get; private set; }

        public Task Handle(TestEvent @event, CancellationToken ct)
        {
            ReceivedMessage = @event.Message;
            return Task.CompletedTask;
        }
    }

    public class EventVoidHandler
    {
        public bool WasCalled { get; private set; }

        public void Handle(TestEvent @event)
        {
            WasCalled = true;
        }
    }

    public class EventCancellableHandler
    {
        public bool TokenWasNotCancelled { get; private set; }

        public Task Handle(TestEvent @event, CancellationToken ct)
        {
            TokenWasNotCancelled = !ct.IsCancellationRequested;
            return Task.CompletedTask;
        }
    }

    [Fact]
    public async Task ForEvent_WithTaskHandler_CompilesCorrectly()
    {
        var method = typeof(EventTaskHandler).GetMethod("Handle")!;

        var compiled = HandlerCompiler.ForEvent<TestEvent>(typeof(EventTaskHandler), method);

        var handler = new EventTaskHandler();
        var task = compiled(handler, new TestEvent("msg"), CancellationToken.None);

        task.Should().NotBeNull();
        await task!;
        handler.ReceivedMessage.Should().Be("msg");
    }

    [Fact]
    public void ForEvent_WithVoidHandler_CompilesAndReturnsNull()
    {
        var method = typeof(EventVoidHandler).GetMethod("Handle")!;

        var compiled = HandlerCompiler.ForEvent<TestEvent>(typeof(EventVoidHandler), method);

        var handler = new EventVoidHandler();
        var task = compiled(handler, new TestEvent("test"), CancellationToken.None);

        task.Should().BeNull();
        handler.WasCalled.Should().BeTrue();
    }

    [Fact]
    public async Task ForEvent_WithCancellationToken_PassesToken()
    {
        var method = typeof(EventCancellableHandler).GetMethod("Handle")!;
        using var cts = new CancellationTokenSource();

        var compiled = HandlerCompiler.ForEvent<TestEvent>(typeof(EventCancellableHandler), method);

        var handler = new EventCancellableHandler();
        var task = compiled(handler, new TestEvent("test"), cts.Token);

        await task!;
        handler.TokenWasNotCancelled.Should().BeTrue();
    }
}

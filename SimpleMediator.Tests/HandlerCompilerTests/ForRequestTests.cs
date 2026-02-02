using FluentAssertions;
using SimpleMediator.Internals;

namespace SimpleMediator.Tests.HandlerCompilerTests;

/// <summary>
/// Tests for HandlerCompiler.ForRequest method.
/// </summary>
public class ForRequestTests
{
    public record TestRequest(string Value);

    public class TaskOfTHandler
    {
        public Task<string> Handle(TestRequest request, CancellationToken ct)
            => Task.FromResult($"Handled: {request.Value}");
    }

    public class TaskHandler
    {
        public bool WasCalled { get; private set; }

        public Task Handle(TestRequest request, CancellationToken ct)
        {
            WasCalled = true;
            return Task.CompletedTask;
        }
    }

    public class SyncHandler
    {
        public int Handle(TestRequest request) => 42;
    }

    public class CancellableHandler
    {
        public Task<bool> Handle(TestRequest request, CancellationToken ct)
            => Task.FromResult(!ct.IsCancellationRequested);
    }

    public class NoCancellationHandler
    {
        public Task<string> Handle(TestRequest request)
            => Task.FromResult($"Got: {request.Value}");
    }

    [Fact]
    public async Task ForRequest_WithTaskOfTHandler_CompilesCorrectly()
    {
        var method = typeof(TaskOfTHandler).GetMethod("Handle")!;

        var compiled = HandlerCompiler.ForRequest<TestRequest, string>(typeof(TaskOfTHandler), method);

        var handler = new TaskOfTHandler();
        var result = await compiled(handler, new TestRequest("test"), CancellationToken.None);

        result.Should().Be("Handled: test");
    }

    [Fact]
    public async Task ForRequest_WithTaskHandler_CompilesAndReturnsUnit()
    {
        var method = typeof(TaskHandler).GetMethod("Handle")!;

        var compiled = HandlerCompiler.ForRequest<TestRequest, Unit>(typeof(TaskHandler), method);

        var handler = new TaskHandler();
        var result = await compiled(handler, new TestRequest("test"), CancellationToken.None);

        result.Should().Be(Unit.Value);
        handler.WasCalled.Should().BeTrue();
    }

    [Fact]
    public async Task ForRequest_WithSyncHandler_CompilesCorrectly()
    {
        var method = typeof(SyncHandler).GetMethod("Handle")!;

        var compiled = HandlerCompiler.ForRequest<TestRequest, int>(typeof(SyncHandler), method);

        var handler = new SyncHandler();
        var result = await compiled(handler, new TestRequest("test"), CancellationToken.None);

        result.Should().Be(42);
    }

    [Fact]
    public async Task ForRequest_WithCancellationToken_PassesToken()
    {
        var method = typeof(CancellableHandler).GetMethod("Handle")!;
        using var cts = new CancellationTokenSource();

        var compiled = HandlerCompiler.ForRequest<TestRequest, bool>(typeof(CancellableHandler), method);

        var handler = new CancellableHandler();
        var result = await compiled(handler, new TestRequest("test"), cts.Token);

        result.Should().BeTrue();
    }

    [Fact]
    public async Task ForRequest_WithoutCancellationToken_StillWorks()
    {
        var method = typeof(NoCancellationHandler).GetMethod("Handle")!;

        var compiled = HandlerCompiler.ForRequest<TestRequest, string>(typeof(NoCancellationHandler), method);

        var handler = new NoCancellationHandler();
        var result = await compiled(handler, new TestRequest("data"), CancellationToken.None);

        result.Should().Be("Got: data");
    }
}

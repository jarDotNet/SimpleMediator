using FluentAssertions;

namespace SimpleMediator.Tests;

public class UnitTests
{
    [Fact]
    public void Unit_Value_ReturnsDefaultInstance()
    {
        var unit1 = Unit.Value;
        var unit2 = Unit.Value;

        unit1.Should().Be(unit2);
    }

    [Fact]
    public void Unit_IsValueType()
    {
        var unit = Unit.Value;

        unit.Should().BeAssignableTo<ValueType>();
    }

    [Fact]
    public void Unit_DefaultEqualsValue()
    {
        var defaultUnit = default(Unit);
        var valueUnit = Unit.Value;

        defaultUnit.Should().Be(valueUnit);
    }

    [Fact]
    public async Task Unit_CanBeUsedAsTaskResult()
    {
        var task = Task.FromResult(Unit.Value);

        task.Should().NotBeNull();
        var result = await task;
        result.Should().Be(Unit.Value);
    }

    [Fact]
    public async Task Unit_SupportsCommandPattern()
    {
        var command = new UnitTestCommand { Data = "Test" };
        var handler = new UnitTestCommandHandler();

        var result = await handler.Handle(command);

        result.Should().Be(Unit.Value);
    }
}

// Test types for Unit
public class UnitTestCommand
{
    public string Data { get; set; } = string.Empty;
}

public class UnitTestCommandHandler
{
    public Task<Unit> Handle(UnitTestCommand command)
    {
        return Task.FromResult(Unit.Value);
    }
}

# SimpleMediator.Benchmarks

Performance benchmarks for the SimpleMediator library using [BenchmarkDotNet](https://benchmarkdotnet.org/).

## Prerequisites

- .NET 9.0 SDK or later
- Release build is required for accurate benchmark results

## Running Benchmarks

### Run All Benchmarks

```bash
dotnet run -c Release --project SimpleMediator.Benchmarks
```

Then select which benchmark to run from the interactive menu, or press `*` to run all.

### Run Specific Benchmark Class

```bash
# Core mediator operations (Send, Publish)
dotnet run -c Release --project SimpleMediator.Benchmarks -- --filter "*MediatorBenchmarks*"

# Pipeline middleware overhead
dotnet run -c Release --project SimpleMediator.Benchmarks -- --filter "*PipelineBenchmarks*"

# Handler caching impact
dotnet run -c Release --project SimpleMediator.Benchmarks -- --filter "*CachingBenchmarks*"

# Throughput (sequential vs parallel)
dotnet run -c Release --project SimpleMediator.Benchmarks -- --filter "*ThroughputBenchmarks*"
```

### Quick Dry Run (for testing)

```bash
dotnet run -c Release --project SimpleMediator.Benchmarks -- --job dry --filter "*MediatorBenchmarks*"
```

### Running from Visual Studio

1. Set `SimpleMediator.Benchmarks` as the startup project (right-click → **Set as Startup Project**)
2. Switch to **Release** configuration (very important for accurate results)
3. Press `Ctrl+F5` (Start Without Debugging) or `F5` (Start Debugging)
4. When prompted, select which benchmark to run:
   - Enter `*` to run all benchmarks
   - Enter a number (e.g., `0`) to run a specific benchmark
   - Enter a name (e.g., `MediatorBenchmarks`) to filter by class
   - Enter multiple selections separated by space (e.g., `0 1 2`)

**To skip the interactive menu**, configure command line arguments:

1. Right-click `SimpleMediator.Benchmarks` → **Properties**
2. Go to **Debug** → **General** → **Open debug launch profiles UI**
3. Add command line arguments: `--filter *` (runs all) or `--filter *MediatorBenchmarks*` (specific class)

## Benchmark Classes

### MediatorBenchmarks

Core performance tests for the mediator operations:

| Benchmark | Description |
|-----------|-------------|
| `Send<TResponse>(command)` | Command with return value |
| `Send(command) - void` | Command without return value (Unit) |
| `Send<TResponse>(query)` | Query with return value |
| `Publish(event) - 3 handlers` | Event with 3 handlers |

### PipelineBenchmarks

Measures the overhead of pipeline middlewares:

| Benchmark | Description |
|-----------|-------------|
| No pipeline | Baseline without any middlewares |
| 1 pipeline middleware | Single pass-through middleware |
| 2 pipeline middlewares | Two chained middlewares |
| 3 pipeline middlewares | Three chained middlewares |

### CachingBenchmarks

Validates the effectiveness of handler wrapper caching (each Mediator instance has its own cache):

| Benchmark | Description |
|-----------|-------------|
| Cached handler (warm) | Using pre-warmed cache |
| New mediator instance (cold cache) | New mediator with empty cache (shows compilation cost) |

### ThroughputBenchmarks

Tests throughput with varying request counts (10, 100, 1000):

| Benchmark | Description |
|-----------|-------------|
| Sequential commands | Process commands one at a time |
| Parallel commands | Process all commands concurrently |
| Sequential events | Publish events one at a time |
| Parallel events | Publish all events concurrently |

## Output

Results are displayed in the console and saved to the `BenchmarkDotNet.Artifacts` folder, including:

- Summary tables with execution times
- Memory allocation statistics
- HTML and Markdown reports

## Example Output

```
| Method                        | Mean      | Error    | StdDev   | Rank | Gen0   | Allocated |
|------------------------------ |----------:|---------:|---------:|-----:|-------:|----------:|
| Send(command) - void          |  72.67 ns | 1.484 ns | 1.929 ns |    1 | 0.0204 |     256 B |
| Send<TResponse>(command)      |  92.81 ns | 1.848 ns | 2.651 ns |    2 | 0.0459 |     576 B |
| Publish(event) - 3 handlers   | 101.59 ns | 1.998 ns | 1.771 ns |    3 | 0.0229 |     288 B |
| Send<TResponse>(query)        | 125.14 ns | 2.492 ns | 2.967 ns |    4 | 0.0496 |     624 B |
```

> **Note:** These results use compiled expression delegates for handler invocation, providing ~25-40% 
> better performance than reflection-based `MethodInfo.Invoke()`.

## Tips

- Always run benchmarks in **Release** mode for accurate results
- Close other applications to reduce noise
- Run multiple times to ensure consistent results
- Use `--job short` for faster iterations during development

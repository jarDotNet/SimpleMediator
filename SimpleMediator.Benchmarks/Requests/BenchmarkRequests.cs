namespace SimpleMediator.Benchmarks.Requests;

// Simple command with response
public record SimpleCommand(string Value);

// Command without response (returns Unit)
public record VoidCommand(string Value);

// Query with response
public record SimpleQuery(int Id);

// Event for publish benchmark
public record SimpleEvent(int Id, string Message);

// Response DTO
public record SimpleResponse(int Id, string Message);

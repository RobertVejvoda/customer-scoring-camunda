namespace ScoringAPI.Camunda.Command;

public record CreateInstanceRequest(
    [Required] string BpmnProcessId,
    int? Version,
    string? RequestTimeout,
    string[]? FetchVariables,
    object? Variables);
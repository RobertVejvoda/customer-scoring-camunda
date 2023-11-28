namespace ScoringAPI.Camunda.Command;

public record CreateInstanceRequest(
    string BpmnProcessId,
    int? Version,
    bool? WithResult,
    string? RequestTimeout,
    string[]? FetchVariables,
    object Variables);
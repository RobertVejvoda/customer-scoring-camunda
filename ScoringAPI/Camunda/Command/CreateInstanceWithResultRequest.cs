namespace ScoringAPI.Camunda.Command;

public record CreateInstanceWithResultRequest(
    string BpmnProcessId,
    int? Version,
    bool? WithResult,
    string? RequestTimeout,
    string[]? FetchVariables,
    object Variables);
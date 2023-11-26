namespace ScoringAPI.Camunda.Command;

public record CreateInstanceWithResultResponse(
    long? ProcessDefinitionKey,
    string? BpmnProcessId,
    int? Version,
    long? ProcessInstanceKey,
    string? Variables);
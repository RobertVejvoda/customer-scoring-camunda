namespace ScoringAPI.Camunda.Command;

public record PublishMessageRequest(
    [Required] string MessageName,
    [Required] string CorrelationKey,
    string? MessageId,
    string? TimeToLive,
    object? Variables);
namespace ScoringAPI.Camunda.Command;

public record SaveBinaryFileRequest(
    [Required] string FileName,
    [Required] string Data,
    object? Metadata);
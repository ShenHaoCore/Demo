namespace Demo.Saga.Api.Entities;

public enum TripStatus
{
    Running,
    Completed,
    Compensated,
    Failed
}

public enum StepStatus
{
    Pending,
    Succeeded,
    Failed,
    Compensated,
    Skipped
}

/// <summary>Saga 步骤。</summary>
public record SagaStep(string Name, StepStatus Status, string? Detail, DateTimeOffset? UpdatedAt);

/// <summary>行程实体。</summary>
public record Trip(
    Guid Id,
    string Destination,
    string Traveler,
    TripStatus Status,
    List<SagaStep> Steps,
    DateTimeOffset CreatedAt,
    DateTimeOffset? CompletedAt);

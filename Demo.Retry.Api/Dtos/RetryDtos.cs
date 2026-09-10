namespace Demo.Retry.Api.Dtos;

/// <summary>不稳定下游调用结果。</summary>
public sealed record UnstableResultDto(bool Success, int Attempt, double FailureRate, string Message, DateTimeOffset Timestamp);

/// <summary>失败率配置输入。</summary>
public sealed record UnstableConfigDto(double FailureRate);

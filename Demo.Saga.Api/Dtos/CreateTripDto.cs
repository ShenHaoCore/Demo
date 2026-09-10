namespace Demo.Saga.Api.Dtos;

/// <summary>创建行程输入。</summary>
public record CreateTripDto(string Destination, string? Traveler);

/// <summary>Saga 强制失败选项（演示用）。</summary>
public record SagaFailOptions(bool FailFlight, bool FailHotel, bool FailCar);

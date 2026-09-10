using Demo.Saga.Api.Dtos;
using Demo.Saga.Api.Entities;

namespace Demo.Saga.Api.Application;

/// <summary>行程 Saga 应用服务契约。</summary>
public interface ITripAppService
{
    Trip CreateTrip(string destination, string traveler);

    Trip? GetTrip(Guid id);

    IReadOnlyList<Trip> GetAll();

    Task<Trip> RunSagaAsync(Guid tripId, SagaFailOptions options);
}

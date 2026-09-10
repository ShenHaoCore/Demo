using System.Collections.Concurrent;
using Demo.Saga.Api.Dtos;
using Demo.Saga.Api.Entities;

namespace Demo.Saga.Api.Application;

/// <summary>行程 Saga 应用服务。</summary>
public sealed class TripAppService : ITripAppService
{
    private readonly ConcurrentDictionary<Guid, Trip> _trips = new();

    public Trip CreateTrip(string destination, string traveler)
    {
        var trip = new Trip(
            Guid.NewGuid(),
            destination,
            traveler,
            TripStatus.Running,
            [
                new SagaStep("订机票", StepStatus.Pending, null, null),
                new SagaStep("订酒店", StepStatus.Pending, null, null),
                new SagaStep("租车", StepStatus.Pending, null, null)
            ],
            DateTimeOffset.UtcNow,
            null);

        _trips[trip.Id] = trip;
        return Clone(trip);
    }

    public Trip? GetTrip(Guid id) =>
        _trips.TryGetValue(id, out var trip) ? Clone(trip) : null;

    public IReadOnlyList<Trip> GetAll() =>
        _trips.Values.OrderByDescending(t => t.CreatedAt).Select(Clone).ToList();

    public async Task<Trip> RunSagaAsync(Guid tripId, SagaFailOptions options)
    {
        if (!_trips.TryGetValue(tripId, out var trip))
        {
            throw new InvalidOperationException("行程不存在");
        }

        var steps = trip.Steps.Select(s => s with { }).ToList();
        var completed = new Stack<int>();

        if (!await ExecuteStepAsync(steps, 0, options.FailFlight, "机票已预订", "订机票失败"))
        {
            MarkSkipped(steps);
            return Save(trip, TripStatus.Failed, steps);
        }

        completed.Push(0);

        if (!await ExecuteStepAsync(steps, 1, options.FailHotel, "酒店已预订", "订酒店失败"))
        {
            await CompensateAsync(steps, completed);
            MarkSkipped(steps);
            return Save(trip, TripStatus.Compensated, steps);
        }

        completed.Push(1);

        if (!await ExecuteStepAsync(steps, 2, options.FailCar, "租车成功", "租车失败"))
        {
            await CompensateAsync(steps, completed);
            MarkSkipped(steps);
            return Save(trip, TripStatus.Compensated, steps);
        }

        return Save(trip, TripStatus.Completed, steps);
    }

    private Trip Save(Trip trip, TripStatus status, List<SagaStep> steps)
    {
        var updated = trip with
        {
            Status = status,
            Steps = steps,
            CompletedAt = DateTimeOffset.UtcNow
        };
        _trips[trip.Id] = updated;
        return Clone(updated);
    }

    private static async Task<bool> ExecuteStepAsync(
        List<SagaStep> steps,
        int index,
        bool forceFail,
        string okDetail,
        string failDetail)
    {
        await Task.Delay(50);
        var now = DateTimeOffset.UtcNow;
        if (forceFail)
        {
            steps[index] = steps[index] with
            {
                Status = StepStatus.Failed,
                Detail = failDetail,
                UpdatedAt = now
            };
            return false;
        }

        steps[index] = steps[index] with
        {
            Status = StepStatus.Succeeded,
            Detail = okDetail,
            UpdatedAt = now
        };
        return true;
    }

    private static async Task CompensateAsync(List<SagaStep> steps, Stack<int> completed)
    {
        while (completed.Count > 0)
        {
            var index = completed.Pop();
            await Task.Delay(30);
            var name = steps[index].Name;
            steps[index] = steps[index] with
            {
                Status = StepStatus.Compensated,
                Detail = $"已补偿：取消{name}",
                UpdatedAt = DateTimeOffset.UtcNow
            };
        }
    }

    private static void MarkSkipped(List<SagaStep> steps)
    {
        for (var i = 0; i < steps.Count; i++)
        {
            if (steps[i].Status == StepStatus.Pending)
            {
                steps[i] = steps[i] with
                {
                    Status = StepStatus.Skipped,
                    Detail = "因前序失败未执行",
                    UpdatedAt = DateTimeOffset.UtcNow
                };
            }
        }
    }

    private static Trip Clone(Trip trip) =>
        trip with { Steps = trip.Steps.Select(s => s with { }).ToList() };
}

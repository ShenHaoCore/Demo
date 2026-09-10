using Demo.Saga.Api.Application;
using Demo.Saga.Api.Dtos;
using Demo.Saga.Api.Entities;
using Microsoft.AspNetCore.Mvc;

namespace Demo.Saga.Api.Controllers;

[ApiController]
[Route("api/trips")]
public sealed class TripsController(ITripAppService tripAppService) : ControllerBase
{
    [HttpPost]
    [EndpointName("CreateTrip")]
    [EndpointSummary("编排行程：订机票→订酒店→租车；可用 failFlight/failHotel/failCar 强制失败并补偿")]
    public async Task<IActionResult> CreateAsync(
        [FromBody] CreateTripDto input,
        [FromQuery] bool? failFlight,
        [FromQuery] bool? failHotel,
        [FromQuery] bool? failCar)
    {
        if (string.IsNullOrWhiteSpace(input.Destination))
        {
            return BadRequest(new { message = "destination 不能为空" });
        }

        var trip = tripAppService.CreateTrip(input.Destination.Trim(), input.Traveler?.Trim() ?? "游客");
        var options = new SagaFailOptions(failFlight == true, failHotel == true, failCar == true);

        var latest = await tripAppService.RunSagaAsync(trip.Id, options);
        var statusCode = latest.Status == TripStatus.Completed
            ? StatusCodes.Status201Created
            : StatusCodes.Status409Conflict;

        return StatusCode(statusCode, latest);
    }

    [HttpGet("{id:guid}")]
    [EndpointName("GetTrip")]
    [EndpointSummary("查看行程步骤与补偿状态")]
    public IActionResult GetAsync(Guid id)
    {
        var trip = tripAppService.GetTrip(id);
        return trip is null
            ? NotFound(new { message = "行程不存在", id })
            : Ok(trip);
    }

    [HttpGet]
    [EndpointName("ListTrips")]
    public IActionResult GetListAsync() => Ok(tripAppService.GetAll());
}

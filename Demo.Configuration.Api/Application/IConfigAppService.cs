using Demo.Configuration.Api.Dtos;

namespace Demo.Configuration.Api.Application;

public interface IConfigAppService
{
    ConfigSnapshotDto GetSnapshot();
    ConfigProvidersResultDto GetProviders();
}

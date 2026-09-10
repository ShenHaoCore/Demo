namespace Demo.CircuitBreaker.Api.Dtos;

/// <summary>下游失败开关配置输入。</summary>
public record DownstreamConfigDto(bool ForceFail);

namespace Demo.DiLifetime.Api.Services;

/// <summary>带实例 Id，便于观察生命周期。</summary>
public interface IHasId
{
    Guid Id { get; }
}

public interface ISingletonDemoService : IHasId;
public interface IScopedDemoService : IHasId;
public interface ITransientDemoService : IHasId;

/// <summary>Singleton 示例服务（基础设施）。</summary>
public sealed class SingletonDemoService : ISingletonDemoService
{
    public Guid Id { get; } = Guid.NewGuid();
}

/// <summary>Scoped 示例服务（基础设施）。</summary>
public sealed class ScopedDemoService : IScopedDemoService
{
    public Guid Id { get; } = Guid.NewGuid();
}

/// <summary>Transient 示例服务（基础设施）。</summary>
public sealed class TransientDemoService : ITransientDemoService
{
    public Guid Id { get; } = Guid.NewGuid();
}

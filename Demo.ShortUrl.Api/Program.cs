using Demo.ShortUrl.Api.Application;
using Demo.ShortUrl.Api.Options;
using Demo.ShortUrl.Api.Services;
using Scalar.AspNetCore;
using StackExchange.Redis;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddOpenApi();
builder.Services.AddControllers();
builder.Services.Configure<ShortUrlOptions>(builder.Configuration.GetSection(ShortUrlOptions.SectionName));

var shortUrl = builder.Configuration.GetSection(ShortUrlOptions.SectionName).Get<ShortUrlOptions>()
               ?? new ShortUrlOptions();
if (string.Equals(shortUrl.Provider, "Redis", StringComparison.OrdinalIgnoreCase))
{
    var mux = ConnectionMultiplexer.Connect(shortUrl.Redis);
    builder.Services.AddSingleton<IConnectionMultiplexer>(mux);
    builder.Services.AddSingleton<IShortUrlStore, RedisShortUrlStore>();
}
else
{
    builder.Services.AddSingleton<IShortUrlStore, MemoryShortUrlStore>();
}

builder.Services.AddSingleton<IShortUrlAppService, ShortUrlAppService>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference(options => options.WithDarkMode(false));
}

app.MapControllers();
app.Run();

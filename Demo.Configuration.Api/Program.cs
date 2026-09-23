using Demo.Configuration.Api.Application;
using Demo.Configuration.Api.Options;
using Microsoft.Extensions.Options;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

// WebApplication.CreateBuilder 默认加载顺序（后覆盖前）：
// 1. appsettings.json
// 2. appsettings.{Environment}.json
// 3. User Secrets（仅 Development；dotnet user-secrets set "Demo:ApiKey" "xxx"）
// 4. 环境变量（Demo__ApiKey）
// 5. 命令行（--Demo:ApiKey=xxx）
// 环境名：ASPNETCORE_ENVIRONMENT / DOTNET_ENVIRONMENT（见 launchSettings 多 Profile）
//
// 警告：本 Demo 的 ApiKey 均为假值，仅供教学。生产密钥请用环境变量 / 密钥保管服务，切勿提交仓库。

builder.Services.AddOpenApi();
builder.Services.AddControllers();
builder.Services
    .AddOptions<DemoOptions>()
    .Bind(builder.Configuration.GetSection(DemoOptions.SectionName))
    .ValidateOnStart();
builder.Services.AddSingleton<IValidateOptions<DemoOptions>, DemoOptionsValidator>();
builder.Services.AddSingleton<IConfigAppService, ConfigAppService>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}

app.MapControllers();
app.Run();

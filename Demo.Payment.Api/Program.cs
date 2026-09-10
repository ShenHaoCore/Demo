using Demo.Payment.Api.Application;
using Demo.Payment.Api.Dtos;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddOpenApi();
builder.Services.AddControllers();
builder.Services.AddSingleton<IPaymentAppService, PaymentAppService>();

// 演示用密钥，可通过配置覆盖
var notifySecret = builder.Configuration["Payment:NotifySecret"] ?? "demo-payment-secret";
builder.Services.AddSingleton(new PaymentOptions(notifySecret));

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}

app.MapControllers();
app.Run();

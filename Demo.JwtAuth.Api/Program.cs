using System.Text;
using Demo.JwtAuth.Api.Application;
using Demo.JwtAuth.Api.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

const string issuer = "Demo.JwtAuth.Api";
const string audience = "Demo.JwtAuth.Client";
var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes("DemoJwtAuthSigningKey_AtLeast32Bytes!!"));

builder.Services.AddOpenApi();
builder.Services.AddControllers();
builder.Services.AddSingleton(new JwtTokenService(issuer, audience, signingKey));
builder.Services.AddSingleton<IAuthAppService, AuthAppService>();
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateIssuerSigningKey = true,
            ValidateLifetime = true,
            ValidIssuer = issuer,
            ValidAudience = audience,
            IssuerSigningKey = signingKey,
            ClockSkew = TimeSpan.FromSeconds(30)
        };
    });
builder.Services.AddAuthorization();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}

app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.Run();

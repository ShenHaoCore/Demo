using Demo.Versioning.Api.Application;
using Demo.Versioning.Api.Extensions;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSingleton<IProductAppService, ProductAppService>();
builder.Services.AddControllers().AddJsonOptions(options =>
{
    options.JsonSerializerOptions.DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull;
});

builder.Services.AddVersioning();
builder.Services.AddVersionedOpenApiDocuments();

var app = builder.Build();
if (app.Environment.IsDevelopment()) { app.MapVersionedOpenApiAndScalar(); }
app.MapControllers();
app.Run();

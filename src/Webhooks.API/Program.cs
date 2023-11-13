using Microsoft.Identity.Web;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();
builder.AddDefaultOpenApi();
builder.AddApplicationServices();

var app = builder.Build();

app.UseDefaultOpenApi();

app.MapDefaultEndpoints();

app.MapGroup("/api/v1/webhooks")
   .WithTags("WebHooks API")
   .MapWebhooksApi()
   .RequireAuthorization()
   .RequireScope("webhooks");

app.Run();

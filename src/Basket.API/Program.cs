using Microsoft.Identity.Web;

var builder = WebApplication.CreateBuilder(args);

builder.AddBasicServiceDefaults();
builder.AddApplicationServices();

builder.Services.AddGrpc();

var app = builder.Build();

app.MapDefaultEndpoints();

app.MapGrpcService<BasketService>()
   .RequireAuthorization()
   .RequireScope(app.Configuration["AzureAD:Scopes"].Split(' '));

app.Run();

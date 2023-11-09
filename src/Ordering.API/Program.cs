using Microsoft.Identity.Web;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();
builder.AddDefaultOpenApi();
builder.AddApplicationServices();

builder.Services.AddProblemDetails();

var app = builder.Build();

app.UseDefaultOpenApi();

app.MapDefaultEndpoints();

app.MapGroup("/api/v1/orders")
   .MapOrdersApi()
   .RequireAuthorization()
   .RequireScope(app.Configuration.GetValue<string>("AzureAD:Scopes").Split(' '));

app.Run();

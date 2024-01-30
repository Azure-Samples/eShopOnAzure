var builder = DistributedApplication.CreateBuilder(args);

var appInsights = builder.AddApplicationInsights("appInsights");
var redis = builder.AddRedisContainer("redis");
var serviceBus = builder.AddAzureServiceBus("EventBus", topicNames: ["eshop_event_bus"]);
var postgres = builder.AddPostgresContainer("postgres")
    .WithAnnotation(new ContainerImageAnnotation
    {
        Image = "ankane/pgvector",
        Tag = "latest"
    });

var catalogDb = postgres.AddDatabase("CatalogDB");
var orderDb = postgres.AddDatabase("OrderingDB");
var webhooksDb = postgres.AddDatabase("WebHooksDB");

// Services
var basketApi = builder.AddProject<Projects.Basket_API>("basket-api")
    .WithReference(redis)
    .WithReference(serviceBus)
    .WithReference(appInsights);

var catalogApi = builder.AddProject<Projects.Catalog_API>("catalog-api")
    .WithReference(serviceBus)
    .WithReference(catalogDb)
    .WithReference(appInsights);

var orderingApi = builder.AddProject<Projects.Ordering_API>("ordering-api")
    .WithReference(serviceBus)
    .WithReference(orderDb)
	.WithReference(appInsights);

builder.AddProject<Projects.OrderProcessor>("order-processor")
    .WithReference(serviceBus)
    .WithReference(orderDb)
    .WithReference(appInsights);

builder.AddProject<Projects.PaymentProcessor>("payment-processor")
    .WithReference(serviceBus)
    .WithReference(appInsights);

var webHooksApi = builder.AddProject<Projects.Webhooks_API>("webhooks-api")
    .WithReference(serviceBus)
    .WithReference(webhooksDb)
	.WithReference(appInsights);

// Reverse proxies
builder.AddProject<Projects.Mobile_Bff_Shopping>("mobile-bff")
    .WithReference(catalogApi)
	.WithReference(appInsights);

// Apps
var webhooksClient = builder.AddProject<Projects.WebhookClient>("webhooksclient")
    .WithReference(webHooksApi)
	.WithReference(appInsights);

var webApp = builder.AddProject<Projects.WebApp>("webapp")
    .WithReference(basketApi)
    .WithReference(catalogApi)
    .WithReference(orderingApi)
    .WithReference(serviceBus)
	.WithReference(appInsights)
    .WithLaunchProfile("https");

// Wire up the callback urls (self referencing)
webApp.WithEnvironment("CallBackUrl", webApp.GetEndpoint("https"));
webhooksClient.WithEnvironment("CallBackUrl", webhooksClient.GetEndpoint("https"));

builder.Build().Run();

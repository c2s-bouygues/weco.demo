using OpenTelemetry.Instrumentation.AspNetCore;
using OpenTelemetry.Logs;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using System.Diagnostics;
using System.Diagnostics.Metrics;
using System.Reflection;
using System.Web;
using WeCo.API.Models;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddHttpClient();
builder.Services.AddHttpClient("IsAlive", httpClient => {
    httpClient.BaseAddress = new Uri(builder.Configuration.GetValue<string>("IsAlive:Endpoint"));
});
builder.Services.AddHttpClient("Bureaucracy", httpClient => {
    httpClient.BaseAddress = new Uri(builder.Configuration.GetValue<string>("Bureaucracy:Endpoint"));
});

var assemblyVersion = Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? "unknown";

Action<ResourceBuilder> configureResource = r => r.AddService(
    "WeCo.API", serviceVersion: assemblyVersion, serviceInstanceId: Environment.MachineName);

AppContext.SetSwitch("System.Net.Http.SocketsHttpHandler.Http2UnencryptedSupport", true);

builder.Services.AddOpenTelemetryTracing((options) => {
    options
        .ConfigureResource(configureResource)
        .SetSampler(new AlwaysOnSampler())
        .AddHttpClientInstrumentation()
        .AddAspNetCoreInstrumentation()
        .AddSource("WeCo.API.ActivitySource")
        .AddOtlpExporter(otlpOptions => {
            otlpOptions.Endpoint = new Uri(builder.Configuration.GetValue<string>("Otlp:Endpoint"));
        })
        //.AddConsoleExporter()
        ;
});
// For options which can be bound from IConfiguration.
builder.Services.Configure<AspNetCoreInstrumentationOptions>(builder.Configuration.GetSection("AspNetCoreInstrumentation"));

// Logging
//builder.Logging.ClearProviders();

builder.Logging.AddOpenTelemetry(options => {
    options.ConfigureResource(configureResource)
        .AddOtlpExporter(otlpOptions => {
            otlpOptions.Endpoint = new Uri(builder.Configuration.GetValue<string>("Otlp:Endpoint"));
        })
        //.AddConsoleExporter()
        ;
});

builder.Services.Configure<OpenTelemetryLoggerOptions>(opt => {
    opt.IncludeScopes = true;
    opt.ParseStateValues = true;
    opt.IncludeFormattedMessage = true;
});

// Metrics
builder.Services.AddOpenTelemetryMetrics(options => {
    options.ConfigureResource(configureResource)
        .AddRuntimeInstrumentation()
        .AddHttpClientInstrumentation()
        .AddAspNetCoreInstrumentation()
        .AddMeter("ContactMetrics")
        .AddOtlpExporter(otlpOptions => {
            otlpOptions.Endpoint = new Uri(builder.Configuration.GetValue<string>("Otlp:Endpoint"));
        })
        //.AddConsoleExporter()
        ;
});

var app = builder.Build();

app.Logger.LogInformation("Otlp:Endpoint = {endpoint}", app.Configuration.GetValue<string>("Otlp:Endpoint"));

// Configure the HTTP request pipeline.
app.UseSwagger();
app.UseSwaggerUI();

// Create a route (GET /) that will make an http call, increment a metric and log a trace
var activitySource = new ActivitySource("WeCo.API.ActivitySource");
var meter = new Meter("ContactMetrics");
var requestsCounter = meter.CreateCounter<int>("requests");
var alivesCounter = meter.CreateCounter<int>("alives");
var deadsCounter = meter.CreateCounter<int>("deads");

app.MapGet("/contact", async (ILogger<Program> logger, IHttpClientFactory httpClientFactory) => {
    requestsCounter.Add(1);

    var isAliveHttpClient = httpClientFactory.CreateClient("IsAlive");
    var bureaucracyHttpClient = httpClientFactory.CreateClient("Bureaucracy");

    using (var activity = activitySource.StartActivity("Get Contact")) {
        var liveness = await isAliveHttpClient.GetFromJsonAsync<IsContactAlive>("/is-alive");

        activity?.AddTag("is-alive", liveness?.IsAlive ?? false);

        var locationsUrl = $"/locations?is-alive={liveness?.IsAlive == true}&fullName={HttpUtility.UrlEncode(liveness?.FullName)}";
        var datesUrl = $"/dates?is-alive={liveness?.IsAlive == true}&fullName={HttpUtility.UrlEncode(liveness?.FullName)}";

        var locationsTask = bureaucracyHttpClient.GetFromJsonAsync<ContactLocations>(locationsUrl);
        var datesTask = bureaucracyHttpClient.GetFromJsonAsync<ContactDates>(datesUrl);

        await Task.WhenAll(
            locationsTask,
            datesTask
        );

        if (locationsTask.IsFaulted || datesTask.IsFaulted)
            return Results.StatusCode(500);

        var locations = locationsTask.Result!;
        var dates = datesTask.Result!;

        var contact = new Contact {
            IsAlive = liveness?.IsAlive ?? false,
            FullName = liveness?.FullName ?? "John Doe",
            BirthDay = dates.BirthDay,
            BirthLocation = locations.BirthLocation,
            DeathDay = dates.DeathDay,
            DeathLocation = locations.DeathLocation
        };

        if (liveness?.IsAlive == true)
            alivesCounter.Add(1);
        else
            deadsCounter.Add(1);

        logger.LogInformation(
            "Here we go : {fullName} met life at {location}",
            contact.FullName,
            contact.BirthLocation);

        return Results.Json(contact);
    }
});

app.Run();
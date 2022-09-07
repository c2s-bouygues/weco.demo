using Bogus;
using OpenTelemetry.Instrumentation.AspNetCore;
using OpenTelemetry.Logs;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using System.Diagnostics;
using System.Diagnostics.Metrics;
using System.Reflection;
using WeCo.SensorsAPI.Models;
using Serilog;

var builder = WebApplication.CreateBuilder(args);
builder.WebHost.UseSerilog((context, conf) => conf.ReadFrom.Configuration(context.Configuration));

// Add services to the container.
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddHttpClient();
builder.Services.AddSingleton(sp => new Random());
builder.Services.AddSingleton(sp => new Faker("fr"));
builder.Services.AddHttpClient("Alerts", httpClient => {
    httpClient.BaseAddress = new Uri(builder.Configuration.GetValue<string>("Alerts:Endpoint"));
});

var assemblyVersion = Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? "unknown";

Action<ResourceBuilder> configureResource = r => r.AddService(
    "WeCo.SensorsAPI", serviceVersion: assemblyVersion, serviceInstanceId: Environment.MachineName);

if (builder.Environment.IsDevelopment())
    AppContext.SetSwitch("System.Net.Http.SocketsHttpHandler.Http2UnencryptedSupport", true);

builder.Services.AddOpenTelemetryTracing((options) => {
    options
        .ConfigureResource(configureResource)
        .SetSampler(new AlwaysOnSampler())
        .AddHttpClientInstrumentation()
        .AddAspNetCoreInstrumentation()
        .AddSource("WeCo.SensorsAPI.ActivitySource")
        .AddOtlpExporter(otlpOptions => {
            otlpOptions.Endpoint = new Uri(builder.Configuration.GetValue<string>("Otlp:Endpoint"));
        })
    ;
});
// For options which can be bound from IConfiguration.
builder.Services.Configure<AspNetCoreInstrumentationOptions>(builder.Configuration.GetSection("AspNetCoreInstrumentation"));

// Logging
//builder.Logging.ClearProviders();

builder.Logging.AddOpenTelemetry(options => {
    options.ConfigureResource(configureResource);

    options.AddOtlpExporter(otlpOptions => {
        otlpOptions.Endpoint = new Uri(builder.Configuration.GetValue<string>("Otlp:Endpoint"));
    });
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
        .AddMeter("LivenessMetrics")
        .AddOtlpExporter(otlpOptions => {
            otlpOptions.Endpoint = new Uri(builder.Configuration.GetValue<string>("Otlp:Endpoint"));
        })
    ;
});

var app = builder.Build();

app.Logger.LogInformation("Otlp:Endpoint = {endpoint}", app.Configuration.GetValue<string>("Otlp:Endpoint"));

// Configure the HTTP request pipeline.
app.UseSwagger();
app.UseSwaggerUI();

// Create a route (GET /) that will make an http call, increment a metric and log a trace
var activitySource = new ActivitySource("WeCo.SensorsAPI.ActivitySource");
var meter = new Meter("SensorsMetrics");
var requestsCounter = meter.CreateCounter<int>("requests");
var activesCounter = meter.CreateCounter<int>("actives");
var deadsCounter = meter.CreateCounter<int>("deads");

app.MapGet("/is-active", async (ILogger<Program> logger, IHttpClientFactory httpClientFactory, Random random, Faker faker) => {
    var bureaucracyHttpClient = httpClientFactory.CreateClient("Alerts");

    requestsCounter.Add(1);

    using (var activity = activitySource.StartActivity("Define Liveness")) {
        var isactive = random.Next(10) >= 5;
        activity?.AddTag("is-active", isactive);

        // Useless condition
        if (isactive)
            await bureaucracyHttpClient.GetAsync($"/brace-yourself?is-active={isactive}");

        var sensor = new Sensor {
            IsActive = isactive,
            SensorId = faker.Commerce.Ean8()
        };

        if (isactive == true)
            activesCounter.Add(1);
        else
            deadsCounter.Add(1);

        logger.LogInformation(
            "Courtesy from death herself : is {sensorId} active ? {isactive}",
            sensor.SensorId,
            sensor.IsActive ? "YES" : "No ..."
        );

        return Results.Json(sensor);
    }
});

app.Run();
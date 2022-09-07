using OpenTelemetry.Instrumentation.AspNetCore;
using OpenTelemetry.Logs;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using System.Diagnostics;
using System.Diagnostics.Metrics;
using System.Reflection;
using System.Web;
using WeCo.API.Noises.Models;
using Serilog;

var builder = WebApplication.CreateBuilder(args);
builder.WebHost.UseSerilog((context, conf) => conf.ReadFrom.Configuration(context.Configuration));

// Add services to the container.
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddHttpClient();
builder.Services.AddHttpClient("Sensors", httpClient => {
    httpClient.BaseAddress = new Uri(builder.Configuration.GetValue<string>("Sensors:Endpoint"));
});
builder.Services.AddHttpClient("Alerts", httpClient => {
    httpClient.BaseAddress = new Uri(builder.Configuration.GetValue<string>("Alerts:Endpoint"));
});

var assemblyVersion = Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? "unknown";

Action<ResourceBuilder> configureResource = r => r.AddService(
    "WeCo.API.Noises", serviceVersion: assemblyVersion, serviceInstanceId: Environment.MachineName);

AppContext.SetSwitch("System.Net.Http.SocketsHttpHandler.Http2UnencryptedSupport", true);

builder.Services.AddOpenTelemetryTracing((options) => {
    options
        .ConfigureResource(configureResource)
        .SetSampler(new AlwaysOnSampler())
        .AddHttpClientInstrumentation()
        .AddAspNetCoreInstrumentation()
        .AddSource("WeCo.API.Noises.ActivitySource")
        .AddOtlpExporter(otlpOptions => {
            otlpOptions.Endpoint = new Uri(builder.Configuration.GetValue<string>("Otlp:Endpoint"));
        })
        ;
});
// For options which can be bound from IConfiguration.
builder.Services.Configure<AspNetCoreInstrumentationOptions>(builder.Configuration.GetSection("AspNetCoreInstrumentation"));

// Logging
builder.Logging.AddOpenTelemetry(options => {
    options.ConfigureResource(configureResource)
        .AddOtlpExporter(otlpOptions => {
            otlpOptions.Endpoint = new Uri(builder.Configuration.GetValue<string>("Otlp:Endpoint"));
        })
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
        .AddMeter("NoisesMetrics")
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

app.UseSerilogRequestLogging();

// Create a route (GET /) that will make an http call, increment a metric and log a trace
var activitySource = new ActivitySource("WeCo.API.Noises.ActivitySource");
var meter = new Meter("NoisesMetrics");
var requestsCounter = meter.CreateCounter<int>("requests");
var activesCounter = meter.CreateCounter<int>("actives");
var deadsCounter = meter.CreateCounter<int>("deads");

app.MapGet("/", async (ILogger<Program> logger, IHttpClientFactory httpClientFactory) => {
    requestsCounter.Add(1);

    var sensorsHttpClient = httpClientFactory.CreateClient("Sensors");
    var alertsHttpClient = httpClientFactory.CreateClient("Alerts");

    using (var activity = activitySource.StartActivity("Get Noise Sensor")) {
        var sensor = await sensorsHttpClient.GetFromJsonAsync<Sensor>("/is-active");

        if (sensor == null)
            return Results.StatusCode(500);

        activity?.AddTag("is-active", sensor?.IsActive ?? false);

        var locationsUrl = $"/locations/last?is-active={sensor?.IsActive == true}&sensorId={HttpUtility.UrlEncode(sensor?.SensorId)}";
        var datesUrl = $"/alerts/last?is-active={sensor?.IsActive == true}&sensorId={HttpUtility.UrlEncode(sensor?.SensorId)}";

        var pingTask = alertsHttpClient.GetFromJsonAsync<Ping>(locationsUrl);
        var alertTask = alertsHttpClient.GetFromJsonAsync<Alert>(datesUrl);

        await Task.WhenAll(
            pingTask,
            alertTask
        );

        if (pingTask.IsFaulted || alertTask.IsFaulted)
            return Results.StatusCode(500);

        var ping = pingTask.Result!;
        var alert = alertTask.Result!;

        var noise = new NoiseSensor {
            IsActive = sensor!.IsActive,
            SensorId = sensor!.SensorId,
            LastPing = ping.TimeStamp,
            LastLocation = ping.Location,
            AlertTimestamp = alert.TimeStamp,
            AlertLocation = alert.Location
        };

        if (sensor?.IsActive == true)
            activesCounter.Add(1);
        else
            deadsCounter.Add(1);

        logger.LogInformation(
            "Here we go : {sensorId} was seen in {location} at {timestamp} for the last time",
            noise.SensorId,
            noise.LastLocation,
            noise.LastPing);

        return Results.Json(noise);
    }
});

app.Run();
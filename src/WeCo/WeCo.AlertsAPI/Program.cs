using Bogus;
using OpenTelemetry.Instrumentation.AspNetCore;
using OpenTelemetry.Logs;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using System.Diagnostics;
using System.Diagnostics.Metrics;
using System.Reflection;
using System.Web;
using WeCo.AlertsAPI.Models;
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

var assemblyVersion = Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? "unknown";

Action<ResourceBuilder> configureResource = r => r.AddService(
    "WeCo.AlertsAPI", serviceVersion: assemblyVersion, serviceInstanceId: Environment.MachineName);

if (builder.Environment.IsDevelopment())
    AppContext.SetSwitch("System.Net.Http.SocketsHttpHandler.Http2UnencryptedSupport", true);

builder.Services.AddOpenTelemetryTracing((options) => {
    options
        .ConfigureResource(configureResource)
        .SetSampler(new AlwaysOnSampler())
        .AddHttpClientInstrumentation()
        .AddAspNetCoreInstrumentation()
        .AddSource("WeCo.AlertsAPI.ActivitySource")
        .AddOtlpExporter(otlpOptions => {
            otlpOptions.Endpoint = new Uri(builder.Configuration.GetValue<string>("Otlp:Endpoint"));
        })
        ;
});
// For options which can be bound from IConfiguration.
builder.Services.Configure<AspNetCoreInstrumentationOptions>(builder.Configuration.GetSection("AspNetCoreInstrumentation"));

// Logging
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
        .AddMeter("AlertsMetrics")
        .AddOtlpExporter(otlpOptions => {
            otlpOptions.Endpoint = new Uri(builder.Configuration.GetValue<string>("Otlp:Endpoint"));
        })
        ;
});

var app = builder.Build();

app.Logger.LogInformation("Otlp:Endpoint = {endpoint}", app.Configuration.GetValue<string>("Otlp:Endpoint"));

app.UseSwagger();
app.UseSwaggerUI();

// Create a route (GET /) that will make an http call, increment a metric and log a trace
var activitySource = new ActivitySource("WeCo.AlertsAPI.ActivitySource");
var meter = new Meter("AlertsMetrics");
var requestsCounter = meter.CreateCounter<int>("requests");
var bracesCounter = meter.CreateCounter<int>("braces");
var pingCounter = meter.CreateCounter<int>("pings");
var alertsCounter = meter.CreateCounter<int>("alerts");

app.MapGet("/brace-yourself", (ILogger<Program> logger, HttpRequest request) => {
    requestsCounter.Add(1);

    using (var activity = activitySource.StartActivity("Brace What?")) {
        var isActiveQueryString = request.Query["is-active"];
        if (!Boolean.TryParse(isActiveQueryString, out var isActive))
            throw new InvalidDataException();

        bracesCounter.Add(1);
        activity?.AddTag("is-active", isActive);

        logger.LogInformation("I'm braced");

        return Results.Ok();
    }
});
app.MapGet("/locations/last", (ILogger<Program> logger, HttpRequest request, Random random, Faker faker) => {
    requestsCounter.Add(1);

    using (var activity = activitySource.StartActivity("Get sensor last ping")) {
        var isActiveQueryString = request.Query["is-active"];
        if (!Boolean.TryParse(isActiveQueryString, out var isActive))
            throw new InvalidDataException();
        var sensorId = HttpUtility.UrlDecode(request.Query["sensorId"]);

        pingCounter.Add(1);
        activity?.AddTag("is-active", isActive);

        var ping = new Ping {
            Location = faker.Address.City(),
            TimeStamp = isActive
                ? DateTime.UtcNow.AddMinutes(random.Next(0, 15))
                : DateTime.UtcNow.AddDays(random.Next(10, 25))
        };

        logger.LogInformation(
            "Last ping of sensor '{sensorId}' was '{alert_date}' at {location}",
            sensorId,
            ping.TimeStamp,
            ping.Location);

        return Results.Json(ping);
    }
});
app.MapGet("/alerts/last", (ILogger<Program> logger, HttpRequest request, Random random, Faker faker) => {
    requestsCounter.Add(1);

    using (var activity = activitySource.StartActivity("Get sensor last alert")) {
        var isActiveQueryString = request.Query["is-active"];
        if (!Boolean.TryParse(isActiveQueryString, out var isActive))
            throw new InvalidDataException();
        var sensorId = HttpUtility.UrlDecode(request.Query["sensorId"]);

        alertsCounter.Add(1);
        activity?.AddTag("is-active", isActive);

        var hasAlert = random.Next(10) >= 5;
        activity?.AddTag("has-alert", hasAlert);

        if (!hasAlert) {
            logger.LogInformation("No alerts known for {sensorId}", sensorId);
            return Results.Json(new Alert());
        } else {
            var alert = new Alert {
                Location = faker.Address.City(),
                TimeStamp = isActive
                    ? DateTime.UtcNow.AddMinutes(random.Next(0, 15))
                    : DateTime.UtcNow.AddDays(random.Next(10, 25))
            };

            logger.LogInformation(
                "Last alert of '{sensorId}' was '{alert_date}' at {location}",
                sensorId,
                alert.TimeStamp,
                alert.Location);

            return Results.Json(alert);
        }
    }
});

app.Run();
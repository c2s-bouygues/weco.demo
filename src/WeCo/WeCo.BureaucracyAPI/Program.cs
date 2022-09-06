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
using WeCo.BureaucracyAPI.Models;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddHttpClient();
builder.Services.AddSingleton(sp => new Random());
builder.Services.AddSingleton(sp => new Faker("fr"));

var assemblyVersion = Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? "unknown";

Action<ResourceBuilder> configureResource = r => r.AddService(
    "WeCo.BureaucracyAPI", serviceVersion: assemblyVersion, serviceInstanceId: Environment.MachineName);

if (builder.Environment.IsDevelopment())
    AppContext.SetSwitch("System.Net.Http.SocketsHttpHandler.Http2UnencryptedSupport", true);

builder.Services.AddOpenTelemetryTracing((options) => {
    options
        .ConfigureResource(configureResource)
        .SetSampler(new AlwaysOnSampler())
        .AddHttpClientInstrumentation()
        .AddAspNetCoreInstrumentation()
        .AddSource("WeCo.BureaucracyAPI.ActivitySource")
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
    options.ConfigureResource(configureResource);

    options.AddOtlpExporter(otlpOptions => {
        otlpOptions.Endpoint = new Uri(builder.Configuration.GetValue<string>("Otlp:Endpoint"));
    });
    //options.AddConsoleExporter();
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
        .AddMeter("BureaucracyMetrics")
        .AddOtlpExporter(otlpOptions => {
            otlpOptions.Endpoint = new Uri(builder.Configuration.GetValue<string>("Otlp:Endpoint"));
        })
        //.AddConsoleExporter()
        ;
});

var app = builder.Build();

app.Logger.LogInformation("Otlp:Endpoint = {endpoint}", app.Configuration.GetValue<string>("Otlp:Endpoint"));

app.UseSwagger();
app.UseSwaggerUI();

// Create a route (GET /) that will make an http call, increment a metric and log a trace
var activitySource = new ActivitySource("WeCo.BureaucracyAPI.ActivitySource");
var meter = new Meter("BureaucracyMetrics");
var requestsCounter = meter.CreateCounter<int>("requests");
var bracesCounter = meter.CreateCounter<int>("braces");
var deadsCounter = meter.CreateCounter<int>("deads");
var locationsCounter = meter.CreateCounter<int>("locations");
var datesCounter = meter.CreateCounter<int>("dates");

app.MapGet("/brace-yourself", (ILogger<Program> logger, HttpRequest request) => {
    requestsCounter.Add(1);

    using (var activity = activitySource.StartActivity("Brace What?")) {
        var isAliveQueryString = request.Query["is-alive"];
        if (!Boolean.TryParse(isAliveQueryString, out var isAlive))
            throw new InvalidDataException();

        bracesCounter.Add(1);
        activity?.AddTag("is-alive", isAlive);

        logger.LogInformation("I'm braced");

        return Results.Ok();
    }
});
app.MapGet("/locations", (ILogger<Program> logger, HttpRequest request, Faker faker) => {
    requestsCounter.Add(1);

    using (var activity = activitySource.StartActivity("Get contact locations")) {
        var isAliveQueryString = request.Query["is-alive"];
        if (!Boolean.TryParse(isAliveQueryString, out var isAlive))
            throw new InvalidDataException();
        var fullName = HttpUtility.UrlDecode(request.Query["fullName"]);

        locationsCounter.Add(1);
        activity?.AddTag("is-alive", isAlive);

        var contact = new ContactLocations {
            BirthLocation = faker.Address.Country(),
            DeathLocation = isAlive ? faker.Address.Country() : string.Empty
        };

        logger.LogInformation(
            "Birth registry : {fullName} birthed in {location}",
            fullName,
            contact.BirthLocation);

        if (!isAlive) {
            logger.LogInformation(
                "Death registry : {fullName} died in {location}",
                fullName,
                contact.DeathLocation);
            deadsCounter.Add(1);
        }

        return Results.Json(contact);
    }
});
app.MapGet("/dates", (ILogger<Program> logger, HttpRequest request, Random random, Faker faker) => {
    requestsCounter.Add(1);

    using (var activity = activitySource.StartActivity("Get contact dates")) {
        var isAliveQueryString = request.Query["is-alive"];
        if (!Boolean.TryParse(isAliveQueryString, out var isAlive))
            throw new InvalidDataException();
        var fullName = HttpUtility.UrlDecode(request.Query["fullName"]);

        datesCounter.Add(1);
        activity?.AddTag("is-alive", isAlive);

        var maxAge = random.Next(30, 65);
        var contact = new ContactDates {
            BirthDay = faker.Date.Past(maxAge),
            DeathDay = isAlive ? faker.Date.Past(20) : null
        };

        logger.LogInformation(
            "Birth registry : {fullName} birthed at {date}",
            fullName,
            contact.BirthDay);

        if (!isAlive) {
            logger.LogInformation(
                "Death registry : {fullName} died at {location}",
                fullName,
                contact.DeathDay);
            deadsCounter.Add(1);
        }

        return Results.Json(contact);
    }
});

app.Run();
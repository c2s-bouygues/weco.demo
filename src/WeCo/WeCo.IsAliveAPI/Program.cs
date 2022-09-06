using Bogus;
using OpenTelemetry.Instrumentation.AspNetCore;
using OpenTelemetry.Logs;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using System.Diagnostics;
using System.Diagnostics.Metrics;
using System.Reflection;
using WeCo.IsAliveAPI.Models;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddHttpClient();
builder.Services.AddSingleton(sp => new Random());
builder.Services.AddSingleton(sp => new Faker("fr"));
builder.Services.AddHttpClient("Bureaucracy", httpClient => {
    httpClient.BaseAddress = new Uri(builder.Configuration.GetValue<string>("Bureaucracy:Endpoint"));
});

var assemblyVersion = Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? "unknown";

Action<ResourceBuilder> configureResource = r => r.AddService(
    "WeCo.IsAliveAPI", serviceVersion: assemblyVersion, serviceInstanceId: Environment.MachineName);

if (builder.Environment.IsDevelopment())
    AppContext.SetSwitch("System.Net.Http.SocketsHttpHandler.Http2UnencryptedSupport", true);

builder.Services.AddOpenTelemetryTracing((options) => {
    options
        .ConfigureResource(configureResource)
        .SetSampler(new AlwaysOnSampler())
        .AddHttpClientInstrumentation()
        .AddAspNetCoreInstrumentation()
        .AddSource("WeCo.IsAliveAPI.ActivitySource")
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
        .AddMeter("LivenessMetrics")
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
var activitySource = new ActivitySource("WeCo.IsAliveAPI.ActivitySource");
var meter = new Meter("LivenessMetrics");
var requestsCounter = meter.CreateCounter<int>("requests");
var alivesCounter = meter.CreateCounter<int>("alives");
var deadsCounter = meter.CreateCounter<int>("deads");

app.MapGet("/is-alive", async (ILogger<Program> logger, IHttpClientFactory httpClientFactory, Random random, Faker faker) => {
    var bureaucracyHttpClient = httpClientFactory.CreateClient("Bureaucreacy");

    requestsCounter.Add(1);

    using (var activity = activitySource.StartActivity("Define Liveness")) {
        var isAlive = random.Next(10) >= 5;
        activity?.AddTag("is-alive", isAlive);

        // Useless condition
        if (isAlive)
            await bureaucracyHttpClient.GetAsync($"/brace-yourself?is-alive={isAlive}");

        var contact = new IsContactAlive {
            IsAlive = isAlive,
            FullName = faker.Name.FullName()
        };

        if (isAlive == true)
            alivesCounter.Add(1);
        else
            deadsCounter.Add(1);

        logger.LogInformation(
            "Courtesy from death herself : is {fullName} alive ? {isAlive}",
            contact.FullName,
            contact.IsAlive);

        return Results.Json(contact);
    }
});

app.Run();
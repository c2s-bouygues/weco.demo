using InfluxDB.Client;
using Microsoft.Extensions.Options;
using WeCo.ETL.TemperaturesToInfluxDB;
using WeCo.Ingesters.Shared;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSingleton(sp => {
    var influx = sp.GetRequiredService<IOptions<InfluxOptions>>().Value;
    builder.Configuration.GetSection("InfluxDB").Bind(influx);

    return InfluxDBClientFactory.Create(
        influx.Url,
        influx.Token.ToCharArray()
    );
});
builder.Services.AddOptions<InfluxOptions>()
    .Configure<IConfiguration>((option, configuration) => {
        configuration.GetSection("InfluxDB").Bind(option);
    });
builder.Services.AddPulsar().AddProducer();
builder.Services.AddHostedService<PulsarListener>();

var app = builder.Build();

app.MapGet("/", () => "Hello World!");

app.Run();
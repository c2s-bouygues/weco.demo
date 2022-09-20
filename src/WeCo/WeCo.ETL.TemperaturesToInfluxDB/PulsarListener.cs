using DotPulsar;
using DotPulsar.Abstractions;
using DotPulsar.Extensions;
using InfluxDB.Client;
using InfluxDB.Client.Api.Domain;
using InfluxDB.Client.Writes;
using Microsoft.Extensions.Options;
using System.Text.Json;
using Task = System.Threading.Tasks.Task;

namespace WeCo.ETL.TemperaturesToInfluxDB;

public class PulsarListener
    : IHostedService {
    private readonly ILogger _logger;
    private readonly CancellationTokenSource _tokenSource;
    private readonly IPulsarClient _pulsarClient;
    private readonly InfluxDBClient _influxDBClient;
    private readonly IConsumer<string> _azoteConsumer;
    private readonly IConsumer<string> _oxygenConsumer;
    private readonly IConsumer<string> _co2Consumer;
    private readonly IOptions<InfluxOptions> _influxOptions;
    private Task _azoteTask;
    private Task _oxygenTask;
    private Task _co2Task;

    public PulsarListener(
        ILoggerFactory loggerFactory,
        InfluxDBClient influxDBClient,
        IPulsarClient pulsarClient,
        IOptions<InfluxOptions> influxOptions) {
        _logger = loggerFactory.CreateLogger<PulsarListener>();
        _tokenSource = new CancellationTokenSource();
        _pulsarClient = pulsarClient;
        _influxDBClient = influxDBClient;
        _influxOptions = influxOptions;

        _azoteConsumer = _pulsarClient.NewConsumer(Schema.String)
            .StateChangedHandler(Monitor)
            .SubscriptionName("GasInfluxWriter")
            .Topic("persistent://iot/gas/azote")
            .SubscriptionType(SubscriptionType.Shared)
            .Create();
        _oxygenConsumer = _pulsarClient.NewConsumer(Schema.String)
            .StateChangedHandler(Monitor)
            .SubscriptionName("GasInfluxWriter")
            .Topic("persistent://iot/gas/oxygen")
            .SubscriptionType(SubscriptionType.Shared)
            .Create();
        _co2Consumer = _pulsarClient.NewConsumer(Schema.String)
            .StateChangedHandler(Monitor)
            .SubscriptionName("GasInfluxWriter")
            .Topic("persistent://iot/gas/co2")
            .SubscriptionType(SubscriptionType.Shared)
            .Create();
    }

    private void Monitor(ConsumerStateChanged stateChanged) {
        var topic = stateChanged.Consumer.Topic;
        var state = stateChanged.ConsumerState;
        Console.WriteLine($"The consumer for topic '{topic}' changed state to '{state}'");
    }

    public Task StartAsync(CancellationToken cancellationToken) {
        try {
            _azoteTask = Task.Run(async () => await this.AzoteConsumerAsync(_tokenSource.Token));
            _oxygenTask = Task.Run(async () => await this.OxygenConsumerAsync(_tokenSource.Token));
            _co2Task = Task.Run(async () => await this.CO2ConsumerAsync(_tokenSource.Token));
        } catch (OperationCanceledException) { }

        return Task.CompletedTask;
    }

    public async Task AzoteConsumerAsync(CancellationToken cancellationToken) {
        try {
            await foreach (var message in _azoteConsumer.Messages(cancellationToken)) {
                var rawJson = message.Value();
                _logger.LogInformation("[{gasType}] Received from {messageId}-{sequenceId}: {rawJson}", "azote", message.MessageId, message.SequenceId, rawJson);
                var measure = JsonSerializer.Deserialize<GasMeasure>(rawJson);
                if (measure != null && string.Compare(measure.Type, "azote", true) == 0) {
                    var point = Point
                        .Measurement("azote")
                        .Field(measure.Unit, measure.Value)
                        .Tag(nameof(GasMeasure.DeviceId), measure.DeviceId)
                        .Tag(nameof(GasMeasure.DeviceName), measure.DeviceName)
                        .Tag(nameof(GasMeasure.ExternalId), measure.ExternalId)
                        .Timestamp(measure.Timestamp, WritePrecision.Ms);

                    using (var writeApi = _influxDBClient.GetWriteApi()) {
                        writeApi.WritePoint("azote", _influxOptions.Value.Org, point);
                        writeApi.Flush();
                    }
                }
                await _azoteConsumer.Acknowledge(message, cancellationToken);
            }
        } catch (OperationCanceledException) { }
    }

    public async Task OxygenConsumerAsync(CancellationToken cancellationToken) {
        try {
            await foreach (var message in _oxygenConsumer.Messages(cancellationToken)) {
                var rawJson = message.Value();
                _logger.LogInformation("[{gasType}] Received from {messageId}-{sequenceId}: {rawJson}", "oxygen", message.MessageId, message.SequenceId, rawJson);
                var measure = JsonSerializer.Deserialize<GasMeasure>(rawJson);
                if (measure != null && string.Compare(measure.Type, "oxygen", true) == 0) {
                    var point = Point
                        .Measurement("oxygen")
                        .Field(measure.Unit, measure.Value)
                        .Tag(nameof(GasMeasure.DeviceId), measure.DeviceId)
                        .Tag(nameof(GasMeasure.DeviceName), measure.DeviceName)
                        .Tag(nameof(GasMeasure.ExternalId), measure.ExternalId)
                        .Timestamp(measure.Timestamp, WritePrecision.Ms);

                    using (var writeApi = _influxDBClient.GetWriteApi()) {
                        writeApi.WritePoint("oxygen", _influxOptions.Value.Org, point);
                        writeApi.Flush();
                    }
                }
                await _oxygenConsumer.Acknowledge(message, cancellationToken);
            }
        } catch (OperationCanceledException) { }
    }

    public async Task CO2ConsumerAsync(CancellationToken cancellationToken) {
        try {
            await foreach (var message in _co2Consumer.Messages(cancellationToken)) {
                var rawJson = message.Value();
                _logger.LogInformation("[{gasType}] Received from {messageId}-{sequenceId}: {rawJson}", "co2", message.MessageId, message.SequenceId, rawJson);
                var measure = JsonSerializer.Deserialize<GasMeasure>(rawJson);
                if (measure != null && string.Compare(measure.Type, "co2", true) == 0) {
                    var point = Point
                        .Measurement("co2")
                        .Field(measure.Unit, measure.Value)
                        .Tag(nameof(GasMeasure.DeviceId), measure.DeviceId)
                        .Tag(nameof(GasMeasure.DeviceName), measure.DeviceName)
                        .Tag(nameof(GasMeasure.ExternalId), measure.ExternalId)
                        .Timestamp(measure.Timestamp, WritePrecision.Ms);

                    using (var writeApi = _influxDBClient.GetWriteApi()) {
                        writeApi.WritePoint("co2", _influxOptions.Value.Org, point);
                        writeApi.Flush();
                    }
                }
                await _co2Consumer.Acknowledge(message, cancellationToken);
            }
        } catch (OperationCanceledException) { }
    }

    public async Task StopAsync(CancellationToken cancellationToken) {
        _tokenSource.Cancel();
        await Task.WhenAll(_azoteTask, _oxygenTask, _co2Task);
    }
}
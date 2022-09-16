using DotPulsar;
using DotPulsar.Abstractions;
using DotPulsar.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using WeCo.Ingesters.Shared.Options;

namespace WeCo.Ingesters.Shared.Producers;

public interface IProducerFactory {

    IProducer<string> GetProducer(string topicName, string @namespace = null, bool isPersistent = true);
}

public class ProducerFactory : IProducerFactory {
    private readonly ILogger _logger;
    private readonly IPulsarClient _pulsarClient;
    private readonly IOptions<PulsarOptions> _options;
    private Dictionary<string, IProducer<string>> producers = new();

    public ProducerFactory(ILogger<ProducerFactory> logger, IPulsarClient pulsarClient, IOptions<PulsarOptions> options) {
        _logger = logger;
        _pulsarClient = pulsarClient;
        _options = options;
    }

    private void Monitor(ProducerStateChanged stateChanged, CancellationToken _) {
        var stateMessage = stateChanged.ProducerState switch {
            ProducerState.Connected => "is connected",
            ProducerState.Disconnected => "is disconnected",
            ProducerState.PartiallyConnected => "is partially connected",
            ProducerState.Closed => "has closed",
            ProducerState.Faulted => "has faulted",
            _ => $"has an unknown state '{stateChanged.ProducerState}'"
        };

        var topic = stateChanged.Producer.Topic;
        _logger.LogInformation($"The producer for topic '{topic}' {stateMessage}");
    }

    IProducer<string> IProducerFactory.GetProducer(string topicName, string @namespace, bool isPersistent = true) {
        if (!producers.ContainsKey(topicName))
            producers.Add(topicName, _pulsarClient.NewProducer(Schema.String)
                .Topic($"{(isPersistent ? "persistent" : "non-persistent")}://{_options.Value.Tenant}/{@namespace}/{topicName}")
                .StateChangedHandler(this.Monitor)
                .Create()
            );
        return producers[topicName];
    }
}
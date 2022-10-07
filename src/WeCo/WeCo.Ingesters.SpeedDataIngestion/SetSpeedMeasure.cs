using DotPulsar.Abstractions;
using DotPulsar.Extensions;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Attributes;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Enums;
using Microsoft.Extensions.Logging;
using System.Net;
using System.Text;
using System.Text.Json;
using WeCo.Ingesters.Shared.Producers;
using WeCo.Ingesters.SpeedDataIngestion.Messages;
using WeCo.Ingesters.SpeedDataIngestion.Models;

namespace WeCo.Ingesters.SpeedDataIngestion {

    public class GetSpeedMeasure {
        private readonly ILogger _logger;
        private readonly IProducer<string> _rawProducer;
        private readonly IProducer<string> _velocityProducer;
        private readonly IProducer<string> _temperaturesProducer;

        public GetSpeedMeasure(ILoggerFactory loggerFactory, IProducerFactory producerFactory) {
            _logger = loggerFactory.CreateLogger<GetSpeedMeasure>();

            _rawProducer = producerFactory.GetProducer("planes", "raw");
            _velocityProducer = producerFactory.GetProducer("velocity", "planes");
            _temperaturesProducer = producerFactory.GetProducer("temperatures", "planes");
        }

        [Function("SetSpeedMeasure")]
        [OpenApiOperation(operationId: "SetSpeedMeasure", tags: new[] { "planes" }, Summary = "Télémétries des avions", Description = "Enregistrement des données de télémétries des avions", Visibility = OpenApiVisibilityType.Important)]
        [OpenApiRequestBody(contentType: "application/json", bodyType: typeof(Speed), Required = true, Description = "Données de télémétries des avions")]
        [OpenApiResponseWithBody(statusCode: HttpStatusCode.OK, contentType: "application/text", bodyType: typeof(string))]
        public async Task<HttpResponseData> Run([HttpTrigger(AuthorizationLevel.Function, "post")] HttpRequestData req) {
            var raw = await req.ReadAsStringAsync(Encoding.UTF8);
            //_logger.LogInformation("Raw JSON Received : " + raw);

            var speed = JsonSerializer.Deserialize<Speed>(raw);

            _logger.LogDebug("Send raw message");
            await _rawProducer.Send(JsonSerializer.Serialize(speed));

            _logger.LogDebug("Send Velocity");
            await _velocityProducer.Send(JsonSerializer.Serialize(new VelocityMessage(speed)));

            _logger.LogDebug("Send Temperatures");
            await _temperaturesProducer.Send(JsonSerializer.Serialize(new TemperatureMessage(speed)));

            _logger.LogInformation("Measures dispatched.");

            var response = req.CreateResponse(HttpStatusCode.OK);
            response.Headers.Add("Content-Type", "text/plain; charset=utf-8");
            response.WriteString($"Measure from device '{speed.DeviceId}' has been dispatched");

            return response;
        }
    }
}
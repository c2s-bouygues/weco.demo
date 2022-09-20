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
using WeCo.Ingesters.GasDataIngestion.Messages;
using WeCo.Ingesters.GasDataIngestion.Model;
using WeCo.Ingesters.Shared.Producers;

namespace WeCo.Ingesters.GasDataIngestion {

    public class GetGasMeasure {
        private readonly ILogger _logger;
        private readonly IProducer<string> _rawProducer;
        private readonly IProducer<string> _co2Producer;
        private readonly IProducer<string> _oxygenProducer;
        private readonly IProducer<string> _azoteProducer;

        public GetGasMeasure(ILoggerFactory loggerFactory, IProducerFactory producerFactory) {
            _logger = loggerFactory.CreateLogger<GetGasMeasure>();

            _rawProducer = producerFactory.GetProducer("gas", "raw");
            _co2Producer = producerFactory.GetProducer("co2", "gas");
            _oxygenProducer = producerFactory.GetProducer("oxygen", "gas");
            _azoteProducer = producerFactory.GetProducer("azote", "gas");
        }

        [Function("SetGasMeasure")]
        [OpenApiOperation(operationId: "SetGasMeasure", tags: new[] { "gas" }, Summary = "Télémétries de qualité de l'air", Description = "Enregistrement des données de télémétries de qualité de l'air", Visibility = OpenApiVisibilityType.Important)]
        [OpenApiRequestBody(contentType: "application/json", bodyType: typeof(Gas), Required = true, Description = "Données de télémétries de qualité de l'air")]
        [OpenApiResponseWithBody(statusCode: HttpStatusCode.OK, contentType: "application/text", bodyType: typeof(string))]
        public async Task<HttpResponseData> Run([HttpTrigger(AuthorizationLevel.Function, "post")] HttpRequestData req) {
            var raw = await req.ReadAsStringAsync(Encoding.UTF8);
            _logger.LogInformation("Raw JSON Received : " + raw);

            var gas = JsonSerializer.Deserialize<Gas>(raw);

            _logger.LogDebug("Send raw message", );
            await _rawProducer.Send(JsonSerializer.Serialize(gas));

            var co2 = gas.Measures.Where(m => string.Compare(m.Type, "co2", true) == 0).FirstOrDefault();
            if (co2 != null) {
                _logger.LogDebug("Send CO2");
                await _co2Producer.Send(JsonSerializer.Serialize(new MeasureMessage(gas, co2)));
            }

            var oxygen = gas.Measures.Where(m => string.Compare(m.Type, "oxygen", true) == 0).FirstOrDefault();
            if (oxygen != null) {
                _logger.LogDebug("Send Oxygen");
                await _oxygenProducer.Send(JsonSerializer.Serialize(new MeasureMessage(gas, oxygen)));
            }

            var azote = gas.Measures.Where(m => string.Compare(m.Type, "azote", true) == 0).FirstOrDefault();
            if (azote != null) {
                _logger.LogDebug("Send Azote");
                await _azoteProducer.Send(JsonSerializer.Serialize(new MeasureMessage(gas, azote)));
            }

            _logger.LogInformation("Measures dispatched.");

            var response = req.CreateResponse(HttpStatusCode.OK);
            response.Headers.Add("Content-Type", "text/plain; charset=utf-8");
            response.WriteString($"Measure from device '{gas.DeviceId}' has been dispatched");

            return response;
        }
    }
}
using DotPulsar;
using Microsoft.Azure.Functions.Worker.Extensions.OpenApi.Extensions;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Abstractions;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Configurations;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.OpenApi.Models;
using WeCo.Ingesters.Shared.Options;
using WeCo.Ingesters.Shared.Producers;

var host = new HostBuilder()
    .ConfigureFunctionsWorkerDefaults()
    .ConfigureOpenApi()
    .ConfigureLogging(builder => {
        builder.AddApplicationInsights();
    })
    .ConfigureServices(services => {
        services.AddSingleton<IOpenApiConfigurationOptions>(_ => {
            var options = new OpenApiConfigurationOptions() {
                Info = new OpenApiInfo() {
                    Version = "1.0",
                    Title = $"Gas Data Ingester",
                    Description = "API for gas measurement data ingestion",
                    TermsOfService = new Uri("https://c2s-bouygues.fr"),
                    Contact = new OpenApiContact() {
                        Name = "Emilien GUIMINEAU",
                        Email = "eguilmineau@c2s.fr",
                        Url = new Uri("https://c2s-bouygues.fr"),
                    },
                    License = new OpenApiLicense() {
                        Name = "MIT",
                        Url = new Uri("http://opensource.org/licenses/MIT"),
                    }
                },
                Servers = DefaultOpenApiConfigurationOptions.GetHostNames(),
                OpenApiVersion = DefaultOpenApiConfigurationOptions.GetOpenApiVersion(),
                IncludeRequestingHostName = DefaultOpenApiConfigurationOptions.IsFunctionsRuntimeEnvironmentDevelopment(),
                ForceHttps = DefaultOpenApiConfigurationOptions.IsHttpsForced(),
                ForceHttp = DefaultOpenApiConfigurationOptions.IsHttpForced(),
            };

            return options;
        })
            .AddSingleton<IOpenApiHttpTriggerAuthorization, DefaultOpenApiHttpTriggerAuthorization>()
            .AddSingleton<IOpenApiCustomUIOptions, DefaultOpenApiCustomUIOptions>()
            ;

        services.AddOptions<PulsarOptions>()
            .Configure<IConfiguration>((option, configuration) => { configuration.GetSection("Pulsar").Bind(option); });

        services.AddSingleton(sp => {
            var options = sp.GetRequiredService<IOptions<PulsarOptions>>().Value;

            var builder = PulsarClient.Builder()
                .ServiceUrl(new Uri(options.ServiceUrl));

            if (!string.IsNullOrEmpty(options.Token))
                builder.Authentication(AuthenticationFactory.Token(options.Token));

            return builder.Build();
        });

        services.AddSingleton<IProducerFactory, ProducerFactory>();
    })
    .Build();

host.Run();
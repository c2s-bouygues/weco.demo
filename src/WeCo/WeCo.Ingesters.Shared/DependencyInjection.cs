using DotPulsar;
using DotPulsar.Abstractions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using WeCo.Ingesters.Shared.Options;
using WeCo.Ingesters.Shared.Producers;

namespace WeCo.Ingesters.Shared;

public static class DependencyInjection {

    public static IPulsarServiceCollection AddPulsar(this IServiceCollection services) {
        services.AddOptions<PulsarOptions>()
            .Configure<IConfiguration>((option, configuration) => {
                configuration.GetSection("Pulsar").Bind(option);
            });

        services.AddSingleton(sp => {
            var options = sp.GetRequiredService<IOptions<PulsarOptions>>().Value;

            var builder = PulsarClient.Builder()
                .ServiceUrl(new Uri(options.ServiceUrl));

            builder.ExceptionHandler(new ConsoleExceptionHandler());

            if (!string.IsNullOrEmpty(options.Token))
                builder.Authentication(AuthenticationFactory.Token(options.Token));

            return builder.Build();
        });

        return new PulsarServiceCollection(services);
    }

    public static IPulsarServiceCollection AddProducer(this IPulsarServiceCollection pulsar) {
        pulsar.Services.AddSingleton<IProducerFactory, ProducerFactory>();
        return pulsar;
    }
}

internal class ConsoleExceptionHandler : IHandleException {

    public ValueTask OnException(ExceptionContext exceptionContext) {
        Console.WriteLine(exceptionContext.Exception.Message);
        return new ValueTask();
    }
}

public interface IPulsarServiceCollection {
    IServiceCollection Services { get; }
}

public class PulsarServiceCollection
    : IPulsarServiceCollection {

    public PulsarServiceCollection(IServiceCollection services) {
        Services = services;
    }

    public IServiceCollection Services { get; }
}
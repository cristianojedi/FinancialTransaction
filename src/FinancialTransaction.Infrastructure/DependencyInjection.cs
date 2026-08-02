using Confluent.Kafka;
using FinancialTransaction.Application.Abstractions.Messaging;
using FinancialTransaction.Application.Abstractions.Persistence;
using FinancialTransaction.Infrastructure.Messaging;
using FinancialTransaction.Infrastructure.Persistence;
using FinancialTransaction.Infrastructure.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace FinancialTransaction.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("PostgreSql")
            ?? throw new InvalidOperationException("A connection string 'PostgreSql' não foi configurada.");

        services.AddDbContext<FinancialTransactionDbContext>(options =>
            options.UseNpgsql(connectionString));

        services.AddScoped<IAccountRepository, AccountRepository>();
        services.AddScoped<IFinancialTransactionRepository, FinancialTransactionRepository>();
        services.AddScoped<IUnitOfWork, UnitOfWork>();

        services.Configure<KafkaOptions>(configuration.GetSection(KafkaOptions.SectionName));

        services.AddSingleton<IProducer<string, string>>(sp =>
        {
            var options = sp.GetRequiredService<IOptions<KafkaOptions>>().Value;

            if (string.IsNullOrWhiteSpace(options.BootstrapServers))
            {
                throw new InvalidOperationException("A configuração 'Kafka:BootstrapServers' não foi definida.");
            }

            var producerConfig = new ProducerConfig
            {
                BootstrapServers = options.BootstrapServers,
                Acks = Acks.All,
                EnableIdempotence = true,
                MessageTimeoutMs = 5000,
            };

            return new ProducerBuilder<string, string>(producerConfig).Build();
        });

        services.AddSingleton<IEventPublisher, KafkaEventPublisher>();

        services.AddSingleton<IConsumer<string, string>>(sp =>
        {
            var options = sp.GetRequiredService<IOptions<KafkaOptions>>().Value;

            if (string.IsNullOrWhiteSpace(options.BootstrapServers))
            {
                throw new InvalidOperationException("A configuração 'Kafka:BootstrapServers' não foi definida.");
            }

            if (string.IsNullOrWhiteSpace(options.ConsumerGroupId))
            {
                throw new InvalidOperationException("A configuração 'Kafka:ConsumerGroupId' não foi definida.");
            }

            var consumerConfig = new ConsumerConfig
            {
                BootstrapServers = options.BootstrapServers,
                GroupId = options.ConsumerGroupId,
                AutoOffsetReset = AutoOffsetReset.Earliest,
                EnableAutoCommit = false,
            };

            return new ConsumerBuilder<string, string>(consumerConfig).Build();
        });

        return services;
    }
}

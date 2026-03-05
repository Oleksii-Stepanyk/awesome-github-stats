using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace AwesomeGithubStats.Api.Services;

public class SelfPingService : BackgroundService
{
    private static readonly TimeSpan PingInterval = TimeSpan.FromMinutes(5);

    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<SelfPingService> _logger;
    private readonly string? _selfUrl;

    public SelfPingService(IHttpClientFactory httpClientFactory, ILogger<SelfPingService> logger)
    {
        _httpClientFactory = httpClientFactory;
        _logger = logger;

        // Render sets RENDER_EXTERNAL_URL automatically; fall back to a manually configured value.
        var externalUrl = Environment.GetEnvironmentVariable("RENDER_EXTERNAL_URL")
                          ?? Environment.GetEnvironmentVariable("SELF_PING_URL");

        _selfUrl = string.IsNullOrWhiteSpace(externalUrl) ? null : externalUrl.TrimEnd('/') + "/health";
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (_selfUrl is null)
        {
            _logger.LogInformation("SelfPingService: no target URL configured, service is inactive.");
            return;
        }

        _logger.LogInformation("SelfPingService started – pinging {Url} every {Interval} minutes.", _selfUrl, PingInterval.TotalMinutes);

        using var timer = new PeriodicTimer(PingInterval);

        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            try
            {
                var client = _httpClientFactory.CreateClient(nameof(SelfPingService));
                var response = await client.GetAsync(_selfUrl, stoppingToken);
                _logger.LogInformation("SelfPingService: ping returned {StatusCode}.", (int)response.StatusCode);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogWarning(ex, "SelfPingService: ping to {Url} failed.", _selfUrl);
            }
        }
    }
}

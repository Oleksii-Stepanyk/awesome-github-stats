using AwesomeGithubStats.Api.Configuration;
using AwesomeGithubStats.Api.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.ResponseCompression;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;
using Serilog.Events;
using System;
using System.IO.Compression;
using System.Linq;
using System.Net.Mime;

var builder = WebApplication.CreateBuilder(args);

// --- Serilog (equivalente ao seu Main com try/catch) ---
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Override("Microsoft", LogEventLevel.Information)
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .CreateLogger();

builder.Host.UseSerilog();

// --- Kestrel: remover header "Server" ---
builder.WebHost.ConfigureKestrel(o => o.AddServerHeader = false);

try
{
    var services = builder.Services;
    var config = builder.Configuration;

    // --- MVC + Swagger só em Development ---
    if (builder.Environment.IsDevelopment())
    {
        services
            .AddMvcCore(options => options.Filters.Add<LogRequestTimeFilterAttribute>())
            .AddApiExplorer();

        services.AddSwaggerGen();
    }
    else
    {
        services.AddMvcCore();
    }

    services.AddResponseCaching();

    services.AddResponseCompression(options =>
    {
        options.Providers.Add<BrotliCompressionProvider>();
        options.Providers.Add<GzipCompressionProvider>();
        options.EnableForHttps = true;
        options.MimeTypes = ResponseCompressionDefaults.MimeTypes.Concat(new[]
        {
            "image/jpeg",
            "image/png",
            "application/font-woff2",
            "image/svg+xml",
            MediaTypeNames.Application.Json
        });
    });

    services.AddCors(options =>
    {
        options.AddPolicy("AllowAnyOrigin",
            policy => policy.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader());
    });

    services.Configure<BrotliCompressionProviderOptions>(options =>
    {
        options.Level = CompressionLevel.Optimal;
    });

    services.ConfigureGithubServices(config);

    services.AddHttpClient(nameof(SelfPingService));
    services.AddHostedService<SelfPingService>();

    services.AddHealthChecks();

    var app = builder.Build();

    // --- Pipeline ---
    app.UseResponseCompression();
    app.UseResponseCaching();

    if (app.Environment.IsDevelopment())
    {
        app.UseDeveloperExceptionPage();
        app.UseSwagger();
        app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "Awesome Github Stats v1"));
    }

    app.UseCors("AllowAnyOrigin");
    app.UseHttpsRedirection();

    app.UseDefaultFiles(new DefaultFilesOptions
    {
        DefaultFileNames = new[] { "index.html" }
    });

    app.UseStaticFiles();
    app.UseRouting();

    app.MapHealthChecks("/health");
    app.MapControllers();

    Log.Information("Starting web host");
    app.Run();

    return 0;
}
catch (Exception ex)
{
    Log.Fatal(ex, "Host terminated unexpectedly");
    return 1;
}
finally
{
    Log.CloseAndFlush();
}

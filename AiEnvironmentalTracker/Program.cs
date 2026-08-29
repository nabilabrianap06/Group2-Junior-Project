using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using AiEnvironmentalTracker.Data;
using AiEnvironmentalTracker.Interfaces;
using AiEnvironmentalTracker.Models;
using AiEnvironmentalTracker.Repositories;
using AiEnvironmentalTracker.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace AiEnvironmentalTracker
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // ── Service Registration ────────────────────────────────────────

            // EF Core + PostgreSQL (Supabase)
            string connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
                ?? throw new InvalidOperationException("DefaultConnection is missing from appsettings.json.");

            builder.Services.AddDbContext<AppDbContext>(options =>
                options.UseNpgsql(connectionString));

            // Core Calculation Engine
            builder.Services.AddSingleton<CalculationEngine>();
            builder.Services.AddSingleton<IElectricityCalculator>(sp => sp.GetRequiredService<CalculationEngine>());
            builder.Services.AddSingleton<ICarbonCalculator>(sp => sp.GetRequiredService<CalculationEngine>());
            builder.Services.AddSingleton<IWaterCalculator>(sp => sp.GetRequiredService<CalculationEngine>());

            // Repository
            builder.Services.AddScoped<IImpactRepository, ImpactRepository>();

            // Proxy Router & Services
            builder.Services.AddSingleton<IProxyRouter, ProxyRouter>();
            builder.Services.AddHttpClient<IAiProxyService, AiProxyService>(client =>
            {
                client.Timeout = TimeSpan.FromMinutes(5); // Generous timeout for long-running streaming/reasoning models
            });

            // Legacy Gemini Service
            builder.Services.AddHttpClient<IGeminiApiService, GeminiApiService>();

            // Swagger / OpenAPI documentation
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen();

            // CORS policy to allow web tools and frontend extensions to connect
            builder.Services.AddCors(options =>
            {
                options.AddDefaultPolicy(policy =>
                {
                    policy.AllowAnyOrigin()
                          .AllowAnyHeader()
                          .AllowAnyMethod();
                });
            });

            var app = builder.Build();

            // ── Database Schema Initialization ──────────────────────────────
            using (var scope = app.Services.CreateScope())
            {
                var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
                var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                try
                {
                    await db.Database.EnsureCreatedAsync();

                    // Ensure ai_usage_logs table exists if database was already partially created
                    string createTableSql = @"
                        CREATE TABLE IF NOT EXISTS ai_usage_logs (
                            id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
                            provider VARCHAR(64) NOT NULL,
                            model_name VARCHAR(128) NOT NULL,
                            prompt_tokens INT NOT NULL DEFAULT 0,
                            completion_tokens INT NOT NULL DEFAULT 0,
                            total_tokens INT NOT NULL DEFAULT 0,
                            energy_kwh DOUBLE PRECISION NOT NULL DEFAULT 0,
                            carbon_grams DOUBLE PRECISION NOT NULL DEFAULT 0,
                            water_ml DOUBLE PRECISION NOT NULL DEFAULT 0,
                            analogy_string VARCHAR(256) DEFAULT '',
                            latency_ms BIGINT NOT NULL DEFAULT 0,
                            is_streaming BOOLEAN NOT NULL DEFAULT FALSE,
                            status_code INT NOT NULL DEFAULT 200,
                            created_at TIMESTAMP WITH TIME ZONE DEFAULT (NOW() AT TIME ZONE 'UTC')
                        );
                        CREATE INDEX IF NOT EXISTS ix_ai_usage_logs_created_at ON ai_usage_logs (created_at);
                        CREATE INDEX IF NOT EXISTS ix_ai_usage_logs_provider ON ai_usage_logs (provider);
                        CREATE INDEX IF NOT EXISTS ix_ai_usage_logs_model_name ON ai_usage_logs (model_name);
                    ";
                    await db.Database.ExecuteSqlRawAsync(createTableSql);

                    logger.LogInformation("PostgreSQL schema checked / initialized successfully.");
                }
                catch (Exception ex)
                {
                    logger.LogWarning(ex, "Could not automatically initialize PostgreSQL. Please verify your connection string in appsettings.json.");
                }
            }

            // ── Middleware Pipeline ─────────────────────────────────────────
            app.UseCors();

            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI();
            }

            app.UseDefaultFiles();
            app.UseStaticFiles();

            // ─────────────────────────────────────────────────────────────────
            //  A. OpenAI-Compatible Gateway Endpoints
            // ─────────────────────────────────────────────────────────────────

            // POST /v1/chat/completions (OpenAI compatible proxy entrypoint)
            app.MapPost("/v1/chat/completions", async (HttpContext context, [FromServices] IAiProxyService proxyService) =>
            {
                await proxyService.HandleChatCompletionsAsync(context);
            })
            .WithName("ProxyChatCompletions")
            .WithSummary("OpenAI-compatible reverse proxy endpoint. Intercepts AI requests, streams responses with zero lag, and computes real-time environmental metrics.");

            // GET /v1/models (Model listing for tools auto-discovery)
            app.MapGet("/v1/models", () =>
            {
                var response = new OpenAiModelListResponse
                {
                    Data = new List<OpenAiModelItem>
                    {
                        new() { Id = "gemini-2.5-flash" },
                        new() { Id = "gemini-2.5-pro" },
                        new() { Id = "deepseek-chat" },
                        new() { Id = "deepseek-reasoner" },
                        new() { Id = "gpt-4o" },
                        new() { Id = "gpt-4o-mini" },
                        new() { Id = "o1" },
                        new() { Id = "o3-mini" },
                        new() { Id = "claude-3-5-sonnet-20241022" },
                        new() { Id = "llama-3.3-70b-versatile" }
                    }
                };
                return Results.Ok(response);
            })
            .WithName("ListModels")
            .WithSummary("Lists available AI models supported by the proxy gateway.");

            // ─────────────────────────────────────────────────────────────────
            //  B. Live Telemetry & Observability API Endpoints
            // ─────────────────────────────────────────────────────────────────

            // GET /api/telemetry/summary
            app.MapGet("/api/telemetry/summary", async ([FromServices] IImpactRepository repository) =>
            {
                try
                {
                    var summary = await repository.GetTelemetrySummaryAsync();
                    return Results.Ok(summary);
                }
                catch (Exception ex)
                {
                    return Results.Problem(detail: ex.Message, statusCode: StatusCodes.Status500InternalServerError);
                }
            })
            .WithName("GetTelemetrySummary")
            .WithSummary("Returns aggregate environmental impact (kWh, CO2, water) and equivalent comparisons.");

            // GET /api/telemetry/logs
            app.MapGet("/api/telemetry/logs", async ([FromServices] IImpactRepository repository, [FromQuery] int count = 50) =>
            {
                try
                {
                    var logs = await repository.GetRecentUsageLogsAsync(Math.Clamp(count, 1, 200));
                    return Results.Ok(logs);
                }
                catch (Exception ex)
                {
                    return Results.Problem(detail: ex.Message, statusCode: StatusCodes.Status500InternalServerError);
                }
            })
            .WithName("GetTelemetryLogs")
            .WithSummary("Returns the most recent AI proxy interaction logs with environmental telemetry.");

            // GET /api/telemetry/providers
            app.MapGet("/api/telemetry/providers", async ([FromServices] IImpactRepository repository) =>
            {
                try
                {
                    var breakdown = await repository.GetProviderBreakdownAsync();
                    return Results.Ok(breakdown);
                }
                catch (Exception ex)
                {
                    return Results.Problem(detail: ex.Message, statusCode: StatusCodes.Status500InternalServerError);
                }
            })
            .WithName("GetProviderBreakdown")
            .WithSummary("Returns environmental impact and token breakdown grouped by AI provider.");

            // ─────────────────────────────────────────────────────────────────
            //  C. System & Legacy Endpoints
            // ─────────────────────────────────────────────────────────────────

            app.MapGet("/health", () => Results.Ok(new 
            { 
                status = "Healthy", 
                gateway = "AI Environmental Tracker Proxy Gateway",
                openAiProxyUrl = "http://localhost:5000/v1",
                timestamp = DateTime.UtcNow 
            }))
            .WithName("HealthCheck");

            // Legacy manual chat endpoint
            app.MapPost("/api/chat", async (
                [FromBody] PromptRequest request,
                [FromServices] IGeminiApiService geminiService,
                [FromServices] IImpactRepository repository) =>
            {
                if (string.IsNullOrWhiteSpace(request.Prompt))
                {
                    return Results.BadRequest(new { message = "Prompt cannot be empty." });
                }

                try
                {
                    GeminiChatResult result = await geminiService.SendPromptAsync(request.Prompt);
                    var logEntry = new ChatLog
                    {
                        Id = Guid.NewGuid(),
                        UserPrompt = request.Prompt,
                        AIResponse = result.ResponseText,
                        TotalTokens = result.TokenUsage.TotalTokens,
                        EnergyKWh = result.Impact.EnergyKWh,
                        CarbonGrams = result.Impact.CarbonGrams,
                        WaterML = result.Impact.WaterML,
                        CreatedAt = DateTime.UtcNow
                    };

                    await repository.SaveLogAsync(logEntry);
                    return Results.Ok(result);
                }
                catch (Exception ex)
                {
                    return Results.Problem(detail: ex.Message, statusCode: StatusCodes.Status500InternalServerError);
                }
            });

            Console.WriteLine("=========================================================");
            Console.WriteLine("  AI Environmental Tracker - LLM Observability Gateway");
            Console.WriteLine("  Base Proxy URL : http://localhost:5000/v1");
            Console.WriteLine("  Live Dashboard : http://localhost:5000");
            Console.WriteLine("  Swagger API    : http://localhost:5000/swagger");
            Console.WriteLine("=========================================================");

            await app.RunAsync();
        }
    }

    public record PromptRequest(string Prompt);
}

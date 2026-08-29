using System;
using System.Collections.Generic;
using AiEnvironmentalTracker.Interfaces;
using AiEnvironmentalTracker.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;

namespace AiEnvironmentalTracker.Services
{
    public class ProxyRouter : IProxyRouter
    {
        private readonly IConfiguration _configuration;

        public ProxyRouter(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public UpstreamRoute ResolveRoute(string modelName, HttpRequest request)
        {
            modelName = modelName?.Trim() ?? string.Empty;
            string lowerModel = modelName.ToLowerInvariant();

            string provider = "OpenAI";
            string upstreamUrl = "https://api.openai.com/v1/chat/completions";
            string? fallbackKey = _configuration["OpenAI:ApiKey"];

            // 1. Check custom overrides from appsettings
            var customRoute = _configuration.GetSection($"ProxyRoutes:{modelName}");
            if (customRoute.Exists() && !string.IsNullOrEmpty(customRoute["UpstreamUrl"]))
            {
                provider = customRoute["Provider"] ?? "Custom";
                upstreamUrl = customRoute["UpstreamUrl"]!;
                fallbackKey = customRoute["ApiKey"];
            }
            // 2. Intelligent pattern matching based on model prefix/family
            else if (lowerModel.StartsWith("deepseek-") || lowerModel.Contains("deepseek"))
            {
                provider = "DeepSeek";
                upstreamUrl = _configuration["UpstreamEndpoints:DeepSeek"] ?? "https://api.deepseek.com/chat/completions";
                fallbackKey = _configuration["DeepSeek:ApiKey"];
            }
            else if (lowerModel.StartsWith("gemini-") || lowerModel.StartsWith("gemma-") || lowerModel.Contains("gemini"))
            {
                provider = "Gemini";
                // Google Gemini officially supports OpenAI compatibility endpoint
                upstreamUrl = _configuration["UpstreamEndpoints:Gemini"] 
                    ?? "https://generativelanguage.googleapis.com/v1beta/openai/chat/completions";
                fallbackKey = _configuration["Gemini:ApiKey"];
            }
            else if (lowerModel.StartsWith("claude-") || lowerModel.Contains("anthropic"))
            {
                provider = "Claude";
                upstreamUrl = _configuration["UpstreamEndpoints:Claude"] ?? "https://api.anthropic.com/v1/messages";
                fallbackKey = _configuration["Claude:ApiKey"] ?? _configuration["Anthropic:ApiKey"];
            }
            else if (lowerModel.StartsWith("llama-") || lowerModel.StartsWith("mixtral-") || lowerModel.StartsWith("gemma2-"))
            {
                provider = "Groq";
                upstreamUrl = _configuration["UpstreamEndpoints:Groq"] ?? "https://api.groq.com/openai/v1/chat/completions";
                fallbackKey = _configuration["Groq:ApiKey"];
            }
            else if (lowerModel.StartsWith("ollama/") || lowerModel.StartsWith("local/"))
            {
                provider = "Ollama";
                upstreamUrl = _configuration["UpstreamEndpoints:Ollama"] ?? "http://localhost:11434/v1/chat/completions";
                fallbackKey = "ollama";
            }
            else if (lowerModel.Contains("/"))
            {
                // OpenRouter style e.g. "meta-llama/llama-3-70b-instruct"
                provider = "OpenRouter";
                upstreamUrl = _configuration["UpstreamEndpoints:OpenRouter"] ?? "https://openrouter.ai/api/v1/chat/completions";
                fallbackKey = _configuration["OpenRouter:ApiKey"];
            }
            else
            {
                // Default: OpenAI
                provider = "OpenAI";
                upstreamUrl = _configuration["UpstreamEndpoints:OpenAI"] ?? "https://api.openai.com/v1/chat/completions";
                fallbackKey = _configuration["OpenAI:ApiKey"];
            }

            // Extract client Authorization header
            string? clientAuth = null;
            if (request.Headers.TryGetValue("Authorization", out var authHeader) && !string.IsNullOrWhiteSpace(authHeader))
            {
                clientAuth = authHeader.ToString();
            }
            else if (request.Headers.TryGetValue("x-api-key", out var xApiKey) && !string.IsNullOrWhiteSpace(xApiKey))
            {
                clientAuth = $"Bearer {xApiKey}";
            }

            // Resolve final API key to forward
            string? resolvedKey = clientAuth;
            if (string.IsNullOrWhiteSpace(resolvedKey) && !string.IsNullOrWhiteSpace(fallbackKey))
            {
                resolvedKey = fallbackKey.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase) 
                    ? fallbackKey 
                    : $"Bearer {fallbackKey}";
            }

            var route = new UpstreamRoute
            {
                Provider = provider,
                UpstreamUrl = upstreamUrl,
                ResolvedApiKey = resolvedKey,
                Model = modelName
            };

            // Copy additional relevant headers
            if (request.Headers.TryGetValue("OpenAI-Organization", out var orgHeader))
            {
                route.Headers["OpenAI-Organization"] = orgHeader.ToString();
            }
            if (request.Headers.TryGetValue("anthropic-version", out var anthropicVer))
            {
                route.Headers["anthropic-version"] = anthropicVer.ToString();
            }

            return route;
        }
    }
}

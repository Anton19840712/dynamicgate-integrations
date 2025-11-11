using System;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace CameraManagementAPI.Middleware;

/// <summary>
/// Basic Authentication Middleware для Camera Management API
/// </summary>
public class BasicAuthMiddleware
{
    private readonly RequestDelegate _next;
    private readonly IConfiguration _configuration;
    private readonly ILogger<BasicAuthMiddleware> _logger;
    private readonly string _expectedUsername;
    private readonly string _expectedPassword;

    public BasicAuthMiddleware(RequestDelegate next, IConfiguration configuration, ILogger<BasicAuthMiddleware> logger)
    {
        _next = next;
        _configuration = configuration;
        _logger = logger;

        // Читаем учетные данные из конфигурации
        _expectedUsername = _configuration["BasicAuth:Username"] ?? "anton_test";
        _expectedPassword = _configuration["BasicAuth:Password"] ?? "anton_password";

        _logger.LogInformation("Basic Auth middleware initialized with username: {Username}", _expectedUsername);
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // Пропускаем Swagger endpoints
        if (context.Request.Path.StartsWithSegments("/swagger") ||
            context.Request.Path.StartsWithSegments("/favicon.ico"))
        {
            await _next(context);
            return;
        }

        // Проверяем наличие заголовка Authorization
        if (!context.Request.Headers.ContainsKey("Authorization"))
        {
            _logger.LogWarning("Request to {Path} missing Authorization header", context.Request.Path);
            await SendUnauthorized(context, "Missing Authorization header");
            return;
        }

        var authHeader = context.Request.Headers["Authorization"].ToString();

        // Проверяем формат Basic Auth
        if (!authHeader.StartsWith("Basic ", StringComparison.OrdinalIgnoreCase))
        {
            _logger.LogWarning("Request to {Path} has invalid Authorization header format", context.Request.Path);
            await SendUnauthorized(context, "Invalid Authorization header format");
            return;
        }

        try
        {
            // Извлекаем и декодируем credentials
            var encodedCredentials = authHeader.Substring("Basic ".Length).Trim();
            var decodedCredentials = Encoding.UTF8.GetString(Convert.FromBase64String(encodedCredentials));
            var credentials = decodedCredentials.Split(':', 2);

            if (credentials.Length != 2)
            {
                _logger.LogWarning("Request to {Path} has malformed credentials", context.Request.Path);
                await SendUnauthorized(context, "Malformed credentials");
                return;
            }

            var username = credentials[0];
            var password = credentials[1];

            // Проверяем учетные данные
            if (username == _expectedUsername && password == _expectedPassword)
            {
                _logger.LogDebug("Successful authentication for user {Username} to {Path}", username, context.Request.Path);
                await _next(context);
                return;
            }
            else
            {
                _logger.LogWarning("Failed authentication attempt for user {Username} to {Path}", username, context.Request.Path);
                await SendUnauthorized(context, "Invalid credentials");
                return;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing Basic Auth for {Path}", context.Request.Path);
            await SendUnauthorized(context, "Authentication error");
            return;
        }
    }

    private static async Task SendUnauthorized(HttpContext context, string message)
    {
        context.Response.StatusCode = 401;
        context.Response.Headers.Add("WWW-Authenticate", "Basic realm=\"Camera Management API\"");
        context.Response.ContentType = "application/json";

        var response = System.Text.Json.JsonSerializer.Serialize(new
        {
            status = "error",
            error = "Unauthorized",
            message = message
        });

        await context.Response.WriteAsync(response);
    }
}
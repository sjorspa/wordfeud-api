using Wordfeud.Api.Interfaces;
using Wordfeud.Api.Services;
using Wordfeud.Api.Models;
using Wordfeud.Api.Serialization;
using Microsoft.OpenApi.Models;
using System.Text.Json;
using System.Text.Json.Serialization;

public sealed class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        // Add services to the container
        // Configure JSON serialization with BoardConverter and camelCase naming policy
        builder.Services.AddControllers()
            .AddJsonOptions(options =>
            {
                options.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
                options.JsonSerializerOptions.Converters.Add(new BoardConverter());
                options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
            });

        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddSwaggerGen(c =>
        {
            c.SwaggerDoc("v1", new OpenApiInfo
            {
                Title = "Wordfeud API",
                Version = "v1",
                Description = "A REST API for playing Wordfeud (Dutch Scrabble-like word game).",
                Contact = new OpenApiContact
                {
                    Name = "Wordfeud API",
                    Url = new Uri("https://github.com/sjorspa/wordfeud-api")
                }
            });
            c.EnableAnnotations();
        });

        // Register Dutch dictionary service
        builder.Services.AddSingleton<IDutchDictionaryService, DutchDictionaryService>();

        // Register game service
        builder.Services.AddSingleton<IGameService, GameService>();

        // Configure logging
        builder.Services.AddLogging(loggingBuilder =>
        {
            loggingBuilder.AddConsole();
            loggingBuilder.AddDebug();
            loggingBuilder.SetMinimumLevel(LogLevel.Information);
        });

        // Prevent auto-shutdown
        builder.Services.AddHttpClient();

        var app = builder.Build();

        // Initialize Dutch dictionary
        using (var scope = app.Services.CreateScope())
        {
            var dictionaryService = scope.ServiceProvider.GetRequiredService<IDutchDictionaryService>();
            dictionaryService.InitializeAsync().Wait();
        }

        // Configure the HTTP request pipeline
        if (app.Environment.IsDevelopment())
        {
            app.UseSwagger();
            app.UseSwaggerUI(c =>
            {
                c.SwaggerEndpoint("/swagger/v1/swagger.json", "Wordfeud API v1");
                c.RoutePrefix = "swagger";
            });
        }

        if (!app.Environment.IsDevelopment())
        {
            app.UseHttpsRedirection();

            // Add security headers
            app.Use(async (context, next) =>
            {
                context.Response.Headers.Append("X-Content-Type-Options", "nosniff");
                context.Response.Headers.Append("X-Frame-Options", "DENY");
                context.Response.Headers.Append("X-XSS-Protection", "1; mode=block");
                context.Response.Headers.Append("Referrer-Policy", "strict-origin-when-cross-origin");
                context.Response.Headers.Append("Permissions-Policy", "camera=(), microphone=(), geolocation=()");

                if (context.Request.Scheme == "http")
                {
                    context.Response.Headers.Append("Strict-Transport-Security", "max-age=31536000; includeSubDomains");
                }

                await next(context);
            });
        }
        app.UseAuthorization();
        app.MapControllers();

        // Ensure the application runs continuously
        app.Urls.Add("http://0.0.0.0:8080");

        app.Run();
    }
}

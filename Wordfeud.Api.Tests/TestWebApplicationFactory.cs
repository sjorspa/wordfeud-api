using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Wordfeud.Api.Serialization;

namespace Wordfeud.Api.Tests;

/// <summary>
/// Custom WebApplicationFactory that configures JSON serialization
/// with the TileArrayConverter for 2D board deserialization in tests.
/// </summary>
public class TestWebApplicationFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            // Add JSON options with TileArrayConverter to the MVC configuration
            services.Configure<Microsoft.AspNetCore.Mvc.JsonOptions>(options =>
            {
                options.JsonSerializerOptions.Converters.Add(new TileArrayConverter());
            });
        });
    }
}

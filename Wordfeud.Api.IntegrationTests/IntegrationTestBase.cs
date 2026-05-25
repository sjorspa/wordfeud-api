using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Wordfeud.Api.Serialization;

namespace Wordfeud.Api.IntegrationTests;

/// <summary>
/// Base class for integration tests that provides a configured WebApplicationFactory.
/// </summary>
public class IntegrationTestBase : IDisposable
{
    private readonly TestWebApplicationFactory _factory;
    private bool _disposed;

    public IntegrationTestBase()
    {
        _factory = new TestWebApplicationFactory();
        Client = _factory.CreateClient();
    }

    /// <summary>
    /// Gets the HTTP client for making requests to the test API.
    /// </summary>
    protected HttpClient Client { get; }

    /// <summary>
    /// Gets the service provider from the test application.
    /// Useful for resolving services directly for unit-style testing.
    /// </summary>
    protected IServiceProvider Services => _factory.Services;

    /// <inheritdoc />
    public void Dispose()
    {
        if (_disposed)
            return;

        _factory.Dispose();
        _disposed = true;
    }
}

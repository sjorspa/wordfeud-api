using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;

namespace Wordfeud.Web.Controllers;

/// <summary>
/// Proxies API requests to the Wordfeud.Api backend service.
/// All frontend JavaScript calls /api/* on the same host, and this controller
/// forwards them to the configured ApiUrl (e.g., http://wordfeud-api:8080).
/// </summary>
[ApiController]
public class ProxyController : ControllerBase
{
    private readonly HttpClient _httpClient;
    private readonly string _apiBaseUrl;
    private readonly ILogger<ProxyController> _logger;

    public ProxyController(IHttpClientFactory httpClientFactory, IConfiguration configuration, ILogger<ProxyController> logger)
    {
        _logger = logger;
        _apiBaseUrl = configuration["ApiUrl"] ?? "http://localhost:8080";
        _httpClient = httpClientFactory.CreateClient("Api");
    }

    /// <summary>
    /// Catch-all proxy for all /api/* routes.
    /// Forwards the request to the Wordfeud.Api backend service.
    /// </summary>
    [ApiExplorerSettings(IgnoreApi = true)]
    [Route("api/{*path}")]
    public async Task<IActionResult> Proxy(string path)
    {
        // Build the target URL
        var targetPath = string.IsNullOrEmpty(path) ? "" : $"/{path}";
        var targetUrl = $"{_apiBaseUrl.TrimEnd('/')}{targetPath}";

        // Forward the request method and content
        using var requestMessage = new HttpRequestMessage();

        requestMessage.Method = new HttpMethod(HttpContext.Request.Method);
        requestMessage.RequestUri = new Uri(targetUrl);

        // Copy headers (except host-related ones)
        foreach (var header in HttpContext.Request.Headers)
        {
            if (!header.Key.Equals("Host", StringComparison.OrdinalIgnoreCase) &&
                !header.Key.Equals("Host", StringComparison.OrdinalIgnoreCase))
            {
                requestMessage.Headers.TryAddWithoutValidation(header.Key, header.Value.AsEnumerable());
            }
        }

        // Forward the body for POST/PUT/PATCH
        if (HttpContext.Request.HasFormContentType || HttpContext.Request.ContentLength > 0)
        {
            var bodyBytes = await ReadBodyAsync();
            if (bodyBytes != null && bodyBytes.Length > 0)
            {
                requestMessage.Content = new ByteArrayContent(bodyBytes);
                var contentType = HttpContext.Request.ContentType;
                if (!string.IsNullOrEmpty(contentType))
                {
                    requestMessage.Content.Headers.ContentType = System.Net.Http.Headers.MediaTypeHeaderValue.Parse(contentType);
                }
            }
        }

        try
        {
            var response = await _httpClient.SendAsync(requestMessage, HttpContext.RequestAborted);

            // Copy response headers
            foreach (var header in response.Headers)
            {
                if (!header.Key.Equals("Transfer-Encoding", StringComparison.OrdinalIgnoreCase))
                {
                    HttpContext.Response.Headers.Append(header.Key, new Microsoft.Extensions.Primitives.StringValues(header.Value.ToArray()));
                }
            }
            if (response.Content.Headers.ContentType != null)
            {
                HttpContext.Response.Headers.ContentType = response.Content.Headers.ContentType.ToString();
            }

            // Copy response body
            var bodyBytes = await response.Content.ReadAsByteArrayAsync();

            // Set status code and return the body as a file content result
            HttpContext.Response.StatusCode = (int)response.StatusCode;
            return new FileContentResult(bodyBytes, response.Content.Headers.ContentType?.ToString() ?? "application/octet-stream");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error proxying request to {TargetUrl}", targetUrl);
            return StatusCode(502, new
            {
                error = "Bad Gateway",
                message = "Unable to reach the API backend service."
            });
        }
    }

    private async Task<byte[]?> ReadBodyAsync()
    {
        if (!HttpContext.Request.HasFormContentType || HttpContext.Request.ContentLength == null)
        {
            return null;
        }

        HttpContext.Request.EnableBuffering();
        using var reader = new System.IO.StreamReader(HttpContext.Request.Body, leaveOpen: true);
        var body = await reader.ReadToEndAsync();
        HttpContext.Request.Body.Position = 0;

        return string.IsNullOrEmpty(body) ? null : System.Text.Encoding.UTF8.GetBytes(body);
    }
}

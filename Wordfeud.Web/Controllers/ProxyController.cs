using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;

namespace Wordfeud.Web.Controllers;

/// <summary>
/// Proxies API requests to the Wordfeud.Api backend service.
/// All frontend JavaScript calls /api/* on the same host, and this controller
/// forwards them to the configured ApiUrl (e.g., http://wordfeud-api:8080).
/// </summary>
public class ProxyController : Controller
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
            if (!header.Key.Equals("Host", StringComparison.OrdinalIgnoreCase))
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

            // Read response body into bytes
            var bodyBytes = await response.Content.ReadAsByteArrayAsync();
            var contentType = response.Content.Headers.ContentType?.ToString() ?? "application/octet-stream";

            // Set status code and headers on Response
            Response.StatusCode = (int)response.StatusCode;
            foreach (var header in response.Headers)
            {
                if (!header.Key.Equals("Transfer-Encoding", StringComparison.OrdinalIgnoreCase))
                {
                    Response.Headers[header.Key] = header.Value.ToString();
                }
            }

            // Return body via File() helper
            return File(bodyBytes, contentType);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error proxying request to {TargetUrl}", targetUrl);
            return new ContentResult
            {
                Content = System.Text.Json.JsonSerializer.Serialize(new { error = "Bad Gateway", message = "Unable to reach the API backend service." }),
                ContentType = "application/json",
                StatusCode = 502
            };
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

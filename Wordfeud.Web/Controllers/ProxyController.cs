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
        var targetPath = string.IsNullOrEmpty(path) ? "/api" : $"/api/{path}";
        var targetUrl = $"{_apiBaseUrl.TrimEnd('/')}{targetPath}";

        using var requestMessage = new HttpRequestMessage();
        requestMessage.Method = new HttpMethod(HttpContext.Request.Method);
        requestMessage.RequestUri = new Uri(targetUrl);

        foreach (var header in HttpContext.Request.Headers)
        {
            var lowerKey = header.Key.ToLowerInvariant();
            if (lowerKey == "host" || lowerKey == "content-length" ||
                lowerKey == "transfer-encoding" || lowerKey == "connection")
            {
                continue;
            }
            requestMessage.Headers.TryAddWithoutValidation(header.Key, header.Value.AsEnumerable());
        }

        if (HttpContext.Request.HasFormContentType || 
            HttpContext.Request.ContentLength > 0 || 
            !string.IsNullOrEmpty(HttpContext.Request.ContentType))
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

            var bodyBytes = await response.Content.ReadAsByteArrayAsync();
            var contentType = response.Content.Headers.ContentType?.ToString() ?? "application/octet-stream";

            Response.StatusCode = (int)response.StatusCode;
            foreach (var header in response.Headers)
            {
                if (!header.Key.Equals("Transfer-Encoding", StringComparison.OrdinalIgnoreCase))
                {
                    Response.Headers[header.Key] = header.Value.ToString();
                }
            }

            return File(bodyBytes, contentType);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error proxying request to {TargetUrl}", targetUrl);
            Response.StatusCode = 502;
            Response.ContentType = "application/json";
            var errorJson = System.Text.Json.JsonSerializer.Serialize(new { error = "Bad Gateway", message = "Unable to reach the API backend service." });
            return Content(errorJson, "application/json");
        }
    }

    private async Task<byte[]?> ReadBodyAsync()
    {
        HttpContext.Request.EnableBuffering();
        using var reader = new System.IO.StreamReader(HttpContext.Request.Body, leaveOpen: true);
        var body = await reader.ReadToEndAsync();
        HttpContext.Request.Body.Position = 0;

        return string.IsNullOrEmpty(body) ? null : System.Text.Encoding.UTF8.GetBytes(body);
    }
}

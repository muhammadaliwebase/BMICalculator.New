using System.Net;
using System.Text;
using System.Text.Json;
using BMICalculator.Agent.Configuration;
using BMICalculator.Agent.Models;

namespace BMICalculator.Agent.Services;

public class FaceIdListenerService : IFaceIdListenerService, IDisposable
{
    private readonly FaceIdListenerConfig _config;
    private readonly JsonSerializerOptions _jsonOptions;
    private HttpListener? _listener;
    private CancellationTokenSource? _cancellationTokenSource;
    private Task? _listenTask;

    public event EventHandler<FaceIdScanEvent>? PersonScanned;

    public bool IsListening => _listener?.IsListening ?? false;
    public string? ListenUrl { get; private set; }

    public FaceIdListenerService(FaceIdListenerConfig config)
    {
        _config = config;
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };
    }

    public Task StartAsync()
    {
        if (_listener != null && _listener.IsListening)
        {
            return Task.CompletedTask;
        }

        _listener = new HttpListener();
        ListenUrl = $"http://+:{_config.ListenPort}{_config.ListenEndpoint}/";
        _listener.Prefixes.Add(ListenUrl);

        try
        {
            _listener.Start();
            _cancellationTokenSource = new CancellationTokenSource();
            _listenTask = ListenLoop(_cancellationTokenSource.Token);
        }
        catch (HttpListenerException ex)
        {
            // If port is already in use or requires admin, try localhost only
            _listener = new HttpListener();
            ListenUrl = $"http://localhost:{_config.ListenPort}{_config.ListenEndpoint}/";
            _listener.Prefixes.Clear();
            _listener.Prefixes.Add(ListenUrl);
            _listener.Start();
            _cancellationTokenSource = new CancellationTokenSource();
            _listenTask = ListenLoop(_cancellationTokenSource.Token);
        }

        return Task.CompletedTask;
    }

    public async Task StopAsync()
    {
        _cancellationTokenSource?.Cancel();

        if (_listener != null && _listener.IsListening)
        {
            _listener.Stop();
            _listener.Close();
        }

        if (_listenTask != null)
        {
            try
            {
                await _listenTask;
            }
            catch (OperationCanceledException)
            {
                // Expected
            }
        }

        _listener = null;
        _listenTask = null;
        ListenUrl = null;
    }

    private async Task ListenLoop(CancellationToken cancellationToken)
    {
        System.Diagnostics.Debug.WriteLine($"[FaceID] ListenLoop started on {ListenUrl}");

        while (!cancellationToken.IsCancellationRequested && _listener != null && _listener.IsListening)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine("[FaceID] Waiting for request...");
                var context = await _listener.GetContextAsync();
                System.Diagnostics.Debug.WriteLine("[FaceID] Request received!");
                _ = ProcessRequestAsync(context);
            }
            catch (HttpListenerException) when (cancellationToken.IsCancellationRequested)
            {
                break;
            }
            catch (ObjectDisposedException)
            {
                break;
            }
            catch (Exception)
            {
                // Log and continue
            }
        }
    }

    private async Task ProcessRequestAsync(HttpListenerContext context)
    {
        try
        {
            System.Diagnostics.Debug.WriteLine($"[FaceID] Request received: {context.Request.HttpMethod} from {context.Request.RemoteEndPoint}");

            if (context.Request.HttpMethod == "POST")
            {
                using var reader = new StreamReader(context.Request.InputStream, Encoding.UTF8);
                var body = await reader.ReadToEndAsync();

                System.Diagnostics.Debug.WriteLine($"[FaceID] Body: {body}");

                // Extract JSON from multipart/form-data if needed
                var json = ExtractJsonFromBody(body);
                System.Diagnostics.Debug.WriteLine($"[FaceID] Extracted JSON: {json}");

                var scanEvent = ParseHikVisionEvent(json, context.Request.RemoteEndPoint?.Address?.ToString());
                System.Diagnostics.Debug.WriteLine($"[FaceID] ParseResult: {(scanEvent != null ? $"PersonId={scanEvent.PersonId}" : "NULL")}");

                if (scanEvent != null)
                {
                    OnPersonScanned(scanEvent);
                }

                // Send OK response
                context.Response.StatusCode = 200;
                context.Response.ContentType = "application/json";
                var responseBytes = Encoding.UTF8.GetBytes("{\"status\":\"ok\"}");
                await context.Response.OutputStream.WriteAsync(responseBytes);
            }
            else
            {
                context.Response.StatusCode = 405;
            }
        }
        catch (Exception)
        {
            context.Response.StatusCode = 500;
        }
        finally
        {
            context.Response.Close();
        }
    }

    private string ExtractJsonFromBody(string body)
    {
        var trimmed = body.Trim();

        // If already JSON, return as-is
        if (trimmed.StartsWith("{"))
            return trimmed;

        // Extract JSON from multipart/form-data
        var startIndex = body.IndexOf('{');
        var endIndex = body.LastIndexOf('}');

        if (startIndex >= 0 && endIndex > startIndex)
        {
            return body.Substring(startIndex, endIndex - startIndex + 1);
        }

        return body;
    }

    private FaceIdScanEvent? ParseHikVisionEvent(string json, string? remoteIp)
    {
        try
        {
            var hikEvent = JsonSerializer.Deserialize<HikVisionEventDto>(json, _jsonOptions);
            if (hikEvent?.AccessControllerEvent == null)
                return null;

            var ace = hikEvent.AccessControllerEvent;

            // Only process events with employee number (valid face scan)
            if (string.IsNullOrEmpty(ace.EmployeeNoString))
                return null;

            return new FaceIdScanEvent
            {
                PersonId = ace.EmployeeNoString,
                EmployeeNo = ace.EmployeeNoString,
                Name = ace.Name,
                ScanTime = hikEvent.DateTime ?? DateTime.Now,
                DeviceId = ace.DeviceName,
                DeviceIp = hikEvent.IpAddress ?? remoteIp
            };
        }
        catch (JsonException)
        {
            return null;
        }
    }

    protected virtual void OnPersonScanned(FaceIdScanEvent e)
    {
        PersonScanned?.Invoke(this, e);
    }

    public void Dispose()
    {
        StopAsync().Wait();
        _cancellationTokenSource?.Dispose();
    }
}

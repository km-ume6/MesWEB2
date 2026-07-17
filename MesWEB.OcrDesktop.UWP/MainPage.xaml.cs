using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Windows.Graphics.Imaging;
using Windows.Media.Ocr;
using Windows.Storage.Streams;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace MesWEB.OcrDesktop.UWP
{
    public sealed partial class MainPage : Page
    {
        private TcpListener? _listener;
        private CancellationTokenSource? _cts;
        private readonly ILogger<MainPage> _logger;

        public MainPage()
        {
            this.InitializeComponent();

            using var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
            _logger = loggerFactory.CreateLogger<MainPage>();

            UpdateStatus("Ready to start server.");
        }

        private async void UpdateStatus(string message)
        {
            await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
            {
                StatusText.Text = message;
            });
        }

        private async void StartButton_Click(object sender, RoutedEventArgs e)
        {
            if (_listener != null) return;

            try
            {
                _cts = new CancellationTokenSource();
                _listener = new TcpListener(IPAddress.Any, 8080);
                _listener.Start();

                await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
                {
                    StatusText.Text = "Server started. Listening...";
                    StartButton.IsEnabled = false;
                    StopButton.IsEnabled = true;
                });

                await Task.Run(() => ListenForClients(_cts.Token), _cts.Token);
            }
            catch (Exception ex)
            {
                UpdateStatus($"Error starting server: {ex.Message}");
                _logger.LogError(ex, "Failed to start server");
            }
        }

        private async void StopButton_Click(object sender, RoutedEventArgs e)
        {
            _cts?.Cancel();
            _listener?.Stop();
            _listener = null;

            await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
            {
                StatusText.Text = "Server stopped.";
                StartButton.IsEnabled = true;
                StopButton.IsEnabled = false;
            });
        }

        private async Task ListenForClients(CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {
                try
                {
                    var client = await _listener!.AcceptTcpClientAsync();
                    _ = Task.Run(() => HandleClient(client), token);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error accepting client");
                }
            }
        }

        private async Task HandleClient(TcpClient client)
        {
            using (client)
            {
                var stream = client.GetStream();
                var reader = new StreamReader(stream, Encoding.UTF8);
                var writer = new StreamWriter(stream, Encoding.UTF8) { AutoFlush = true };

                try
                {
                    var requestJson = await reader.ReadLineAsync();
                    if (string.IsNullOrEmpty(requestJson)) return;

                    var request = JsonSerializer.Deserialize<OcrRequest>(requestJson);
                    if (request == null || request.ImageBytes == null) return;

                    var imageBytes = Convert.FromBase64String(request.ImageBytes);

                    // Create SoftwareBitmap from image bytes
                    SoftwareBitmap? softwareBitmap = null;
                    using (var randomAccessStream = new InMemoryRandomAccessStream())
                    {
                        await randomAccessStream.WriteAsync(imageBytes.AsBuffer());
                        randomAccessStream.Seek(0);

                        var decoder = await BitmapDecoder.CreateAsync(randomAccessStream);
                        softwareBitmap = await decoder.GetSoftwareBitmapAsync();
                    }

                    if (softwareBitmap == null)
                    {
                        var errorResponse = new OcrResponse { Status = "Error", ErrorMessage = "Failed to create image buffer from image" };
                        var errorJson = JsonSerializer.Serialize(errorResponse);
                        await writer.WriteLineAsync(errorJson);
                        return;
                    }

                    // Create OcrEngine
                    var language = string.IsNullOrEmpty(request.Language) ? "ja" : request.Language;
                    var ocrEngine = OcrEngine.TryCreateFromLanguage(new Windows.Globalization.Language(language));

                    if (ocrEngine == null)
                    {
                        var errorResponse = new OcrResponse { Status = "Error", ErrorMessage = $"OCR engine for language '{language}' is not available" };
                        var errorJson = JsonSerializer.Serialize(errorResponse);
                        await writer.WriteLineAsync(errorJson);
                        return;
                    }

                    // Perform OCR
                    var ocrResult = await ocrEngine.RecognizeAsync(softwareBitmap);

                    var response = new OcrResponse
                    {
                        Status = "Success",
                        ExtractedText = ocrResult.Text,
                        ErrorMessage = null
                    };

                    var responseJson = JsonSerializer.Serialize(response);
                    await writer.WriteLineAsync(responseJson);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error handling client");
                    var errorResponse = new OcrResponse { Status = "Error", ErrorMessage = ex.Message };
                    var errorJson = JsonSerializer.Serialize(errorResponse);
                    await writer.WriteLineAsync(errorJson);
                }
            }
        }
    }

    public class OcrRequest
    {
        public string? ImageBytes { get; set; }
        public string? FileName { get; set; }
        public string? Language { get; set; }
        public bool? SkipPerspective { get; set; }
        public bool? IsTableMode { get; set; }
        public bool? IsDigitsOnlyMode { get; set; }
    }

    public class OcrResponse
    {
        public string? Status { get; set; }
        public string? ExtractedText { get; set; }
        public string? ErrorMessage { get; set; }
    }
}

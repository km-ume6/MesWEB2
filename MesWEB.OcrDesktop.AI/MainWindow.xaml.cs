using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using System.Windows;
using MesWEB.OcrDesktop.AI.Services;

namespace MesWEB.OcrDesktop.AI;

public partial class MainWindow : Window
{
    private TcpListener? _listener;
    private CancellationTokenSource? _cts;
    private readonly ILogger<MainWindow> _logger;
    private EasyOcrService? _ocrService;
    private const int ServerPort = 8080;

    public MainWindow()
    {
        InitializeComponent();

        using var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
        _logger = loggerFactory.CreateLogger<MainWindow>();

        StatusText.Text = "EasyOCR サービスを初期化中...";

        // 自動起動
        _ = InitializeAndStartAsync();
    }

    private async Task InitializeAndStartAsync()
    {
        try
        {
            // OCR サービスを初期化
            _ocrService = new EasyOcrService();
            _logger.LogInformation("EasyOCR service initialized successfully");

            // サーバーを自動起動
            await StartServerAsync();
        }
        catch (Exception ex)
        {
            StatusText.Text = $"❌ 初期化エラー: {ex.Message}\n\n" +
                             $"📝 セットアップ手順:\n" +
                             $"1. Python をインストール (3.9+)\n" +
                             $"2. pip install easyocr\n" +
                             $"3. アプリを再起動";
            _logger.LogError(ex, "Initialization failed");
        }
    }

    private async Task StartServerAsync()
    {
        if (_listener != null)
        {
            _logger.LogWarning("Server is already running");
            return;
        }

        try
        {
            _cts = new CancellationTokenSource();
            _listener = new TcpListener(IPAddress.Any, ServerPort);
            _listener.Start();

            StatusText.Text = $"✅ EasyOCR サーバー起動完了\n" +
                             $"ポート: {ServerPort}\n\n" +
                             $"📡 制御コマンド:\n" +
                             $"  - START: サーバー起動\n" +
                             $"  - STOP: サーバー停止\n" +
                             $"  - RESTART: サーバー再起動\n" +
                             $"  - STATUS: ステータス確認\n\n" +
                             $"待機中...";

            _logger.LogInformation($"Server started on port {ServerPort}");

            await Task.Run(() => ListenForClients(_cts.Token), _cts.Token);
        }
        catch (Exception ex)
        {
            StatusText.Text = $"❌ サーバー起動エラー: {ex.Message}";
            _logger.LogError(ex, "Failed to start server");
        }
    }

    private async Task StopServerAsync()
    {
        _cts?.Cancel();
        _listener?.Stop();
        _listener = null;

        StatusText.Text += "\n⏹️ サーバー停止";
        _logger.LogInformation("Server stopped");

        await Task.Delay(100);
    }

    private async Task RestartServerAsync()
    {
        StatusText.Text += "\n🔄 サーバー再起動中...";
        _logger.LogInformation("Restarting server...");

        await StopServerAsync();
        await Task.Delay(500);
        await StartServerAsync();
    }

    private async Task ListenForClients(CancellationToken token)
    {
        while (!token.IsCancellationRequested)
        {
            try
            {
                var client = await _listener!.AcceptTcpClientAsync(token);
                _ = Task.Run(() => HandleClient(client), token);

                await Dispatcher.InvokeAsync(() =>
                {
                    StatusText.Text += $"\n📨 接続: {client.Client.RemoteEndPoint}";
                });
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
                if (string.IsNullOrEmpty(requestJson))
                {
                    _logger.LogWarning("空のリクエストを受信");
                    return;
                }

                // 制御コマンドをチェック
                if (await HandleControlCommand(requestJson, writer))
                {
                    return;
                }

                // OCR リクエストとして処理
                var request = JsonSerializer.Deserialize<OcrRequest>(requestJson);
                if (request == null || request.ImageBytes == null)
                {
                    _logger.LogError("無効なリクエスト形式");
                    await SendError(writer, "Invalid request format");
                    return;
                }

                var imageBytes = Convert.FromBase64String(request.ImageBytes);
                _logger.LogInformation($"画像受信: {imageBytes.Length} bytes, ファイル名: {request.FileName}");

                if (_ocrService == null)
                {
                    _logger.LogError("OCR サービスが未初期化");
                    await SendError(writer, "OCR service not initialized");
                    return;
                }

                await Dispatcher.InvokeAsync(() =>
                {
                    StatusText.Text += $"\n🔍 OCR 処理開始: {request.FileName} ({imageBytes.Length} bytes)";
                });

                // EasyOCR 実行
                var language = string.IsNullOrEmpty(request.Language) ? "ja" : request.Language;
                _logger.LogInformation($"OCR 実行中 (言語: {language})...");

                var extractedText = await _ocrService.RecognizeTextAsync(imageBytes, language);

                _logger.LogInformation($"OCR 完了: {extractedText.Length} 文字");

                var response = new OcrResponse
                {
                    Status = "Success",
                    ExtractedText = extractedText,
                    ErrorMessage = null
                };

                var responseJson = JsonSerializer.Serialize(response);
                await writer.WriteLineAsync(responseJson);

                await Dispatcher.InvokeAsync(() =>
                {
                    StatusText.Text += $"\n✅ OCR 完了 ({extractedText.Length} 文字)";
                });

                _logger.LogInformation($"レスポンス送信完了: {extractedText.Length} 文字");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "クライアント処理中にエラー発生");
                await SendError(writer, ex.Message);

                await Dispatcher.InvokeAsync(() =>
                {
                    StatusText.Text += $"\n❌ エラー: {ex.Message}";
                });
            }
        }
    }

    private async Task<bool> HandleControlCommand(string requestJson, StreamWriter writer)
    {
        try
        {
            var controlRequest = JsonSerializer.Deserialize<ControlRequest>(requestJson);
            if (controlRequest?.Command == null)
            {
                return false;
            }

            _logger.LogInformation($"制御コマンド受信: {controlRequest.Command}");

            var response = controlRequest.Command.ToUpper() switch
            {
                "START" => await HandleStartCommand(),
                "STOP" => await HandleStopCommand(),
                "RESTART" => await HandleRestartCommand(),
                "STATUS" => HandleStatusCommand(),
                _ => new ControlResponse { Status = "Error", Message = $"不明なコマンド: {controlRequest.Command}" }
            };

            var responseJson = JsonSerializer.Serialize(response);
            await writer.WriteLineAsync(responseJson);

            await Dispatcher.InvokeAsync(() =>
            {
                StatusText.Text += $"\n🎛️ コマンド実行: {controlRequest.Command} → {response.Status}";
            });

            return true;
        }
        catch
        {
            return false;
        }
    }

    private async Task<ControlResponse> HandleStartCommand()
    {
        if (_listener != null)
        {
            return new ControlResponse { Status = "Info", Message = "サーバーは既に起動しています" };
        }

        await StartServerAsync();
        return new ControlResponse { Status = "Success", Message = "サーバーを起動しました" };
    }

    private async Task<ControlResponse> HandleStopCommand()
    {
        if (_listener == null)
        {
            return new ControlResponse { Status = "Info", Message = "サーバーは既に停止しています" };
        }

        await StopServerAsync();
        return new ControlResponse { Status = "Success", Message = "サーバーを停止しました" };
    }

    private async Task<ControlResponse> HandleRestartCommand()
    {
        await RestartServerAsync();
        return new ControlResponse { Status = "Success", Message = "サーバーを再起動しました" };
    }

    private ControlResponse HandleStatusCommand()
    {
        var isRunning = _listener != null;
        var message = isRunning
            ? $"サーバー稼働中 (ポート: {ServerPort})"
            : "サーバー停止中";

        return new ControlResponse
        {
            Status = "Success",
            Message = message,
            ServerRunning = isRunning,
            Port = ServerPort
        };
    }

    private async Task SendError(StreamWriter writer, string message)
    {
        var errorResponse = new OcrResponse
        {
            Status = "Error",
            ErrorMessage = message
        };
        var errorJson = JsonSerializer.Serialize(errorResponse);
        await writer.WriteLineAsync(errorJson);
        _logger.LogWarning($"エラーレスポンス送信: {message}");
    }

    protected override void OnClosed(EventArgs e)
    {
        _cts?.Cancel();
        _listener?.Stop();
        _ocrService?.Dispose();
        base.OnClosed(e);
    }
}

// リクエスト/レスポンスクラス
public class OcrRequest
{
    public string? ImageBytes { get; set; }
    public string? FileName { get; set; }
    public string? Language { get; set; }
}

public class OcrResponse
{
    public string? Status { get; set; }
    public string? ExtractedText { get; set; }
    public string? ErrorMessage { get; set; }
}

public class ControlRequest
{
    public string? Command { get; set; }
}

public class ControlResponse
{
    public string? Status { get; set; }
    public string? Message { get; set; }
    public bool? ServerRunning { get; set; }
    public int? Port { get; set; }
}

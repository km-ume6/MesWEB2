using System.Diagnostics;
using System.IO;
using System.Text;
using System.Text.Json;

namespace MesWEB.OcrDesktop.AI.Services;

/// <summary>
/// Python の EasyOCR を呼び出す OCR サービス
/// </summary>
public class EasyOcrService : IDisposable
{
    private readonly string _pythonPath;
    private readonly string _scriptPath;
    private bool _disposed;

    // JSON デシリアライゼーションオプション（大文字小文字を区別しない）
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public EasyOcrService()
    {
        // UTF-8 エンコーディングを登録（.NET Core/5+ で必要）
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

        // Python 実行ファイルのパスを検出
        _pythonPath = DetectPythonPath();
        Console.WriteLine($"[DEBUG] Python パス: {_pythonPath}");

        // Python スクリプトのパス
        _scriptPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "python", "easyocr_service.py");
        Console.WriteLine($"[DEBUG] スクリプトパス: {_scriptPath}");

        if (!File.Exists(_scriptPath))
        {
            throw new FileNotFoundException(
                $"EasyOCR スクリプトが見つかりません: {_scriptPath}\n" +
                $"python/easyocr_service.py を配置してください。"
            );
        }
    }

    public async Task<string> RecognizeTextAsync(byte[] imageBytes, string language = "ja")
    {
        Console.WriteLine($"[DEBUG] RecognizeTextAsync 開始: {imageBytes.Length} bytes, 言語={language}");

        // 一時ファイルに画像を保存
        var tempImagePath = Path.GetTempFileName() + ".png";
        var tempResultPath = Path.GetTempFileName() + ".json";

        Console.WriteLine($"[DEBUG] 一時画像パス: {tempImagePath}");
        Console.WriteLine($"[DEBUG] 一時結果パス: {tempResultPath}");

        try
        {
            await File.WriteAllBytesAsync(tempImagePath, imageBytes);
            var fileInfo = new FileInfo(tempImagePath);
            Console.WriteLine($"[DEBUG] 画像ファイル保存完了: {fileInfo.Length} bytes");

            // Python スクリプトを実行
            var result = await RunPythonScript(tempImagePath, tempResultPath, language);

            Console.WriteLine($"[DEBUG] OCR 結果: {result.Length} 文字");
            return result;
        }
        finally
        {
            // 一時ファイルを削除
            if (File.Exists(tempImagePath))
            {
                File.Delete(tempImagePath);
                Console.WriteLine($"[DEBUG] 一時画像ファイル削除: {tempImagePath}");
            }
            if (File.Exists(tempResultPath))
            {
                File.Delete(tempResultPath);
                Console.WriteLine($"[DEBUG] 一時結果ファイル削除: {tempResultPath}");
            }
        }
    }

    public async Task<string> RecognizeTextAsync(string imagePath, string language = "ja")
    {
        var imageBytes = await File.ReadAllBytesAsync(imagePath);
        return await RecognizeTextAsync(imageBytes, language);
    }

    private async Task<string> RunPythonScript(string imagePath, string resultPath, string language)
    {
        Console.WriteLine($"[DEBUG] Python スクリプト実行開始");

        var startInfo = new ProcessStartInfo
        {
            FileName = _pythonPath,
            Arguments = $"\"{_scriptPath}\" \"{imagePath}\" \"{resultPath}\" \"{language}\"",
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
            StandardOutputEncoding = Encoding.UTF8,
            StandardErrorEncoding = Encoding.UTF8
        };

        startInfo.EnvironmentVariables["PYTHONIOENCODING"] = "utf-8";

        using var process = new Process { StartInfo = startInfo };

        var outputBuilder = new StringBuilder();
        var errorBuilder = new StringBuilder();

        process.OutputDataReceived += (sender, e) =>
        {
            if (!string.IsNullOrEmpty(e.Data))
            {
                Console.WriteLine($"[PYTHON STDOUT] {e.Data}");
                outputBuilder.AppendLine(e.Data);
            }
        };

        process.ErrorDataReceived += (sender, e) =>
        {
            if (!string.IsNullOrEmpty(e.Data))
            {
                Console.WriteLine($"[PYTHON STDERR] {e.Data}");
                errorBuilder.AppendLine(e.Data);
            }
        };

        var startTime = DateTime.Now;
        process.Start();
        process.BeginOutputReadLine();
        process.BeginErrorReadLine();

        await process.WaitForExitAsync();
        var duration = DateTime.Now - startTime;

        Console.WriteLine($"[DEBUG] Python プロセス終了: 終了コード={process.ExitCode}, 実行時間={duration.TotalSeconds:F2}秒");

        if (process.ExitCode != 0)
        {
            throw new InvalidOperationException(
                $"EasyOCR 実行エラー (終了コード: {process.ExitCode})\n" +
                $"エラー出力: {errorBuilder}"
            );
        }

        // 結果ファイルを読み込み
        if (!File.Exists(resultPath))
        {
            throw new FileNotFoundException($"OCR 結果ファイルが見つかりません: {resultPath}");
        }

        var resultJson = await File.ReadAllTextAsync(resultPath, Encoding.UTF8);
        Console.WriteLine($"[DEBUG] 結果 JSON 読み込み完了: {resultJson.Length} 文字");
        Console.WriteLine($"[DEBUG] 結果 JSON: {resultJson}");

        // ★ ここが重要！JsonOptions を使ってデシリアライズ
        var result = JsonSerializer.Deserialize<OcrResult>(resultJson, JsonOptions);

        if (result == null)
        {
            throw new InvalidOperationException("OCR 結果のデシリアライズに失敗しました");
        }

        Console.WriteLine($"[DEBUG] デシリアライズ完了: Text={result.Text?.Length ?? 0} 文字");

        if (!string.IsNullOrEmpty(result.Text))
        {
            Console.WriteLine($"[DEBUG] 抽出テキスト: {result.Text.Substring(0, Math.Min(100, result.Text.Length))}...");
        }

        return result.Text ?? string.Empty;
    }

    private string DetectPythonPath()
    {
        // 一般的な Python パスを検索
        var possiblePaths = new[]
        {
            "python",
            "python3",
            @"C:\Python312\python.exe",
            @"C:\Python311\python.exe",
            @"C:\Python310\python.exe",
            @"C:\Python39\python.exe",
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), @"Programs\Python\Python312\python.exe"),
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), @"Programs\Python\Python311\python.exe"),
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), @"Programs\Python\Python310\python.exe")
        };

        foreach (var path in possiblePaths)
        {
            try
            {
                var process = Process.Start(new ProcessStartInfo
                {
                    FileName = path,
                    Arguments = "--version",
                    RedirectStandardOutput = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                });

                if (process != null)
                {
                    process.WaitForExit();
                    if (process.ExitCode == 0)
                    {
                        return path;
                    }
                }
            }
            catch
            {
                continue;
            }
        }

        throw new FileNotFoundException(
            "Python が見つかりません。\n" +
            "Python 3.9 以上をインストールして、PATH に追加してください。"
        );
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            _disposed = true;
        }
        GC.SuppressFinalize(this);
    }

    private class OcrResult
    {
        public string? Text { get; set; }
        public bool Success { get; set; }
        public OcrDetails? Details { get; set; }
    }

    private class OcrDetails
    {
        public int TotalBlocks { get; set; }
        public int FilteredBlocks { get; set; }
        public int AcceptedBlocks { get; set; }
        public int TotalChars { get; set; }
        public double ConfidenceThreshold { get; set; }
    }
}

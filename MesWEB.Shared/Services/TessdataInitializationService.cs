using Microsoft.Extensions.Logging;

namespace MesWEB.Shared.Services
{
    /// <summary>
    /// Tesseract tessdata の自動初期化サービス
    /// 起動時に tessdata が存在しない場合、GitHub から自動的にダウンロード
    /// </summary>
    public class TessdataInitializationService
    {
        private readonly ILogger<TessdataInitializationService> _logger;
        private const string TessdataGithubUrl = "https://github.com/tesseract-ocr/tessdata/raw/main";
        private readonly string[] _requiredLanguages = { "eng", "jpn" };

        public string? TessDataPath { get; private set; }

        public TessdataInitializationService(ILogger<TessdataInitializationService> logger)
        {
            _logger = logger;
        }

        /// <summary>
        /// tessdata を初期化（存在しなければダウンロード）
        /// </summary>
        public async Task<(string tessDataPath, bool success)> InitializeTessdataAsync()
        {
            try
            {
                _logger.LogInformation("tessdata 初期化を開始します");

                // 既存の tessdata パスを検索
                var tessDataPath = FindExistingTessdata();

                if (!string.IsNullOrEmpty(tessDataPath))
                {
                    _logger.LogInformation("✓ 既存の tessdata を検出: {Path}", tessDataPath);
                    TessDataPath = tessDataPath;
                    return (tessDataPath, true);
                }

                _logger.LogInformation("tessdata が見つかりません。ダウンロードを開始します");

                // ダウンロード先ディレクトリを決定
                var downloadDir = DetermineDownloadDirectory();
                if (string.IsNullOrEmpty(downloadDir))
                {
                    _logger.LogError("tessdata をダウンロードするディレクトリを作成できませんでした");
                    return (string.Empty, false);
                }

                // tessdata をダウンロード
                bool downloadSuccess = await DownloadTessdataAsync(downloadDir);
                if (!downloadSuccess)
                {
                    _logger.LogError("tessdata のダウンロードに失敗しました");
                    return (string.Empty, false);
                }

                _logger.LogInformation("✓ tessdata のダウンロードと初期化が完了しました: {Path}", downloadDir);
                TessDataPath = downloadDir;
                return (downloadDir, true);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "tessdata 初期化中にエラーが発生しました");
                return (string.Empty, false);
            }
        }

        /// <summary>
        /// 既存の tessdata を検索
        /// </summary>
        private string? FindExistingTessdata()
        {
            var candidates = new List<string>
            {
                Path.Combine(AppContext.BaseDirectory, "tessdata"),
                Path.Combine(Environment.CurrentDirectory, "tessdata"),
                Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "tessdata"),
                Path.Combine(AppContext.BaseDirectory, "wwwroot", "tessdata"),
                Path.Combine(AppContext.BaseDirectory, "..\\..\\tessdata"),
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "Tesseract-OCR", "tessdata"),
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "Tesseract", "tessdata"),
            };

            foreach (var candidate in candidates)
            {
                try
                {
                    if (Directory.Exists(candidate) && HasRequiredFiles(candidate))
                    {
                        return candidate;
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogDebug("tessdata 検索エラー: {Path} - {Error}", candidate, ex.Message);
                }
            }

            return null;
        }

        /// <summary>
        /// tessdata ディレクトリが必要なファイルを持っているか確認
        /// </summary>
        private bool HasRequiredFiles(string tessdataDir)
        {
            foreach (var lang in _requiredLanguages)
            {
                var trainedDataFile = Path.Combine(tessdataDir, $"{lang}.traineddata");
                if (!File.Exists(trainedDataFile))
                {
                    return false;
                }
            }
            return true;
        }

        /// <summary>
        /// tessdata をダウンロードするディレクトリを決定
        /// </summary>
        private string? DetermineDownloadDirectory()
        {
            // 優先順位順に試行
            var candidates = new List<string>
            {
                Path.Combine(AppContext.BaseDirectory, "tessdata"),
                Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "tessdata"),
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "Tesseract", "tessdata"),
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "tessdata"),
                Path.Combine(Path.GetTempPath(), "tessdata"),
            };

            foreach (var dir in candidates)
            {
                try
                {
                    // ディレクトリを作成
                    Directory.CreateDirectory(dir);

                    // 書き込み可能か確認
                    var testFile = Path.Combine(dir, ".writetest");
                    File.WriteAllText(testFile, "test");
                    File.Delete(testFile);

                    _logger.LogInformation("tessdata ダウンロード先: {Path}", dir);
                    return dir;
                }
                catch (Exception ex)
                {
                    _logger.LogDebug("ディレクトリ '{Path}' に書き込みできません: {Error}", dir, ex.Message);
                }
            }

            return null;
        }

        /// <summary>
        /// GitHub から tessdata をダウンロード
        /// </summary>
        private async Task<bool> DownloadTessdataAsync(string targetDir)
        {
            try
            {
                using var httpClient = new HttpClient { Timeout = TimeSpan.FromMinutes(10) };

                foreach (var lang in _requiredLanguages)
                {
                    var fileName = $"{lang}.traineddata";
                    var url = $"{TessdataGithubUrl}/{fileName}";
                    var outputPath = Path.Combine(targetDir, fileName);

                    if (File.Exists(outputPath))
                    {
                        _logger.LogInformation("✓ {FileName} は既に存在します。スキップ", fileName);
                        continue;
                    }

                    _logger.LogInformation("⬇ {FileName} をダウンロード中... ({Url})", fileName, url);

                    try
                    {
                        var response = await httpClient.GetAsync(url);
                        response.EnsureSuccessStatusCode();

                        using (var contentStream = await response.Content.ReadAsStreamAsync())
                        using (var fileStream = File.Create(outputPath))
                        {
                            await contentStream.CopyToAsync(fileStream);
                        }

                        var fileSize = new FileInfo(outputPath).Length / (1024 * 1024); // MB
                        _logger.LogInformation("✓ {FileName} をダウンロード完了 ({Size:F1}MB)", fileName, fileSize);
                    }
                    catch (HttpRequestException ex)
                    {
                        _logger.LogError("✗ {FileName} のダウンロードに失敗しました: {Error}", fileName, ex.Message);

                        // ダウンロード失敗時は不完全なファイルを削除
                        try { File.Delete(outputPath); } catch { }

                        return false;
                    }
                }

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "tessdata ダウンロード処理中にエラーが発生しました");
                return false;
            }
        }
    }
}

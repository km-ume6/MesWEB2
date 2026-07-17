using Microsoft.Extensions.Logging;
using Microsoft.JSInterop;

namespace MesWEB.Shared.Services
{
    public class DeviceDetectionService
    {
        private readonly ILogger<DeviceDetectionService> _logger;
        private bool _isDetected = false;
        private bool _isAndroid = false;
        private string _userAgent = "";

        public DeviceDetectionService(ILogger<DeviceDetectionService> logger)
        {
            _logger = logger;
        }

        public bool IsAndroid => _isAndroid;
        public bool IsDetected => _isDetected;
        public string UserAgent => _userAgent;
        public string DeviceType => _isAndroid ? "Android" : "PC/その他";

        public event Action? DeviceDetected;

        public async Task DetectDeviceAsync(IJSRuntime jsRuntime, CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Device detection started. Current state: IsDetected={IsDetected}, IsAndroid={IsAndroid}", _isDetected, _isAndroid);

            try
            {
                // UserAgent取得
                _userAgent = await jsRuntime.InvokeAsync<string>("eval", cancellationToken, "navigator.userAgent");

                // iOS判定（除外用）
                bool isiOS = _userAgent.Contains("iPhone") ||
                            _userAgent.Contains("iPad") ||
                            _userAgent.Contains("iPod");

                // Android判定（iOSでない場合のみ）
                bool containsAndroid = _userAgent.Contains("Android");

                // 安全な判定：iOS端末は絶対にAndroidとしない
                _isAndroid = containsAndroid && !isiOS;

                _isDetected = true;
                _logger.LogInformation(
                    "Device detection succeeded. IsAndroid={IsAndroid}, UserAgentLength={UserAgentLength}",
                    _isAndroid,
                    _userAgent?.Length ?? 0);
                DeviceDetected?.Invoke(); // イベントを発火
            }
            catch (TaskCanceledException ex)
            {
                _isAndroid = false;
                _isDetected = true;
                _logger.LogWarning(ex, "Device detection timed out.");
                DeviceDetected?.Invoke();
            }
            catch (JSException ex)
            {
                _isAndroid = false;
                _isDetected = true;
                _logger.LogError(ex, "Device detection failed during JS interop.");
                DeviceDetected?.Invoke();
            }
            catch (Exception ex)
            {
                _isAndroid = false;
                _isDetected = true;
                _logger.LogError(ex, "Device detection failed with unexpected error.");
                DeviceDetected?.Invoke();
            }
            finally
            {
                _logger.LogInformation("Device detection finished. IsDetected={IsDetected}, IsAndroid={IsAndroid}", _isDetected, _isAndroid);
            }
        }

        // 強制的に再検出を行うメソッド
        public async Task ForceRedetectAsync(IJSRuntime jsRuntime, CancellationToken cancellationToken = default)
        {
            _isDetected = false;
            _isAndroid = false;
            _userAgent = "";
            _logger.LogInformation("Device detection state reset. Starting forced re-detection.");
            await DetectDeviceAsync(jsRuntime, cancellationToken);
        }

        // デバッグ用メソッド：手動でAndroidとして設定
        public void SetAsAndroid()
        {
            _isAndroid = true;
            _isDetected = true;
            _userAgent = "Debug: Manually set as Android";
            _logger.LogInformation("Device manually set as Android for debugging.");
            DeviceDetected?.Invoke();
        }

        // デバッグ用メソッド：検出状態をリセット
        public void Reset()
        {
            _isDetected = false;
            _isAndroid = false;
            _userAgent = "";
            _logger.LogInformation("Device detection state has been reset.");
        }
    }
}

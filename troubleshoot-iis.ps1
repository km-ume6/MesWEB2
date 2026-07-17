# IIS トラブルシューティングスクリプト
# 使用方法: .\troubleshoot-iis.ps1 -SiteName "MesWEB"

param(
    [string]$SiteName = "生産技術",
    [string]$DeployPath = "C:\inetpub2"
)

$ErrorActionPreference = "Continue"

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "IIS トラブルシューティング" -ForegroundColor Cyan
Write-Host "Site: $SiteName" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan

# 1. IIS サイトの状態確認
Write-Host "`n[1] IIS サイトの状態:" -ForegroundColor Green
try {
    Import-Module WebAdministration -ErrorAction Stop
    $site = Get-Website -Name $SiteName -ErrorAction SilentlyContinue
    
    if ($null -eq $site) {
        Write-Host "✗ サイト '$SiteName' が見つかりません" -ForegroundColor Red
    } else {
        Write-Host "✓ サイト名: $($site.Name)" -ForegroundColor Green
        Write-Host "  物理パス: $($site.PhysicalPath)" -ForegroundColor White
        Write-Host "  状態: $($site.State)" -ForegroundColor $(if ($site.State -eq "Started") {"Green"} else {"Red"})
        Write-Host "  バインディング:" -ForegroundColor White
        foreach ($binding in $site.Bindings.Collection) {
            Write-Host "    - $($binding.protocol)://$($binding.bindingInformation)" -ForegroundColor White
        }
    }
} catch {
    Write-Host "✗ IIS PowerShell モジュールが見つかりません: $_" -ForegroundColor Red
}

# 2. ログファイルの確認
Write-Host "`n[2] stdout ログファイル:" -ForegroundColor Green
$logsPath = Join-Path $DeployPath "logs"
$logFile = Join-Path $logsPath "stdout.log"

if (Test-Path $logFile) {
    $logSize = (Get-Item $logFile).Length / 1MB
    Write-Host "✓ ログファイル: $logFile" -ForegroundColor Green
    Write-Host "  ファイルサイズ: $([Math]::Round($logSize, 2)) MB" -ForegroundColor White
    
    # 最後の50行を表示
    Write-Host "`n  --- ログの最後の50行 ---" -ForegroundColor Cyan
    $content = Get-Content $logFile -Tail 50
    if ($null -ne $content) {
        $content | ForEach-Object { Write-Host "  $_" -ForegroundColor White }
    }
    Write-Host "  --- ログ終了 ---" -ForegroundColor Cyan
    
    # iOS Safari 関連のエラーを検索
    Write-Host "`n  iOS/Safari 関連エラーを検索中..." -ForegroundColor Yellow
    $iosErrors = Get-Content $logFile | Select-String -Pattern "iOS|Safari|SignalR|Circuit" -Context 1
    if ($iosErrors) {
        Write-Host "  iOS/Safari 関連のログ:" -ForegroundColor Yellow
        $iosErrors | ForEach-Object { Write-Host "    $_" -ForegroundColor White }
    } else {
        Write-Host "  iOS/Safari 関連のエラーは見つかりませんでした" -ForegroundColor Green
    }
} else {
    Write-Host "✗ ログファイルが見つかりません: $logFile" -ForegroundColor Red
    Write-Host "  以下を確認してください:" -ForegroundColor Yellow
    Write-Host "  * web.config で stdoutLogEnabled=\"true\" が設定されているか" -ForegroundColor White
    Write-Host "  * ログディレクトリ ($logsPath) が存在するか" -ForegroundColor White
    Write-Host "  * IIS AppPool がディレクトリへの書き込み権限を持っているか" -ForegroundColor White
}

# 3. web.config の確認
Write-Host "`n[3] web.config の確認:" -ForegroundColor Green
$webConfigPath = Join-Path $DeployPath "web.config"
if (Test-Path $webConfigPath) {
    Write-Host "✓ web.config が存在します: $webConfigPath" -ForegroundColor Green
    
    # aspNetCore セクションを確認
    [xml]$webConfig = Get-Content $webConfigPath
    $aspNetCore = $webConfig.configuration.system.webServer.aspNetCore
    
    if ($null -ne $aspNetCore) {
        Write-Host "  processPath: $($aspNetCore.processPath)" -ForegroundColor White
        Write-Host "  arguments: $($aspNetCore.arguments)" -ForegroundColor White
        Write-Host "  stdoutLogEnabled: $($aspNetCore.stdoutLogEnabled)" -ForegroundColor White
        Write-Host "  stdoutLogFile: $($aspNetCore.stdoutLogFile)" -ForegroundColor White
        Write-Host "  hostingModel: $($aspNetCore.hostingModel)" -ForegroundColor White
    } else {
        Write-Host "✗ aspNetCore セクションが見つかりません" -ForegroundColor Red
    }
} else {
    Write-Host "✗ web.config が見つかりません: $webConfigPath" -ForegroundColor Red
}

# 4. 必要なファイルの確認
Write-Host "`n[4] 必要なファイルの確認:" -ForegroundColor Green
$requiredFiles = @("MesWEB.dll", "appsettings.json", "web.config")
foreach ($file in $requiredFiles) {
    $filePath = Join-Path $DeployPath $file
    if (Test-Path $filePath) {
        $fileSize = (Get-Item $filePath).Length / 1MB
        Write-Host "✓ $file ($([Math]::Round($fileSize, 2)) MB)" -ForegroundColor Green
    } else {
        Write-Host "✗ $file が見つかりません" -ForegroundColor Red
    }
}

# 5. tessdata の確認
Write-Host "`n[5] tessdata の状態:" -ForegroundColor Green
$tessdataPath = Join-Path $DeployPath "tessdata"
if (Test-Path $tessdataPath) {
    $files = Get-ChildItem $tessdataPath -Filter "*.traineddata"
    Write-Host "✓ tessdata ディレクトリが存在します: $tessdataPath" -ForegroundColor Green
    Write-Host "  検出されたファイル: $($files.Count)" -ForegroundColor White
    foreach ($file in $files) {
        $size = $file.Length / 1MB
        Write-Host "    - $($file.Name) ($([Math]::Round($size, 1)) MB)" -ForegroundColor White
    }
} else {
    Write-Host "⚠ tessdata ディレクトリが見つかりません (初回起動時に自動ダウンロード)" -ForegroundColor Yellow
}

# 6. アプリケーション プールの状態
Write-Host "`n[6] アプリケーション プール:" -ForegroundColor Green
try {
    $pool = Get-WebAppPool -Name $SiteName -ErrorAction SilentlyContinue
    if ($null -ne $pool) {
        Write-Host "✓ プール名: $($pool.Name)" -ForegroundColor Green
        Write-Host "  状態: $($pool.State)" -ForegroundColor $(if ($pool.State -eq "Started") {"Green"} else {"Red"})
        Write-Host "  .NET バージョン: $($pool.ManagedRuntimeVersion)" -ForegroundColor White
        Write-Host "  パイプラインモード: $($pool.ManagedPipelineMode)" -ForegroundColor White
        Write-Host "  32ビット: $($pool.Enable32BitAppOn64bit)" -ForegroundColor White
        Write-Host "  WebSocket: $(if ($pool.applicationDefaults.webSocket.enabled) {'有効'} else {'無効'})" -ForegroundColor White
    }
} catch {
    Write-Host "⚠ プール情報を取得できません" -ForegroundColor Yellow
}

# 7. イベントログの確認
Write-Host "`n[7] イベントログ（最近のエラー）:" -ForegroundColor Green
try {
    $events = Get-EventLog -LogName Application -Source ".NET Runtime" -Newest 5 -ErrorAction SilentlyContinue
    if ($null -ne $events) {
        foreach ($event in $events) {
            Write-Host "✗ $($event.TimeGenerated): $($event.Message.Substring(0, [Math]::Min(100, $event.Message.Length)))..." -ForegroundColor Red
        }
    } else {
        Write-Host "✓ 最近のエラーは検出されませんでした" -ForegroundColor Green
    }
} catch {
    Write-Host "⚠ イベントログを読み込めません (管理者権限が必要)" -ForegroundColor Yellow
}

# 8. iOS Safari 対応チェック
Write-Host "`n[8] iOS Safari 対応チェック:" -ForegroundColor Green
Write-Host "  SignalR WebSocket の確認:" -ForegroundColor White
try {
    $pool = Get-WebAppPool -Name $SiteName -ErrorAction SilentlyContinue
    if ($null -ne $pool) {
        $wsEnabled = $pool.applicationDefaults.webSocket.enabled
        if ($wsEnabled) {
            Write-Host "    ✓ WebSocket が有効です (iOS Safari で必要)" -ForegroundColor Green
        } else {
            Write-Host "    ✗ WebSocket が無効です" -ForegroundColor Red
            Write-Host "    対策: IIS マネージャー → アプリケーションプール → 詳細設定 → WebSocket プロトコルを有効化" -ForegroundColor Yellow
        }
    }
} catch {
    Write-Host "    ⚠ WebSocket 設定を確認できません" -ForegroundColor Yellow
}

Write-Host "`n  静的ファイルの確認:" -ForegroundColor White
$staticFiles = @("manifest.json", "app.js")
foreach ($file in $staticFiles) {
    $filePath = Join-Path $DeployPath "wwwroot\$file"
    if (Test-Path $filePath) {
        Write-Host "    ✓ $file" -ForegroundColor Green
    } else {
        Write-Host "    ✗ $file が見つかりません (PWA対応に必要)" -ForegroundColor Yellow
    }
}

Write-Host "`n========================================" -ForegroundColor Cyan
Write-Host "トラブルシューティング完了" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan

Write-Host "`n推奨アクション:" -ForegroundColor Yellow
Write-Host "1. ログファイルをご確認ください: $logFile" -ForegroundColor White
Write-Host "2. IIS マネージャーでアプリケーション プールを再開してください" -ForegroundColor White
Write-Host "3. web.config の aspNetCore セクションの内容を確認してください" -ForegroundColor White
Write-Host "4. 詳細なエラーメッセージはイベントビューアで確認できます" -ForegroundColor White
Write-Host "5. iOS Safari でアクセスする場合:" -ForegroundColor Yellow
Write-Host "   - WebSocket プロトコルが有効化されているか確認" -ForegroundColor White
Write-Host "   - ブラウザのコンソール (Safari → 開発 → JavaScriptコンソール) でエラーを確認" -ForegroundColor White
Write-Host "   - SignalR 接続エラーが表示される場合、ファイアウォールやプロキシを確認" -ForegroundColor White

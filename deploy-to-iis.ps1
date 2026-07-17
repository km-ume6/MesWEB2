# IIS デプロイメント & トラブルシューティングスクリプト
# 使用方法: .\deploy-to-iis.ps1 -SiteName "MesWEB" -DeployPath "C:\inetpub\MesWEB"

param(
    [string]$SiteName = "MesWEB",
    [string]$DeployPath = "C:\inetpub\MesWEB",
    [string]$PublishPath = ".\publish"
)

$ErrorActionPreference = "Stop"

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "IIS デプロイメント & 初期化スクリプト" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan

# 1. IIS モジュールが利用可能か確認
Write-Host "`n[1/6] IIS 環境を確認中..." -ForegroundColor Green
try {
    Import-Module WebAdministration -ErrorAction Stop
    Write-Host "✓ IIS PowerShell モジュールを読み込みました" -ForegroundColor Green
} catch {
    Write-Host "✗ IIS PowerShell モジュールが見つかりません" -ForegroundColor Red
    Write-Host "  IIS がインストールされているか確認してください" -ForegroundColor Yellow
    exit 1
}

# 2. publish フォルダが存在するか確認
Write-Host "`n[2/6] publish フォルダを確認中..." -ForegroundColor Green
if (-not (Test-Path $PublishPath)) {
    Write-Host "✗ $PublishPath が見つかりません" -ForegroundColor Red
    Write-Host "  以下のコマンドで publish を実行してください:" -ForegroundColor Yellow
    Write-Host "  dotnet publish -c Release -o publish" -ForegroundColor White
    exit 1
}
Write-Host "✓ publish フォルダを検出: $PublishPath" -ForegroundColor Green

# 3. IIS サイトを作成または確認
Write-Host "`n[3/6] IIS サイトを確認中..." -ForegroundColor Green
$site = Get-Website -Name $SiteName -ErrorAction SilentlyContinue

if ($null -eq $site) {
    Write-Host "✗ サイト '$SiteName' が見つかりません" -ForegroundColor Red
    Write-Host "  以下のコマンドで IIS サイトを作成してください:" -ForegroundColor Yellow
    Write-Host "  New-Website -Name '$SiteName' -PhysicalPath '$DeployPath' -Port 80" -ForegroundColor White
    exit 1
}
Write-Host "✓ IIS サイトを検出: $SiteName ($($site.PhysicalPath))" -ForegroundColor Green

# 4. デプロイディレクトリを準備
Write-Host "`n[4/6] デプロイディレクトリを準備中..." -ForegroundColor Green
if (-not (Test-Path $DeployPath)) {
    New-Item -ItemType Directory -Path $DeployPath -Force | Out-Null
    Write-Host "✓ デプロイディレクトリを作成: $DeployPath" -ForegroundColor Green
}

# 5. ファイルをコピー
Write-Host "`n[5/6] ファイルをコピー中..." -ForegroundColor Green
Write-Host "  ソース: $PublishPath" -ForegroundColor Cyan
Write-Host "  ターゲット: $DeployPath" -ForegroundColor Cyan

try {
    $fileCount = (Get-ChildItem $PublishPath -Recurse | Measure-Object).Count
    Copy-Item -Path "$PublishPath\*" -Destination $DeployPath -Recurse -Force
    Write-Host "✓ $fileCount ファイルをコピーしました" -ForegroundColor Green
} catch {
    Write-Host "✗ ファイルコピーエラー: $_" -ForegroundColor Red
    exit 1
}

# 6. ログディレクトリを作成
Write-Host "`n[6/6] ログディレクトリを設定中..." -ForegroundColor Green
$logsPath = Join-Path $DeployPath "logs"
if (-not (Test-Path $logsPath)) {
    New-Item -ItemType Directory -Path $logsPath -Force | Out-Null
    Write-Host "✓ ログディレクトリを作成: $logsPath" -ForegroundColor Green
}

# 7. アプリケーション プール アイデンティティに権限を付与
Write-Host "`n[7/7] 権限を設定中..." -ForegroundColor Green
try {
    $poolName = (Get-WebAppPool -Name $SiteName -ErrorAction SilentlyContinue).Name
    if ([string]::IsNullOrEmpty($poolName)) {
        # サイトから自動的に決定されるプール名を使用
        $poolName = $SiteName
    }
    
    $acl = Get-Acl $DeployPath
    $ar = New-Object System.Security.AccessControl.FileSystemAccessRule(
        "IIS AppPool\$poolName",
        "Modify",
        "ContainerInherit,ObjectInherit",
        "None",
        "Allow"
    )
    $acl.SetAccessRule($ar)
    Set-Acl -Path $DeployPath -AclObject $acl
    
    Write-Host "✓ IIS AppPool\$poolName に Modify 権限を付与しました" -ForegroundColor Green
} catch {
    Write-Host "⚠ 権限設定に失敗しました (管理者権限が必要です): $_" -ForegroundColor Yellow
}

Write-Host "`n========================================" -ForegroundColor Cyan
Write-Host "✓ デプロイメントが完了しました！" -ForegroundColor Green
Write-Host "========================================" -ForegroundColor Cyan

Write-Host "`n次のステップ:" -ForegroundColor Yellow
Write-Host "1. IIS マネージャーでサイトを開始" -ForegroundColor White
Write-Host "2. ブラウザで http://localhost にアクセス" -ForegroundColor White
Write-Host "`n初回起動時の注意:" -ForegroundColor Yellow
Write-Host "- tessdata の自動ダウンロードが実行されます（5-10分）" -ForegroundColor White
Write-Host "- ログは $logsPath に保存されます" -ForegroundColor White
Write-Host "- エラーが発生した場合は以下を確認してください:" -ForegroundColor White
Write-Host "  * $logsPath\stdout.log" -ForegroundColor Cyan
Write-Host "  * イベントビューア → Windows ログ → アプリケーション" -ForegroundColor Cyan

# MesWEB IIS Web Deploy パッケージ作成スクリプト

$projectPath = "."
$configuration = "Release"
$networkSharePath = "\\192.168.11.100\share\MES\Deploys\MesWEB\"
$timestamp = Get-Date -Format "yyyyMMdd_HHmmss"
$deploymentFolder = "$networkSharePath${timestamp}_$configuration"

Write-Host "=== MesWEB IIS Web Deploy パッケージ作成 ===" -ForegroundColor Green
Write-Host "プロジェクト: $projectPath" -ForegroundColor Cyan
Write-Host "設定: $configuration" -ForegroundColor Cyan
Write-Host "デプロイ先: $deploymentFolder" -ForegroundColor Cyan
Write-Host ""

# 1. Web Deploy パッケージを生成
Write-Host "ステップ 1: Web Deploy パッケージを生成しています..." -ForegroundColor Yellow
try {
    $publishArgs = @(
        "publish",
        "-c", $configuration,
        "/p:DeployOnBuild=true",
        "/p:PublishProfile=Package"
    )
    
    & dotnet @publishArgs
    
    if ($LASTEXITCODE -ne 0) {
        Write-Host "エラー: dotnet publish コマンドが失敗しました" -ForegroundColor Red
        exit 1
    }
    Write-Host "✓ パッケージ生成完了" -ForegroundColor Green
}
catch {
    Write-Host "エラー: パッケージ生成に失敗しました" -ForegroundColor Red
    Write-Host $_.Exception.Message -ForegroundColor Red
    exit 1
}

# 2. パッケージフォルダを確認
$packageFolder = ".\obj\$configuration\Package"
if (-not (Test-Path $packageFolder)) {
    Write-Host "エラー: Web Deploy パッケージフォルダが見つかりません: $packageFolder" -ForegroundColor Red
    exit 1
}

Write-Host ""
Write-Host "ステップ 2: パッケージをネットワーク共有にコピーしています..." -ForegroundColor Yellow

# 3. ネットワーク共有フォルダを作成
if (-not (Test-Path $networkSharePath)) {
    Write-Host "ネットワーク共有フォルダを作成しています..." -ForegroundColor Cyan
    New-Item -ItemType Directory -Path $networkSharePath -Force | Out-Null
}

# 4. デプロイメントフォルダを作成
if (-not (Test-Path $deploymentFolder)) {
    New-Item -ItemType Directory -Path $deploymentFolder -Force | Out-Null
}

# 5. パッケージファイルをコピー
try {
    Copy-Item "$packageFolder\*" -Destination $deploymentFolder -Recurse -Force
    Write-Host "✓ パッケージをコピーしました: $deploymentFolder" -ForegroundColor Green
}
catch {
    Write-Host "エラー: パッケージのコピーに失敗しました" -ForegroundColor Red
    Write-Host $_.Exception.Message -ForegroundColor Red
    exit 1
}

# 6. デプロイメント情報を表示
Write-Host ""
Write-Host "ステップ 3: デプロイメント情報" -ForegroundColor Yellow
Get-ChildItem $deploymentFolder | ForEach-Object {
    $size = if ($_.PSIsContainer) { "フォルダ" } else { "{0:N2} KB" -f ($_.Length / 1KB) }
    Write-Host "  $($_.Name) ($size)" -ForegroundColor Cyan
}

Write-Host ""
Write-Host "=== 完了 ===" -ForegroundColor Green
Write-Host ""
Write-Host "IIS インポート手順:" -ForegroundColor Yellow
Write-Host "1. IIS マネージャーを開く" -ForegroundColor Cyan
Write-Host "2. [Sites] または [Application Pools] を右クリック" -ForegroundColor Cyan
Write-Host "3. [Import Application] を選択" -ForegroundColor Cyan
Write-Host "4. 以下を選択:" -ForegroundColor Cyan
Write-Host "   $deploymentFolder\MesWEB.zip" -ForegroundColor White
Write-Host ""
Write-Host "デプロイパッケージ情報:" -ForegroundColor Cyan
Write-Host "  zip: $deploymentFolder\MesWEB.zip" -ForegroundColor White
Write-Host "  パラメータ: $deploymentFolder\MesWEB.ParametersFile.xml" -ForegroundColor White
Write-Host "  デプロイスクリプト: $deploymentFolder\MesWEB.deploy.cmd" -ForegroundColor White

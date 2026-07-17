# Tesseract tessdata セットアップスクリプト
# 日本語と英語の言語データをダウンロードして配置します

$ErrorActionPreference = "Stop"

$tessdataUrl = "https://github.com/tesseract-ocr/tessdata/raw/main"
$languages = @("eng", "jpn")

# 複数の出力先ディレクトリに対応
$targetDirs = @(
    # 開発環境
    (Join-Path $PSScriptRoot "MesWEB\tessdata"),
    (Join-Path $PSScriptRoot "MesWEB\bin\Debug\net10.0\tessdata"),
    (Join-Path $PSScriptRoot "MesWEB\bin\Release\net10.0\tessdata"),
    
    # 発行ディレクトリ
    (Join-Path $PSScriptRoot "MesWEB\bin\Release\net10.0-windows\tessdata"),
    (Join-Path $PSScriptRoot "publish\tessdata"),
    
    # ローカルシステムパス
    (Join-Path $env:PROGRAMFILES "Tesseract-OCR\tessdata"),
    (Join-Path $env:PROGRAMFILES(x86) "Tesseract-OCR\tessdata")
)

# プロジェクトルートに tessdata を配置（推奨）
$primaryTargetDir = Join-Path $PSScriptRoot "MesWEB\tessdata"

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "Tesseract tessdata セットアップ" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan

# プライマリディレクトリを作成
Write-Host "`n1. プライマリディレクトリを作成中: $primaryTargetDir" -ForegroundColor Green
New-Item -ItemType Directory -Path $primaryTargetDir -Force | Out-Null

# 言語ファイルをダウンロード
$successCount = 0
foreach ($lang in $languages) {
    $trainedDataFile = "$lang.traineddata"
    $url = "$tessdataUrl/$trainedDataFile"
    $outputPath = Join-Path $primaryTargetDir $trainedDataFile
    
    if (Test-Path $outputPath) {
        Write-Host "  ✓ $trainedDataFile は既に存在します。スキップ" -ForegroundColor Yellow
        $successCount++
        continue
    }
    
    Write-Host "  ⬇  $trainedDataFile をダウンロード中..." -ForegroundColor Cyan
    try {
        Invoke-WebRequest -Uri $url -OutFile $outputPath -UseBasicParsing
        Write-Host "      ✓ 完了: $outputPath" -ForegroundColor Green
        $successCount++
    } catch {
        Write-Host "      ✗ エラー: $_" -ForegroundColor Red
    }
}

# 他のディレクトリにもコピー（存在する場合）
if ($successCount -eq $languages.Count) {
    Write-Host "`n2. 他の出力ディレクトリにコピー中..." -ForegroundColor Green
    
    foreach ($targetDir in $targetDirs) {
        if ($targetDir -eq $primaryTargetDir) { continue }
        
        if (Test-Path (Split-Path $targetDir)) {
            Write-Host "  → $targetDir" -ForegroundColor Cyan
            New-Item -ItemType Directory -Path $targetDir -Force | Out-Null
            
            Copy-Item -Path "$primaryTargetDir\*" -Destination $targetDir -Force
            Write-Host "      ✓ コピー完了" -ForegroundColor Green
        }
    }
}

Write-Host "`n========================================" -ForegroundColor Cyan
if ($successCount -eq $languages.Count) {
    Write-Host "✓ tessdata のセットアップが完了しました！" -ForegroundColor Green
    Write-Host "`n以下いずれかで アプリを起動してください:" -ForegroundColor Yellow
    Write-Host "  開発環境: dotnet run --project MesWEB" -ForegroundColor White
    Write-Host "  発行:     dotnet publish -c Release" -ForegroundColor White
} else {
    Write-Host "✗ ダウンロードに失敗しました。インターネット接続を確認してください。" -ForegroundColor Red
}
Write-Host "========================================" -ForegroundColor Cyan

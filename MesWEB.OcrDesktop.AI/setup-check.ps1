# EasyOCR 環境セットアップ確認スクリプト

Write-Host "==================================" -ForegroundColor Cyan
Write-Host "  EasyOCR 環境確認スクリプト" -ForegroundColor Cyan
Write-Host "==================================" -ForegroundColor Cyan
Write-Host ""

# Python バージョン確認
Write-Host "1. Python バージョン確認中..." -ForegroundColor Yellow
try {
    $pythonVersion = python --version 2>&1
    Write-Host "   ✅ $pythonVersion" -ForegroundColor Green
} catch {
    Write-Host "   ❌ Python が見つかりません" -ForegroundColor Red
    Write-Host "   Python 3.9 以上をインストールしてください: https://www.python.org/downloads/" -ForegroundColor Red
    exit 1
}

# pip 確認
Write-Host ""
Write-Host "2. pip 確認中..." -ForegroundColor Yellow
try {
    $pipVersion = pip --version 2>&1
    Write-Host "   ✅ $pipVersion" -ForegroundColor Green
} catch {
    Write-Host "   ❌ pip が見つかりません" -ForegroundColor Red
    exit 1
}

# EasyOCR インストール確認
Write-Host ""
Write-Host "3. EasyOCR インストール確認中..." -ForegroundColor Yellow
$easyocrCheck = python -c "import easyocr; print(easyocr.__version__)" 2>&1

if ($LASTEXITCODE -eq 0) {
    Write-Host "   ✅ EasyOCR バージョン: $easyocrCheck" -ForegroundColor Green
} else {
    Write-Host "   ❌ EasyOCR がインストールされていません" -ForegroundColor Red
    Write-Host ""
    Write-Host "   インストールしますか? (Y/N)" -ForegroundColor Yellow
    $response = Read-Host
    
    if ($response -eq "Y" -or $response -eq "y") {
        Write-Host "   EasyOCR をインストール中..." -ForegroundColor Cyan
        pip install easyocr
        
        if ($LASTEXITCODE -eq 0) {
            Write-Host "   ✅ EasyOCR のインストールが完了しました" -ForegroundColor Green
        } else {
            Write-Host "   ❌ インストールに失敗しました" -ForegroundColor Red
            exit 1
        }
    } else {
        Write-Host "   インストールをスキップしました" -ForegroundColor Yellow
        Write-Host "   手動でインストールしてください: pip install easyocr" -ForegroundColor Yellow
        exit 1
    }
}

# PyTorch 確認（EasyOCR の依存関係）
Write-Host ""
Write-Host "4. PyTorch 確認中..." -ForegroundColor Yellow
$torchCheck = python -c "import torch; print(torch.__version__)" 2>&1

if ($LASTEXITCODE -eq 0) {
    Write-Host "   ✅ PyTorch バージョン: $torchCheck" -ForegroundColor Green
} else {
    Write-Host "   ⚠️  PyTorch が見つかりません（EasyOCR が自動インストールします）" -ForegroundColor Yellow
}

# GPU サポート確認
Write-Host ""
Write-Host "5. GPU サポート確認中..." -ForegroundColor Yellow
$cudaCheck = python -c "import torch; print('CUDA 利用可能' if torch.cuda.is_available() else 'CPU のみ')" 2>&1

if ($LASTEXITCODE -eq 0) {
    if ($cudaCheck -like "*CUDA*") {
        Write-Host "   ✅ $cudaCheck" -ForegroundColor Green
    } else {
        Write-Host "   ℹ️  $cudaCheck（GPU なしでも動作します）" -ForegroundColor Cyan
    }
} else {
    Write-Host "   ℹ️  GPU サポート不明（CPU で動作します）" -ForegroundColor Cyan
}

Write-Host ""
Write-Host "==================================" -ForegroundColor Cyan
Write-Host "  環境確認完了！" -ForegroundColor Green
Write-Host "==================================" -ForegroundColor Cyan
Write-Host ""
Write-Host "📝 次のステップ:" -ForegroundColor Yellow
Write-Host "   1. dotnet run でアプリを起動" -ForegroundColor White
Write-Host "   2. 「サーバー起動」ボタンをクリック" -ForegroundColor White
Write-Host "   3. 初回実行時はモデルダウンロードに 1-2 分かかります" -ForegroundColor White
Write-Host ""

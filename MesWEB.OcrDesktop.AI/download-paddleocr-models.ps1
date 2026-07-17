# PaddleOCR ONNX モデルをダウンロードするスクリプト

$modelsDir = "models"
if (-not (Test-Path $modelsDir)) {
    New-Item -ItemType Directory -Path $modelsDir
}

Write-Host "PaddleOCR ONNX モデルをダウンロード中..." -ForegroundColor Cyan

# テキスト検出モデル (Detection)
$detUrl = "https://paddleocr.bj.bcebos.com/PP-OCRv4/chinese/ch_PP-OCRv4_det_infer.tar"
$detFile = "$modelsDir\ch_PP-OCRv4_det_infer.tar"

Write-Host "検出モデルをダウンロード中..." -ForegroundColor Yellow
Invoke-WebRequest -Uri $detUrl -OutFile $detFile

# テキスト認識モデル (Recognition - Japanese)
$recUrl = "https://paddleocr.bj.bcebos.com/PP-OCRv4/japanese/japan_PP-OCRv4_rec_infer.tar"
$recFile = "$modelsDir\japan_PP-OCRv4_rec_infer.tar"

Write-Host "認識モデル（日本語）をダウンロード中..." -ForegroundColor Yellow
Invoke-WebRequest -Uri $recUrl -OutFile $recFile

Write-Host "ダウンロード完了！" -ForegroundColor Green
Write-Host "次のステップ: tar ファイルを展開してください" -ForegroundColor Cyan
Write-Host "展開後、ONNX ファイル (.onnx) を models/ フォルダに配置してください" -ForegroundColor Cyan

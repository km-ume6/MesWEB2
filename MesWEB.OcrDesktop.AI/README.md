# MesWEB.OcrDesktop.AI - EasyOCR Edition

Python の EasyOCR を使用した OCR デスクトップサーバー (.NET 10 WPF)

## 概要

.NET 10 WPF アプリケーションから Python の EasyOCR を呼び出して、高精度な OCR 処理を提供します。
TCP/JSON プロトコルでクライアントからの OCR リクエストを処理します。

## 技術スタック

- **.NET 10 WPF** - フロントエンド UI
- **Python 3.9+** - EasyOCR バックエンド
- **EasyOCR** - 高精度 OCR ライブラリ
- **TCP/JSON** - クライアント通信プロトコル

## 必要な環境

### 1. Python のインストール

Python 3.9 以上が必要です。

**Windows の場合:**
```sh
# Microsoft Store から Python をインストール
# または公式サイトからダウンロード
https://www.python.org/downloads/
```

**インストール確認:**
```sh
python --version
# Python 3.11.0 などと表示されれば OK
```

### 2. EasyOCR のインストール

```sh
pip install easyocr
```

**日本語モデルのダウンロード（初回実行時に自動）:**
EasyOCR は初回実行時に日本語モデルを自動ダウンロードします（約 100MB）。

**オプション: GPU サポート（NVIDIA GPU がある場合）:**
```sh
pip install torch torchvision torchaudio --index-url https://download.pytorch.org/whl/cu118
pip install easyocr
```

### 3. 環境確認スクリプト

```sh
python -c "import easyocr; print('EasyOCR バージョン:', easyocr.__version__)"
```

## セットアップ

### 1. プロジェクトのビルド

```sh
cd MesWEB.OcrDesktop.AI
dotnet build
```

### 2. 実行

```sh
dotnet run
```

または Visual Studio で実行します。

## 使用方法

### サーバー起動

1. アプリケーションを起動
2. 「サーバー起動」ボタンをクリック
3. ポート 8080 でリスニング開始

### クライアントからのリクエスト

**リクエスト形式 (JSON)**

```json
{
  "ImageBytes": "base64エンコードされた画像データ",
  "FileName": "image.png",
  "Language": "ja"
}
```

**レスポンス形式 (JSON)**

```json
{
  "Status": "Success",
  "ExtractedText": "認識されたテキスト",
  "ErrorMessage": null
}
```

## プロジェクト構成

```
MesWEB.OcrDesktop.AI/
├── MesWEB.OcrDesktop.AI.csproj   # プロジェクトファイル
├── App.xaml / App.xaml.cs         # WPF アプリ
├── MainWindow.xaml / MainWindow.xaml.cs  # TCP サーバー UI
├── Services/
│   └── EasyOcrService.cs         # Python EasyOCR 呼び出しサービス
└── python/
    └── easyocr_service.py        # Python OCR スクリプト
```

## 機能

✅ EasyOCR による高精度 OCR  
✅ 日本語・英語対応  
✅ GPU アクセラレーション対応（オプション）  
✅ TCP/JSON プロトコルでの通信  
✅ 複数クライアント同時接続対応  
✅ リアルタイムステータス表示  

## トラブルシューティング

### Python が見つからない

```
❌ Python が見つかりません
```

**解決方法:**
1. Python 3.9+ をインストール
2. PATH に Python を追加
3. コマンドプロンプトで `python --version` を確認

### EasyOCR がインストールされていない

```
ModuleNotFoundError: No module named 'easyocr'
```

**解決方法:**
```sh
pip install easyocr
```

### GPU エラー

GPU がない環境で GPU モードを使用しようとするとエラーになります。

**解決方法:**
`python/easyocr_service.py` の以下の行を変更：
```python
reader = easyocr.Reader(languages, gpu=False)  # GPU を無効化
```

### ポート 8080 が使用中

**解決方法:**
`MainWindow.xaml.cs` の以下の行を変更：
```csharp
_listener = new TcpListener(IPAddress.Any, 8080); // ← ポート番号を変更
```

## パフォーマンス

### 初回実行

EasyOCR は初回実行時にモデルをダウンロードするため、時間がかかります（約 1-2 分）。

### 2回目以降

モデルがキャッシュされるため、高速に動作します。

### 処理時間（目安）

- CPU のみ: 1-3 秒 / 画像
- GPU 使用: 0.2-0.5 秒 / 画像

## サポートされている言語

EasyOCR は 80 以上の言語をサポートしています：

- 日本語 (`ja`)
- 英語 (`en`)
- 中国語 (`ch_sim`, `ch_tra`)
- 韓国語 (`ko`)
- など

詳細: https://github.com/JaidedAI/EasyOCR

## 参考リンク

- [EasyOCR GitHub](https://github.com/JaidedAI/EasyOCR)
- [EasyOCR ドキュメント](https://www.jaided.ai/easyocr/)
- [Python.NET](https://github.com/pythonnet/pythonnet)

## ライセンス

社内プロジェクト

---

**開発:** 生産技術グループ  
**日付:** 2025-01

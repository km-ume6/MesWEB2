# EasyOCR サーバー統合ガイド

## 📋 概要

MesWEB.ImageCapture の OCR 機能は、以下の優先順位で動作します：

1. **EasyOCR デスクトップサーバー**（推奨）- 高精度
2. **ローカル Tesseract**（フォールバック）- 基本的な認識

---

## 🚀 セットアップ手順

### 1. EasyOCR サーバーの起動

#### サーバーを起動

```powershell
cd MesWEB.OcrDesktop.AI
dotnet run
```

サーバーは自動的にポート `8080` で起動します。

#### サーバーの状態確認

```powershell
cd MesWEB.OcrDesktop.AI.ControlClient
dotnet run STATUS
```

**期待される出力:**

```
========================================
  制御結果
========================================
ステータス: Success
メッセージ: サーバー稼働中 (ポート: 8080)
サーバー状態: 稼働中
ポート: 8080
========================================
```

---

### 2. MesWEB の設定

#### `appsettings.json` を編集

```json
{
  "OcrRemoteHost": "localhost",  // または EasyOCR サーバーのIPアドレス
  "OcrRemotePort": 8080
}
```

#### ネットワーク経由で接続する場合

別のPC上でEasyOCRサーバーを動かしている場合：

```json
{
  "OcrRemoteHost": "192.168.1.104",  // EasyOCRサーバーのIPアドレス
  "OcrRemotePort": 8080
}
```

---

### 3. MesWEB を起動

```powershell
cd MesWEB
dotnet run
```

ブラウザで `http://localhost:5000/image-capture` にアクセス

---

## 🧪 動作テスト

### テストクライアントで確認

```powershell
# EasyOCR サーバーに画像を送信
cd MesWEB.OcrDesktop.AI.TestClient
dotnet run path\to\test_image.png eng
```

**成功例:**

```
========================================
  OCR 結果
========================================
✅ ステータス: Success
📝 認識テキスト (150 文字):
Hello World
Test Image
...
========================================
```

### Blazor アプリから確認

1. `http://localhost:5000/image-capture` にアクセス
2. 画像をアップロードまたはカメラで撮影
3. 「OCR 処理を開始」をクリック

**ログで確認:**

```
[INFO] OCR 処理を開始します: test_image.png
[INFO] リモートOCR成功
[INFO] OCR 完了 (150 文字)
```

---

## 📊 動作モード

### モード 1: EasyOCR サーバー（優先）

- **精度**: ★★★★★
- **速度**: 中速（初回はモデルダウンロードで遅い）
- **対応言語**: 80+ 言語
- **記号認識**: ★★★☆☆（改善中）

### モード 2: ローカル Tesseract（フォールバック）

- **精度**: ★★★☆☆
- **速度**: 高速
- **対応言語**: インストール済み言語のみ
- **記号認識**: ★★☆☆☆

---

## 🔧 トラブルシューティング

### ❌ 問題: 常にローカルTesseractが使われる

**ログ:**

```
[WARN] リモートOCR失敗
[INFO] ローカル処理にフォールバック
```

**原因と解決策:**

| 原因 | 確認方法 | 解決方法 |
|------|---------|---------|
| EasyOCRサーバーが起動していない | `dotnet run STATUS` | サーバーを起動 |
| ポート番号が違う | `appsettings.json` 確認 | ポート番号を `8080` に修正 |
| ファイアウォールでブロック | `telnet localhost 8080` | ファイアウォール設定を確認 |
| IPアドレスが間違っている | `ping 192.168.1.104` | 正しいIPアドレスに修正 |

---

### ❌ 問題: EasyOCRサーバーが起動しない

**エラー:**

```
❌ EasyOCR 初期化エラー: Python が見つかりません
```

**解決方法:**

```powershell
# Python がインストールされているか確認
python --version

# EasyOCR をインストール
pip install easyocr

# サーバーを再起動
cd MesWEB.OcrDesktop.AI
dotnet run
```

---

### ⚠️ 問題: 初回実行が非常に遅い

**これは正常です！**

EasyOCR は初回実行時に以下をダウンロードします：

- 日本語認識モデル（約 100MB）
- テキスト検出モデル（約 50MB）

**対処法:** 初回は **2-3分** 待ってください。2回目以降は高速になります。

---

### ❌ 問題: 記号が認識されない

**原因:** EasyOCR のモデルは記号の認識が弱い

**解決策:**

1. `MesWEB.OcrDesktop.AI\python\easyocr_service.py` で信頼度フィルタリングを調整

```python
CONFIDENCE_THRESHOLD = 0.25  # この値を下げる（例: 0.1）
INCLUDE_LOW_CONFIDENCE = False  # True にすると全て含める
```

2. 画像の前処理を試す

```powershell
cd MesWEB.OcrDesktop.AI
python python\image_preprocessing.py input.png output.png balanced
```

---

## 📁 ファイル構成

```
MesWEB/
├── appsettings.json                      # ← OcrRemoteHost/Port を設定
│
MesWEB.ImageCapture/
├── Services/
│   └── OcrService.cs                     # ← リモートOCR接続ロジック
│
MesWEB.OcrDesktop.AI/
├── MainWindow.xaml.cs                    # EasyOCR サーバー
├── Services/
│   └── EasyOcrService.cs                 # Python 呼び出し
└── python/
    └── easyocr_service.py                # EasyOCR 実行スクリプト
│
MesWEB.OcrDesktop.AI.ControlClient/
└── Program.cs                            # サーバー制御クライアント
│
MesWEB.OcrDesktop.AI.TestClient/
└── Program.cs                            # テストクライアント
```

---

## 🎯 推奨設定

### 開発環境

```json
{
  "OcrRemoteHost": "localhost",
  "OcrRemotePort": 8080
}
```

- EasyOCRサーバーとMesWEBを同じPCで実行
- デバッグが容易

### 本番環境（専用OCRサーバー）

```json
{
  "OcrRemoteHost": "192.168.1.104",
  "OcrRemotePort": 8080
}
```

- 高性能PCでEasyOCRサーバーを実行
- クライアントPCはWebアプリのみ実行

---

## 🔄 サーバー制御コマンド

### ステータス確認

```powershell
dotnet run STATUS
```

### サーバー再起動

```powershell
dotnet run RESTART
```

### サーバー停止

```powershell
dotnet run STOP
```

### サーバー起動

```powershell
dotnet run START
```

---

## 📝 ログの見方

### EasyOCR サーバー（デバッグビルド）

コンソールウィンドウが表示され、リアルタイムでログが出力されます：

```
[DEBUG] Python パス: python
[DEBUG] RecognizeTextAsync 開始: 1991878 bytes, 言語=eng
[PYTHON STDERR] [DEBUG] OCR 完了。結果件数: 83
[DEBUG] OCR 結果: 441 文字
```

### MesWEB（Blazor）

Visual Studio の出力ウィンドウまたはコンソール：

```
[INFO] OCR 処理を開始します: test_image.png
[INFO] リモートOCR成功
[INFO] OCR 完了 (150 文字)
```

---

## 🆘 サポート

問題が解決しない場合、以下の情報を収集してください：

```powershell
# 環境情報
python --version
pip show easyocr
dotnet --version

# EasyOCR サーバーのログ
cd MesWEB.OcrDesktop.AI
dotnet run > server.log 2>&1

# MesWEB のログ
cd MesWEB
dotnet run > webapp.log 2>&1
```

---

## ✅ チェックリスト

統合が完了したら、以下を確認してください：

- [ ] Python がインストールされている（`python --version`）
- [ ] EasyOCR がインストールされている（`pip show easyocr`）
- [ ] EasyOCR サーバーが起動している（`dotnet run STATUS`）
- [ ] `appsettings.json` で `OcrRemoteHost` と `OcrRemotePort` が設定されている
- [ ] MesWEB が起動している
- [ ] `/image-capture` ページにアクセスできる
- [ ] テスト画像でOCR処理が成功する
- [ ] ログに「リモートOCR成功」と表示される

---

**これで EasyOCR デスクトップサーバーの統合が完了です！🎉**

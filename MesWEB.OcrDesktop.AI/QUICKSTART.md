# 🚀 EasyOCR サーバー クイックスタート

## 📋 5分でセットアップ

### ステップ 1: 環境確認

```powershell
cd MesWEB.OcrDesktop.AI
.\setup-check.ps1
```

**このスクリプトが以下を確認します:**
- ✅ Python のインストール
- ✅ pip の動作
- ✅ EasyOCR のインストール状態
- ✅ GPU サポート（オプション）

### ステップ 2: EasyOCR をインストール（未インストールの場合）

```powershell
pip install easyocr
```

### ステップ 3: アプリを起動

```powershell
dotnet run
```

または Visual Studio で `F5` を押します。

### ステップ 4: サーバーを起動

1. GUI が表示されます
2. 「サーバー起動」ボタンをクリック
3. 初回実行時はモデルダウンロードに **1-2分** かかります

---

## 🧪 動作確認

### テストクライアントを使用

```powershell
cd MesWEB.OcrDesktop.AI.TestClient
dotnet run path\to\image.png ja
```

**出力例:**
```
========================================
  OCR 結果
========================================
✅ ステータス: Success
📝 認識テキスト (15 文字):
こんにちは世界
========================================
```

### C# コードから直接テスト

```csharp
using System.Net.Sockets;
using System.Text;
using System.Text.Json;

var client = new TcpClient("localhost", 8080);
var stream = client.GetStream();
var writer = new StreamWriter(stream, Encoding.UTF8) { AutoFlush = true };
var reader = new StreamReader(stream, Encoding.UTF8);

var imageBytes = File.ReadAllBytes("test.png");
var request = new {
    ImageBytes = Convert.ToBase64String(imageBytes),
    Language = "ja"
};

await writer.WriteLineAsync(JsonSerializer.Serialize(request));
var response = await reader.ReadLineAsync();
Console.WriteLine(response);
```

---

## ⚙️ トラブルシューティング

### ❌ Python が見つからない

**原因:** Python がインストールされていないか、PATH に追加されていません。

**解決方法:**
1. [Python 公式サイト](https://www.python.org/downloads/) から Python 3.9+ をダウンロード
2. インストール時に「Add Python to PATH」をチェック
3. コマンドプロンプトを再起動
4. `python --version` で確認

### ❌ EasyOCR がインストールされていない

**解決方法:**
```powershell
pip install easyocr
```

### ❌ ModuleNotFoundError: No module named 'easyocr'

**原因:** 複数の Python がインストールされている可能性があります。

**解決方法:**
```powershell
# Python のパスを確認
where python

# 正しい Python で pip を実行
python -m pip install easyocr
```

### ⚠️ 初回実行が遅い

**これは正常です！**

EasyOCR は初回実行時に以下をダウンロードします:
- 日本語認識モデル (約 100MB)
- テキスト検出モデル (約 50MB)

**ダウンロード先:**
- Windows: `C:\Users\<ユーザー名>\.EasyOCR\model`
- Linux/Mac: `~/.EasyOCR/model`

### ❌ ポート 8080 が使用中

**解決方法:**

`MainWindow.xaml.cs` の以下の行を変更:
```csharp
_listener = new TcpListener(IPAddress.Any, 8081); // ← ポート番号を変更
```

---

## 🎯 よくある質問

### Q1: GPU を使用するには？

**回答:** `python/easyocr_service.py` の以下の行を変更:
```python
reader = easyocr.Reader(languages, gpu=True)  # GPU を有効化
```

**注意:** NVIDIA GPU と CUDA が必要です。

### Q2: 対応している言語は？

**回答:** EasyOCR は 80+ 言語に対応しています。

主な言語:
- 日本語 (`ja`)
- 英語 (`en`)
- 中国語 (`ch_sim`, `ch_tra`)
- 韓国語 (`ko`)
- など

[全リスト](https://github.com/JaidedAI/EasyOCR#supported-languages)

### Q3: 複数言語を同時に認識するには？

**回答:** `python/easyocr_service.py` を編集:
```python
languages = ['ja', 'en', 'ko']  # 複数言語を指定
reader = easyocr.Reader(languages)
```

### Q4: 認識精度を上げるには？

**回答:**
1. 画像を高解像度にする（300 DPI 推奨）
2. コントラストを高くする
3. 背景をシンプルにする
4. GPU を使用する

---

## 📚 次のステップ

1. ✅ [README.md](README.md) - 詳細ドキュメント
2. ✅ [python/easyocr_service.py](python/easyocr_service.py) - カスタマイズ
3. ✅ テストクライアントで動作確認

---

**お疲れ様でした！EasyOCR サーバーが稼働中です 🎉**

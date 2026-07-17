# OCR デバッグガイド

## 🔍 OCR 結果が0文字の場合のデバッグ手順

### ステップ 1: テスト画像を生成

```powershell
cd MesWEB.OcrDesktop.AI

# Pillow をインストール
pip install pillow

# テスト画像を生成
python python/create_test_image.py test_image.png "テスト文字列"
```

### ステップ 2: サーバーを起動（詳細ログ有効）

```powershell
dotnet run
```

**コンソール出力を確認:**
- `[DEBUG]` で始まる行を確認
- `[PYTHON STDOUT]` Python の標準出力
- `[PYTHON STDERR]` Python のエラー出力

### ステップ 3: テストクライアントで画像を送信

```powershell
cd MesWEB.OcrDesktop.AI.TestClient
dotnet run ..\test_image.png ja
```

### ステップ 4: ログを確認

#### サーバー側のログ（重要な項目）

```
[DEBUG] Python パス: python
[DEBUG] スクリプトパス: C:\...\python\easyocr_service.py
[DEBUG] RecognizeTextAsync 開始: 12345 bytes, 言語=ja
[DEBUG] 一時画像パス: C:\...\tmp12345.tmp.png
[DEBUG] 画像ファイル保存完了: 12345 bytes
[PYTHON STDERR] [DEBUG] 画像パス: C:\...\tmp12345.tmp.png
[PYTHON STDERR] [DEBUG] 画像ファイルサイズ: 12345 bytes
[PYTHON STDERR] [DEBUG] EasyOCR リーダーを初期化中...
[PYTHON STDERR] [DEBUG] EasyOCR リーダー初期化完了
[PYTHON STDERR] [DEBUG] OCR 実行中...
[PYTHON STDERR] [DEBUG] OCR 完了。結果件数: 2    ← ★ここが0の場合は画像に問題
[PYTHON STDERR] [DEBUG] 結果 1: テキスト='こんにちは', 信頼度=0.95
[PYTHON STDERR] [DEBUG] 結果 2: テキスト='世界', 信頼度=0.92
[DEBUG] OCR 結果: 10 文字
```

### ステップ 5: 問題の特定

#### 🔴 **結果件数が0の場合**

**原因:** 画像にテキストが含まれていない、または認識できない

**確認事項:**
1. 画像ファイルが正しく送信されているか
   - `画像ファイル保存完了: X bytes` を確認
   - サイズが0でないか確認

2. 画像の内容を確認
   ```powershell
   # 一時ファイルを保持するようにコードを修正して確認
   # または元の画像を直接確認
   explorer test_image.png
   ```

3. 画像の品質
   - 解像度が低すぎないか（最低 300x300 推奨）
   - コントラストが低すぎないか
   - テキストがぼやけていないか

#### 🔴 **Python エラーが出る場合**

**エラー例:**
```
[PYTHON STDERR] ModuleNotFoundError: No module named 'easyocr'
```

**解決方法:**
```powershell
pip install easyocr
```

#### 🔴 **文字化けする場合**

**原因:** エンコーディング問題

**確認:**
- Python スクリプトの先頭に UTF-8 設定があるか
- .NET 側で `StandardOutputEncoding = Encoding.UTF8` が設定されているか

### ステップ 6: Python スクリプトを直接実行してテスト

```powershell
cd MesWEB.OcrDesktop.AI\bin\Debug\net10.0-windows10.0.22621.0

python python\easyocr_service.py "..\..\..\test_image.png" "result.json" "ja"

# 結果を確認
type result.json
```

**正常な出力:**
```json
{
  "text": "こんにちは世界\nHello World",
  "success": true,
  "details": {
    "total_blocks": 2,
    "total_chars": 18
  }
}
```

### ステップ 7: 画像の前処理を試す

テキストが検出されない場合、画像を前処理してみます：

```python
# グレースケール化、コントラスト強調
from PIL import Image, ImageEnhance

img = Image.open('test_image.png')
img = img.convert('L')  # グレースケール
enhancer = ImageEnhance.Contrast(img)
img = enhancer.enhance(2.0)  # コントラスト2倍
img.save('test_image_enhanced.png')
```

## 📊 チェックリスト

- [ ] Python がインストールされているか (`python --version`)
- [ ] EasyOCR がインストールされているか (`pip show easyocr`)
- [ ] テスト画像が生成できるか
- [ ] サーバーが起動するか
- [ ] テストクライアントが接続できるか
- [ ] Python スクリプトが直接実行できるか
- [ ] ログに `[DEBUG] OCR 完了。結果件数: X` が表示されるか
- [ ] 結果件数が0より大きいか

## 🆘 よくある問題と解決方法

| 問題 | 原因 | 解決方法 |
|------|------|----------|
| OCR 結果が0文字 | 画像にテキストがない | テスト画像で確認 |
| Python エラー | EasyOCR 未インストール | `pip install easyocr` |
| 文字化け | エンコーディング問題 | UTF-8 設定を確認 |
| 処理が遅い | CPU モード | GPU を有効化（オプション） |
| モデルダウンロード失敗 | ネットワーク問題 | 再実行またはプロキシ設定 |

## 📝 サポート情報の収集

問題が解決しない場合、以下の情報を収集してください：

```powershell
# 環境情報
python --version
pip show easyocr
dotnet --version

# ログを保存
dotnet run > server.log 2>&1

# テスト実行
cd MesWEB.OcrDesktop.AI.TestClient
dotnet run test_image.png ja > client.log 2>&1
```

---

**デバッグ完了後、本番環境では詳細ログを無効化することを推奨します。**

# EasyOCR サーバー - UI なし版

## 変更点

### 自動起動
- アプリ起動時に自動でサーバー開始
- アプリ終了時に自動でサーバー停止

### TCP 制御コマンド

サーバーを TCP 経由で制御できます：

| コマンド | 説明 |
|---------|------|
| `STATUS` | サーバーのステータスを確認 |
| `START` | サーバーを起動 |
| `STOP` | サーバーを停止 |
| `RESTART` | サーバーを再起動 |

## 使用方法

### サーバー起動

```powershell
cd MesWEB.OcrDesktop.AI
dotnet run
```

→ 自動的にポート 8080 でサーバーが起動します

### 制御クライアントの使用

```powershell
# ステータス確認
cd MesWEB.OcrDesktop.AI.ControlClient
dotnet run STATUS

# サーバー再起動
dotnet run RESTART

# サーバー停止
dotnet run STOP

# サーバー起動
dotnet run START
```

### 制御コマンドの例（C#）

```csharp
using System.Net.Sockets;
using System.Text;
using System.Text.Json;

var client = new TcpClient("localhost", 8080);
var stream = client.GetStream();
var writer = new StreamWriter(stream, Encoding.UTF8) { AutoFlush = true };
var reader = new StreamReader(stream, Encoding.UTF8);

// ステータス確認
var request = new { Command = "STATUS" };
await writer.WriteLineAsync(JsonSerializer.Serialize(request));
var response = await reader.ReadLineAsync();
Console.WriteLine(response);
```

### 制御レスポンス形式

```json
{
  "Status": "Success",
  "Message": "サーバー稼働中 (ポート: 8080)",
  "ServerRunning": true,
  "Port": 8080
}
```

## OCR リクエスト

通常の OCR リクエストは従来通り：

```csharp
var request = new {
    ImageBytes = Convert.ToBase64String(imageBytes),
    Language = "ja"
};
await writer.WriteLineAsync(JsonSerializer.Serialize(request));
```

## XAML 手動修正が必要

`MainWindow.xaml` から以下を削除してください：

```xml
<!-- この部分を削除 -->
<StackPanel Grid.Row="1" Orientation="Horizontal" HorizontalAlignment="Center" Margin="0,0,0,20">
    <Button x:Name="StartButton" 
            Content="サーバー起動" 
            Click="StartButton_Click" 
            .../>
    <Button x:Name="StopButton" 
            Content="サーバー停止" 
            Click="StopButton_Click" 
            .../>
</StackPanel>
```

ステータス表示のみを残してください。

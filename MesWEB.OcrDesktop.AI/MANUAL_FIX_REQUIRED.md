# ⚠️ 手動修正が必要: MainWindow.xaml

## 問題

`MesWEB.OcrDesktop.AI\MainWindow.xaml` に古いボタンイベントハンドラが残っており、ビルドエラーが発生しています。

```
CS1061: 'MainWindow' に 'StartButton_Click' の定義が含まれておらず...
CS1061: 'MainWindow' に 'StopButton_Click' の定義が含まれておらず...
```

---

## 🔧 修正手順

### Visual Studio で MainWindow.xaml を開く

ファイルパス: `MesWEB.OcrDesktop.AI\MainWindow.xaml`

### 以下の内容に置き換える

```xaml
<Window x:Class="MesWEB.OcrDesktop.AI.MainWindow"
        xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
        xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
        Title="EasyOCR サーバー" Height="500" Width="700"
        WindowStartupLocation="CenterScreen">
    <Grid>
        <Grid.RowDefinitions>
            <RowDefinition Height="Auto"/>
            <RowDefinition Height="*"/>
        </Grid.RowDefinitions>

        <!-- ヘッダー -->
        <Border Grid.Row="0" Background="#2196F3" Padding="15">
            <StackPanel>
                <TextBlock Text="EasyOCR サーバー" 
                          FontSize="24" 
                          FontWeight="Bold" 
                          Foreground="White"/>
                <TextBlock Text="Python EasyOCR を使用した高精度 OCR サービス" 
                          FontSize="12" 
                          Foreground="White" 
                          Opacity="0.9"
                          Margin="0,5,0,0"/>
            </StackPanel>
        </Border>

        <!-- ステータス表示エリア -->
        <Border Grid.Row="1" Background="#f5f5f5" Padding="20">
            <ScrollViewer VerticalScrollBarVisibility="Auto">
                <TextBlock x:Name="StatusText" 
                          FontFamily="Consolas" 
                          FontSize="13"
                          TextWrapping="Wrap"
                          Foreground="#333"
                          Text="起動中..."/>
            </ScrollViewer>
        </Border>
    </Grid>
</Window>
```

### 保存して再ビルド

```powershell
dotnet build
```

---

## ✅ 修正完了後の動作

- アプリ起動時に自動でサーバー開始
- UI はステータス表示のみ（ボタンなし）
- TCP 経由で制御コマンドを受け付け（START/STOP/RESTART/STATUS）

---

## 📝 参考

- [UI_MIGRATION.md](UI_MIGRATION.md) - UI 変更の詳細
- [INTEGRATION.md](INTEGRATION.md) - Blazor アプリとの統合ガイド

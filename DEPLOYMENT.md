# MesWEB デプロイメントガイド

## 概要
このドキュメントでは、MesWEB アプリケーションを開発環境とサーバー環境にデプロイする方法を説明します。

## 重要な変更：tessdata の自動初期化

**v2.0 以降、アプリケーション起動時に tessdata が自動的にダウンロードされるようになりました。**

- ✅ 起動時に tessdata を自動検出
- ✅ 見つからない場合は GitHub から自動ダウンロード
- ✅ 複数の保存場所をサポート

手動でセットアップする必要はもうありません！

## 開発環境での実行

```bash
# tessdata をセットアップする必要なし！
# アプリを実行するだけで自動的にダウンロードされます

dotnet run --project MesWEB
```

**初回起動時の注意：**
- インターネット接続が必要です（GitHub からダウンロード）
- 初回は 5-10 分かかることがあります
- ダウンロード中はコンソールに進捗が表示されます

## サーバー環境へのデプロイ

### 自動デプロイスクリプト（推奨）

PowerShell スクリプトでワンコマンドデプロイ：

```powershell
# 1. Release ビルド
dotnet build -c Release

# 2. Publish
dotnet publish -c Release -o publish

# 3. IIS にデプロイ（自動でサイト作成、権限設定）
.\deploy-to-iis.ps1 -SiteName "MesWEB" -DeployPath "C:\inetpub\MesWEB" -PublishPath ".\publish"
```

このスクリプトが自動的に以下を実行：
- ✅ IIS サイトの確認
- ✅ ファイルをコピー
- ✅ ログディレクトリを作成
- ✅ IIS AppPool に権限を付与

### 手動デプロイ

スクリプトが使えない場合の手動手順：

#### 手順1: ビルド＆発行

```bash
dotnet build -c Release
dotnet publish -c Release -o publish
```

#### 手順2: IIS にファイルをコピー

```powershell
Copy-Item -Path "publish\*" -Destination "C:\inetpub\MesWEB" -Recurse -Force
```

#### 手順3: IIS マネージャーで設定

1. **新しいサイトを作成**
   - サイト名: `MesWEB`
   - 物理パス: `C:\inetpub\MesWEB`
   - ポート: `80` または `443`（HTTPS）

2. **アプリケーション プールの設定**
   - .NET CLR バージョン: `マネージドコードなし`（ASP.NET Core のため）
   - 32ビット アプリケーションの有効化: `False`

3. **ホストファイル設定（web.config）**

```xml
<?xml version="1.0" encoding="utf-8"?>
<configuration>
  <system.webServer>
    <handlers>
      <add name="aspNetCore" path="*" verb="*" modules="AspNetCoreModuleV2" resourceType="Unspecified" />
    </handlers>
    <aspNetCore processPath="dotnet" arguments=".\MesWEB.dll" stdoutLogEnabled="true" stdoutLogFile=".\logs\stdout" hostingModel="outofprocess" />
  </system.webServer>
</configuration>
```

#### 手順4: ログディレクトリを作成

```powershell
New-Item -ItemType Directory -Path "C:\inetpub\MesWEB\logs" -Force
```

#### 手順5: 権限を付与

```powershell
$folderPath = "C:\inetpub\MesWEB"
$acl = Get-Acl $folderPath
$ar = New-Object System.Security.AccessControl.FileSystemAccessRule(
    "IIS AppPool\MesWEB",
    "Modify",
    "ContainerInherit,ObjectInherit",
    "None",
    "Allow"
)
$acl.SetAccessRule($ar)
Set-Acl -Path $folderPath -AclObject $acl
```

### 初回起動時

1. IIS マネージャーでサイトを開始
2. ブラウザで http://localhost にアクセス
3. **重要**: tessdata の自動ダウンロードが実行されます（5-10分）
4. ログは `C:\inetpub\MesWEB\logs\stdout.log` に出力されます

## トラブルシューティング

### HTTP 500.30 エラーが出た場合

自動トラブルシューティングスクリプトを実行：

```powershell
.\troubleshoot-iis.ps1 -SiteName "MesWEB" -DeployPath "C:\inetpub\MesWEB"
```

このスクリプトが以下をチェック：
- ✓ IIS サイトの状態
- ✓ stdout ログの内容
- ✓ web.config の設定
- ✓ 必要なファイルの確認
- ✓ tessdata の状態
- ✓ アプリケーション プールの設定
- ✓ イベントログのエラー

### エラー: "tessdata の初期化に失敗しました"

**原因：**
- インターネット接続がない
- GitHub へのアクセスがファイアウォールでブロックされている
- ディスク容量が不足している

**対処：**

1. **インターネット接続を確認**
   ```powershell
   Test-NetConnection github.com -Port 443
   ```

2. **ファイアウォール設定を確認**
   - GitHub へのアクセスを許可する必要があります
   - URL: `https://github.com/tesseract-ocr/tessdata/raw/main`

3. **手動でダウンロード**
   ```powershell
   # GitHub から直接ダウンロード
   # https://github.com/tesseract-ocr/tessdata/raw/main/eng.traineddata
   # https://github.com/tesseract-ocr/tessdata/raw/main/jpn.traineddata
   
   # C:\inetpub\MesWEB\tessdata\ フォルダに配置
   ```

4. **ディスク容量を確認**
   - tessdata には 450+ MB 必要

### エラー: "スクリプトの実行が無効"

PowerShell スクリプトの実行ポリシーを変更：

```powershell
# 現在のセッションのみ許可
Set-ExecutionPolicy -ExecutionPolicy Bypass -Scope Process

# または、スクリプトファイルの署名を確認
Get-Content .\deploy-to-iis.ps1 | Unblock-File
```

### ログファイルを確認

```powershell
# stdout ログを表示
Get-Content "C:\inetpub\MesWEB\logs\stdout.log" -Tail 100

# リアルタイムで監視
Get-Content "C:\inetpub\MesWEB\logs\stdout.log" -Wait
```

### アプリケーション プールを再開

```powershell
# アプリケーション プールを再開
Import-Module WebAdministration
Restart-WebAppPool -Name "MesWEB"

# 状態を確認
Get-WebAppPool -Name "MesWEB" | Select-Object Name, State
```

## よくある問題

| 問題 | 原因 | 対処 |
|------|------|------|
| HTTP 500.30 | アプリ起動エラー | `troubleshoot-iis.ps1` を実行して stdout ログを確認 |
| tessdata が見つからない | ダウンロード失敗 | GitHub へのアクセスを確認、手動ダウンロード |
| アクセスが拒否される | 権限不足 | `deploy-to-iis.ps1` で権限を設定 |
| タイムアウト | ダウンロード時間が長い | 初回は5-10分かかる、待機が必要 |

## 参考資料

- [Tesseract OCR GitHub](https://github.com/tesseract-ocr/)
- [tessdata Download](https://github.com/tesseract-ocr/tessdata)
- [ASP.NET Core ホスティング](https://docs.microsoft.com/en-us/aspnet/core/host-and-deploy/)
- [IIS での ASP.NET Core](https://docs.microsoft.com/en-us/aspnet/core/host-and-deploy/iis/)

# データ復元スクリプト (.bak ファイルから)
# PowerShell で実行してください

# 設定
$backupFilePath = "C:\Backup\CGNotes.bak"  # ← バックアップファイルのパスを変更
$sqlServerInstance = "192.168.11.15,1433"  # ← SQL Serverインスタンスを変更
$username = "maru"  # ← ユーザー名を変更
$password = "627864"  # ← パスワードを変更
$currentDatabase = "CGNotes"  # ← 本番データベース名を変更
$backupDatabase = "CGNotes_Backup"
$dataFilePath = "C:\SQLData\CGNotes_Backup.mdf"  # ← データファイルの保存先を変更
$logFilePath = "C:\SQLData\CGNotes_Backup_log.ldf"  # ← ログファイルの保存先を変更

Write-Host "=== データ復元スクリプト開始 ===" -ForegroundColor Green

# SQL接続文字列
$connectionString = "Server=$sqlServerInstance;User Id=$username;Password=$password;TrustServerCertificate=True;"

# ステップ1: バックアップファイルの論理名を取得
Write-Host "ステップ1: バックアップファイルの内容を確認中..." -ForegroundColor Yellow
$sqlFileList = @"
RESTORE FILELISTONLY FROM DISK = '$backupFilePath';
"@

try {
    $fileList = Invoke-Sqlcmd -ConnectionString $connectionString -Query $sqlFileList
    $logicalDataName = $fileList[0].LogicalName
    $logicalLogName = $fileList[1].LogicalName
    Write-Host "  データファイル論理名: $logicalDataName" -ForegroundColor Cyan
    Write-Host "  ログファイル論理名: $logicalLogName" -ForegroundColor Cyan
} catch {
    Write-Host "エラー: バックアップファイルの読み込みに失敗しました。" -ForegroundColor Red
    Write-Host $_.Exception.Message
    exit 1
}

# ステップ2: バックアップをCGNotes_Backupに復元
Write-Host "ステップ2: バックアップデータベースを復元中..." -ForegroundColor Yellow
$sqlRestore = @"
RESTORE DATABASE [$backupDatabase]
FROM DISK = '$backupFilePath'
WITH 
    MOVE '$logicalDataName' TO '$dataFilePath',
    MOVE '$logicalLogName' TO '$logFilePath',
    REPLACE,
    RECOVERY;
"@

try {
    Invoke-Sqlcmd -ConnectionString $connectionString -Query $sqlRestore -QueryTimeout 300
    Write-Host "  復元完了: $backupDatabase" -ForegroundColor Green
} catch {
    Write-Host "エラー: データベース復元に失敗しました。" -ForegroundColor Red
    Write-Host $_.Exception.Message
    exit 1
}

# ステップ3: データを移行
Write-Host "ステップ3: GrowthNoteParameters にデータを移行中..." -ForegroundColor Yellow
$sqlMigrateParams = @"
USE [$currentDatabase];

INSERT INTO GrowthNoteParameters (
    CGNoteId, GrowthStartTime, ShoulderEndTime, NeckingVoltage, GrowthVoltage,
    OutputVoltage, OutputCurrent, SeedHeightPosition, GravityCenterWC, HeightPositionWC,
    RingHeightPosition, StartPullingSpeed, ShoulderEndPullingSpeed, LastPullingSpeed,
    FirstRotationalSpeed, ShoulderEndRotationalSpeed, LastRotationalSpeed, DeltaT
)
SELECT 
    g.Id, b.GrowthStartTime, b.ShoulderEndTime, b.NeckingVoltage, b.GrowthVoltage,
    b.OutputVoltage, b.OutputCurrent, b.SeedHeightPosition, b.GravityCenterWC, b.HeightPositionWC,
    b.RingHeightPosition, b.StartPullingSpeed, b.ShoulderEndPullingSpeed, b.LastPullingSpeed,
    b.FirstRotationalSpeed, b.ShoulderEndRotationalSpeed, b.LastRotationalSpeed, b.DeltaT
FROM [$currentDatabase].dbo.GrowthNotes g
INNER JOIN [$backupDatabase].dbo.GrowthNotes b ON g.Id = b.Id
WHERE NOT EXISTS (SELECT 1 FROM [$currentDatabase].dbo.GrowthNoteParameters p WHERE p.CGNoteId = g.Id);

SELECT @@ROWCOUNT AS InsertedCount;
"@

try {
    $result = Invoke-Sqlcmd -ConnectionString $connectionString -Query $sqlMigrateParams
    Write-Host "  挿入件数: $($result.InsertedCount)" -ForegroundColor Green
} catch {
    Write-Host "エラー: GrowthNoteParameters へのデータ移行に失敗しました。" -ForegroundColor Red
    Write-Host $_.Exception.Message
}

Write-Host "ステップ4: GrowthNoteInsulations にデータを移行中..." -ForegroundColor Yellow
$sqlMigrateInsulation = @"
USE [$currentDatabase];

INSERT INTO GrowthNoteInsulations (
    CGNoteId, InsideInsulationComposition, BottomInsulationComposition,
    FurnaceCondition1, FurnaceCondition2, UsingDisk, LiquidLevel
)
SELECT 
    g.Id, b.InsideInsulationComposition, b.BottomInsulationComposition,
    b.FurnaceCondition1, b.FurnaceCondition2, b.UsingDisk, b.LiquidLevel
FROM [$currentDatabase].dbo.GrowthNotes g
INNER JOIN [$backupDatabase].dbo.GrowthNotes b ON g.Id = b.Id
WHERE NOT EXISTS (SELECT 1 FROM [$currentDatabase].dbo.GrowthNoteInsulations i WHERE i.CGNoteId = g.Id);

SELECT @@ROWCOUNT AS InsertedCount;
"@

try {
    $result = Invoke-Sqlcmd -ConnectionString $connectionString -Query $sqlMigrateInsulation
    Write-Host "  挿入件数: $($result.InsertedCount)" -ForegroundColor Green
} catch {
    Write-Host "エラー: GrowthNoteInsulations へのデータ移行に失敗しました。" -ForegroundColor Red
    Write-Host $_.Exception.Message
}

# ステップ5: データ確認
Write-Host "ステップ5: データを確認中..." -ForegroundColor Yellow
$sqlVerify = @"
USE [$currentDatabase];

SELECT 
    'GrowthNotes' AS TableName, COUNT(*) AS Count FROM GrowthNotes
UNION ALL
SELECT 'GrowthNoteParameters', COUNT(*) FROM GrowthNoteParameters
UNION ALL
SELECT 'GrowthNoteInsulations', COUNT(*) FROM GrowthNoteInsulations;
"@

try {
    $verification = Invoke-Sqlcmd -ConnectionString $connectionString -Query $sqlVerify
    Write-Host "データ件数:" -ForegroundColor Cyan
    $verification | Format-Table -AutoSize
} catch {
    Write-Host "警告: データ確認に失敗しました。" -ForegroundColor Yellow
}

# ステップ6: バックアップデータベースを削除
Write-Host "ステップ6: 一時データベースを削除中..." -ForegroundColor Yellow
$sqlCleanup = "DROP DATABASE [$backupDatabase];"

try {
    Invoke-Sqlcmd -ConnectionString $connectionString -Query $sqlCleanup
    Write-Host "  削除完了: $backupDatabase" -ForegroundColor Green
} catch {
    Write-Host "警告: 一時データベースの削除に失敗しました。手動で削除してください。" -ForegroundColor Yellow
}

Write-Host "=== データ復元完了 ===" -ForegroundColor Green

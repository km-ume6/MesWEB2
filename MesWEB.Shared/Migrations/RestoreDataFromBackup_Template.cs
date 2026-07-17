#nullable disable

using Microsoft.EntityFrameworkCore.Migrations;

namespace MesWEB.Shared.Migrations
{
    /// <inheritdoc />
    public partial class RestoreDataFromBackup : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // バックアップテーブル名を指定（実際の名前に変更してください）
            var backupTableName = "GrowthNotes_Backup_20251209";  // ← ここを変更

            // GrowthNoteParameters にデータを復元
            migrationBuilder.Sql($@"
                INSERT INTO GrowthNoteParameters (
                    CGNoteId, GrowthStartTime, ShoulderEndTime, NeckingVoltage, GrowthVoltage,
                    OutputVoltage, OutputCurrent, SeedHeightPosition, GravityCenterWC, HeightPositionWC,
                    RingHeightPosition, StartPullingSpeed, ShoulderEndPullingSpeed, LastPullingSpeed,
                    FirstRotationalSpeed, ShoulderEndRotationalSpeed, LastRotationalSpeed, DeltaT
                )
                SELECT 
                    Id, GrowthStartTime, ShoulderEndTime, NeckingVoltage, GrowthVoltage,
                    OutputVoltage, OutputCurrent, SeedHeightPosition, GravityCenterWC, HeightPositionWC,
                    RingHeightPosition, StartPullingSpeed, ShoulderEndPullingSpeed, LastPullingSpeed,
                    FirstRotationalSpeed, ShoulderEndRotationalSpeed, LastRotationalSpeed, DeltaT
                FROM {backupTableName}
                WHERE Id IN (SELECT Id FROM GrowthNotes)
                AND NOT EXISTS (SELECT 1 FROM GrowthNoteParameters WHERE CGNoteId = {backupTableName}.Id)
            ");

            // GrowthNoteInsulations にデータを復元
            migrationBuilder.Sql($@"
                INSERT INTO GrowthNoteInsulations (
                    CGNoteId, InsideInsulationComposition, BottomInsulationComposition,
                    FurnaceCondition1, FurnaceCondition2, UsingDisk, LiquidLevel
                )
                SELECT 
                    Id, InsideInsulationComposition, BottomInsulationComposition,
                    FurnaceCondition1, FurnaceCondition2, UsingDisk, LiquidLevel
                FROM {backupTableName}
                WHERE Id IN (SELECT Id FROM GrowthNotes)
                AND NOT EXISTS (SELECT 1 FROM GrowthNoteInsulations WHERE CGNoteId = {backupTableName}.Id)
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // ロールバック時はデータを削除
            migrationBuilder.Sql("DELETE FROM GrowthNoteParameters");
            migrationBuilder.Sql("DELETE FROM GrowthNoteInsulations");
        }
    }
}

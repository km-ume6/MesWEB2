#nullable disable

using Microsoft.EntityFrameworkCore.Migrations;

namespace MesWEB.Shared.Migrations
{
    /// <inheritdoc />
    public partial class NormalizeGrowthNoteSchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // ステップ1: 新しいテーブルを作成
            migrationBuilder.CreateTable(
                name: "GrowthNoteParameters",
                columns: table => new
                {
                    ParametersId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CGNoteId = table.Column<int>(type: "int", nullable: false),
                    GrowthStartTime = table.Column<TimeSpan>(type: "time", nullable: true),
                    ShoulderEndTime = table.Column<TimeSpan>(type: "time", nullable: true),
                    NeckingVoltage = table.Column<float>(type: "real", nullable: true),
                    GrowthVoltage = table.Column<float>(type: "real", nullable: true),
                    OutputVoltage = table.Column<int>(type: "int", nullable: true),
                    OutputCurrent = table.Column<int>(type: "int", nullable: true),
                    SeedHeightPosition = table.Column<float>(type: "real", nullable: true),
                    GravityCenterWC = table.Column<string>(type: "nvarchar(16)", maxLength: 16, nullable: true),
                    HeightPositionWC = table.Column<string>(type: "nvarchar(16)", maxLength: 16, nullable: true),
                    RingHeightPosition = table.Column<string>(type: "nvarchar(16)", maxLength: 16, nullable: true),
                    StartPullingSpeed = table.Column<float>(type: "real", nullable: true),
                    ShoulderEndPullingSpeed = table.Column<float>(type: "real", nullable: true),
                    LastPullingSpeed = table.Column<float>(type: "real", nullable: true),
                    FirstRotationalSpeed = table.Column<float>(type: "real", nullable: true),
                    ShoulderEndRotationalSpeed = table.Column<float>(type: "real", nullable: true),
                    LastRotationalSpeed = table.Column<float>(type: "real", nullable: true),
                    DeltaT = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GrowthNoteParameters", x => x.ParametersId);
                    table.ForeignKey(
                        name: "FK_GrowthNoteParameters_GrowthNotes_CGNoteId",
                        column: x => x.CGNoteId,
                        principalTable: "GrowthNotes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "GrowthNoteInsulations",
                columns: table => new
                {
                    InsulationId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CGNoteId = table.Column<int>(type: "int", nullable: false),
                    InsideInsulationComposition = table.Column<string>(type: "nvarchar(8)", maxLength: 8, nullable: true),
                    BottomInsulationComposition = table.Column<string>(type: "nvarchar(8)", maxLength: 8, nullable: true),
                    FurnaceCondition1 = table.Column<string>(type: "nvarchar(16)", maxLength: 16, nullable: true),
                    FurnaceCondition2 = table.Column<string>(type: "nvarchar(16)", maxLength: 16, nullable: true),
                    UsingDisk = table.Column<string>(type: "nvarchar(16)", maxLength: 16, nullable: true),
                    LiquidLevel = table.Column<string>(type: "nvarchar(16)", maxLength: 16, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GrowthNoteInsulations", x => x.InsulationId);
                    table.ForeignKey(
                        name: "FK_GrowthNoteInsulations_GrowthNotes_CGNoteId",
                        column: x => x.CGNoteId,
                        principalTable: "GrowthNotes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            // ステップ2: Remarks列のサイズを拡張（既に削除されていない場合のみ）
            migrationBuilder.Sql(@"
                IF EXISTS (SELECT * FROM INFORMATION_SCHEMA.COLUMNS 
                          WHERE TABLE_NAME = 'GrowthNotes' AND COLUMN_NAME = 'Remarks' 
                          AND CHARACTER_MAXIMUM_LENGTH = 128)
                BEGIN
                    ALTER TABLE GrowthNotes ALTER COLUMN Remarks nvarchar(256) NULL;
                END
            ");

            // インデックスを作成
            migrationBuilder.CreateIndex(
                name: "IX_GrowthNoteParameters_CGNoteId",
                table: "GrowthNoteParameters",
                column: "CGNoteId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_GrowthNoteInsulations_CGNoteId",
                table: "GrowthNoteInsulations",
                column: "CGNoteId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // テーブルを削除
            migrationBuilder.DropTable(name: "GrowthNoteParameters");
            migrationBuilder.DropTable(name: "GrowthNoteInsulations");

            // 列を復元
            migrationBuilder.AddColumn<string>(name: "BottomInsulationComposition", table: "GrowthNotes", type: "nvarchar(8)", maxLength: 8, nullable: true);
            migrationBuilder.AddColumn<int>(name: "DeltaT", table: "GrowthNotes", type: "int", nullable: true);
            migrationBuilder.AddColumn<float>(name: "FirstRotationalSpeed", table: "GrowthNotes", type: "real", nullable: true);
            migrationBuilder.AddColumn<string>(name: "FurnaceCondition1", table: "GrowthNotes", type: "nvarchar(16)", maxLength: 16, nullable: true);
            migrationBuilder.AddColumn<string>(name: "FurnaceCondition2", table: "GrowthNotes", type: "nvarchar(16)", maxLength: 16, nullable: true);
            migrationBuilder.AddColumn<string>(name: "GravityCenterWC", table: "GrowthNotes", type: "nvarchar(16)", maxLength: 16, nullable: true);
            migrationBuilder.AddColumn<TimeSpan>(name: "GrowthStartTime", table: "GrowthNotes", type: "time", nullable: true);
            migrationBuilder.AddColumn<float>(name: "GrowthVoltage", table: "GrowthNotes", type: "real", nullable: true);
            migrationBuilder.AddColumn<string>(name: "HeightPositionWC", table: "GrowthNotes", type: "nvarchar(16)", maxLength: 16, nullable: true);
            migrationBuilder.AddColumn<string>(name: "InsideInsulationComposition", table: "GrowthNotes", type: "nvarchar(8)", maxLength: 8, nullable: true);
            migrationBuilder.AddColumn<float>(name: "LastPullingSpeed", table: "GrowthNotes", type: "real", nullable: true);
            migrationBuilder.AddColumn<float>(name: "LastRotationalSpeed", table: "GrowthNotes", type: "real", nullable: true);
            migrationBuilder.AddColumn<string>(name: "LiquidLevel", table: "GrowthNotes", type: "nvarchar(16)", maxLength: 16, nullable: true);
            migrationBuilder.AddColumn<float>(name: "NeckingVoltage", table: "GrowthNotes", type: "real", nullable: true);
            migrationBuilder.AddColumn<int>(name: "OutputCurrent", table: "GrowthNotes", type: "int", nullable: true);
            migrationBuilder.AddColumn<int>(name: "OutputVoltage", table: "GrowthNotes", type: "int", nullable: true);
            migrationBuilder.AddColumn<string>(name: "RingHeightPosition", table: "GrowthNotes", type: "nvarchar(16)", maxLength: 16, nullable: true);
            migrationBuilder.AddColumn<float>(name: "SeedHeightPosition", table: "GrowthNotes", type: "real", nullable: true);
            migrationBuilder.AddColumn<float>(name: "ShoulderEndPullingSpeed", table: "GrowthNotes", type: "real", nullable: true);
            migrationBuilder.AddColumn<float>(name: "ShoulderEndRotationalSpeed", table: "GrowthNotes", type: "real", nullable: true);
            migrationBuilder.AddColumn<TimeSpan>(name: "ShoulderEndTime", table: "GrowthNotes", type: "time", nullable: true);
            migrationBuilder.AddColumn<float>(name: "StartPullingSpeed", table: "GrowthNotes", type: "real", nullable: true);
            migrationBuilder.AddColumn<string>(name: "UsingDisk", table: "GrowthNotes", type: "nvarchar(16)", maxLength: 16, nullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Remarks",
                table: "GrowthNotes",
                type: "nvarchar(128)",
                maxLength: 128,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(256)",
                oldMaxLength: 256,
                oldNullable: true);
        }
    }
}

using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MesWEB.Shared.Data
{
    /// <summary>
    /// 結晶育成の操作パラメータ（電圧、速度、回転数など）
    /// </summary>
    public class GrowthNoteParameters
    {
        [Key]
        public int ParametersId { get; set; }

        [Required]
        public int CGNoteId { get; set; }

        // 時間関連
        public TimeSpan? GrowthStartTime { get; set; }
        public TimeSpan? ShoulderEndTime { get; set; }

        // 電源関連
        public float? NeckingVoltage { get; set; }
        public float? GrowthVoltage { get; set; }
        public int? OutputVoltage { get; set; }
        public int? OutputCurrent { get; set; }

        // 位置関連
        public float? SeedHeightPosition { get; set; }
        [StringLength(16)]
        public string? GravityCenterWC { get; set; }
        [StringLength(16)]
        public string? HeightPositionWC { get; set; }
        [StringLength(16)]
        public string? RingHeightPosition { get; set; }

        // 引上げ速度関連
        public float? StartPullingSpeed { get; set; }
        public float? ShoulderEndPullingSpeed { get; set; }
        public float? LastPullingSpeed { get; set; }

        // 回転数関連
        public float? FirstRotationalSpeed { get; set; }
        public float? ShoulderEndRotationalSpeed { get; set; }
        public float? LastRotationalSpeed { get; set; }

        // その他
        public int? DeltaT { get; set; }

        // ナビゲーション
        [ForeignKey(nameof(CGNoteId))]
        public GrowthNoteItem GrowthNote { get; set; } = null!;
    }
}

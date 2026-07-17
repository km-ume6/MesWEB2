using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MesWEB.Shared.Data
{
    /// <summary>
    /// 結晶育成の基本記録情報（正規化後）
    /// </summary>
    public class GrowthNoteItem
    {
        [Key]
        [Column("Id")]
        public int CGNoteId { get; set; }

        // メタデータ
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // 基本情報
        public DateTime? CrystalGrowthDate { get; set; }
        [StringLength(8)]
        public string? Operator { get; set; }
        [StringLength(8)]
        public string? MachineName { get; set; }

        // 添加情報
        [StringLength(8)]
        public string? Dopants { get; set; }
        public float? DopantAmount { get; set; }
        public int? ppm { get; set; }

        // ロット番号
        [StringLength(24)]
        public string? CrystalLot { get; set; }

        // ペレット情報
        public int? PelletFeed { get; set; }
        [StringLength(8)]
        public string? PelletLot1 { get; set; }
        public int? Pellet1Feed { get; set; }
        [StringLength(8)]
        public string? PelletLot2 { get; set; }
        public int? Pellet2Feed { get; set; }
        [StringLength(8)]
        public string? PelletLot3 { get; set; }
        public int? Pellet3Feed { get; set; }

        // カレット情報
        public int? CulletFeed { get; set; }
        [StringLength(16)]
        public string? CulletLot1 { get; set; }
        public int? Cullet1Feed { get; set; }
        [StringLength(16)]
        public string? CulletLot2 { get; set; }
        public int? Cullet2Feed { get; set; }
        [StringLength(16)]
        public string? CulletLot3 { get; set; }
        public int? Cullet3Feed { get; set; }

        // 坩堝情報
        [StringLength(8)]
        public string? CrucibleName { get; set; }
        public int? CrucibleCount { get; set; }

        // 結晶寸法
        public int? HeadDiameter { get; set; }
        public int? TailDiameter { get; set; }

        // 備考
        [StringLength(256)]
        public string? Remarks { get; set; }

        // ナビゲーションプロパティ（1:1リレーション）
        public GrowthNoteParameters? Parameters { get; set; }
        public GrowthNoteInsulation? Insulation { get; set; }
    }
}

using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MesWEB.Shared.Data
{
    /// <summary>
    /// 結晶育成の断熱材・炉関連情報
    /// </summary>
    public class GrowthNoteInsulation
    {
        [Key]
        public int InsulationId { get; set; }

        [Required]
        public int CGNoteId { get; set; }

        // 断熱材情報
        [StringLength(8)]
        public string? InsideInsulationComposition { get; set; }
        [StringLength(8)]
        public string? BottomInsulationComposition { get; set; }

        // 炉組条件
        [StringLength(16)]
        public string? FurnaceCondition1 { get; set; }
        [StringLength(16)]
        public string? FurnaceCondition2 { get; set; }

        // その他
        [StringLength(16)]
        public string? UsingDisk { get; set; }
        [StringLength(16)]
        public string? LiquidLevel { get; set; }

        // ナビゲーション
        [ForeignKey(nameof(CGNoteId))]
        public GrowthNoteItem GrowthNote { get; set; } = null!;
    }
}

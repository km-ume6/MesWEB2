using Microsoft.EntityFrameworkCore;

namespace MesWEB.Shared.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        public DbSet<GrowthNoteItem> GrowthNotes { get; set; } = null!;
        public DbSet<GrowthNoteParameters> GrowthNoteParameters { get; set; } = null!;
        public DbSet<GrowthNoteInsulation> GrowthNoteInsulations { get; set; } = null!;
        public DbSet<PageAccessCounter> PageAccessCounters { get; set; } = null!;
        public DbSet<CellMappingLabel> CellMappingLabels { get; set; } = null!;
        public DbSet<CellMappingTemplate> CellMappingTemplates { get; set; } = null!;
        public DbSet<CellMappingItem> CellMappingItems { get; set; } = null!;

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // GrowthNoteItem と GrowthNoteParameters (1:1)
            modelBuilder.Entity<GrowthNoteItem>()
                .HasOne(g => g.Parameters)
                .WithOne(p => p.GrowthNote)
                .HasForeignKey<GrowthNoteParameters>(p => p.CGNoteId)
                .OnDelete(DeleteBehavior.Cascade);

            // GrowthNoteItem と GrowthNoteInsulation (1:1)
            modelBuilder.Entity<GrowthNoteItem>()
                .HasOne(g => g.Insulation)
                .WithOne(i => i.GrowthNote)
                .HasForeignKey<GrowthNoteInsulation>(i => i.CGNoteId)
                .OnDelete(DeleteBehavior.Cascade);

            // CellMappingTemplateとCellMappingItemの親子関係定義
            modelBuilder.Entity<CellMappingTemplate>()
                .HasMany(t => t.MappingItems)
                .WithOne(i => i.Template)
                .HasForeignKey(i => i.TemplateId)
                .OnDelete(DeleteBehavior.Cascade);

            // CellMappingLabel と CellMappingTemplate のリレーション (Label 1 - n Templates)
            modelBuilder.Entity<CellMappingLabel>()
                .HasMany(l => l.Templates)
                .WithOne(t => t.Label)
                .HasForeignKey(t => t.LabelId)
                .OnDelete(DeleteBehavior.SetNull);

            // 明示的にテーブル名を指定しておく
            modelBuilder.Entity<CellMappingLabel>().ToTable("CellMappingLabels");
            modelBuilder.Entity<CellMappingTemplate>().ToTable("CellMappingTemplates");
        }
    }
}

namespace MesWEB.Data
{
    // Type aliases for backward compatibility
    // MesWEB.Data.AppDbContext is now an alias for MesWEB.Shared.Data.AppDbContext
    // This ensures backward compatibility while using the shared DbContext
    public class AppDbContext : MesWEB.Shared.Data.AppDbContext
    {
        public AppDbContext(Microsoft.EntityFrameworkCore.DbContextOptions<MesWEB.Shared.Data.AppDbContext> options) : base(options) { }
    }
}
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace MesWEB.Data
{
    // Keep a single design-time factory in the startup project to avoid duplicate discovery.
    public class AppDbContextFactory : IDesignTimeDbContextFactory<MesWEB.Shared.Data.AppDbContext>
    {
        public MesWEB.Shared.Data.AppDbContext CreateDbContext(string[] args)
        {
            var builder = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: true);

            var config = builder.Build();

            var conn = config.GetConnectionString("Default");

            if (string.IsNullOrWhiteSpace(conn))
            {
                throw new InvalidOperationException("appsettings.json の ConnectionStrings:Default を設定してください。");
            }

            var optionsBuilder = new DbContextOptionsBuilder<MesWEB.Shared.Data.AppDbContext>();
            optionsBuilder.UseSqlServer(conn, sqlOptions => sqlOptions.EnableRetryOnFailure());

            return new MesWEB.Shared.Data.AppDbContext(optionsBuilder.Options);
        }
    }
}

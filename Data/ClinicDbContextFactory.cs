using healthcoreclinic_app_demo.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace healthcoreclinic_app_demo.Data
{
    public class ClinicDbContextFactory : IDesignTimeDbContextFactory<ClinicDbContext>
    {
        public ClinicDbContext CreateDbContext(string[] args)
        {
            var optionsBuilder = new DbContextOptionsBuilder<ClinicDbContext>();
            optionsBuilder.UseSqlServer(
                "Server=desktop-5i67rfl\\sqlexpress;Database=HealthCoreDB;Trusted_Connection=True;TrustServerCertificate=True;");

            return new ClinicDbContext(optionsBuilder.Options);
        }
    }
}

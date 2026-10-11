using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Configuration.Json;
using System.IO;
using Microsoft.EntityFrameworkCore.Design;

namespace Talabat.Repository.Identity
{
    public class AppIdentityDbContextFactory : IDesignTimeDbContextFactory<AppIdentityDbContext>
    {
        public AppIdentityDbContext CreateDbContext(string[] args)
        {
            var currentDirectory = Directory.GetCurrentDirectory();
            var configurationPath = new[]
            {
                Path.Combine(currentDirectory, "Talabat.APIs"),
                Path.Combine(currentDirectory, "..", "Talabat.APIs"),
                currentDirectory
            }
            .Select(Path.GetFullPath)
            .FirstOrDefault(path => File.Exists(Path.Combine(path, "appsettings.json")))
                ?? currentDirectory;

            var configuration = new ConfigurationBuilder()
                .SetBasePath(configurationPath)
                .AddJsonFile("appsettings.json", optional: true)
                .Build();

            var optionsBuilder = new DbContextOptionsBuilder<AppIdentityDbContext>();
            var connectionString = configuration.GetConnectionString("IdentityConnection")
                ?? Environment.GetEnvironmentVariable("ConnectionStrings__IdentityConnection")
                ?? "Server=DESKTOP-H7IIOQD;Database=TalabatIdentity;Integrated Security=True;TrustServerCertificate=True;Encrypt=False";

            optionsBuilder.UseSqlServer<AppIdentityDbContext>(connectionString);

            return new AppIdentityDbContext(optionsBuilder.Options);
        }
    }
}

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Configuration.Json;

using System.IO;
using Microsoft.EntityFrameworkCore.Design;

namespace Talabat.Repository.Data
{
    public class StoreContextFactory : IDesignTimeDbContextFactory<StoreContext>
    {
        public StoreContext CreateDbContext(string[] args)
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

            var optionsBuilder = new DbContextOptionsBuilder<StoreContext>();
            var connectionString = configuration.GetConnectionString("DefaultConnection")
                ?? Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection")
                ?? "Server=DESKTOP-H7IIOQD;Database=TalabatDb;Integrated Security=True;TrustServerCertificate=True;Encrypt=False";

            optionsBuilder.UseSqlServer<StoreContext>(connectionString);

            return new StoreContext(optionsBuilder.Options);
        }
    }
}

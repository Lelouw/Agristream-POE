using AgriStream.Web.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace AgriStream.Web.Data;

public static class DbInitializer
{
    public static async Task SeedAsync(IServiceProvider services, IConfiguration configuration)
    {
        var db = services.GetRequiredService<AppDbContext>();
        await db.Database.EnsureCreatedAsync();

        if (!await db.Users.AnyAsync(u => u.Username == "admin"))
        {
            var hasher = services.GetRequiredService<IPasswordHasher<AppUser>>();
            var admin = new AppUser { Username = "admin", Role = Roles.Admin };
            admin.PasswordHash = hasher.HashPassword(admin, configuration["SeedAdminPassword"] ?? "Admin@12345");
            db.Users.Add(admin);
        }

        if (!await db.Devices.AnyAsync())
        {
            db.Devices.AddRange(
                new Device { DeviceId = "VALVE-A1", Name = "Valve A1", FieldName = "Field-A" },
                new Device { DeviceId = "VALVE-B1", Name = "Valve B1", FieldName = "Field-B" },
                new Device { DeviceId = "PUMP-C1", Name = "Pump C1", FieldName = "Field-C" });
        }

        await db.SaveChangesAsync();
    }
}

using Microsoft.EntityFrameworkCore;

namespace Blink.ApiService.Database;

public sealed class BlinkDbContext(DbContextOptions<BlinkDbContext> options) : DbContext(options)
{
    public DbSet<BlinkUser> Users => Set<BlinkUser>();
}
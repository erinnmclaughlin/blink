using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace Blink.WebApp.Data;

public class BlinkDbContext(DbContextOptions<BlinkDbContext> options) : IdentityDbContext<BlinkUser, IdentityRole<Guid>, Guid>(options)
{
}

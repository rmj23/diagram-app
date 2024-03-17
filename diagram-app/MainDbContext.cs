using diagram_app;
using Microsoft.EntityFrameworkCore;

public class WebAppDbContext : DbContext
{
    public WebAppDbContext(DbContextOptions<WebAppDbContext> options)
        : base(options)
    {
    }

    public DbSet<ExternalServiceToken> ExternalServiceToken { get; set; }
}

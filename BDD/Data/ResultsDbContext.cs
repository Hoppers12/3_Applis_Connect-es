using Microsoft.EntityFrameworkCore;
using BDD.Models;

namespace BDD.Data
{
    public class ResultsDbContext : DbContext
    {
        public ResultsDbContext(DbContextOptions<ResultsDbContext> options) : base(options)
        {
        }

        public DbSet<Result> Results { get; set; }
    }
}

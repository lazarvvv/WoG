using Microsoft.EntityFrameworkCore;
using WoG.Combat.Services.Api.Models;

namespace WoG.Combat.Services.Api.Data
{
    public class ApplicationDbContext(DbContextOptions options) : DbContext(options)
    {
        public DbSet<Duel> Duels { get; set; }
        public DbSet<DuelSpell> Spells { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<DuelEvent>();

            modelBuilder.Entity<DuelSpell>();

            modelBuilder.Entity<Duel>()
                .HasMany(x => x.Events)
                .WithOne()
                .HasForeignKey(x => x.DuelId);

            modelBuilder.Entity<Duel>()
                .HasMany(x => x.LegalSpells)
                .WithMany();
        }
    }
}

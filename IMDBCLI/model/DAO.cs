namespace IMDBCLI.model;
using Microsoft.EntityFrameworkCore;


public class DAO : DbContext
{
    public DbSet<MovieEntry> Movies { get; set; }
    public DbSet<ActorEntry> Actors { get; set; }
    public DbSet<DirectorEntry> Directors { get; set; }
    public DbSet<TagEntry> Tags { get; set; }
    
    public DbSet<MovieSimilarities> MovieSimilarities { get; set; }

    public DAO() => Database.EnsureCreated();
    
    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        optionsBuilder.UseNpgsql("Host=localhost;Port=5432;Database=usersdb;Username=postgres;Password=postgres;Timeout=300;CommandTimeout=300;Include Error Detail=true");
    }
    
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {

        modelBuilder.Entity<MovieEntry>()
            .HasMany(ma => ma.Actors)
            .WithMany(p => p.Movies)
            .UsingEntity<MovieActor>(
                l => l.HasOne<ActorEntry>(e => e.ActorEntry).WithMany(e => e.MovieActors).HasForeignKey(e => e.ActorEntryId),
        r => r.HasOne<MovieEntry>(e => e.MovieEntry).WithMany(e => e.MovieActors).HasForeignKey(e => e.MovieEntryId));
        
        modelBuilder.Entity<MovieEntry>()
            .HasMany(ma => ma.Directors)
            .WithMany(p => p.Movies)
            .UsingEntity<MovieDirector>(
                l => l.HasOne<DirectorEntry>(e => e.DirectorEntry).WithMany(e => e.MovieDirectors).HasForeignKey(e => e.DirectorEntryId),
                r => r.HasOne<MovieEntry>(e => e.MovieEntry).WithMany(e => e.MovieDirectors).HasForeignKey(e => e.MovieEntryId));
        // modelBuilder.Entity<MovieEntry>()
        //     .HasMany(ma => ma.Directors)
        //     .WithMany(p => p.Movies);
        
        modelBuilder.Entity<MovieEntry>()
            .HasMany(ma => ma.Tags)
            .WithMany(p => p.Movies)
            .UsingEntity<MovieTag>(
                l => l.HasOne<TagEntry>(e => e.TagEntry).WithMany(e => e.MovieTags).HasForeignKey(e => e.TagEntryId),
                r => r.HasOne<MovieEntry>(e => e.MovieEntry).WithMany(e => e.MovieTags).HasForeignKey(e => e.MovieEntryId));
        modelBuilder.Entity<MovieSimilarities>()
            .HasOne(ms => ms.MovieEntry1)
            .WithMany()
            .HasForeignKey(ms => ms.MovieEntryId1)
            .OnDelete(DeleteBehavior.Restrict); // Нужно, чтобы не удалять связанные записи

        modelBuilder.Entity<MovieSimilarities>()
            .HasOne(ms => ms.MovieEntry2)
            .WithMany()
            .HasForeignKey(ms => ms.MovieEntryId2)
            .OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<MovieSimilarities>()
            .HasKey(ms => new { ms.MovieEntryId1, ms.MovieEntryId2 });
        // modelBuilder.Entity<MovieEntry>()
        //     .HasMany(ma => ma.Tags)
        //     .WithMany(t => t.Movies);
    }
}
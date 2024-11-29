using System.ComponentModel.DataAnnotations;

namespace IMDBCLI.model;

public class DirectorEntry
{
    [Key]
    [StringLength(402)]
    public string Name { get; set; }
    
    // public string DirectorId { get; set; }

    public ICollection<MovieEntry> Movies { get; set; } = new HashSet<MovieEntry>();
    public ICollection<MovieDirector> MovieDirectors { get; set; } = new HashSet<MovieDirector>();
}
using System.ComponentModel.DataAnnotations;

namespace IMDBCLI.model;

public class ActorEntry
{
    [Key]
    [StringLength(401)]
    public string Name { get; set; }

    public ICollection<MovieEntry> Movies { get; set; } = new HashSet<MovieEntry>();
    public ICollection<MovieActor> MovieActors { get; set; } = new HashSet<MovieActor>();
}
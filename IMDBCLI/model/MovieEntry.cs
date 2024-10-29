using System.ComponentModel.DataAnnotations;

namespace IMDBCLI.model;

public class MovieEntry
{
    [Key]
    [StringLength(703)]
    public string MovieName { get; set; }
    public float Rating { get; set; }

    public ICollection<ActorEntry> Actors { get; set; } = new HashSet<ActorEntry>();
    public ICollection<DirectorEntry> Directors { get; set; } = new HashSet<DirectorEntry>();
    public ICollection<TagEntry> Tags { get; set; } = new HashSet<TagEntry>();
    
    public ICollection<MovieActor> MovieActors { get; set; } = new HashSet<MovieActor>();
    public ICollection<MovieDirector> MovieDirectors { get; set; } = new HashSet<MovieDirector>();
    public ICollection<MovieTag> MovieTags { get; set; } = new HashSet<MovieTag>();
    
}
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace IMDBCLI.model;

public class TagEntry
{
    [Key]
    [StringLength(201)]
    public string Name { get; set; } = string.Empty;
    
    public ICollection<MovieEntry> Movies { get; set; } = new HashSet<MovieEntry>();
    public ICollection<MovieTag> MovieTags { get; set; } = new HashSet<MovieTag>();
}
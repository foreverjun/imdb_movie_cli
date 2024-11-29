namespace IMDBCLI.model;

public record Tag(string name, float score)
{
    public virtual bool Equals(Tag? other)
    {
        if (other == null)
        {
            return false;
        }
        
        return this.name == other.name;
    }
    
    public override int GetHashCode() => name.GetHashCode();
}

class TagComparer : IComparer<Tag>
{
    public int Compare(Tag? x, Tag? y)
    {
        if (x == null) return 1;
        if (y == null) return -1;
        return y.score.CompareTo(x.score);
    }
}

public record SimilarMovie(Movie movie, double similarity);

using System.Globalization;

namespace IMDBCLI.model;

public class Movie(string movieName)
{
    public string movieName = movieName;
    public HashSet<string> actors = [];
    public HashSet<string> tags = [];
    public HashSet<string> production = [];
    public Dictionary<Movie, Double> similar = [];
    public string rating = "0.0";
    
    
    public override string ToString()
    {
        return $"Название: {movieName}\nАктеры: {string.Join(", ", actors)}\nРежиссер/Директор: {string.Join(", ", production)}\nТэги: {string.Join(", ", tags)}\nРейтинг: {rating}";
    }
    public static Movie FromEntity(MovieEntry entity)
    {
        var movie = new Movie(entity.MovieName)
        {
            rating = entity.Rating.ToString(CultureInfo.InvariantCulture.NumberFormat)
        };

        foreach (var actor in entity.Actors)
        {
            movie.actors.Add(actor.Name);
        }

        foreach (var tag in entity.Tags)
        {
            movie.tags.Add(tag.Name);
        }

        foreach (var production in entity.Directors)
        {
            movie.production.Add(production.Name);
        }

        return movie;
    }

    public Double Similarity(MovieEntry movieEntry)
    {
        var movie = FromEntity(movieEntry);
        var currentPeople = new HashSet<string>();
        var otherPeople = new HashSet<string>();
        var unionPeople = new HashSet<string>();
        
        var unionTags = new HashSet<string>();
        
        currentPeople.UnionWith(actors);
        currentPeople.UnionWith(production);
        otherPeople.UnionWith(movie.actors);
        otherPeople.UnionWith(movie.production);
        unionPeople.UnionWith(currentPeople);
        unionPeople.UnionWith(otherPeople);
        
        unionTags.UnionWith(movie.tags);
        unionTags.UnionWith(tags);

        var peopleIntersect = currentPeople.Intersect(otherPeople);
        var tagsIntersect = movie.tags.Intersect(tags);
        
        Double corr = 0.0;

        if(unionPeople.Count > 0){
            corr += (double)peopleIntersect.Count() / unionPeople.Count;
        }
        if (unionTags.Count > 0){
            corr += (double)tagsIntersect.Count() / unionTags.Count;
        }

        corr /= 4;

        corr += float.Parse(rating, CultureInfo.InvariantCulture.NumberFormat) / 20;
        
        
        return corr;
    }
}
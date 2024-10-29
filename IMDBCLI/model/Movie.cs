using System.Globalization;

namespace IMDBCLI.model;

public class Movie(string movieName)
{
    public string movieName = movieName;
    public HashSet<string> actors = [];
    public HashSet<string> tags = [];
    public HashSet<string> production = [];
    public string rating = string.Empty;
    
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
}
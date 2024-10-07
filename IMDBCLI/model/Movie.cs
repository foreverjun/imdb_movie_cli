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
}
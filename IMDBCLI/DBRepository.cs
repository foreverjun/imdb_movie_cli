using System.Diagnostics;
using System.Globalization;
using IMDBCLI.model;

namespace IMDBCLI;

public class DBRepository
{
    private const string actorDirectorsCodesPath = "../../../../ml-latest/ActorsDirectorsCodes_IMDB.tsv";
    private const string actorDirectorsNamesPath = "../../../../ml-latest/ActorsDirectorsNames_IMDB.txt";
    private const string ratingsPath = "../../../../ml-latest/Ratings_IMDB.tsv";
    private const string movieCodesPath = "../../../../ml-latest/MovieCodes_IMDB.tsv";
    private const string tagCodesPath = "../../../../ml-latest/TagCodes_MovieLens.csv";
    private const string tagScoresPath = "../../../../ml-latest/TagScores_MovieLens.csv";
    private const string linksPath = "../../../../ml-latest/links_IMDB_MovieLens.csv";

    private Dictionary<string, Movie> movies = new();
    private Dictionary<string, HashSet<Movie>> peopleToMovie = new();
    private Dictionary<string, HashSet<Movie>> tagToMovie = new();

    private Dictionary<string, string> tagToName = new();
    private Dictionary<int, string> movieIDtoImdb = new();
    private Dictionary<string, string> idToTag = new();
    private Dictionary<string, string> mvIdToName = new();

    void loadMovies()
    {
        try
        {
            using var sr = new StreamReader(movieCodesPath);
            bool flag = true;
            Span<Range> ranges = stackalloc Range[8];
            while (sr.ReadLine() is { } line)
            {
                if (flag)
                {
                    flag = false;
                    continue;
                }

                var span = line.AsSpan();

                span.Split(ranges, '\t');
                var id = span[ranges[0]].ToString();
                var title = span[ranges[2]].ToString();
                var region = span[ranges[3]];
                var language = span[ranges[4]];
                if (region is "US" || region is "GB" || region is "RU" || region is "AU" || region is "EU" ||
                    language is "en" || language is "ru")
                {
                    if (mvIdToName.ContainsKey(id)) continue;
                    if (movies.ContainsKey(title)) continue;
                    var movie = new Movie(title);
                    movies.TryAdd(title, movie);
                    mvIdToName.TryAdd(id, title);
                }
            }
        }
        catch (Exception e)
        {
            Console.WriteLine("The file could not be read(movieCodes):");
            Console.WriteLine(e.Message);
        }

        try
        {
            using var sr = new StreamReader(ratingsPath);
            bool flag = true;
            Span<Range> ranges = stackalloc Range[3];
            while (sr.ReadLine() is { } line)
            {
                if (flag)
                {
                    flag = false;
                    continue;
                }

                var span = line.AsSpan();

                span.Split(ranges, '\t');
                var movieId = span[ranges[0]].ToString();
                var rating = span[ranges[1]].ToString();
                if (!mvIdToName.TryGetValue(movieId, out string? value)) continue;
                movies[value].rating = rating;
            }
        }
        catch (Exception e)
        {
            Console.WriteLine("The file could not be read(ratings):");
            Console.WriteLine(e.Message);
        }
    }

    void loadPeople()
    {
        try
        {
            using var sr = new StreamReader(actorDirectorsNamesPath);
            bool flag = true;
            Span<Range> ranges = stackalloc Range[6];
            while (sr.ReadLine() is { } line)
            {
                if (flag)
                {
                    flag = false;
                    continue;
                }

                var span = line.AsSpan();

                span.Split(ranges, '\t');
                var id = span[ranges[0]].ToString();
                var name = span[ranges[1]].ToString();
                tagToName.TryAdd(id, name);
            }
        }
        catch (Exception e)
        {
            Console.WriteLine("The file could not be read(actorDirectorsNames):");
            Console.WriteLine(e.Message);
        }

        try
        {
            using var sr = new StreamReader(actorDirectorsCodesPath);
            bool flag = true;
            Span<Range> ranges = stackalloc Range[6];
            while (sr.ReadLine() is { } line)
            {
                if (flag)
                {
                    flag = false;
                    continue;
                }

                var span = line.AsSpan();

                span.Split(ranges, '\t');

                var movieId = span[ranges[0]].ToString();
                var personId = span[ranges[2]].ToString();


                if (!mvIdToName.TryGetValue(movieId, out string? value)) continue;
                Movie mv = movies[value];
                if (!tagToName.TryGetValue(personId, out string? tag)) continue;
                var category = span[ranges[3]];
                if (category is "actor" || category is "actress")
                {
                    lock (mv.actors)
                    {
                        mv.actors.Add(tag);
                    }

                    if (!peopleToMovie.TryGetValue(tag, out HashSet<Movie>? movie))
                    {
                        movie = ( []);
                        peopleToMovie[tag] = movie;
                    }

                    lock (movie)
                    {
                        movie.Add(mv);
                    }
                }
                else if (category is "producer" || category is "director")
                {
                    lock (mv.production)
                    {
                        mv.production.Add(tag);
                    }

                    if (!peopleToMovie.TryGetValue(tag, out HashSet<Movie>? movie))
                    {
                        movie = new HashSet<Movie>();
                        peopleToMovie[tag] = movie;
                    }

                    lock (movie)
                    {
                        movie.Add(mv);
                    }
                }
            }
        }
        catch (Exception e)
        {
            Console.WriteLine("The file could not be read(actorDirectorsCodes):");
            Console.WriteLine(e.Message);
        }
    }

    void loadTags()
    {
        try
        {
            using var sr = new StreamReader(linksPath);
            bool flag = true;
            Span<Range> ranges = stackalloc Range[3];
            while (sr.ReadLine() is { } line)
            {
                if (flag)
                {
                    flag = false;
                    continue;
                }

                var span = line.AsSpan();

                span.Split(ranges, ',');
                var movieId = int.Parse(span[ranges[0]]);
                var imdpId = span[ranges[1]].ToString();
                movieIDtoImdb.TryAdd(movieId, imdpId);
            }
        }
        catch (Exception e)
        {
            Console.WriteLine("The file could not be read(links):");
            Console.WriteLine(e.Message);
        }

        try
        {
            using var sr = new StreamReader(tagCodesPath);
            bool flag = true;
            while (sr.ReadLine() is { } line)
            {
                if (flag)
                {
                    flag = false;
                    continue;
                }

                var lineData = line.Trim().Split(',');
                idToTag.Add(lineData[0], lineData[1]);
            }
        }
        catch (Exception e)
        {
            Console.WriteLine("The file could not be read(tagCodes):");
            Console.WriteLine(e.Message);
        }

        try
        {
            using var sr = new StreamReader(tagScoresPath);
            bool flag = true;
            Span<Range> ranges = stackalloc Range[3];
            while (sr.ReadLine() is { } line)
            {
                if (flag)
                {
                    flag = false;
                    continue;
                }

                var span = line.AsSpan();

                span.Split(ranges, ',');
                var relevance = span[ranges[2]];
                if (float.Parse(relevance, CultureInfo.InvariantCulture.NumberFormat) > 0.5)
                {
                    var movieId = span[ranges[0]];
                    var tagId = span[ranges[1]].ToString();
                    var imdbId = "tt" + movieIDtoImdb[int.Parse(movieId)];
                    if (!mvIdToName.TryGetValue(imdbId, out string? value)) continue;
                    Movie mv = movies[value];

                    var tag = idToTag[tagId];
                    lock (mv.tags)
                    {
                        mv.tags.Add(tag);
                    }

                    if (!tagToMovie.ContainsKey(tag)) tagToMovie[tag] = [];
                    lock (tagToMovie[tag])
                    {
                        tagToMovie[tag].Add(mv);
                    }
                }
            }
        }

        catch (Exception e)
        {
            Console.WriteLine("The file could not be read(tagScores):");
            Console.WriteLine(e.Message);
        }
    }

    public Movie? getMovie(string imdbId)
    {
        if (!movies.TryGetValue(imdbId, out Movie? value)) return null;
        return value;
    }

    public HashSet<Movie>? getPerson(string imdbId)
    {
        if (!peopleToMovie.TryGetValue(imdbId, out HashSet<Movie>? value)) return null;
        return value;
    }

    public HashSet<Movie>? getTag(string imdbId)
    {
        if (!tagToMovie.TryGetValue(imdbId, out HashSet<Movie>? value)) return null;
        return value;
    }

    public int getMoviesNum()
    {
        return movies.Count;
    }

    public int getPeopleNum()
    {
        return peopleToMovie.Count;
    }

    public int getTagNum()
    {
        return tagToMovie.Count;
    }

    public void initalizeData()
    {
        if (movies.Count == 0)
        {
            Stopwatch stopwatch = Stopwatch.StartNew();
            loadMovies();
            loadPeople();
            loadTags();

            // Останавливаем Stopwatch
            stopwatch.Stop();

            // Получаем прошедшее время
            TimeSpan elapsed = stopwatch.Elapsed;
            Console.WriteLine($"Время выполнения: {elapsed.TotalSeconds} секунд");
        }
    }
}
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Globalization;
using IMDBCLI.model;

namespace IMDBCLI;

public class AsyncRepository
{
    private const string actorDirectorsCodesPath = "../../../../ml-latest/ActorsDirectorsCodes_IMDB.tsv";
    private const string actorDirectorsNamesPath = "../../../../ml-latest/ActorsDirectorsNames_IMDB.txt";
    private const string ratingsPath = "../../../../ml-latest/Ratings_IMDB.tsv";
    private const string movieCodesPath = "../../../../ml-latest/MovieCodes_IMDB.tsv";
    private const string tagCodesPath = "../../../../ml-latest/TagCodes_MovieLens.csv";
    private const string tagScoresPath = "../../../../ml-latest/TagScores_MovieLens.csv";
    private const string linksPath = "../../../../ml-latest/links_IMDB_MovieLens.csv";

    private ConcurrentDictionary<string, Movie> movies = new();
    private ConcurrentDictionary<string, HashSet<Movie>> peopleToMovie = new();
    private ConcurrentDictionary<string, HashSet<Movie>> tagToMovie = new();

    private ConcurrentDictionary<string, string> tagToName = new();
    private ConcurrentDictionary<int, string> movieIDtoImdb = new();
    private ConcurrentDictionary<string, string> idToTag = new();
    private ConcurrentDictionary<string, string> mvIdToName = new();


    void loadMoviesNamesLinksTags()
    {
        BlockingCollection<string> moviesCollection = new();
        BlockingCollection<string> idToNameCollection = new();
        BlockingCollection<string> linksCollection = new();
        BlockingCollection<string> tagsCollection = new();


        var moviesProducer = Task.Factory.StartNew(() =>
        {
            using var sr = new StreamReader(movieCodesPath);
            bool flag = true;
            while (sr.ReadLine() is { } line)
            {
                if (flag)
                {
                    flag = false;
                    continue;
                }

                moviesCollection.Add(line);
            }

            moviesCollection.CompleteAdding();
        }, TaskCreationOptions.LongRunning);

        var idToNameProducer = Task.Factory.StartNew(() =>
        {
            using var sr = new StreamReader(actorDirectorsNamesPath);
            var flag = true;
            while (sr.ReadLine() is { } line)
            {
                if (flag)
                {
                    flag = false;
                    continue;
                }

                idToNameCollection.Add(line);
            }

            idToNameCollection.CompleteAdding();
        }, TaskCreationOptions.LongRunning);

        var linksProducer = Task.Factory.StartNew(() =>
        {
            using var sr = new StreamReader(linksPath);
            var flag = true;
            while (sr.ReadLine() is { } line)
            {
                if (flag)
                {
                    flag = false;
                    continue;
                }

                linksCollection.Add(line);
            }

            linksCollection.CompleteAdding();
        }, TaskCreationOptions.LongRunning);

        var tagsProducer = Task.Factory.StartNew(() =>
        {
            using var sr = new StreamReader(tagCodesPath);
            var flag = true;
            while (sr.ReadLine() is { } line)
            {
                if (flag)
                {
                    flag = false;
                    continue;
                }

                tagsCollection.Add(line);
            }

            tagsCollection.CompleteAdding();
        }, TaskCreationOptions.LongRunning);

        // Setup threads

        var processorCount = Environment.ProcessorCount / 4;
        var moviesTasks = new Task[processorCount];

        for (int i = 0; i < processorCount; i++)
        {
            moviesTasks[i] = Task.Factory.StartNew(() =>
            {
                Span<Range> ranges = stackalloc Range[8];
                foreach (var line in moviesCollection.GetConsumingEnumerable())
                {
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
            }, TaskCreationOptions.LongRunning);
        }

        var idToNameTasks = new Task[processorCount];
        for (int i = 0; i < processorCount; i++)
        {
            idToNameTasks[i] = Task.Factory.StartNew(() =>
            {
                Span<Range> ranges = stackalloc Range[6];
                foreach (var line in idToNameCollection.GetConsumingEnumerable())
                {
                    var span = line.AsSpan();

                    span.Split(ranges, '\t');
                    var id = span[ranges[0]].ToString();
                    var name = span[ranges[1]].ToString();
                    tagToName.TryAdd(id, name);
                }
            }, TaskCreationOptions.LongRunning);
        }

        var linksTasks = new Task[processorCount];
        for (int i = 0; i < processorCount; i++)
        {
            linksTasks[i] = Task.Factory.StartNew(() =>
            {
                Span<Range> ranges = stackalloc Range[3];
                foreach (var line in linksCollection.GetConsumingEnumerable())
                {
                    var span = line.AsSpan();

                    span.Split(ranges, ',');
                    var movieId = int.Parse(span[ranges[0]]);
                    var imdpId = span[ranges[1]].ToString();
                    movieIDtoImdb.TryAdd(movieId, imdpId);
                }
            }, TaskCreationOptions.LongRunning);
        }

        var tagsTasks = new Task[processorCount];
        for (int i = 0; i < processorCount; i++)
        {
            tagsTasks[i] = Task.Factory.StartNew(() =>
            {
                Span<Range> ranges = stackalloc Range[2];
                foreach (var line in tagsCollection.GetConsumingEnumerable())
                {
                    var span = line.AsSpan();

                    span.Split(ranges, ',');
                    var tagId = span[ranges[0]].ToString();
                    var tag = span[ranges[1]].ToString();
                    idToTag.TryAdd(tagId, tag);
                }
            }, TaskCreationOptions.LongRunning);
        }

        // Wait for the producer and all processor tasks to complete
        Task.WaitAll(new[] { moviesProducer }.Concat(moviesTasks).ToArray());
        Task.WaitAll(new[] { idToNameProducer }.Concat(idToNameTasks).ToArray());
        Task.WaitAll(new[] { linksProducer }.Concat(linksTasks).ToArray());
        Task.WaitAll(new[] { tagsProducer }.Concat(tagsTasks).ToArray());
    }

    void loadRatingsPeopleTags()
    {
        BlockingCollection<string> ratingsCollection = new();
        BlockingCollection<string> peopleToMovieCollection = new();
        BlockingCollection<string> tagsCollection = new();

        var ratingsProducer = Task.Factory.StartNew(() =>
        {
            using var sr = new StreamReader(ratingsPath);
            bool flag = true;
            while (sr.ReadLine() is { } line)
            {
                if (flag)
                {
                    flag = false;
                    continue;
                }

                ratingsCollection.Add(line);
            }

            ratingsCollection.CompleteAdding();
        }, TaskCreationOptions.LongRunning);

        var peopleToMovieProducer = Task.Factory.StartNew(() =>
        {
            using var sr = new StreamReader(actorDirectorsCodesPath);
            bool flag = true;
            while (sr.ReadLine() is { } line)
            {
                if (flag)
                {
                    flag = false;
                    continue;
                }

                peopleToMovieCollection.Add(line);
            }

            peopleToMovieCollection.CompleteAdding();
        }, TaskCreationOptions.LongRunning);

        var tagsToMovieProducer = Task.Factory.StartNew(() =>
        {
            using var sr = new StreamReader(tagScoresPath);
            bool flag = true;
            while (sr.ReadLine() is { } line)
            {
                if (flag)
                {
                    flag = false;
                    continue;
                }

                tagsCollection.Add(line);
            }

            tagsCollection.CompleteAdding();
        }, TaskCreationOptions.LongRunning);

        var ratingsTasks = new Task[2];
        for (int i = 0; i < 2; i++)
        {
            ratingsTasks[i] = Task.Factory.StartNew(() =>
            {
                Span<Range> ranges = stackalloc Range[3];
                foreach (var line in ratingsCollection.GetConsumingEnumerable())
                {
                    var span = line.AsSpan();

                    span.Split(ranges, '\t');
                    var movieId = span[ranges[0]].ToString();
                    var rating = span[ranges[1]].ToString();
                    if (!mvIdToName.TryGetValue(movieId, out string? value)) continue;
                    movies[value].rating = rating;
                }
            }, TaskCreationOptions.LongRunning);
        }

        var peopleToMovieTasks = new Task[8];
        for (int i = 0; i < 8; i++)
        {
            peopleToMovieTasks[i] = Task.Factory.StartNew(() =>
            {
                Span<Range> ranges = stackalloc Range[6];
                foreach (var line in peopleToMovieCollection.GetConsumingEnumerable())
                {
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
            }, TaskCreationOptions.LongRunning);
        }

        var tagsTasks = new Task[4];
        for (int i = 0; i < 4; i++)
        {
            tagsTasks[i] = Task.Factory.StartNew(() =>
            {
                Span<Range> ranges = stackalloc Range[3];
                foreach (var line in tagsCollection.GetConsumingEnumerable())
                {
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
            }, TaskCreationOptions.LongRunning);
        }

        Task.WaitAll(new[] { ratingsProducer }.Concat(ratingsTasks).ToArray());
        Task.WaitAll(new[] { peopleToMovieProducer }.Concat(peopleToMovieTasks).ToArray());
        Task.WaitAll(new[] { tagsToMovieProducer }.Concat(tagsTasks).ToArray());
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
            loadMoviesNamesLinksTags();
            loadRatingsPeopleTags();

            // Останавливаем Stopwatch
            stopwatch.Stop();

            // Получаем прошедшее время
            TimeSpan elapsed = stopwatch.Elapsed;
            Console.WriteLine($"Время выполнения: {elapsed.TotalSeconds} секунд");
        }
    }
}
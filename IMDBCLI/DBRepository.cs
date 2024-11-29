using System.Diagnostics;
using System.Globalization;
using EFCore.BulkExtensions;
using IMDBCLI.model;
using Microsoft.EntityFrameworkCore;

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
                        mv.actors.Add(tag);

                    if (!peopleToMovie.TryGetValue(tag, out HashSet<Movie>? movie))
                    {
                        movie = ( []);
                        peopleToMovie[tag] = movie;
                    }
                    
                        movie.Add(mv);
                }
                else if (category is "producer" || category is "director")
                {
                        mv.production.Add(tag);

                    if (!peopleToMovie.TryGetValue(tag, out HashSet<Movie>? movie))
                    {
                        movie = new HashSet<Movie>();
                        peopleToMovie[tag] = movie;
                    }
                    
                        movie.Add(mv);
                  
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
                var relevanceFloat = float.Parse(relevance, CultureInfo.InvariantCulture.NumberFormat);
                if (relevanceFloat > 0.5)
                {
                    var movieId = span[ranges[0]];
                    var tagId = span[ranges[1]].ToString();
                    var imdbId = "tt" + movieIDtoImdb[int.Parse(movieId)];
                    if (!mvIdToName.TryGetValue(imdbId, out string? value)) continue;
                    Movie mv = movies[value];

                    var tag = idToTag[tagId];
                    mv.tags.Add(tag);

                    if (!tagToMovie.ContainsKey(tag)) tagToMovie[tag] = [];
                        tagToMovie[tag].Add(mv);
                }
            }
        }

        catch (Exception e)
        {
            Console.WriteLine("The file could not be read(tagScores):");
            Console.WriteLine(e.Message);
        }
    }

    List<SimilarMovie> calculateSimilarity( String movieName)
    {
        Console.WriteLine(movies.Count);
        var moviesToCompare = new HashSet<MovieEntry>();
        

        using (DAO db = new DAO())
        {
            Console.WriteLine("Calculating");
            
            var movie = db.Movies
                .Include(m => m.Actors).ThenInclude(actorEntry => actorEntry.Movies)
                .Include(m => m.Directors).ThenInclude(directorEntry => directorEntry.Movies)
                .Include(m => m.Tags).ThenInclude(tagEntry => tagEntry.Movies).AsSplitQuery()
                .FirstOrDefaultAsync(m => m.MovieName == movieName).Result;
            Console.WriteLine("Calculated");
            if (movie == null)
            {
                return [];
            }
            
            
            var actorMovies = movie.Actors.SelectMany(a => a.Movies);
            var directorMovies = movie.Directors.SelectMany(d => d.Movies);
            var tagMovies = movie.Tags.SelectMany(t => t.Movies);
            
            
            moviesToCompare.UnionWith(actorMovies);
            moviesToCompare.UnionWith(directorMovies);
            moviesToCompare.UnionWith(tagMovies);
            moviesToCompare.Remove(movie);
            
            Console.WriteLine("Union");
            var mv = Movie.FromEntity(movie);
            var similar = new HashSet<(MovieEntry, Double)>();
            foreach (var m in moviesToCompare)
            {
                similar.Add((m, mv.Similarity(m)));
            }

            var top10 = similar.OrderByDescending(pair => pair.Item2).Take(10).ToList();
            foreach (var sim in top10)
            {
                db.MovieSimilarities.Add(new MovieSimilarities
                {
                    MovieEntryId1 = movieName,
                    MovieEntryId2 = sim.Item1.MovieName,
                    Similarity = sim.Item2,
                    MovieEntry1 = movie,
                    MovieEntry2 = sim.Item1
                });
            }

            db.SaveChanges();
            return top10.ConvertAll( sim => new SimilarMovie(Movie.FromEntity(sim.Item1), sim.Item2));
        }
        
    }

    public Movie? getMovie(string imdbId)
    {
        using (DAO db = new DAO())
        {
            var movieEntry = db.Movies
                .Include(m => m.Actors)
                .Include(m => m.Directors)
                .Include(m => m.Tags)
                .FirstOrDefault(m => m.MovieName == imdbId);

            if (movieEntry != null)
            {
                return Movie.FromEntity(movieEntry);
            }

            return null;
        }
    }

    public HashSet<Movie>? getPerson(string imdbId)
    {
        using (DAO db = new DAO())
        {
            var moviesSet = new HashSet<Movie>();
            // Try to find the actor with the given ID
            var actor = db.Actors
                .Include(a => a.Movies)
                .FirstOrDefault(a => a.Name == imdbId);

            if (actor != null)
            {
                var movies = actor.Movies
                    .Select(Movie.FromEntity)
                    .ToHashSet();

                moviesSet.UnionWith(movies);
            }

            // If not found as an actor, try as a director
            var director = db.Directors
                .Include(d => d.Movies)
                .FirstOrDefault(d => d.Name == imdbId);

            if (director != null)
            {
                var movies = director.Movies
                    .Select(Movie.FromEntity)
                    .ToHashSet();

                moviesSet.UnionWith(movies);
            }

            if (moviesSet.Count == 0) return null;

            return moviesSet;
        }
    }

    public HashSet<Movie>? getTag(string imdbId)
    {
        using (DAO db = new DAO())
        {
            var tag = db.Tags
                .Include(t => t.Movies)
                .FirstOrDefault(t => t.Name == imdbId);

            if (tag != null)
            {
                var movies = tag.Movies
                    .Select(Movie.FromEntity)
                    .ToHashSet();
                return movies;
            }
            return null;
        }
    }

    public int getMoviesNum()
    {
        using (DAO db = new DAO())
        {
            return db.Movies.Count();
        }
    }

    public int getPeopleNum()
    {
        using (DAO db = new DAO())
        {
            return db.Actors.Count() + db.Directors.Count();
        }
    }

    public int getTagNum()
    {
        using (DAO db = new DAO())
        {
            return db.Tags.Count();
        }
    }
    
    public List<SimilarMovie> GetSimilarMovies(string movieName)
    {
        var movieId = movieName;

        using (DAO db = new DAO())
        {
            var similarMovies = db.MovieSimilarities
                .Where(ms => ms.MovieEntryId1 == movieId)
                .Include(m => m.MovieEntry1)
                .Include(m => m.MovieEntry2)
                .ToList();
            
            Console.WriteLine("Fetched movies");
            if (similarMovies.Count != 0)
            {
                Console.WriteLine("Found movies");
                var similarMoviesList = new List<SimilarMovie>();
                foreach (var sim in similarMovies)
                {
                    similarMoviesList.Add(new SimilarMovie(Movie.FromEntity(sim.MovieEntry2), sim.Similarity));
                }

                return similarMoviesList;
            }
            return calculateSimilarity(movieName);
        }
    }

    public void loadToDB()

    {
        initalizeData();
        Stopwatch stopwatch = Stopwatch.StartNew();
        using (DAO db = new DAO())
        {
            db.Database.EnsureDeleted();
            db.Database.EnsureCreated();
            var count = 0;
            Dictionary<string, ActorEntry> actorDict = new Dictionary<string, ActorEntry>();
            Dictionary<string, DirectorEntry> directorDict = new Dictionary<string, DirectorEntry>();
            Dictionary<string, TagEntry> tagDict = new Dictionary<string, TagEntry>();

            var movieEntriesList = new List<MovieEntry>();
            var movieActorRelations = new List<MovieActor>();
            var movieDirectorRelations = new List<MovieDirector>();
            var movieTagRelations = new List<MovieTag>();
            
            db.ChangeTracker.AutoDetectChangesEnabled = false; 
            foreach (var mv in movies)
            {
                Movie movie = mv.Value;
                if (movie.movieName.Length >= 800) continue;
                if (movie.rating == "" ) movie.rating = "0.0";

                // Создаем MovieEntry для текущего фильма
                MovieEntry movieEntry = new MovieEntry
                    { MovieName = movie.movieName, Rating = float.Parse(movie.rating, CultureInfo.InvariantCulture.NumberFormat) };
                movieEntriesList.Add(movieEntry);
                // Обрабатываем актеров
                foreach (string actorName in movie.actors)
                {
                    if (!actorDict.TryGetValue(actorName, out var actorEntry))
                    {
                        actorEntry = new ActorEntry { Name = actorName };
                        actorDict.Add(actorName, actorEntry);
                    }

                    // movieEntry.Actors.Add(actorEntry);
                    movieActorRelations.Add(new MovieActor
                    {
                        MovieEntry = movieEntry,
                        ActorEntry = actorEntry
                    });
                }

                // Обрабатываем режиссеров
                foreach (string directorName in movie.production) 
                {
                    DirectorEntry directorEntry;
                    if (!directorDict.TryGetValue(directorName, out directorEntry))
                    {
                        directorEntry = new DirectorEntry { Name = (directorName) };
                        directorDict.Add(directorName, directorEntry);
                    }

                    // Создаем связь между фильмом и режиссером
                    movieDirectorRelations.Add(new MovieDirector
                    {
                        MovieEntry = movieEntry,
                        DirectorEntry = directorEntry
                    });
                }

                // Обрабатываем теги
                foreach (string tagName in movie.tags)
                {
                    TagEntry tagEntry;
                    if (!tagDict.TryGetValue(tagName, out tagEntry))
                    {
                        tagEntry = new TagEntry { Name = tagName };
                        tagDict.Add(tagName, tagEntry);
                    }

                    // movieEntry.Tags.Add(tagEntry);
                    
                    // Создаем связь между фильмом и тегом
                    movieTagRelations.Add(new MovieTag
                    {
                        MovieEntry = movieEntry,
                        TagEntry = tagEntry
                    });
                }

                // // Добавляем MovieEntry в контекст базы данных
                // db.Movies.Add(movieEntry);

                count++;
                // if (count % 1000 == 0) Console.WriteLine($"Loaded {count} movie(s)");
                // if (count % 1_000 == 0) db.BulkSaveChanges();
            }

            // // Добавляем актеров, режиссеров и теги в базу данных
            // db.Actors.AddRange(actorDict.Values);
            // db.Directors.AddRange(directorDict.Values);
            // db.Tags.AddRange(tagDict.Values);
            
            // Массовая вставка актеров, режиссеров и тегов
            db.BulkInsert(actorDict.Values);
            db.BulkInsert(directorDict.Values);
            db.BulkInsert(tagDict.Values);

            // Массовая вставка фильмов
            db.BulkInsert(movieEntriesList);

            // После вставки фильмов и других сущностей, установим идентификаторы для связей
            foreach (var relation in movieActorRelations)
            {
                relation.MovieEntryId = relation.MovieEntry.MovieName;
                relation.ActorEntryId = relation.ActorEntry.Name;
            }

            foreach (var relation in movieDirectorRelations)
            {
                relation.MovieEntryId = relation.MovieEntry.MovieName;
                relation.DirectorEntryId = relation.DirectorEntry.Name;
            }

            foreach (var relation in movieTagRelations)
            {
                relation.MovieEntryId = relation.MovieEntry.MovieName;
                relation.TagEntryId = relation.TagEntry.Name;
            }

            // Массовая вставка связей
            db.BulkInsert(movieTagRelations);
            db.BulkInsert(movieActorRelations);
            db.BulkInsert(movieDirectorRelations);
            

            // Сохраняем все изменения
            // db.BulkSaveChanges();
            db.ChangeTracker.AutoDetectChangesEnabled = true;
            TimeSpan elapsed = stopwatch.Elapsed;
            Console.WriteLine($"Время выполнения: {elapsed.TotalSeconds} секунд");
        }
    }

    public void initalizeData()
    {
        if (movies.Count == 0)
        {
            Stopwatch stopwatch = Stopwatch.StartNew();
            loadMovies();
            loadPeople();
            loadTags();
            // calculateSimilarity();


            // Останавливаем Stopwatch
            stopwatch.Stop();

            // Получаем прошедшее время
            TimeSpan elapsed = stopwatch.Elapsed;
            Console.WriteLine($"Время выполнения: {elapsed.TotalSeconds} секунд");
        }
    }
}
using IMDBCLI.model;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace BlazorIMDB.Services;

public class MainService
{
    private readonly DAO db;
    private readonly IMemoryCache cache;
    private readonly TimeSpan cacheExpiration = TimeSpan.FromMinutes(5);

    public MainService(DAO db, IMemoryCache cache)
    {
        this.db = db;
        this.cache = cache;
    }

    public async Task<PagedResult<MovieEntry>> SearchMoviesWithPaginationAsync(string searchQuery, int pageNumber,
        int pageSize, SearchType searchType)
    {
        string cacheKey = $"SearchMovies_{searchQuery.Trim().ToLower()}_Page_{pageNumber}_Size_{pageSize}";

        if (!cache.TryGetValue(cacheKey, out PagedResult<MovieEntry> cachedResults))
        {
            string normalizedQuery = searchQuery.Trim().ToLower();
            IQueryable<MovieResult> query;
            switch (searchType)
            {
                case SearchType.Movie:
                    query = db.Movies
                        .Where(m => m.MovieName.ToLower().Contains(normalizedQuery))
                        .Select(m => new
                            MovieResult
                            {
                                Movie = m,
                                MatchScore = m.MovieName.ToLower().StartsWith(normalizedQuery)
                                    ? 2
                                    : m.MovieName.ToLower().Contains(normalizedQuery)
                                        ? 1
                                        : 0
                            })
                        .OrderByDescending(m => m.MatchScore)
                        .ThenBy(m => m.Movie.MovieName)
                        .AsSplitQuery();
                    break;
                case SearchType.Person:
                    var buf = db.Actors
                        .Where(a => a.Name.ToLower().Contains(normalizedQuery))
                        .SelectMany(a => a.Movies, (a, m) => new MovieResult
                        {
                            Movie = m,
                            MatchScore = a.Name.ToLower().StartsWith(normalizedQuery)
                                ? 2
                                : a.Name.ToLower().Contains(normalizedQuery)
                                    ? 1
                                    : 0
                        })
                        .OrderByDescending(m => m.MatchScore)
                        .ThenBy(m => m.Movie.Rating)
                        .ThenBy(m => m.Movie.MovieName)
                        .AsSplitQuery();
                    query = buf.Union(db.Directors
                        .Where(d => d.Name.ToLower().Contains(normalizedQuery))
                        .SelectMany(d => d.Movies, (d, m) => new
                            MovieResult
                            {
                                Movie = m,
                                MatchScore = d.Name.ToLower().StartsWith(normalizedQuery)
                                    ? 2
                                    : d.Name.ToLower().Contains(normalizedQuery)
                                        ? 1
                                        : 0
                            })
                        .OrderByDescending(m => m.MatchScore)
                        .ThenBy(m => m.Movie.Rating)
                        .ThenBy(m => m.Movie.MovieName)
                        .AsSplitQuery());
                    break;
                case SearchType.Tag:
                    query = db.Tags
                        .Where(t => t.Name.ToLower().Contains(normalizedQuery))
                        .SelectMany(t => t.Movies, (t, m) => new MovieResult
                        {
                            Movie = m,
                            MatchScore = t.Name.ToLower().StartsWith(normalizedQuery)
                                ? 2
                                : t.Name.ToLower().Contains(normalizedQuery)
                                    ? 1
                                    : 0
                        })
                        .OrderByDescending(m => m.MatchScore)
                        .ThenBy(m => m.Movie.Rating)
                        .ThenBy(m => m.Movie.MovieName)
                        .AsSplitQuery();
                    break;
                default:
                    return null;
            }


            int totalCount = await query.CountAsync();

            var ids = await query
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .Select(q => q.Movie.MovieName)
                .ToListAsync();

            var movies = await db.Movies
                .Where(m => ids.Contains(m.MovieName))
                .Include(m => m.Actors)
                .Include(m => m.Directors)
                .Include(m => m.Tags)
                .ToListAsync();


            cachedResults = new PagedResult<MovieEntry>
            {
                Items = movies,
                TotalCount = totalCount,
                PageNumber = pageNumber,
                PageSize = pageSize
            };

            cache.Set(cacheKey, cachedResults, cacheExpiration);
        }

        return cachedResults;
    }

    public async Task<PagedResult<MovieEntry>> TopRating(int pageNumber, int pageSize)
    {
        string cacheKey = $"TopRating_{pageNumber}_Size_{pageSize}";
        if (!cache.TryGetValue(cacheKey, out PagedResult<MovieEntry> cachedResults))
        {
            var query = db.Movies
                .OrderByDescending(m => m.Rating)
                .ThenBy(m => m.MovieName);


            int totalCount = await query.CountAsync();

            var ids = await query
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .Select(q => q.MovieName)
                .ToListAsync();

            var movies = await db.Movies
                .Where(m => ids.Contains(m.MovieName))
                .Include(m => m.Actors)
                .Include(m => m.Directors)
                .Include(m => m.Tags)
                .ToListAsync();


            cachedResults = new PagedResult<MovieEntry>
            {
                Items = movies,
                TotalCount = totalCount,
                PageNumber = pageNumber,
                PageSize = pageSize
            };

            cache.Set(cacheKey, cachedResults, cacheExpiration);
        }

        return cachedResults;
    }

    public async Task<MovieEntry> GetMovieExactAsync(string movieName)
    {
        string cacheKey = $"MovieDetail_{movieName}";
        if (!cache.TryGetValue(cacheKey, out MovieEntry cachedResults))
        {
            var query = db.Movies
                .Where(m => m.MovieName == movieName)
                .Include(m => m.Actors)
                .Include(m => m.Directors)
                .Include(m => m.Tags)
                .FirstOrDefault();
            cachedResults = query;
            cache.Set(cacheKey, cachedResults, cacheExpiration);
        }

        return cachedResults;
    }

    private async Task<List<SimilarMovie>> calculateSimilarity(String movieName)
    {
        var moviesToCompare = new HashSet<MovieEntry>();
        var movie = await db.Movies
            .Include(m => m.Actors).ThenInclude(actorEntry => actorEntry.Movies)
            .Include(m => m.Directors).ThenInclude(directorEntry => directorEntry.Movies)
            .Include(m => m.Tags).ThenInclude(tagEntry => tagEntry.Movies).AsSplitQuery()
            .FirstOrDefaultAsync(m => m.MovieName == movieName);

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
        return top10.ConvertAll(sim => new SimilarMovie(Movie.FromEntity(sim.Item1), sim.Item2));
    }

    public async Task<List<SimilarMovie>> GetSimilarMovies(string movieName)
    {
        var movieId = movieName;


        var similarMovies = db.MovieSimilarities
            .Where(ms => ms.MovieEntryId1 == movieId)
            .Include(m => m.MovieEntry1)
            .Include(m => m.MovieEntry2)
            .ToList();

        if (similarMovies.Count != 0)
        {
            var similarMoviesList = new List<SimilarMovie>();
            foreach (var sim in similarMovies)
            {
                similarMoviesList.Add(new SimilarMovie(Movie.FromEntity(sim.MovieEntry2), sim.Similarity));
            }

            return similarMoviesList;
        }

        return await calculateSimilarity(movieName);
    }
}

public class PagedResult<T>
{
    public List<T> Items { get; set; } = new();
    public int TotalCount { get; set; }
    public int PageNumber { get; set; }
    public int PageSize { get; set; }
    public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);
}

public enum SearchType
{
    Movie,
    Person,
    Tag
}

public class MovieResult
{
    public MovieEntry Movie { get; set; }
    public int MatchScore { get; set; }
}

public class MovieDetailViewModel
{
    public string MovieName { get; set; }
    public string Rating { get; set; }
    public string PosterUrl { get; set; }
    public string Description { get; set; }
    public List<ActorEntry> Actors { get; set; }
    public List<DirectorEntry> Directors { get; set; }
    public List<TagEntry> Tags { get; set; }
}
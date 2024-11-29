// using System.Diagnostics;
// using System.Globalization;
// using IMDBCLI.model;
//
// namespace IMDBCLI;
//
// public class Repository
// {
//     private const string actorDirectorsCodesPath = "../../../../ml-latest/ActorsDirectorsCodes_IMDB.tsv";
//     private const string actorDirectorsNamesPath = "../../../../ml-latest/ActorsDirectorsNames_IMDB.txt";
//     private const string ratingsPath = "../../../../ml-latest/Ratings_IMDB.tsv";
//     private const string movieCodesPath = "../../../../ml-latest/MovieCodes_IMDB.tsv";
//     private const string tagCodesPath = "../../../../ml-latest/TagCodes_MovieLens.csv";
//     private const string tagScoresPath = "../../../../ml-latest/TagScores_MovieLens.csv";
//     private const string linksPath = "../../../../ml-latest/links_IMDB_MovieLens.csv";
//
//     private Dictionary<string, Movie> movies = new();
//     private Dictionary<string, HashSet<Movie>> peopleToMovie = new();
//     private Dictionary<string, HashSet<Movie>> tagToMovie = new();
//     
//     private Dictionary<string, string> tagToName = new ();
//     private Dictionary<int,string> movieIDtoImdb = new();
//     private Dictionary<string, string> idToTag = new ();
//     private Dictionary<string, string> mvIdToName = new();
//     
//         void loadMovies(){
//             try
//             {
//                 using var sr = new StreamReader(movieCodesPath);
//                 bool flag = true;
//                 while (sr.ReadLine() is { } line)
//                 {
//                     if (flag)
//                     {
//                         flag = false;
//                         continue;
//                     }
//                     var lineData = line.Trim().Split('\t');
//                     if (lineData[3] == "US" || lineData[3] == "GB" || lineData[3] == "RU" || lineData[3] == "AU" || lineData[3] == "EU" || lineData[4] == "en" || lineData[4] == "ru")
//                     {
//                         if (mvIdToName.ContainsKey(lineData[0])) continue;
//                         if (movies.ContainsKey(lineData[2])) continue;
//                         var movie = new Movie(lineData[2]);
//                         movies.Add(lineData[2], movie);
//                         mvIdToName.Add(lineData[0], lineData[2]);
//                     }
//                 }
//             }
//             catch (Exception e)
//             {
//                 Console.WriteLine("The file could not be read(movieCodes):");
//                 Console.WriteLine(e.Message);
//             }
//
//             try
//             {
//                 using var sr = new StreamReader(ratingsPath);
//                 bool flag = true;
//                 while (sr.ReadLine() is { } line)
//                 {
//                     if (flag)
//                     {
//                         flag = false;
//                         continue;
//                     }
//                     var lineData = line.Trim().Split('\t', ' ');
//                     if (!mvIdToName.TryGetValue(lineData[0], out string? value)) continue;
//                     movies[value].rating = lineData[1];
//                 }
//             }
//             catch (Exception e)
//             {
//                 Console.WriteLine("The file could not be read(ratings):");
//                 Console.WriteLine(e.Message);
//             }
//         }
//
//         void loadPeople()
//         {
//             try
//             {
//                 using var sr = new StreamReader(actorDirectorsNamesPath);
//                 bool flag = true;
//                 while (sr.ReadLine() is { } line)
//                 {
//                     if (flag)
//                     {
//                         flag = false;
//                         continue;
//                     }
//                     var lineData = line.Trim().Split('\t');
//                     tagToName.Add(lineData[0], lineData[1]);
//                 }
//             }
//             catch (Exception e)
//             {
//                 Console.WriteLine("The file could not be read(actorDirectorsNames):");
//                 Console.WriteLine(e.Message);
//             }
//             
//             try
//             {
//                 using var sr = new StreamReader(actorDirectorsCodesPath);
//                 bool flag = true;
//                 while (sr.ReadLine() is { } line)
//                 {
//                     if (flag)
//                     {
//                         flag = false;
//                         continue;
//                     }
//                     var lineData = line.Trim().Split('\t');
//                     if (!mvIdToName.TryGetValue(lineData[0], out string? value)) continue;
//                     Movie mv = movies[value];
//                     if(!tagToName.TryGetValue(lineData[2], out string? tag)) continue;
//                     if (lineData[3] == "actor" || lineData[3] == "actress"){
//                         
//                         
//                         mv.actors.Add(tag);
//                         
//                         if (!peopleToMovie.ContainsKey(tag))
//                         {
//                             peopleToMovie[tag] = [];
//                         }
//                         peopleToMovie[tag].Add(mv);
//                     } else if (lineData[3] == "producer" || lineData[3] == "director")
//                     {
//                         
//
//                         mv.production.Add(tag);
//                         if (!peopleToMovie.ContainsKey(tag))
//                         {
//                             peopleToMovie[tag] = [];
//                         }
//                         peopleToMovie[tag].Add(mv);
//                     }
//                 }
//             }
//             catch (Exception e)
//             {
//                 Console.WriteLine("The file could not be read(actorDirectorsCodes):");
//                 Console.WriteLine(e.Message);
//             }
//         }
//
//         void loadTags()
//         {
//             try
//             {
//                 using var sr = new StreamReader(linksPath);
//                 bool flag = true;
//                 while (sr.ReadLine() is { } line)
//                 {
//                     if (flag)
//                     {
//                         flag = false;
//                         continue;
//                     }
//                     var lineData = line.Trim().Split(',');
//                     movieIDtoImdb.Add(int.Parse(lineData[0]), lineData[1]);
//                 }
//             }
//             catch (Exception e)
//             {
//                 Console.WriteLine("The file could not be read(links):");
//                 Console.WriteLine(e.Message);
//             }
//             
//             try
//             {
//                 using var sr = new StreamReader(tagCodesPath);
//                 bool flag = true;
//                 while (sr.ReadLine() is { } line)
//                 {
//                     if (flag)
//                     {
//                         flag = false;
//                         continue;
//                     }
//                     var lineData = line.Trim().Split(',');
//                     idToTag.Add(lineData[0], lineData[1]);
//                 }
//             }
//             catch (Exception e)
//             {
//                 Console.WriteLine("The file could not be read(tagCodes):");
//                 Console.WriteLine(e.Message);
//             }
//             try
//             {
//                 using var sr = new StreamReader(tagScoresPath);
//                 bool flag = true;
//                 while (sr.ReadLine() is { } line)
//                 {
//                     if (flag)
//                     {
//                         flag = false;
//                         continue;
//                     }
//                     var lineData = line.Trim().Split(',');
//                     if (float.Parse(lineData[2], CultureInfo.InvariantCulture.NumberFormat) > 0.5)
//                     {
//                         var imdbId = "tt" + movieIDtoImdb[int.Parse(lineData[0])];
//                         if (!mvIdToName.TryGetValue(imdbId, out string? value)) continue;
//                         Movie mv = movies[value];
//
//                         var tag = idToTag[lineData[1]];
//                         mv.tags.Add(tag);
//                         if (!tagToMovie.ContainsKey(tag)) tagToMovie[tag] = [];
//                         tagToMovie[tag].Add(mv);
//                     }
//                 }
//             }
//             catch (Exception e)
//             {
//                 Console.WriteLine("The file could not be read(tagScores):");
//                 Console.WriteLine(e.Message);
//             }
//         }
//
//         public Movie? getMovie(string imdbId)
//         {
//             initalizeData();
//             if (!movies.TryGetValue(imdbId, out Movie? value)) return null;
//             return value;
//         }
//
//         public HashSet<Movie>? getPerson(string imdbId)
//         {
//             initalizeData();
//             if (!peopleToMovie.TryGetValue(imdbId, out HashSet<Movie>? value)) return null;
//             return value;
//         }
//
//         public HashSet<Movie>? getTag(string imdbId)
//         {
//             initalizeData();
//             if (!tagToMovie.TryGetValue(imdbId, out HashSet<Movie>? value)) return null;
//             return value;
//         }
//
//         public int getMoviesNum()
//         {
//             initalizeData();
//             return movies.Count;
//         }
//
//         public int getPeopleNum()
//         {
//             initalizeData();
//             return peopleToMovie.Count;
//         }
//
//         public int getTagNum()
//         {
//             initalizeData();
//             return tagToMovie.Count;
//         }
//         
//         public void loadToDB(){}
//         
//         public void initalizeData()
//         {
//             if (movies.Count == 0)
//             {
//                 Stopwatch stopwatch = Stopwatch.StartNew();
//                 loadMovies();
//                 loadPeople();
//                 loadTags();
//                 
//                 // Останавливаем Stopwatch
//                 stopwatch.Stop();
//
//                 // Получаем прошедшее время
//                 TimeSpan elapsed = stopwatch.Elapsed;
//                 Console.WriteLine($"Время выполнения: {elapsed.TotalSeconds} секунд");
//                 
//             }
//         }
// }
using System;
using System.Collections.Generic;
using System.Linq;
using IMDBCLI;
using IMDBCLI.model;
using static IMDBCLI.Repository;

class Program
{
    // Словари для хранения данных
    
    static DBRepository repository = new();

    static void Main(string[] args)
    {
        
        Console.WriteLine("Привет! Добро пожаловать в Movie CLI.");
        Console.WriteLine("Введите команду. Для справки введите 'help'. Для выхода введите 'exit'.");
        Console.WriteLine();

        while (true)
        {
            Console.Write("movie-cli> ");
            string input = Console.ReadLine();
            
            if (string.IsNullOrWhiteSpace(input))
            {
                continue;
            }

            var tokens = Tokenize(input);
            if (tokens.Count == 0)
            {
                continue;
            }

            string command = tokens[0].ToLower();
            List<string> arguments = tokens.Skip(1).ToList();
            
            switch (command)
            {
                case "exit":
                case "quit":
                    Console.WriteLine("Выход из программы...");
                    return;

                case "help":
                    PrintHelp();
                    break;

                case "--movies":
                case "-m":
                    HandleMovies(arguments);
                    break;

                case "--people":
                case "-p":
                    HandlePeople(arguments);
                    break;

                case "--tags":
                case "-t":
                    HandleTags(arguments);
                    break;
                case "--db":
                    HandleDB();
                    break;

                default:
                    Console.WriteLine($"Неизвестная команда: {command}");
                    Console.WriteLine("Введите 'help' для списка доступных команд.");
                    break;
            }

            Console.WriteLine();
        }
    }

    /// <summary>
    /// Разбиение строки на токены с учетом кавычек.
    /// </summary>
    static List<string> Tokenize(string input)
    {
        var tokens = new List<string>();
        bool inQuotes = false;
        string current = "";

        foreach (char c in input)
        {
            if (c == '\"')

            {
                inQuotes = !inQuotes;
                continue;
            }

            if (char.IsWhiteSpace(c) && !inQuotes)
            {
                if (!string.IsNullOrEmpty(current))
                {
                    tokens.Add(current);
                    current = "";
                }
            }
            else
            {
                current += c;
            }
        }

        if (!string.IsNullOrEmpty(current))
        {
            tokens.Add(current);
        }

        return tokens;
    }

    static void HandleMovies(List<string> args)
    {
        if (args.Count == 0)
        {
            Console.WriteLine($"Количество фильмов: {repository.getMoviesNum()}");
            return;
        }

        string title = args[0];
        Movie? movie = repository.getMovie(title);
        if (movie != null)
        {
            Console.WriteLine(movie);
        }
        else
        {
            Console.WriteLine($"Фильм \"{title}\" не найден.");
        }
    }

    static void HandlePeople(List<string> args)
    {
        if (args.Count == 0)
        {
            Console.WriteLine($"Количество людей (актеров и режиссеров): {repository.getPeopleNum()}");
            return;
        }

        string name = args[0];
        HashSet<Movie>? movies = repository.getPerson(name);
        if (movies != null)
        {
            Console.WriteLine($"Человек: {name}");
            foreach (var movie in movies)
            {
                Console.WriteLine($"- {movie.movieName}");
            }
        }
        else
        {
            Console.WriteLine($"Человек \"{name}\" не найден.");
        }
    }

    static void HandleTags(List<string> args)
    {
        if (args.Count == 0)
        {
            Console.WriteLine($"Количество тегов: {repository.getTagNum()}");
            return;
        }

        string tag = args[0];

        HashSet<Movie>? movies = repository.getTag(tag);
        if (movies != null)
        {
            Console.WriteLine($"Тэг: {tag}");
            foreach (var movie in movies)
            {
                Console.WriteLine($"- {movie.movieName}");
            }
        }
        else
        {
            Console.WriteLine($"Тэг \"{tag}\" не найден.");
        }
    }
    
    static void HandleDB()
    {
        repository.loadToDB();
    }

    static void PrintHelp()
    {
        Console.WriteLine("Доступные команды:");
        Console.WriteLine("  --movies [название] или -m [название]   : Получить информацию о фильме или количество фильмов");
        Console.WriteLine("  --people [имя] или -p [имя]           : Получить фильмы по имени актера/режиссера или количество людей");
        Console.WriteLine("  --tags [тэг] или -t [тэг]             : Получить фильмы по тэгу или количество тэгов");
        Console.WriteLine("  --db                                   : Загрузить данные из текстового файла в базу данных");
        Console.WriteLine("  help                                   : Показать эту справку");
        Console.WriteLine("  exit или quit                          : Выйти из программы");
        Console.WriteLine();
        Console.WriteLine("Примеры использования:");
        Console.WriteLine("  --movies \"Inception\"");
        Console.WriteLine("  --movies");
        Console.WriteLine("  --people \"Christopher Nolan\"");
        Console.WriteLine("  --tags \"Sci-Fi\"");
    }
}

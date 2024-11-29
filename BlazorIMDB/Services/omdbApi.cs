namespace BlazorIMDB.Services;

public class OmdbApi
{
    private readonly HttpClient _httpClient;

    public OmdbApi(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<OmdbResponse> GetMovieDetailsAsync(string movieTitle)
    {
        var apiKey = "9a49d8d2";
        var url = $"https://www.omdbapi.com/?apikey={apiKey}&t={Uri.EscapeDataString(movieTitle)}";

        var response = await _httpClient.GetFromJsonAsync<OmdbResponse>(url);

        return response;
    }

    public class OmdbResponse
    {
        public string imdbID { get; set; }
        public string Title { get; set; }
        public string Poster { get; set; }
        public string Plot { get; set; } 
    }
}
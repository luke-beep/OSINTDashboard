using System.Diagnostics;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using OSINTDashboard.Models;
using OSINTDashboard.Utilities;

namespace OSINTDashboard.Controllers;

public class OSINTController : Controller
{
    private readonly ILogger<OSINTController> _logger;
    private readonly IHttpClientFactory _httpClientFactory;

    private const string APIUri = "https://server.leakosint.com/";
    private const string ContentType = "application/json";

    public OSINTController(ILogger<OSINTController> logger, IHttpClientFactory httpClientFactory)
    {
        _logger = logger;
        _httpClientFactory = httpClientFactory;
    }

    [HttpGet]
    public IActionResult Index()
    {
        var compositeModel = InitializeCompositeViewModel();
        LoadSearchHistory(compositeModel);
        _logger.LogInformation("Index page visited");
        return View(compositeModel);
    }

    private void LoadSearchHistory(CompositeViewModel viewModel)
    {
        viewModel.History = HttpContext.Session.GetObjectFromJson<List<HistoryViewModel>>("SearchHistory") ?? new List<HistoryViewModel>();
    }

    private void SaveSearchHistoryToSession(RequestViewModel requestModel, string jsonResponse)
    {
        var searchHistoryList = HttpContext.Session.GetObjectFromJson<List<HistoryViewModel>>("SearchHistory") ?? new List<HistoryViewModel>();

        var historyEntry = new HistoryViewModel
        {
            Id = searchHistoryList.Any() ? searchHistoryList.Max(h => h.Id) + 1 : 1,
            Token = requestModel.token,
            Request = requestModel.request,
            Limit = requestModel.limit,
            Language = requestModel.lang,
            JsonResponse = jsonResponse,
            SearchTime = DateTime.UtcNow
        };

        searchHistoryList.Add(historyEntry);

        HttpContext.Session.SetObjectAsJson("SearchHistory", searchHistoryList);
    }

    public async Task<IActionResult> LoadHistoryItem(int id)
    {
        var searchHistoryList = HttpContext.Session.GetObjectFromJson<List<HistoryViewModel>>("SearchHistory");
        var historyItem = searchHistoryList?.FirstOrDefault(h => h.Id == id);

        if (historyItem == null)
        {
            return NotFound();
        }

        var compositeViewModel = InitializeCompositeViewModel();
        compositeViewModel.Request = new RequestViewModel
        {
            token = historyItem.Token,
            request = historyItem.Request,
            limit = historyItem.Limit,
            lang = historyItem.Language
        };

        var success = await HandleResponseFromString(historyItem.JsonResponse, compositeViewModel);

        if (!success)
        {
            LogError("Failed to load history item {Id}", id);
        }

        compositeViewModel.History = searchHistoryList;

        return View("Index", compositeViewModel);
    }

    private Task<bool> HandleResponseFromString(string jsonResponse, CompositeViewModel compositeViewModel)
    {
        try
        {
            var searchResult = JsonSerializer.Deserialize<SearchResultViewModel>(jsonResponse);

            if (searchResult != null)
            {
                compositeViewModel.SearchResult = searchResult;
                if (compositeViewModel.Response != null) compositeViewModel.Response.JsonResponse = jsonResponse;
                return Task.FromResult(true);
            }
            else
            {
                LogError("Failed to deserialize the JSON response.");
                ModelState.AddModelError("", "Failed to process the server response.");
                return Task.FromResult(false);
            }
        }
        catch (JsonException ex)
        {
            LogError("JSON deserialization exception: {Message}", ex.Message);
            ModelState.AddModelError("", "Exception occurred while processing JSON response.");
            return Task.FromResult(false);
        }
    }

    [HttpPost]
    public IActionResult DownloadJsonResponse(int id)
    {
        var searchHistoryList = HttpContext.Session.GetObjectFromJson<List<HistoryViewModel>>("SearchHistory");
        var historyItem = searchHistoryList?.FirstOrDefault(h => h.Id == id);

        if (historyItem == null)
        {
            _logger.LogWarning("The JSON response for history item {Id} is not available for download.", id);
            return NotFound("The JSON response is not available.");
        }

        _logger.LogInformation("Providing the JSON response for history item {Id} for download.", id);
        var content = Encoding.UTF8.GetBytes(historyItem.JsonResponse);
        var fileName = $"response_{id}.json";
        return File(content, ContentType, fileName);
    }


    [HttpPost]
    public async Task<IActionResult> Index(CompositeViewModel model)
    {
        var compositeViewModel = InitializeCompositeViewModel();
        if (!ModelState.IsValid) return View(compositeViewModel);
        try
        {
            await SendRequestAndUpdateViewModel(model, compositeViewModel);
        }
        catch (Exception ex)
        {
            HandleException(ex);
        }
        return View(compositeViewModel);
    }

    private static CompositeViewModel InitializeCompositeViewModel()
    {
        return new CompositeViewModel
        {
            Request = new RequestViewModel(),
            Response = new RespondViewModel(),
            SearchResult = new SearchResultViewModel(),
            LanguageOptions = GetLanguageOptions()
        };
    }
    private async Task SendRequestAndUpdateViewModel(CompositeViewModel model, CompositeViewModel compositeViewModel)
    {
        var client = _httpClientFactory.CreateClient();

        LogInformation("Sending request to {url}", APIUri);

        if (model.Request != null)
        {
            var response = await SendHttpPostRequest(model.Request, client, APIUri);
            var success = await HandleResponse(response, compositeViewModel);
            if (success)
            {
                SaveSearchHistoryToSession(model.Request, compositeViewModel.Response?.JsonResponse ?? string.Empty);
            }
        }
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }

    public IActionResult DownloadResponse()
    {
        const string fileName = "response.json";

        var jsonResponse = HttpContext.Session.GetString("JsonResponse");

        if (string.IsNullOrEmpty(jsonResponse))
        {
            _logger.LogWarning("The JSON response is not available for download.");
            return NotFound("The JSON response is not available.");
        }

        _logger.LogInformation("Providing the JSON response for download.");
        var content = Encoding.UTF8.GetBytes(jsonResponse);
        return File(content, ContentType, fileName);
    }

    private void LogInformation(string message, params object[] args)
    {
        _logger.LogInformation(message, args);
    }

    private void LogError(string message, params object[] args)
    {
        _logger.LogError(message, args);
    }

    private static async Task<HttpResponseMessage> SendHttpPostRequest(RequestViewModel requestModel, HttpClient client, string url)
    {
        var jsonContent = JsonSerializer.Serialize(requestModel);
        var content = new StringContent(jsonContent, Encoding.UTF8, ContentType);

        return await client.PostAsync(url, content);
    }

    private async Task<bool> HandleResponse(HttpResponseMessage response, CompositeViewModel compositeViewModel)
    {
        LogInformation("Response status code: {StatusCode}", response.StatusCode);

        if (response.IsSuccessStatusCode)
        {
            var jsonResponse = await response.Content.ReadAsStringAsync();
            var searchResult = JsonSerializer.Deserialize<SearchResultViewModel>(jsonResponse);

            if (searchResult != null)
            {
                if (compositeViewModel.Response != null)
                {
                    compositeViewModel.SearchResult = searchResult;
                    compositeViewModel.Response.JsonResponse = jsonResponse;
                }
                HttpContext.Session.SetString("JsonResponse", jsonResponse);
                return true;
            }

            LogError("Failed to deserialize the JSON response.");
            ModelState.AddModelError("", "Failed to process the server response.");
            return false;
        }

        var errorResponse = await response.Content.ReadAsStringAsync();
        LogError("Error from server: {errorResponse}", errorResponse);
        ModelState.AddModelError("", $"Error occurred while sending request: {response.StatusCode}");
        return false;
    }

    private void HandleException(Exception ex)
    {
        if (ex is HttpRequestException httpRequestException)
        {
            LogError("HTTP request exception: {Message}", httpRequestException.Message);
            ModelState.AddModelError("", "Exception occurred while sending request.");
        }
        else
        {
            LogError("General exception: {Message}", ex.Message);
            ModelState.AddModelError("", "An unexpected error occurred.");
        }
    }
    private static Dictionary<string, string> GetLanguageOptions()
    {
        return new Dictionary<string, string>
        {
            { "af", "Afrikaans" },
            { "sq", "Albanian" },
            { "am", "Amharic" },
            { "ar", "Arabic" },
            { "hy", "Armenian" },
            { "as", "Assamese" },
            { "ay", "Aymara" },
            { "az", "Azerbaijani" },
            { "bm", "Bambara" },
            { "eu", "Basque" },
            { "be", "Belarusian" },
            { "bn", "Bengali" },
            { "bho", "Bhojpuri" },
            { "bs", "Bosnian" },
            { "bg", "Bulgarian" },
            { "ca", "Catalan" },
            { "ceb", "Cebuano" },
            { "zh-CN", "Chinese (Simplified)" },
            { "zh-TW", "Chinese (Traditional)" },
            { "co", "Corsican" },
            { "hr", "Croatian" },
            { "cs", "Czech" },
            { "da", "Danish" },
            { "dv", "Dhivehi" },
            { "doi", "Dogri" },
            { "nl", "Dutch" },
            { "en", "English" },
            { "eo", "Esperanto" },
            { "et", "Estonian" },
            { "ee", "Ewe" },
            { "fil", "Filipino (Tagalog)" },
            { "fi", "Finnish" },
            { "fr", "French" },
            { "fy", "Frisian" },
            { "gl", "Galician" },
            { "ka", "Georgian" },
            { "de", "German" },
            { "el", "Greek" },
            { "gn", "Guarani" },
            { "gu", "Gujarati" },
            { "ht", "Haitian Creole" },
            { "ha", "Hausa" },
            { "haw", "Hawaiian" },
            { "he", "Hebrew" },
            { "hi", "Hindi" },
            { "hmn", "Hmong" },
            { "hu", "Hungarian" },
            { "is", "Icelandic" },
            { "ig", "Igbo" },
            { "ilo", "Ilocano" },
            { "id", "Indonesian" },
            { "ga", "Irish" },
            { "it", "Italian" },
            { "ja", "Japanese" },
            { "jv", "Javanese" },
            { "kn", "Kannada" },
            { "kk", "Kazakh" },
            { "km", "Khmer" },
            { "rw", "Kinyarwanda" },
            { "gom", "Konkani" },
            { "ko", "Korean" },
            { "kri", "Krio" },
            { "ku", "Kurdish" },
            { "ckb", "Kurdish (Sorani)" },
            { "ky", "Kyrgyz" },
            { "lo", "Lao" },
            { "la", "Latin" },
            { "lv", "Latvian" },
            { "ln", "Lingala" },
            { "lt", "Lithuanian" },
            { "lg", "Luganda" },
            { "lb", "Luxembourgish" },
            { "mk", "Macedonian" },
            { "mai", "Maithili" },
            { "mg", "Malagasy" },
            { "ms", "Malay" },
            { "ml", "Malayalam" },
            { "mt", "Maltese" },
            { "mi", "Maori" },
            { "mr", "Marathi" },
            { "mni-Mtei", "Meiteilon (Manipuri)" },
            { "lus", "Mizo" },
            { "mn", "Mongolian" },
            { "my", "Myanmar (Burmese)" },
            { "ne", "Nepali" },
            { "no", "Norwegian" },
            { "ny", "Nyanja (Chichewa)" },
            { "or", "Odia (Oriya)" },
            { "om", "Oromo" },
            { "ps", "Pashto" },
            { "fa", "Persian" },
            { "pl", "Polish" },
            { "pt", "Portuguese (Portugal, Brazil)" },
            { "pa", "Punjabi" },
            { "qu", "Quechua" },
            { "ro", "Romanian" },
            { "ru", "Russian" },
            { "sm", "Samoan" },
            { "sa", "Sanskrit" },
            { "gd", "Scots Gaelic" },
            { "nso", "Sepedi" },
            { "sr", "Serbian" },
            { "st", "Sesotho" },
            { "sn", "Shona" },
            { "sd", "Sindhi" },
            { "si", "Sinhala (Sinhalese)" },
            { "sk", "Slovak" },
            { "sl", "Slovenian" },
            { "so", "Somali" },
            { "es", "Spanish" },
            { "su", "Sundanese" },
            { "sw", "Swahili" },
            { "sv", "Swedish" },
            { "tl", "Tagalog (Filipino)" },
            { "tg", "Tajik" },
            { "ta", "Tamil" },
            { "tt", "Tatar" },
            { "te", "Telugu" },
            { "th", "Thai" },
            { "ti", "Tigrinya" },
            { "ts", "Tsonga" },
            { "tr", "Turkish" },
            { "tk", "Turkmen" },
            { "ak", "Twi (Akan)" },
            { "uk", "Ukrainian" },
            { "ur", "Urdu" },
            { "ug", "Uyghur" },
            { "uz", "Uzbek" },
            { "vi", "Vietnamese" },
            { "cy", "Welsh" },
            { "xh", "Xhosa" },
            { "yi", "Yiddish" },
            { "yo", "Yoruba" },
            { "zu", "Zulu" }
        };
    }
}


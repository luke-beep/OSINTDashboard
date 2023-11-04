using System.Diagnostics;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using OSINTDashboard.Models;

namespace OSINTDashboard.Controllers;

public class OSINTController : Controller
{
    private readonly ILogger<OSINTController> _logger;
    private readonly IHttpClientFactory _httpClientFactory;

    public OSINTController(ILogger<OSINTController> logger, IHttpClientFactory httpClientFactory)
    {
        _logger = logger;
        _httpClientFactory = httpClientFactory;
    }

    [HttpGet]
    public IActionResult Index()
    {
        var compositeModel = new OSINTCompositeViewModel
        {
            Request = new POSTViewModel(),
            Response = new OSINTViewModel(),
            LanguageOptions = GetLanguageOptions() 
        };

        _logger.LogInformation("Index page visited");
        return View(compositeModel);
    }

    [HttpPost]
    public async Task<IActionResult> Index(OSINTCompositeViewModel model)
    {
        var compositeViewModel = new OSINTCompositeViewModel
        {
            Request = new POSTViewModel(),
            Response = new OSINTViewModel(),
            LanguageOptions = GetLanguageOptions()
        };
        if (!ModelState.IsValid) return View(compositeViewModel);
        var url = "https://server.leakosint.com/";
        var client = _httpClientFactory.CreateClient();
        _logger.LogInformation("Sending request to {url}", url);
        try
        {
            var jsonContent = JsonSerializer.Serialize(model.Request);
            var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

            var response = await client.PostAsync(url, content);
            _logger.LogInformation("Response status code: {StatusCode}", response.StatusCode);
            if (response.IsSuccessStatusCode)
            {
                var jsonResponse = await response.Content.ReadAsStringAsync();
                _logger.LogInformation("JSON Response: {jsonResponse}", jsonResponse);
                compositeViewModel.Response.JsonResponse = jsonResponse;
                HttpContext.Session.SetString("JsonResponse", jsonResponse);
            }
            else
            {
                var errorResponse = await response.Content.ReadAsStringAsync();
                _logger.LogError("Error from server: {errorResponse}", errorResponse);
                ModelState.AddModelError("", $"Error occurred while sending request: {response.StatusCode}");
            }
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError("HTTP request exception: {Message}", ex.Message);
            ModelState.AddModelError("", "Exception occurred while sending request.");
        }
        catch (Exception ex)
        {
            _logger.LogError("General exception: {Message}", ex.Message);
            ModelState.AddModelError("", "An unexpected error occurred.");
        }

        return View(compositeViewModel);
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }

    public IActionResult DownloadResponse()
    {
        var jsonResponse = HttpContext.Session.GetString("JsonResponse") ?? string.Empty;

        if (string.IsNullOrEmpty(jsonResponse)) return NotFound("The JSON response is not available.");
        var content = Encoding.UTF8.GetBytes(jsonResponse);
        var contentType = "application/json";
        var fileName = "response.json";
        return File(content, contentType, fileName);

    }

    private Dictionary<string, string> GetLanguageOptions()
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


namespace OSINTDashboard.Models
{
    public class OSINTCompositeViewModel
    {
        public OSINTViewModel? Response { get; set; }
        public POSTViewModel? Request { get; set; }
        public Dictionary<string, string> LanguageOptions { get; set; } = new();
    }
}

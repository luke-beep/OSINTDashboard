﻿namespace OSINTDashboard.Models
{
    public class CompositeViewModel
    {
        public AuthenticationViewModel? Authentication { get; set; }
        public RespondViewModel? Response { get; set; }
        public RequestViewModel? Request { get; set; }
        public SearchResultViewModel? SearchResult { get; set; }
        public List<HistoryViewModel>? History { get; set; }
        public Dictionary<string, string> LanguageOptions { get; set; } = new();
        public bool ShowTokenField { get; set; } = true;
    }
}

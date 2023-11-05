#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
namespace OSINTDashboard.Models;

public class HistoryViewModel
{
    public int Id { get; set; }
    public string Token { get; set; }
    public string Request { get; set; }
    public int Limit { get; set; }
    public string Language { get; set; }
    public string JsonResponse { get; set; }
    public DateTime SearchTime { get; set; }
}
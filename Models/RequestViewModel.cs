using System.ComponentModel.DataAnnotations;
#pragma warning disable IDE1006
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.

namespace OSINTDashboard.Models;

public class RequestViewModel
{
    [Required(ErrorMessage = "Please enter a token")]
    public string token { get; set; }

    [Required(ErrorMessage = "Please enter a query")]
    public string request { get; set; }

    [Required(ErrorMessage = "Please enter a limit")]
    public int limit { get; set; }

    [Required(ErrorMessage = "Please enter a language")]
    public string lang { get; set; }
}
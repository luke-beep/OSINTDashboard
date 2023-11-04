using System.ComponentModel.DataAnnotations;

namespace OSINTDashboard.Models;

public class POSTViewModel
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
namespace MeetAmalfiCoast.Models;

public class GoogleReviewsViewModel
{
    public string Name { get; set; } = "";
    public double Rating { get; set; }
    public int UserRatingCount { get; set; }
    public List<GoogleReviewViewModel> Reviews { get; set; } = [];
}

public class GoogleReviewViewModel
{
    public string Author { get; set; } = "";
    public double Rating { get; set; }
    public string Text { get; set; } = "";
}
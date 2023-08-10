namespace CryptoBank.WebApi.Data.Entities;

public class News
{
    public Guid Id { get; set; }

    public string Title { get; set; } = string.Empty;

    public DateTime Date { get; set; }

    public string Content { get; set; } = string.Empty;

    public string Author { get; set; } = string.Empty;
}

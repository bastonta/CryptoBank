using CryptoBank.WebApi.Configurations;
using CryptoBank.WebApi.Data.Entities;
using CryptoBank.WebApi.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace CryptoBank.WebApi.Controllers;

[ApiController]
[Route("[controller]")]
public class NewsController : ControllerBase
{
    private readonly INewsService _newsService;
    private readonly NewsOptions _newsOptions;

    public NewsController(
        INewsService newsService,
        IOptions<NewsOptions> options)
    {
        _newsService = newsService;
        _newsOptions = options.Value;
    }

    [HttpGet]
    public async Task<News[]> Get(CancellationToken cancellationToken)
    {
        var news = await _newsService.GetNews(_newsOptions.Count, cancellationToken);
        return news;
    }
}

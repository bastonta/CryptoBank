namespace CryptoBank.WebApi.Features.Identity.Extensions;

public static class CookieExtension
{
    private const string COOKIE_KEY = "refresh_token";

    public static void AddTokenToCookie(this HttpContext httpContext, string token, DateTime expired)
    {
        var cookieOptions = new CookieOptions
        {
            HttpOnly = true,
            Expires = expired,
            SameSite = SameSiteMode.Strict,
            Secure = true,
            Path = "/identity/refreshtoken"
        };
        httpContext.Response.Cookies.Append(COOKIE_KEY, token, cookieOptions);
    }

    public static string? GetTokenFromCookie(this HttpContext httpContext)
    {
        return httpContext.Request.Cookies[COOKIE_KEY];
    }
}

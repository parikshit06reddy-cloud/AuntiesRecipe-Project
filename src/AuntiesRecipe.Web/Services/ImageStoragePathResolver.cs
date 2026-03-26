namespace AuntiesRecipe.Web.Services;

public static class ImageStoragePathResolver
{
    public static string Resolve(IWebHostEnvironment environment, IConfiguration configuration)
    {
        var configuredRoot = configuration["ImageStorage:RootPath"];
        if (!string.IsNullOrWhiteSpace(configuredRoot))
        {
            return Path.GetFullPath(configuredRoot);
        }

        // Azure App Service sets WEBSITE_SITE_NAME and HOME.
        var siteName = Environment.GetEnvironmentVariable("WEBSITE_SITE_NAME");
        var home = Environment.GetEnvironmentVariable("HOME");
        if (!string.IsNullOrWhiteSpace(siteName) && !string.IsNullOrWhiteSpace(home))
        {
            return Path.Combine(home, "site", "data", "auntiesrecipe-images");
        }

        // Local/default fallback.
        return Path.Combine(environment.ContentRootPath, "App_Data", "images");
    }
}

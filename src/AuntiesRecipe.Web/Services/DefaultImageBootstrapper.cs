namespace AuntiesRecipe.Web.Services;

public static class DefaultImageBootstrapper
{
    public static void EnsureSeedImages(IWebHostEnvironment environment, IConfiguration configuration)
    {
        var sourceRoot = Path.Combine(environment.ContentRootPath, "Defaults", "menu");
        if (!Directory.Exists(sourceRoot))
        {
            return;
        }

        var storageRoot = ImageStoragePathResolver.Resolve(environment, configuration);
        var destinationRoot = Path.Combine(storageRoot, "menu");
        Directory.CreateDirectory(destinationRoot);

        foreach (var sourceFile in Directory.GetFiles(sourceRoot, "*.png", SearchOption.TopDirectoryOnly))
        {
            var fileName = Path.GetFileName(sourceFile);
            var destinationFile = Path.Combine(destinationRoot, fileName);
            CopyWithRetry(sourceFile, destinationFile);
        }
    }

    private static void CopyWithRetry(string sourceFile, string destinationFile)
    {
        const int maxAttempts = 3;
        for (var attempt = 1; attempt <= maxAttempts; attempt++)
        {
            try
            {
                // Overwrite makes startup idempotent and avoids file-exists races
                // when multiple app hosts start in parallel (CI integration tests).
                File.Copy(sourceFile, destinationFile, overwrite: true);
                return;
            }
            catch (IOException) when (attempt < maxAttempts)
            {
                Thread.Sleep(50);
            }
            catch (UnauthorizedAccessException) when (attempt < maxAttempts)
            {
                Thread.Sleep(50);
            }
        }
    }
}

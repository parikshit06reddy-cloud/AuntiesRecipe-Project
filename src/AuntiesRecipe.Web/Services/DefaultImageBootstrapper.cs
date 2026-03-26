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

            if (!File.Exists(destinationFile))
            {
                File.Copy(sourceFile, destinationFile, overwrite: false);
            }
        }
    }
}

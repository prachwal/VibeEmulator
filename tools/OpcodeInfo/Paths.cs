namespace OpcodeInfo;

/// <summary>Odnajdywanie katalogu repo i wyświetlanie ścieżek.</summary>
internal static class Paths
{
    private const string SolutionFile = "VibeEmulator.slnx";

    /// <summary>
    /// Szuka katalogu repo (pliku <c>VibeEmulator.slnx</c>) w górę od katalogu bieżącego,
    /// a potem od katalogu binarki — narzędzie bywa uruchamiane z <c>bin/Release/…</c>.
    /// </summary>
    public static string? FindRepositoryRoot()
    {
        return FindUpwards(Environment.CurrentDirectory) ?? FindUpwards(AppContext.BaseDirectory);
    }

    /// <summary>Ścieżka względna do katalogu bieżącego z ukośnikami <c>/</c>; poza drzewem — ścieżka pełna.</summary>
    public static string Display(string path)
    {
        string full;
        try
        {
            full = Path.GetFullPath(path);
        }
        catch (Exception ex) when (ex is ArgumentException or NotSupportedException or PathTooLongException)
        {
            return path;
        }

        string relative = Path.GetRelativePath(Environment.CurrentDirectory, full);
        if (relative.StartsWith("..", StringComparison.Ordinal) || Path.IsPathRooted(relative))
        {
            return full.Replace('\\', '/');
        }

        return relative.Replace('\\', '/');
    }

    private static string? FindUpwards(string start)
    {
        DirectoryInfo? directory;
        try
        {
            directory = new DirectoryInfo(start);
        }
        catch (Exception ex) when (ex is ArgumentException or NotSupportedException or PathTooLongException)
        {
            return null;
        }

        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, SolutionFile)))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        return null;
    }
}

using System.IO.Compression;
using System.Text.Json;

namespace LibPostal.Net.Data;

/// <summary>
/// Downloads and manages LibPostal model files.
/// </summary>
public class ModelDownloader
{
    private readonly string _dataDirectory;
    private readonly HttpClient _httpClient;
    private const string DefaultVersion = "v1.0.0";

    /// <summary>
    /// Initializes a new instance of the <see cref="ModelDownloader"/> class.
    /// </summary>
    /// <param name="dataDirectory">The directory to store models. If null, uses default (~/.libpostal).</param>
    public ModelDownloader(string? dataDirectory = null)
    {
        _dataDirectory = dataDirectory ?? GetDefaultDataDirectory();
        _httpClient = new HttpClient
        {
            Timeout = TimeSpan.FromMinutes(30)
        };
    }

    /// <summary>
    /// Gets the default data directory path.
    /// </summary>
    public static string GetDefaultDataDirectory()
    {
        var userProfile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        return Path.Combine(userProfile, ".libpostal");
    }

    /// <summary>
    /// Downloads LibPostal models asynchronously.
    /// </summary>
    /// <param name="components">The components to download.</param>
    /// <param name="version">The model version.</param>
    /// <param name="progress">Progress reporter.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    public async Task DownloadModelsAsync(
        ModelComponent components = ModelComponent.Parser,
        string version = DefaultVersion,
        IProgress<DownloadProgress>? progress = null,
        CancellationToken cancellationToken = default)
    {
        // Create data directory
        Directory.CreateDirectory(_dataDirectory);

        var toDownload = GetComponentsToDownload(components);

        foreach (var component in toDownload)
        {
            if (AreModelsDownloaded(component))
            {
                progress?.Report(new DownloadProgress
                {
                    Component = component.ToString(),
                    Status = "Already exists",
                    PercentComplete = 100
                });
                continue;
            }

            await DownloadComponentAsync(component, version, progress, cancellationToken);
        }
    }

    /// <summary>
    /// Checks if models are downloaded for the specified component.
    /// </summary>
    /// <param name="component">The component to check.</param>
    /// <returns>True if models are downloaded; otherwise, false.</returns>
    public bool AreModelsDownloaded(ModelComponent component = ModelComponent.Parser)
    {
        return component switch
        {
            ModelComponent.Parser => File.Exists(Path.Combine(_dataDirectory, "address_parser", "address_parser_crf.dat")),
            ModelComponent.LanguageClassifier => File.Exists(Path.Combine(_dataDirectory, "language_classifier", "language_classifier.dat")),
            ModelComponent.Base => Directory.Exists(Path.Combine(_dataDirectory, "numex")),
            _ => false
        };
    }

    private async Task DownloadComponentAsync(
        ModelComponent component,
        string version,
        IProgress<DownloadProgress>? progress,
        CancellationToken cancellationToken)
    {
        var componentName = component.ToString().ToLowerInvariant();
        var url = GetComponentUrl(component, version);
        var archivePath = Path.Combine(_dataDirectory, $"{componentName}.tar.gz");

        progress?.Report(new DownloadProgress
        {
            Component = componentName,
            Status = "Downloading",
            PercentComplete = 0
        });

        // Download archive
        using (var response = await _httpClient.GetAsync(url, HttpCompletionOption.ResponseHeadersRead, cancellationToken))
        {
            response.EnsureSuccessStatusCode();

            var totalBytes = response.Content.Headers.ContentLength ?? 0;
            var downloadedBytes = 0L;

            using var fileStream = new FileStream(archivePath, FileMode.Create, FileAccess.Write, FileShare.None);
            using var contentStream = await response.Content.ReadAsStreamAsync(cancellationToken);

            var buffer = new byte[8192];
            int bytesRead;

            while ((bytesRead = await contentStream.ReadAsync(buffer, cancellationToken)) > 0)
            {
                await fileStream.WriteAsync(buffer.AsMemory(0, bytesRead), cancellationToken);
                downloadedBytes += bytesRead;

                if (totalBytes > 0)
                {
                    var percent = (int)((downloadedBytes * 100) / totalBytes);
                    progress?.Report(new DownloadProgress
                    {
                        Component = componentName,
                        Status = "Downloading",
                        PercentComplete = percent,
                        DownloadedBytes = downloadedBytes,
                        TotalBytes = totalBytes
                    });
                }
            }
        }

        progress?.Report(new DownloadProgress
        {
            Component = componentName,
            Status = "Extracting",
            PercentComplete = 95
        });

        // Extract archive (requires tar to be installed)
        await ExtractTarGzAsync(archivePath, _dataDirectory);

        // Clean up archive
        File.Delete(archivePath);

        progress?.Report(new DownloadProgress
        {
            Component = componentName,
            Status = "Complete",
            PercentComplete = 100
        });
    }

    private static string GetComponentUrl(ModelComponent component, string version)
    {
        var baseUrl = $"https://github.com/openvenues/libpostal/releases/download/{version}";

        return component switch
        {
            ModelComponent.Parser => $"{baseUrl}/parser.tar.gz",
            ModelComponent.LanguageClassifier => $"{baseUrl}/language_classifier.tar.gz",
            ModelComponent.Base => $"{baseUrl}/libpostal_data.tar.gz",
            _ => throw new ArgumentException($"Unknown component: {component}")
        };
    }

    private static async Task ExtractTarGzAsync(string archivePath, string destinationDir)
    {
        // Note: .NET doesn't have built-in tar.gz support
        // Options:
        // 1. Shell out to tar command (most reliable)
        // 2. Use SharpZipLib / SharpCompress NuGet package
        // 3. Decompress .gz then extract .tar

        // For now, shell out to tar (most portable)
        var tarPath = OperatingSystem.IsWindows() ? "tar.exe" : "tar";

        var processStartInfo = new System.Diagnostics.ProcessStartInfo
        {
            FileName = tarPath,
            Arguments = $"-xzf \"{archivePath}\" -C \"{destinationDir}\"",
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        using var process = System.Diagnostics.Process.Start(processStartInfo);

        if (process == null)
        {
            throw new InvalidOperationException("Failed to start tar process.");
        }

        await process.WaitForExitAsync();

        if (process.ExitCode != 0)
        {
            var error = await process.StandardError.ReadToEndAsync();
            throw new InvalidOperationException($"tar extraction failed: {error}");
        }
    }

    private static List<ModelComponent> GetComponentsToDownload(ModelComponent components)
    {
        var result = new List<ModelComponent>();

        if ((components & ModelComponent.Parser) != 0)
            result.Add(ModelComponent.Parser);

        if ((components & ModelComponent.LanguageClassifier) != 0)
            result.Add(ModelComponent.LanguageClassifier);

        if ((components & ModelComponent.Base) != 0)
            result.Add(ModelComponent.Base);

        return result;
    }
}

/// <summary>
/// Model component flags.
/// </summary>
[Flags]
public enum ModelComponent
{
    /// <summary>
    /// Parser models (CRF, vocabulary, phrases).
    /// </summary>
    Parser = 1,

    /// <summary>
    /// Language classifier model.
    /// </summary>
    LanguageClassifier = 2,

    /// <summary>
    /// Base data (expansions, numex, transliteration).
    /// </summary>
    Base = 4,

    /// <summary>
    /// All components.
    /// </summary>
    All = Parser | LanguageClassifier | Base
}

/// <summary>
/// Download progress information.
/// </summary>
public class DownloadProgress
{
    /// <summary>
    /// Gets or sets the component being downloaded.
    /// </summary>
    public string Component { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the current status.
    /// </summary>
    public string Status { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the percent complete (0-100).
    /// </summary>
    public int PercentComplete { get; set; }

    /// <summary>
    /// Gets or sets the number of bytes downloaded.
    /// </summary>
    public long DownloadedBytes { get; set; }

    /// <summary>
    /// Gets or sets the total bytes to download.
    /// </summary>
    public long TotalBytes { get; set; }
}

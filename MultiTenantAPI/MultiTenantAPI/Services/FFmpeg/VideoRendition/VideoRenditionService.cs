using System.Diagnostics;

namespace MultiTenantAPI.Services.FFmpeg.VideoRendition
{
    public class VideoRenditionService : IVideoRenditionService
    {
        private readonly ILogger<VideoRenditionService> _logger;

        private static readonly Dictionary<string, (int width, int height)> RenditionResolutions = new()
    {
        { "480p", (854, 480) },
        { "720p", (1280, 720) },
        { "1080p", (1920, 1080) },
        { "2160p", (3840, 2160) }
    };


        public VideoRenditionService(ILogger<VideoRenditionService> logger)
        {
            _logger = logger;
        }

        public async Task<string> GenerateVideoRenditionsAsync(string inputPath, string rendition)
        {

            if (!RenditionResolutions.ContainsKey(rendition))
                throw new ArgumentException($"Unsupported rendition: {rendition}");

            var (width, height) = RenditionResolutions[rendition];

            var outputDir = Path.GetDirectoryName(inputPath)!;
            var fileNameWithoutExt = Path.GetFileNameWithoutExtension(inputPath);
            var outputPath = Path.Combine(outputDir, $"{fileNameWithoutExt}_{rendition}.mp4");

            var ffmpegArgs = $"-i \"{inputPath}\" -vf scale={width}:{height} -c:v libx264 -preset fast -crf 23 -c:a aac -strict -2 \"{outputPath}\"";

            var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "ffmpeg",
                    Arguments = ffmpegArgs,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                }
            };

            process.Start();
            string stderr = await process.StandardError.ReadToEndAsync();
            await process.WaitForExitAsync();

            if (process.ExitCode != 0)
                throw new Exception($"FFmpeg failed: {stderr}");

            return outputPath;
        }



        public async Task<bool> GetRenditionLabelAsync(string filePath, string requiredRendition)
        {
            if (!RenditionResolutions.TryGetValue(requiredRendition, out var requiredSize))
            {
                _logger.LogError("Required rendition '{RequiredRendition}' not found", requiredRendition);
                return false;
            }

            Console.WriteLine(filePath);
            var resolution = await GetVideoResolutionAsync(filePath);
            if (string.IsNullOrEmpty(resolution))
            {
                _logger.LogError("Got null from VideoResolution labeling function");
                return false;
            }

            var parts = resolution.Split('x');
            int width = 0, height = 0; // Initialize variables to avoid CS0165 errors
            if (parts.Length == 2 && int.TryParse(parts[0], out width) && int.TryParse(parts[1], out height))
            {
                // Support for portrait/landscape
                var actualWidth = Math.Max(width, height);
                var actualHeight = Math.Min(width, height);

                var requiredWidth = Math.Max(requiredSize.width, requiredSize.height);
                var requiredHeight = Math.Min(requiredSize.width, requiredSize.height);

                // Only allow if actual resolution is greater than or equal to the required one
                if ((actualWidth >= requiredWidth && actualHeight >= requiredHeight) ||
    (actualWidth >= requiredHeight && actualHeight >= requiredWidth))
                {
                    _logger.LogInformation("Video resolution {ActualWidth}x{ActualHeight} meets or exceeds required {RequiredWidth}x{RequiredHeight} for rendition '{Rendition}'",
                        actualWidth, actualHeight, requiredWidth, requiredHeight, requiredRendition);
                    return true;
                }

                _logger.LogError("Actual resolution {ActualWidth}x{ActualHeight} is lower than required {RequiredWidth}x{RequiredHeight}",
                    actualWidth, actualHeight, requiredWidth, requiredHeight);
            }
            else
            {
                _logger.LogError("Parsing error");
            }

            return false;
        }

        private async Task<string?> GetVideoResolutionAsync(string filePath)
        {
            try
            {
                var psi = new ProcessStartInfo
                {
                    FileName = "ffprobe",
                    Arguments = $"-v error -select_streams v:0 -show_entries stream=width,height -of csv=p=0:s=x \"{filePath}\"",
                    RedirectStandardOutput = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                using var process = Process.Start(psi);
                if (process == null) return null;

                string output = await process.StandardOutput.ReadToEndAsync();
                await process.WaitForExitAsync();

                return output.Trim();
            }
            catch
            {
                return null;
            }
        }

    }
}

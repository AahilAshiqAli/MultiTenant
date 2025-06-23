using System.Diagnostics;
using System.Text;

namespace MultiTenantAPI.Services.Converter
{
    public class FFmpegService : IFFmpegService
    {
        private readonly ILogger<FFmpegService> _logger;

        private static readonly Dictionary<string, (int width, int height)> RenditionResolutions = new()
    {
        { "480p", (854, 480) },
        { "720p", (1280, 720) },
        { "1080p", (1920, 1080) },
        { "2160p", (3840, 2160) }
    };


        public FFmpegService(ILogger<FFmpegService> logger)
        {
            _logger = logger;
        }

        public async Task<string> ConvertToMp4Async(string inputPath)
        {
            _logger.LogInformation("Starting MP4 conversion for input: {InputPath}", inputPath);

            string outputPath = Path.ChangeExtension(inputPath, ".mp4");
            string args = $"-i \"{inputPath}\" -c:v libx264 -preset fast -crf 23 -c:a aac -b:a 128k \"{outputPath}\"";

            _logger.LogDebug("FFmpeg arguments: {Arguments}", args);

            var processInfo = new ProcessStartInfo
            {
                FileName = "C:\\ffmpeg\\ffmpeg-7.1.1-essentials_build\\bin\\ffmpeg.exe",
                Arguments = args,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            var outputBuilder = new StringBuilder();
            var errorBuilder = new StringBuilder();

            using var process = new Process { StartInfo = processInfo, EnableRaisingEvents = true };

            process.OutputDataReceived += (sender, e) =>
            {
                if (!string.IsNullOrEmpty(e.Data))
                {
                    outputBuilder.AppendLine(e.Data);
                }
            };

            process.ErrorDataReceived += (sender, e) =>
            {
                if (!string.IsNullOrEmpty(e.Data))
                {
                    errorBuilder.AppendLine(e.Data);
                }
            };

            process.Start();

            _logger.LogInformation("FFmpeg process started. PID: {ProcessId}", process.Id);

            // Begin reading both output streams
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();

            await process.WaitForExitAsync();

            string output = outputBuilder.ToString();
            string error = errorBuilder.ToString();

            _logger.LogDebug("FFmpeg process exited with code {ExitCode}.", process.ExitCode);

            if (!string.IsNullOrWhiteSpace(output))
            {
                _logger.LogInformation("FFmpeg output: {Output}", output);
            }

            if (!string.IsNullOrWhiteSpace(error))
            {
                _logger.LogWarning("FFmpeg error output: {Error}", error);
            }

            if (process.ExitCode != 0)
            {
                _logger.LogError("FFmpeg conversion failed for input: {InputPath}. Error: {Error}", inputPath, error);
                throw new Exception($"FFmpeg error: {error}");
            }

            _logger.LogInformation("Video successfully converted to MP4. Output: {OutputPath}", outputPath);
            return outputPath;
        }


        public async Task<string> ConvertToMp3Async(string inputPath)
        {
            _logger.LogInformation("Starting MP3 conversion for input: {InputPath}", inputPath);

            string outputPath = Path.ChangeExtension(inputPath, ".mp3");
            string args = $"-i \"{inputPath}\" -codec:a libmp3lame -b:a 192k \"{outputPath}\"";

            _logger.LogDebug("FFmpeg arguments: {Arguments}", args);

            var processInfo = new ProcessStartInfo
            {
                FileName = "ffmpeg",
                Arguments = args,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

 
            using var process = Process.Start(processInfo);
            if (process == null)
            {
                _logger.LogError("FFmpeg process could not be started for input: {InputPath}", inputPath);
                throw new Exception("FFmpeg process could not be started. Ensure FFmpeg is installed and available in the system PATH.");
            }

            _logger.LogInformation("FFmpeg process started. PID: {ProcessId}", process.Id);

            var stdErrTask = process.StandardError.ReadToEndAsync();
            var stdOutTask = process.StandardOutput.ReadToEndAsync();



            await process.WaitForExitAsync();

            string error = await stdErrTask;
            string output = await stdOutTask;

            _logger.LogDebug("FFmpeg process exited with code {ExitCode}.", process.ExitCode);
            if (!string.IsNullOrWhiteSpace(output))
            {
                _logger.LogInformation("FFmpeg output: {Output}", output);
            }
            if (!string.IsNullOrWhiteSpace(error))
            {
                _logger.LogWarning("FFmpeg error output: {Error}", error);
            }

            if (process.ExitCode != 0)
            {
                _logger.LogError("FFmpeg conversion failed for input: {InputPath}. Error: {Error}", inputPath, error);
                throw new Exception($"FFmpeg error: {error}");
            }

            _logger.LogInformation("Audio successfully converted to MP3. Output: {OutputPath}", outputPath);

            return outputPath;
        }

        public async Task<bool> ExtractThumbnailAsync(string filePath, string thumbnail)
        {
            _logger.LogInformation("Starting thumbnail extraction for file: {FilePath}", filePath);

            string args = $"-i \"{filePath}\" -ss 00:00:01.000 -vframes 1 \"{thumbnail}\"";
            _logger.LogDebug("FFmpeg arguments: {Arguments}", args);

            var processInfo = new ProcessStartInfo
            {
                FileName = "ffmpeg",
                Arguments = args,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };


            using var process = Process.Start(processInfo);
            if (process == null)
            {
                _logger.LogError("FFmpeg process could not be started for file: {FilePath}", filePath);
                throw new Exception("FFmpeg process could not be started. Ensure FFmpeg is installed and available in the system PATH.");
            }

            _logger.LogInformation("FFmpeg process started. PID: {ProcessId}", process.Id);

            var stdErrTask = process.StandardError.ReadToEndAsync();
            var stdOutTask = process.StandardOutput.ReadToEndAsync();


            await process.WaitForExitAsync();

            string error = await stdErrTask;
            string output = await stdOutTask;

            _logger.LogDebug("FFmpeg process exited with code {ExitCode}.", process.ExitCode);
            if (!string.IsNullOrWhiteSpace(output))
            {
                _logger.LogInformation("FFmpeg output: {Output}", output);
            }
            if (!string.IsNullOrWhiteSpace(error))
            {
                _logger.LogWarning("FFmpeg error output: {Error}", error);
            }

            if (process.ExitCode != 0)
            {
                _logger.LogError("FFmpeg thumbnail extraction failed for file: {FilePath}. Error: {Error}", filePath, error);
                throw new Exception($"FFmpeg Thumbnail error: {error}");
            }

            _logger.LogInformation("Thumbnail successfully extracted. Output: {ThumbnailPath}", thumbnail);

            return true;
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

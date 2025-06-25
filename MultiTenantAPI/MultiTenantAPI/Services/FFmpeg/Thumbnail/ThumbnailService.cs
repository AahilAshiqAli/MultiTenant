using System.Diagnostics;

namespace MultiTenantAPI.Services.FFmpeg.Thumbnail
{
    public class ThumbnailService : IThumbnailService
    {
        private readonly ILogger<ThumbnailService> _logger;
        public ThumbnailService(ILogger<ThumbnailService> logger)
        {
            _logger = logger;
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

    }
}

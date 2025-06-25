using System.Diagnostics;
using System.Text;

namespace MultiTenantAPI.Services.FFmpeg.Converter
{
    public class ConverterService : IConverterService
    {
        private readonly ILogger<ConverterService> _logger;




        public ConverterService(ILogger<ConverterService> logger)
        {
            _logger = logger;
        }

        public async Task<string> ConvertToMp4Async(string inputPath)
        {
            _logger.LogInformation("Starting MP4 conversion for input: {InputPath}", inputPath);

            string outputPath = Path.ChangeExtension(inputPath, ".mp4");
            string args = $"-i \"{inputPath}\" -c:v libx264 -preset fast -crf 23 -c:a aac -b:a 128k \"{outputPath}\"";

            _logger.LogDebug("git  arguments: {Arguments}", args);

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

    }

}

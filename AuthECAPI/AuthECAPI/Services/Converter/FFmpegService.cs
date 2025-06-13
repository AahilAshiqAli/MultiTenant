using System.Diagnostics;

namespace AuthECAPI.Services.Converter
{
    public class FFmpegService : IFFmpegService
    {
        public async Task<string> ConvertToMp4Async(string inputPath)
        {
            string outputPath = Path.ChangeExtension(inputPath, ".mp4");
            string args = $"-i \"{inputPath}\" -c:v libx264 -preset fast -crf 23 -c:a aac -b:a 128k \"{outputPath}\"";

            Console.WriteLine("hello there");

            var processInfo = new ProcessStartInfo
            {
                FileName = "C:\\ffmpeg\\ffmpeg-7.1.1-essentials_build\\bin\\ffmpeg.exe",
                Arguments = args,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using var process = Process.Start(processInfo);
            if (process == null)
            {
                throw new Exception("FFmpeg process could not be started.");
            }

            // Read error and output while process runs
            var stdErrTask = process.StandardError.ReadToEndAsync();
            var stdOutTask = process.StandardOutput.ReadToEndAsync();

            await process.WaitForExitAsync();

            string error = await stdErrTask;
            string output = await stdOutTask;

            if (process.ExitCode != 0)
            {
                throw new Exception($"FFmpeg error: {error}");
            }

            Console.WriteLine("Video converted");


            return outputPath;
        }

        public async Task<string> ConvertToMp3Async(string inputPath)
        {
            string outputPath = Path.ChangeExtension(inputPath, ".mp3");
            string args = $"-i \"{inputPath}\" -codec:a libmp3lame -b:a 192k \"{outputPath}\"";

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
                throw new Exception("FFmpeg process could not be started. Ensure FFmpeg is installed and available in the system PATH.");
            }
            await process.WaitForExitAsync();

            if (process.ExitCode != 0)
            {
                string error = await process.StandardError.ReadToEndAsync();
                throw new Exception($"FFmpeg error: {error}");
            }

            return outputPath;
        }
    }
}

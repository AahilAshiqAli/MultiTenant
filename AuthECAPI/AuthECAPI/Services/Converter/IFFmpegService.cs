namespace AuthECAPI.Services.Converter
{
    public interface IFFmpegService
    {
        public Task<string> ConvertToMp4Async(string inputPath);

        public Task<string> ConvertToMp3Async(string inputPath);
    }
}

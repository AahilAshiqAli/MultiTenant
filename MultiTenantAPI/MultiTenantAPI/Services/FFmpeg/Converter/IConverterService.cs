namespace MultiTenantAPI.Services.FFmpeg.Converter
{
    public interface IConverterService
    {
        public Task<string> ConvertToMp4Async(string inputPath);

        public Task<string> ConvertToMp3Async(string inputPath);
    }
}

namespace AuthECAPI.Services.ProgressStore
{
    public interface IProgressStore
    {
        void SetProgress(string userId, int percent);
        int GetProgress(string userId);
    }

}

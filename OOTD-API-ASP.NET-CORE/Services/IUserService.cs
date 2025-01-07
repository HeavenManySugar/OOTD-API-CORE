namespace OOTD_API.Services
{
    public interface IUserService
    {
        Task<bool> IsUserEnabledAsync(string userId);
    }
}

using OOTD_API.Models;
using Microsoft.EntityFrameworkCore;

namespace OOTD_API.Services
{
    public class UserService : IUserService
    {
        private readonly Ootdv1Context _context;

        public UserService(Ootdv1Context context)
        {
            _context = context;
        }

        public async Task<bool> IsUserEnabledAsync(string userId)
        {
            var user = await _context.Users
                .Where(u => u.Uid.ToString() == userId)
                .FirstOrDefaultAsync();
            return user != null && user.Enabled;
        }
    }
}

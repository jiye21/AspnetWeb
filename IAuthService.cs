using AspnetWeb.Models;
using AspnetWeb.ViewModel;

namespace AspnetWeb
{
    public interface IAuthService
    {
        Task RegisterUserAsync(RegisterViewModel model);
        Task<AspnetUser> LoginUserAsync(LoginViewModel model);
        void UpdateSessionAndCookie(string sessionKey, byte[] userNoBytes);
        void GenerateSession(long uid);
        void RemoveSession(string sessionKey);
        Task<OAuthUser> GetGoogleUser(string code);
    }
}

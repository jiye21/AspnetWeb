using AspnetWeb.Models;
using AspnetWeb.ViewModel;

namespace AspnetWeb
{
    public interface IAuthService
    {
        Task RegisterUserAsync(User model);
        Task<User> LoginUserAsync(LoginViewModel model);
    }
}

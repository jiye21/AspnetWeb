using AspnetWeb.DataContext;
using AspnetWeb.Models;
using AspnetWeb.ViewModel;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using System.Security.Cryptography;
using System.Text;

namespace AspnetWeb
{
    public class AuthService : IAuthService
    {
        private readonly IDistributedCache _redisCache;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public AuthService(IDistributedCache redisCache, IHttpContextAccessor httpContextAccessor)
        {
            _redisCache = redisCache;
            _httpContextAccessor = httpContextAccessor;
        }

        public async Task RegisterUserAsync(User model)
        {
            model.UserPassword = SHA256Hash(model.UserPassword);  // 패스워드 해시화

            using (var db = new AspnetNoteDbContext())
            {
                await db.Users.AddAsync(model);
                db.SaveChanges();                                
            }

        }

        public async Task<User> LoginUserAsync(LoginViewModel model)
        {
            using (var db = new AspnetNoteDbContext())
            {
                var user = await db.Users
                    .FirstOrDefaultAsync(u => u.UserId.Equals(model.UserId) &&
                                    u.UserPassword.Equals(SHA256Hash(model.UserPassword)));
                
                if(user != null)
                {
                    return user;
                }
            }

            return null;
        }

        public void UpdateSessionAndCookie(string sessionKey, byte[] userNoBytes)
        {
            var redisOptions = new DistributedCacheEntryOptions();
            redisOptions.SetAbsoluteExpiration(TimeSpan.FromSeconds(20));
            
            _redisCache.Set(sessionKey, userNoBytes, redisOptions);

            CookieOptions cookieOptions = new CookieOptions();
            cookieOptions.Expires = DateTimeOffset.UtcNow.AddSeconds(20);

            _httpContextAccessor.HttpContext.Response.Cookies.Append("SESSION_KEY", sessionKey, cookieOptions);
        }

        public bool IsSessionValid(string sessionKey)
        {
            var sessionValue = _redisCache.Get(sessionKey);
            if (!string.IsNullOrEmpty(sessionKey) && sessionValue != null)
            {
                UpdateSessionAndCookie(sessionKey, sessionValue);  // 세션이 유효하다면 갱신해줌

                return true;
            }
            return false;
        }


        public string SHA256Hash(string data)
        {
            SHA256 sHA256 = SHA256.Create();
            byte[] hash = sHA256.ComputeHash(Encoding.ASCII.GetBytes(data));  // byte[]형식의 해시값으로 변환

            string hashString = Convert.ToBase64String(hash);

            return hashString;
        }
    }
}

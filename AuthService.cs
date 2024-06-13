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
        AspnetNoteDbContext _noteDbContext;

        public AuthService(IDistributedCache redisCache, IHttpContextAccessor httpContextAccessor)
        {
            _redisCache = redisCache;
            _httpContextAccessor = httpContextAccessor;
        }

        public async Task RegisterUserAsync(RegisterViewModel model)
        {            
            var user = new User()
            { 
                UserName = model.UserName
            };

            var aspnetUser = new AspnetUser()
            {
                UserId = model.UserId,
                Salt = Guid.NewGuid().ToString()
            };
            aspnetUser.UserPassword = SHA256Hash(aspnetUser.Salt + model.UserPassword);  // 패스워드 해시화

            using (var db = new AspnetNoteDbContext())
            {
                await db.Users.AddAsync(user); 
                await db.SaveChangesAsync();  // 기본 키 값이 설정됨

                aspnetUser.UID = user.UID;
                await db.AspnetUsers.AddAsync(aspnetUser);
                db.SaveChanges();                                
            }

        }

        public async Task<AspnetUser> LoginUserAsync(LoginViewModel model)
        {
            using (var db = new AspnetNoteDbContext())
            {
                var userInfo = await db.AspnetUsers
                .FirstOrDefaultAsync(u => u.UserId.Equals(model.UserId));
				if (userInfo == null)
                {
                    return null;
                }

                var password = SHA256Hash(userInfo.Salt + model.UserPassword);  // Linq 쿼리식 안에서 사용자 정의 함수를 사용할 수 없어서 밖으로 뺌

                var user = await db.AspnetUsers
                    .FirstOrDefaultAsync(u => u.UserPassword.Equals(password));
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
            if (!string.IsNullOrEmpty(sessionKey) && _redisCache.Get(sessionKey) != null)
            {
                UpdateSessionAndCookie(sessionKey, _redisCache.Get(sessionKey));  // 세션이 유효하다면 갱신해줌

                return true;
            }
            return false;
        }

        public void GenerateSession(int uid)
        {
            string sessionKey = Guid.NewGuid().ToString();    // GUID는 매우 난수적이며 중복될 가능성이 매우 낮은 값.

            var redisOptions = new DistributedCacheEntryOptions();
            redisOptions.SetAbsoluteExpiration(TimeSpan.FromSeconds(20));   // 현재를 기준으로 절대 만료 시간을 설정
            byte[] userNoBytes = BitConverter.GetBytes(uid);
            _redisCache.Set(sessionKey, userNoBytes, redisOptions);   // redis에 sessionKey 저장, 값은 UID로 해서 어떤 유저인지 식별.
                                                                      // 세션은 연결된 유저가 누구인지 저장하고 있다. 

            CookieOptions cookieOptions = new CookieOptions();
            cookieOptions.Expires = DateTimeOffset.UtcNow.AddSeconds(20);  // 쿠키도 만료시간 설정
            _httpContextAccessor.HttpContext.Response.Cookies.Append("SESSION_KEY", sessionKey, cookieOptions); // 클라이언트에게 세션키 전달
        }

        public void RemoveSession(string sessionKey)
        {
            _httpContextAccessor.HttpContext.Response.Cookies.Delete("SESSION_KEY");     // 클라이언트에게 해당 세션 키를 지우도록 쿠키를 전송
            _redisCache.Remove(sessionKey);   // redis에서도 세션키 삭제
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

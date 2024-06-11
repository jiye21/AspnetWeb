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
        public AuthService(IDistributedCache redisCache)
        {
            _redisCache = redisCache;
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


        public string SHA256Hash(string data)
        {
            SHA256 sHA256 = SHA256.Create();
            byte[] hash = sHA256.ComputeHash(Encoding.ASCII.GetBytes(data));  // byte[]형식의 해시값으로 변환

            string hashString = Convert.ToBase64String(hash);

            return hashString;
        }
    }
}

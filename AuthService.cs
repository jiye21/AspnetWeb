using AspnetWeb.DataContext;
using AspnetWeb.Models;
using AspnetWeb.ViewModel;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Oauth2.v2.Data;
using Google.Apis.Oauth2.v2;
using Google.Apis.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using System.Security.Cryptography;
using System.Text;
using Google.Apis.Auth.OAuth2.Flows;
using Microsoft.IdentityModel.Tokens;



namespace AspnetWeb
{
    public class AuthService : IAuthService
    {
        private readonly IDistributedCache _redisCache;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly GoogleAuthorizationCodeFlow _flow;
        private readonly AspnetNoteDbContext _dbContext;

        public AuthService(IDistributedCache redisCache, IHttpContextAccessor httpContextAccessor,
            GoogleAuthorizationCodeFlow flow, AspnetNoteDbContext dbContext)
        {
            _redisCache = redisCache;
            _httpContextAccessor = httpContextAccessor;
            _flow = flow;
            _dbContext = dbContext;
        }

        /// <summary>
        /// 일반 유저 회원가입
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        public async Task RegisterUserAsync(RegisterViewModel model)
        {
            var user = new User()
            {
                UserName = model.UserName,
				LoginType = 1
			};

            var aspnetUser = new AspnetUser()
            {
                UserId = model.UserId,
                Salt = Guid.NewGuid().ToString()
            };
            aspnetUser.UserPassword = SHA256Hash(aspnetUser.Salt + model.UserPassword);  // 패스워드 해시화

            await _dbContext.Users.AddAsync(user);
            _dbContext.SaveChanges();  // 기본 키 값이 설정됨

            aspnetUser.MUID = user.UID;
            await _dbContext.AspnetUsers.AddAsync(aspnetUser);
            _dbContext.SaveChanges();

        }

        /// <summary>
        /// 일반 유저 로그인
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        public async Task<AspnetUser> LoginUserAsync(LoginViewModel model)
        {
            var userInfo = await _dbContext.AspnetUsers
                .FirstOrDefaultAsync(u => u.UserId.Equals(model.UserId));
            if (userInfo == null)
            {
                return null;
            }

            var password = SHA256Hash(userInfo.Salt + model.UserPassword);  // Linq 쿼리식 안에서 사용자 정의 함수를 사용할 수 없어서 밖으로 뺌

            var user = await _dbContext.AspnetUsers
                .FirstOrDefaultAsync(u => u.UserPassword.Equals(password));
            if (user != null)
            {
                return user;
            }

            return null;
        }

        /// <summary>
        /// 세션 갱신
        /// </summary>
        /// <param name="sessionKey"></param>
        /// <param name="userNoBytes"></param>
        public void UpdateSessionAndCookie(string sessionKey, byte[] userNoBytes)
        {
            var redisOptions = new DistributedCacheEntryOptions();
            redisOptions.SetAbsoluteExpiration(TimeSpan.FromSeconds(180));

            _redisCache.Set(sessionKey, userNoBytes, redisOptions);

            CookieOptions cookieOptions = new CookieOptions();
            cookieOptions.Expires = DateTimeOffset.UtcNow.AddSeconds(180);

            _httpContextAccessor.HttpContext.Response.Cookies.Append("SESSION_KEY", sessionKey, cookieOptions);
        }


        /// <summary>
        /// 세션 생성
        /// </summary>
        /// <param name="uid"></param>
        public void GenerateSession(long uid)
        {
            string sessionKey = Guid.NewGuid().ToString();    // GUID는 매우 난수적이며 중복될 가능성이 매우 낮은 값.

            var redisOptions = new DistributedCacheEntryOptions();
            redisOptions.SetAbsoluteExpiration(TimeSpan.FromSeconds(180));   // 현재를 기준으로 절대 만료 시간을 설정
            byte[] userNoBytes = BitConverter.GetBytes(uid);
            _redisCache.Set(sessionKey, userNoBytes, redisOptions);   // redis에 sessionKey 저장, 값은 UID로 해서 어떤 유저인지 식별.
                                                                      // 세션은 연결된 유저가 누구인지 저장하고 있다. 

            CookieOptions cookieOptions = new CookieOptions();
            cookieOptions.Expires = DateTimeOffset.UtcNow.AddSeconds(180);  // 쿠키도 만료시간 설정
            _httpContextAccessor.HttpContext.Response.Cookies.Append("SESSION_KEY", sessionKey, cookieOptions); // 클라이언트에게 세션키 전달
        }


        /// <summary>
        /// 이미 Google 회원가입한 유저 정보를 가져옴. 
        /// 가입되어있지 않다면 Google에서 받아온 유저이름,이메일 등의 정보로 oAuthUser로 회원가입시킴. 
        /// </summary>
        /// <param name="code"></param>
        /// <returns></returns>
        public async Task<OAuthUser> GetGoogleUser(string code)
        {
            // Google Access Token 가져오기
            string? accessToken = null;
			try
			{
				var tokenResponse = _flow.ExchangeCodeForTokenAsync(null, code,
						"https://localhost:44396/Home/GoogleUserEmailList", CancellationToken.None).Result;

				accessToken = tokenResponse.AccessToken;
			}
			catch (Exception ex)   // 토큰을 정상적으로 받아오지 못했을 때
			{
                return null;
			}


            // 토큰을 정상적으로 받아옴 - 사용자 정보를 DB에 저장하거나 세션에 저장.
            var oauth2Service = new Oauth2Service(new BaseClientService.Initializer()
            {
                HttpClientInitializer = GoogleCredential.FromAccessToken(accessToken),
            });

            Userinfo userInfo = await oauth2Service.Userinfo.Get().ExecuteAsync();

            var oAuthUser = new OAuthUser()
            {
                GoogleEmail = userInfo.Email,
                GoogleUID = userInfo.Id
            };

            // Guid (UserInfo.Id)로 유저 검색
            var userExists = await _dbContext.OAuthUsers
                .FirstOrDefaultAsync(u => u.GoogleUID.Equals(oAuthUser.GoogleUID));

            // 이미 데이터베이스에 유저가 존재할때 - 세션 생성 후 바로 기존 유저정보 리턴
            if (userExists != null)
            {
                GenerateSession(userExists.MUID);

                return userExists;
            }

            // 데이터베이스에 구글유저 저장 - User 테이블, OAuth 테이블 두군데에 저장
            var user = new User()
            {
                UserName = userInfo.Name,
                LoginType = 2
            };
            await _dbContext.Users.AddAsync(user);
            _dbContext.SaveChanges();  // 기본 키 값이 설정됨 (=User.UID 생성)

            oAuthUser.MUID = user.UID;
            await _dbContext.OAuthUsers.AddAsync(oAuthUser);
            _dbContext.SaveChanges();

            GenerateSession(oAuthUser.MUID);

            return oAuthUser;

        }


		/// <summary>
		/// SHA256 해시 함수
		/// </summary>
		/// <param name="data"></param>
		/// <returns></returns>
		public string SHA256Hash(string data)
        {
            SHA256 sHA256 = SHA256.Create();
            byte[] hash = sHA256.ComputeHash(Encoding.ASCII.GetBytes(data));  // byte[]형식의 해시값으로 변환

            string hashString = Convert.ToBase64String(hash);

            return hashString;
        }
    }
}

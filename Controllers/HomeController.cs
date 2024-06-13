using AspnetWeb.DataContext;
using AspnetWeb.Models;
using Google.Apis.Auth.OAuth2.Flows;
using Google.Apis.Auth;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Distributed;
using System.Diagnostics;
using Newtonsoft.Json.Linq;
using Azure.Core;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Oauth2.v2;
using Google.Apis.Services;
using Google.Apis.Oauth2.v2.Data;
using Google.Apis.Auth.OAuth2.Responses;
using Microsoft.EntityFrameworkCore;

namespace AspnetWeb.Controllers
{
    public class HomeController : Controller
    {
		private readonly IDistributedCache _redisCache;
        private readonly IAuthService _authService;
        private readonly GoogleAuthorizationCodeFlow _flow;

        public HomeController(IDistributedCache redisCache, IAuthService authService, GoogleAuthorizationCodeFlow flow)  //  redisCache라는 이름으로 redis를 사용할것이라는걸 명시
		{
			_redisCache = redisCache;
            _authService = authService;
            _flow = flow;
		}

		public IActionResult Index()
        {
            string sessionKey = HttpContext.Request.Cookies["SESSION_KEY"];

            if (_authService.IsSessionValid(sessionKey))
            {
                ViewData["SESSION_KEY"] = sessionKey;  // 내비게이션 바 변경을 위한 ViewData
            }

            return View();
        }

        public IActionResult LoginSuccess()
        {
            // 세션이 없다면 다시 로그인 페이지로 이동시키기.
            // 바로 이 주소로 들어온다면 접속이 가능하기때문.

            string sessionKey = HttpContext.Request.Cookies["SESSION_KEY"];

            if (_authService.IsSessionValid(sessionKey))  // 세션 검증
            {
                var sessionValue = _redisCache.Get(sessionKey);
                int userNo = BitConverter.ToInt32(sessionValue);
                ViewData["USER_NO"] = userNo;
                ViewData["SESSION_KEY"] = sessionKey;  // 내비게이션 바 변경을 위한 ViewData

                ViewData["Page"] = "LoginSuccess";
                return View();
			}

            // redis에 세션이 없거나 쿠키가 만료되었을때
            return RedirectToAction("Login", "Account");
        }

        // 인증 코드를 성공적으로 교환하여 액세스 토큰을 받아오면
        // 이는 사용자가 성공적으로 인증되고 회원가입이 완료되었음을 의미
        public async Task<IActionResult> GoogleUserEmailList(string code)
        {
            // ASP.NET Core에서는 자동으로 쿼리 문자열 매개변수를 컨트롤러 액션 메서드의 매개변수로 바인딩한다.
            // 따라서 GoogleUserEmailList 메서드의 code 매개변수에 인증 코드가 전달된다. 
            if(string.IsNullOrEmpty(code))
            {
                return RedirectToAction("Login", "Account");
            }


            try
            {
                var tokenResponse = _flow.ExchangeCodeForTokenAsync(null, code,
                        "https://localhost:44396/Home/GoogleUserEmailList", CancellationToken.None).Result;

                var accessToken = tokenResponse.AccessToken;

                // 토큰을 정상적으로 받아옴 - 사용자 정보를 DB에 저장하거나 세션에 저장.
                var oauth2Service = new Oauth2Service(new BaseClientService.Initializer()
                {
                    HttpClientInitializer = GoogleCredential.FromAccessToken(accessToken),
                });

                Userinfo userInfo = await oauth2Service.Userinfo.Get().ExecuteAsync();

                var user = new User()
                {
                    UserName = userInfo.Name
                };

                var oAuthUser = new OAuthUser()
                {
                    GoogleEmail = userInfo.Email,
                    GoogleUID = userInfo.Id
                };

                using (var db = new AspnetNoteDbContext())
                {
                    var userExists = await db.OAuthUsers
                        .FirstOrDefaultAsync(u => u.GoogleUID.Equals(oAuthUser.GoogleUID));
                    if (userExists != null)     // 이미 데이터베이스에 유저가 존재할때 - 세션 생성
                    {
                        _authService.GenerateSession(oAuthUser.UID);

                        ViewData["SESSION_KEY"] = string.Empty;  // 내비게이션 바 변경을 위한 ViewData
                        ViewData["Page"] = "Google";
                        return View(oAuthUser);
                    }

                    // 데이터베이스에 구글유저 저장
                    await db.Users.AddAsync(user);
                    await db.SaveChangesAsync();  // 기본 키 값이 설정됨

                    oAuthUser.UID = user.UID;
                    await db.OAuthUsers.AddAsync(oAuthUser);

                    db.SaveChanges();
                }


                ViewData["SESSION_KEY"] = string.Empty;  // 내비게이션 바 변경을 위한 ViewData
                ViewData["Page"] = "Google";

                _authService.GenerateSession(oAuthUser.UID);
                return View(oAuthUser);
            }
            catch (Exception ex)   // 토큰을 정상적으로 받아오지 못했을 때
            {
                return RedirectToAction("Login", "Account");
            }

        }

        public IActionResult MyPage()
        {
            // 세션이 없다면 다시 로그인 페이지로 이동시키기.
            // 바로 이 주소로 들어온다면 접속이 가능하기때문.
            string sessionKey = HttpContext.Request.Cookies["SESSION_KEY"];

            if (_authService.IsSessionValid(sessionKey))  // 세션 검증
            {

                ViewData["SESSION_KEY"] = string.Empty;  // 내비게이션 바 변경을 위한 ViewData

                ViewData["Page"] = "MyPage";
                return View();
            }

            // redis에 세션이 없거나 쿠키가 만료되었을때
            return RedirectToAction("Login", "Account");
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}

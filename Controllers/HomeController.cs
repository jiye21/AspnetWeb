using AspnetWeb.DataContext;
using AspnetWeb.Models;
using Google.Apis.Auth.OAuth2.Flows;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Distributed;
using System.Diagnostics;


namespace AspnetWeb.Controllers
{
    public class HomeController : Controller
    {
		private readonly IDistributedCache _redisCache;
        private readonly IAuthService _authService;
        private readonly AspnetNoteDbContext _dbContext;

        public HomeController(IDistributedCache redisCache, IAuthService authService,
            AspnetNoteDbContext dbContext)
		{
			_redisCache = redisCache;
            _authService = authService;
            _dbContext = dbContext;
		}

		public IActionResult Index()
        {
            if (RequestAuthMiddleware.Session)
            {
                ViewData["SESSION_KEY"] = string.Empty;  // 내비게이션 바 변경을 위한 ViewData
            }

            return View();
        }

        public IActionResult LoginSuccess()
        {
            // 세션이 없다면 다시 로그인 페이지로 이동시키기.
            // 바로 이 주소로 들어온다면 접속이 가능하기때문.

			if (RequestAuthMiddleware.Session)  // 세션 검증
			{
                // 세션 갱신
                string sessionKey = HttpContext.Request.Cookies["SESSION_KEY"];
				var sessionValue = _redisCache.Get(sessionKey);
                _authService.UpdateSessionAndCookie(sessionKey, sessionValue);

				int userNo = BitConverter.ToInt32(sessionValue);
				ViewData["USER_NO"] = userNo;

				ViewData["SESSION_KEY"] = sessionKey;  // 내비게이션 바 변경을 위한 ViewData
				ViewData["Page"] = "LoginSuccess";
				return View();
			}

			// redis에 세션이 없거나 쿠키가 만료되었을때
			return RedirectToAction("Login", "Account");
		}

        /// <summary>
        /// JWT 로그인 성공 페이지 - JWT 검증은 미들웨어에서 이루어짐
        /// </summary>
        /// <returns></returns>
        [Route("/api/JWTPage")]
		public IActionResult JWTPage()
        {
			ViewData["SESSION_KEY"] = string.Empty;  // 내비게이션 바 변경을 위한 ViewData
			ViewData["Page"] = "JWTPage";
			return View();
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

            var googleUser = await _authService.GetGoogleUser(code);

            if (googleUser == null)   // 유저를 정상적으로 받아오지 못했을 때
            {
                return RedirectToAction("Login", "Account");
            }

            ViewData["SESSION_KEY"] = string.Empty;  // 내비게이션 바 변경을 위한 ViewData
            ViewData["Page"] = "Google";

            var userInfo = _dbContext.Users.FirstOrDefault(u=>u.UID.Equals(googleUser.MUID));

            if (userInfo != null) 
            { 
                ViewData["Name"] = userInfo.UserName;
            }

            
            return View(googleUser);
        }

        public IActionResult MyPage()
        {
            // 세션이 없다면 다시 로그인 페이지로 이동시키기.
            // 바로 이 주소로 들어온다면 접속이 가능하기때문.

            if (RequestAuthMiddleware.Session)
            {
				// 세션 갱신
				string sessionKey = HttpContext.Request.Cookies["SESSION_KEY"];
				var sessionValue = _redisCache.Get(sessionKey);
				_authService.UpdateSessionAndCookie(sessionKey, sessionValue);

				ViewData["SESSION_KEY"] = string.Empty;  // 내비게이션 바 변경을 위한 ViewData
                ViewData["Page"] = "MyPage";
                return View();
            }


            return RedirectToAction("Login", "Account");
        }


        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}

using AspnetWeb.DataContext;
using AspnetWeb.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Distributed;
using System.Diagnostics;

namespace AspnetWeb.Controllers
{
    public class HomeController : Controller
    {
		private readonly IDistributedCache _redisCache;
        private readonly IAuthService _authService;
        public HomeController(IDistributedCache redisCache, IAuthService authService)  //  redisCache라는 이름으로 redis를 사용할것이라는걸 명시
		{
			_redisCache = redisCache;
            _authService = authService;
		}

		public IActionResult Index()
        {
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

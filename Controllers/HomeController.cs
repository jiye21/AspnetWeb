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
		public HomeController(IDistributedCache redisCache)        //  redisCache라는 이름으로 redis를 사용할것이라는걸 명시
		{
			_redisCache = redisCache;
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
            if(_redisCache.Get(sessionKey) != null)   // redis에 byte[]로 value가 설정되어있음.
            {
                var sessionValue = _redisCache.Get(sessionKey);
                int userNo = BitConverter.ToInt32(sessionValue);
                ViewData["USER_NAME"] = userNo;
                ViewData["SESSION_KEY"] = sessionKey;
                                
                return View();
			}

            // redis에 세션이 없을 때
            return RedirectToAction("Login", "Account");
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}

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
        public HomeController(IDistributedCache redisCache, IAuthService authService)  //  redisCache��� �̸����� redis�� ����Ұ��̶�°� ���
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
            // ������ ���ٸ� �ٽ� �α��� �������� �̵���Ű��.
            // �ٷ� �� �ּҷ� ���´ٸ� ������ �����ϱ⶧��.

            string sessionKey = HttpContext.Request.Cookies["SESSION_KEY"];

            if (_authService.IsSessionValid(sessionKey))  // ���� ����
            {
                var sessionValue = _redisCache.Get(sessionKey);
                int userNo = BitConverter.ToInt32(sessionValue);
                ViewData["USER_NO"] = userNo;
                ViewData["SESSION_KEY"] = sessionKey;  // ������̼� �� ������ ���� ViewData

                return View();
			}

            // redis�� ������ ���ų� ��Ű�� ����Ǿ�����
            return RedirectToAction("Login", "Account");
        }





        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}

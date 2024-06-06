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
		public HomeController(IDistributedCache redisCache)        //  redisCache��� �̸����� redis�� ����Ұ��̶�°� ���
		{
			_redisCache = redisCache;
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

            if (!string.IsNullOrEmpty(sessionKey) && _redisCache.Get(sessionKey) != null)   // redis�� byte[]�� value�� �����Ǿ�����.
            {
                var sessionValue = _redisCache.Get(sessionKey);
                int userNo = BitConverter.ToInt32(sessionValue);
                ViewData["USER_NO"] = userNo;
                ViewData["SESSION_KEY"] = sessionKey;

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

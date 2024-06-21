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
                ViewData["SESSION_KEY"] = string.Empty;  // ������̼� �� ������ ���� ViewData
            }

            return View();
        }

        public IActionResult LoginSuccess()
        {
            // ������ ���ٸ� �ٽ� �α��� �������� �̵���Ű��.
            // �ٷ� �� �ּҷ� ���´ٸ� ������ �����ϱ⶧��.

			if (RequestAuthMiddleware.Session)  // ���� ����
			{
                // ���� ����
                string sessionKey = HttpContext.Request.Cookies["SESSION_KEY"];
				var sessionValue = _redisCache.Get(sessionKey);
                _authService.UpdateSessionAndCookie(sessionKey, sessionValue);

				int userNo = BitConverter.ToInt32(sessionValue);
				ViewData["USER_NO"] = userNo;

				ViewData["SESSION_KEY"] = sessionKey;  // ������̼� �� ������ ���� ViewData
				ViewData["Page"] = "LoginSuccess";
				return View();
			}

			// redis�� ������ ���ų� ��Ű�� ����Ǿ�����
			return RedirectToAction("Login", "Account");
		}

        /// <summary>
        /// JWT �α��� ���� ������ - JWT ������ �̵����� �̷����
        /// </summary>
        /// <returns></returns>
        [Route("/api/JWTPage")]
		public IActionResult JWTPage()
        {
			ViewData["SESSION_KEY"] = string.Empty;  // ������̼� �� ������ ���� ViewData
			ViewData["Page"] = "JWTPage";
			return View();
		}

		// ���� �ڵ带 ���������� ��ȯ�Ͽ� �׼��� ��ū�� �޾ƿ���
		// �̴� ����ڰ� ���������� �����ǰ� ȸ�������� �Ϸ�Ǿ����� �ǹ�
		public async Task<IActionResult> GoogleUserEmailList(string code)
        {
            // ASP.NET Core������ �ڵ����� ���� ���ڿ� �Ű������� ��Ʈ�ѷ� �׼� �޼����� �Ű������� ���ε��Ѵ�.
            // ���� GoogleUserEmailList �޼����� code �Ű������� ���� �ڵ尡 ���޵ȴ�. 
            if(string.IsNullOrEmpty(code))
            {
                return RedirectToAction("Login", "Account");
            }

            var googleUser = await _authService.GetGoogleUser(code);

            if (googleUser == null)   // ������ ���������� �޾ƿ��� ������ ��
            {
                return RedirectToAction("Login", "Account");
            }

            ViewData["SESSION_KEY"] = string.Empty;  // ������̼� �� ������ ���� ViewData
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
            // ������ ���ٸ� �ٽ� �α��� �������� �̵���Ű��.
            // �ٷ� �� �ּҷ� ���´ٸ� ������ �����ϱ⶧��.

            if (RequestAuthMiddleware.Session)
            {
				// ���� ����
				string sessionKey = HttpContext.Request.Cookies["SESSION_KEY"];
				var sessionValue = _redisCache.Get(sessionKey);
				_authService.UpdateSessionAndCookie(sessionKey, sessionValue);

				ViewData["SESSION_KEY"] = string.Empty;  // ������̼� �� ������ ���� ViewData
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

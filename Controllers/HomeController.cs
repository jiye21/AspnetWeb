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
using Google;

namespace AspnetWeb.Controllers
{
    public class HomeController : Controller
    {
		private readonly IDistributedCache _redisCache;
        private readonly IAuthService _authService;
        private readonly GoogleAuthorizationCodeFlow _flow;
        private readonly AspnetNoteDbContext _dbContext;

        public HomeController(IDistributedCache redisCache, IAuthService authService,
            GoogleAuthorizationCodeFlow flow, AspnetNoteDbContext dbContext)  //  redisCache��� �̸����� redis�� ����Ұ��̶�°� ���
		{
			_redisCache = redisCache;
            _authService = authService;
            _flow = flow;
            _dbContext = dbContext;
		}

		public IActionResult Index()
        {
            string sessionKey = HttpContext.Request.Cookies["SESSION_KEY"];

            if (_authService.IsSessionValid(sessionKey))
            {
                ViewData["SESSION_KEY"] = sessionKey;  // ������̼� �� ������ ���� ViewData
            }

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
                ViewData["Page"] = "LoginSuccess";
                return View();
			}

            // redis�� ������ ���ų� ��Ű�� ����Ǿ�����
            return RedirectToAction("Login", "Account");
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
            return View(googleUser);
        }

        public IActionResult MyPage()
        {
            // ������ ���ٸ� �ٽ� �α��� �������� �̵���Ű��.
            // �ٷ� �� �ּҷ� ���´ٸ� ������ �����ϱ⶧��.
            string sessionKey = HttpContext.Request.Cookies["SESSION_KEY"];

            if (_authService.IsSessionValid(sessionKey))  // ���� ����
            {

                ViewData["SESSION_KEY"] = string.Empty;  // ������̼� �� ������ ���� ViewData
                ViewData["Page"] = "MyPage";
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

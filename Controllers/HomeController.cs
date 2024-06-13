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

        public HomeController(IDistributedCache redisCache, IAuthService authService, GoogleAuthorizationCodeFlow flow)  //  redisCache��� �̸����� redis�� ����Ұ��̶�°� ���
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


            try
            {
                var tokenResponse = _flow.ExchangeCodeForTokenAsync(null, code,
                        "https://localhost:44396/Home/GoogleUserEmailList", CancellationToken.None).Result;

                var accessToken = tokenResponse.AccessToken;

                // ��ū�� ���������� �޾ƿ� - ����� ������ DB�� �����ϰų� ���ǿ� ����.
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
                    if (userExists != null)     // �̹� �����ͺ��̽��� ������ �����Ҷ� - ���� ����
                    {
                        _authService.GenerateSession(oAuthUser.UID);

                        ViewData["SESSION_KEY"] = string.Empty;  // ������̼� �� ������ ���� ViewData
                        ViewData["Page"] = "Google";
                        return View(oAuthUser);
                    }

                    // �����ͺ��̽��� �������� ����
                    await db.Users.AddAsync(user);
                    await db.SaveChangesAsync();  // �⺻ Ű ���� ������

                    oAuthUser.UID = user.UID;
                    await db.OAuthUsers.AddAsync(oAuthUser);

                    db.SaveChanges();
                }


                ViewData["SESSION_KEY"] = string.Empty;  // ������̼� �� ������ ���� ViewData
                ViewData["Page"] = "Google";

                _authService.GenerateSession(oAuthUser.UID);
                return View(oAuthUser);
            }
            catch (Exception ex)   // ��ū�� ���������� �޾ƿ��� ������ ��
            {
                return RedirectToAction("Login", "Account");
            }

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

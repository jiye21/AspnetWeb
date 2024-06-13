using AspnetWeb.ViewModel;
using Microsoft.AspNetCore.Mvc;

using Google.Apis.Auth.AspNetCore3;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Oauth2.v2;
using Google.Apis.Oauth2.v2.Data;
using Google.Apis.Services;
using Google.Apis.Util.Store;
using Google.Apis.Auth.OAuth2.Flows;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication;
using Google.Apis.Auth.OAuth2.Responses;
using Google.Apis.Auth;
using AspnetWeb.Models;


namespace AspnetWeb.Controllers
{
    public class AccountController : Controller
    {
        private readonly IAuthService _authService;
        private readonly GoogleAuthorizationCodeFlow _flow;

        // 생성자 주입을 통한 DI
        public AccountController(IAuthService authService, GoogleAuthorizationCodeFlow flow)
        {
            _authService = authService;
            _flow = flow;
        }


        /// <summary>
        /// 로그인
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        public IActionResult Login()
        {
            ViewData["LoginPage"] = true;
            return View();
        }

        /// <summary>
        /// 로그인 전송
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        [HttpPost]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            if (ModelState.IsValid)
            {
                var user = await _authService.LoginUserAsync(model);

                // 로그인에 성공했을 때 - 세션 생성
                if (user != null)
                {
                    _authService.GenerateSession(user.UID);

                    return RedirectToAction("LoginSuccess", "Home");    // 로그인 성공 페이지로 이동
                }
            }

            // 로그인에 실패했을 때 - 회원가입으로 넘기게 하기
            ViewData["LoginPage"] = true;
            ViewData["LoginFailed"] = true;
            return View(model);
        }

        public IActionResult Logout()
        {
            string sessionKey = HttpContext.Request.Cookies["SESSION_KEY"];  // 클라이언트의 쿠키를 받아옴

            if (!string.IsNullOrEmpty(sessionKey) && _authService.IsSessionValid(sessionKey))
            {
                _authService.RemoveSession(sessionKey);

            }

            return RedirectToAction("Index", "Home");
        }

        /// <summary>
        /// 회원가입
        /// </summary>
        /// <returns></returns>
        public IActionResult Register()
        {
            ViewData["RegisterPage"] = true;
            return View();
        }

        /// <summary>
        /// 회원 가입 전송
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        [HttpPost]
        public IActionResult Register(RegisterViewModel model)
        {
            if (ModelState.IsValid)
            {
                // 계정이 있으면 로그인시키고, 계정 없으면 회원가입 할까요?알림창->구글로 회원가입 시키기.

                _authService.RegisterUserAsync(model);

                return RedirectToAction("Index", "Home");
            }

            ViewData["RegisterPage"] = true;
            return View(model);
        }


        public void GoogleLogin()
        {
            // GoogleWebAuthorizationBroker.AuthorizeAsync API 자체가
            // 인증을 위한 요청 URL에 redirect_uri 파트를 json 파일의 내용으로부터 가져오지 않는다. 
            // 따라서 ASP.NET 환경에서는 인증 요청에 대한 URL을 직접 작성해야 한다. 

            string rediect_uri = "https://localhost:44396/Home/GoogleUserEmailList";

            var request = _flow.CreateAuthorizationCodeRequest(
                rediect_uri);    // redirect uri를 포함시켜 google 인증 요청을 위한 url 생성
            Uri uri = request.Build();

            string authRequestUrl = uri.ToString();
            Response.Redirect(authRequestUrl);  // Google 인증 페이지가 뜨게 함

        }

    }
}

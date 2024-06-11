using AspnetWeb.Models;
using AspnetWeb.ViewModel;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Distributed;


namespace AspnetWeb.Controllers
{
    public class AccountController : Controller
    {
		private readonly IDistributedCache _redisCache;
        private readonly IAuthService _authService;

        // 생성자 주입을 통한 DI
		public AccountController(IDistributedCache redisCache, IAuthService authService)
		{
			_redisCache = redisCache;
            _authService = authService;
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
            if(ModelState.IsValid)
            {
                var user = await _authService.LoginUserAsync(model);

                // 로그인에 성공했을 때 - 세션 생성
                if (user != null)
                {
                    string sessionKey = Guid.NewGuid().ToString();    // GUID는 매우 난수적이며 중복될 가능성이 매우 낮은 값.

                    var redisOptions = new DistributedCacheEntryOptions();
                    redisOptions.SetAbsoluteExpiration(TimeSpan.FromSeconds(20));   // 현재를 기준으로 절대 만료 시간을 설정
                    byte[] userNoBytes = BitConverter.GetBytes(user.UserNo);
                    _redisCache.Set(sessionKey, userNoBytes, redisOptions);   // redis에 sessionKey 저장, 값은 UID로 해서 어떤 유저인지 식별.
                                                                             // 세션은 연결된 유저가 누구인지 저장하고 있다. 

                    CookieOptions cookieOptions = new CookieOptions();
                    cookieOptions.Expires = DateTimeOffset.UtcNow.AddSeconds(20);  // 쿠키도 만료시간 설정
                    HttpContext.Response.Cookies.Append("SESSION_KEY", sessionKey, cookieOptions); // 클라이언트에게 세션키 전달

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
                HttpContext.Response.Cookies.Delete("SESSION_KEY");     // 클라이언트에게 해당 세션 키를 지우도록 쿠키를 전송
                _redisCache.Remove(sessionKey);   // redis에서도 세션키 삭제

			    return RedirectToAction("Index", "Home");
            }

            return null;
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
        public IActionResult Register(User model)
        {
            if(ModelState.IsValid)
            {
                // 계정이 있으면 로그인시키고, 계정 없으면 회원가입 할까요?알림창->구글로 회원가입 시키기.

                _authService.RegisterUserAsync(model);

                return RedirectToAction("Index", "Home");
            }

            ViewData["RegisterPage"] = true;
            return View(model);
        }


    }
}

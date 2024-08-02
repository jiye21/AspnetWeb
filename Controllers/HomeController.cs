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
            return View();
        }

		[Route("/api/Home/MemberIndex")]
		public IActionResult MemberIndex()
        {
            // ViewBag에 데이터 담는 방식은 view를 바로 리턴할 때만 사용가능
			ViewData["SESSION_KEY"] = string.Empty;  // 내비게이션 바 변경을 위한 ViewData

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

			// GetGoogleUser: 이미 Google 회원가입한 유저 정보를 가져옴. 
			// 가입되어있지 않다면 Google에서 받아온 유저이름,이메일 등의 정보로 oAuthUser로 회원가입시키고 그 유저정보를 가져옴. 
			var googleUser = await _authService.GetGoogleUser(code);

            if (googleUser == null)   // 유저를 정상적으로 받아오지 못했을 때
            {
                return RedirectToAction("Login", "Account");
            }

            // 받아온 Google OAuth유저 정보로 User테이블을 검색해 유저이름을 받아옴
            var userInfo = _dbContext.Users.FirstOrDefault(u=>u.UID.Equals(googleUser.MUID));
            if (userInfo != null) 
            { 
                ViewData["Name"] = userInfo.UserName;
            }

            
            ViewData["SESSION_KEY"] = string.Empty;  // 내비게이션 바 변경을 위한 ViewData
            return View(googleUser);
        }


        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}

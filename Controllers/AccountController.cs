using AspnetWeb.DataContext;
using AspnetWeb.Models;
using AspnetWeb.ViewModel;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Distributed;
using System.Security.Cryptography;
using System.Text;
using System.Diagnostics;

namespace AspnetWeb.Controllers
{
    public class AccountController : Controller
    {
		private readonly IDistributedCache _redisCache;
		public AccountController(IDistributedCache redisCache)
		{
			_redisCache = redisCache;
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
        public IActionResult Login(LoginViewModel model)
        {
            if(ModelState.IsValid)
            {
                using (var db = new AspnetNoteDbContext())
                {
                    var user = db.Users
                        .FirstOrDefault(u => u.UserId.Equals(model.UserId) && 
                                        u.UserPassword.Equals(SHA256Hash(model.UserPassword)));
                    if(user != null)
                    {
                        // 로그인에 성공했을 때
                        string sessionKey = Guid.NewGuid().ToString();    // GUID는 매우 난수적이며 중복될 가능성이 매우 낮은 값.

                        var redisOption = new DistributedCacheEntryOptions();
                        redisOption.SetAbsoluteExpiration(TimeSpan.FromSeconds(15));   // 현재를 기준으로 절대 만료 시간을 설정
                        byte[] userNoBytes = BitConverter.GetBytes(user.UserNo);
                        _redisCache.Set(sessionKey, userNoBytes, redisOption);   // redis에 sessionKey 저장, 값은 UID로 해서 어떤 유저인지 식별.
                                                                         // 세션은 연결된 유저가 누구인지 저장하고 있다. 

						HttpContext.Response.Cookies.Append("SESSION_KEY", sessionKey); // 클라이언트에게 세션키 전달
                        return RedirectToAction("LoginSuccess", "Home");    // 로그인 성공 페이지로 이동
                    }
                }
                // 로그인에 실패했을 때 - 회원가입으로 넘기게 하기
                //ModelState.AddModelError(string.Empty, "사용자 ID 혹은 비밀번호가 올바르지 않습니다. ");
                
            }
            ViewData["LoginPage"] = true;
            return View(model);
        }

        public IActionResult Logout()
        {
            string sessionKey = HttpContext.Request.Cookies["SESSION_KEY"];  // 클라이언트의 쿠키를 받아옴. 

            HttpContext.Response.Cookies.Delete("SESSION_KEY");     // 클라이언트에게 해당 세션 키를 지우도록 쿠키를 전송

            _redisCache.Remove(sessionKey);   // redis에서 세션 삭제

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
        public IActionResult Register(User model)
        {
            if(ModelState.IsValid)
            {                
                // 계정이 있으면 로그인시키고, 계정 없으면 회원가입 할까요?알림창->구글로 회원가입 시키기.
                                
                model.UserPassword = SHA256Hash(model.UserPassword);  // 패스워드 해시화

                using (var db = new AspnetNoteDbContext())
                {
                    db.Users.Add(model);
                    db.SaveChanges();
                }
                return RedirectToAction("Index", "Home");
            }

            ViewData["RegisterPage"] = true;
            return View(model);
        }


        public string SHA256Hash(string data)
        {
            SHA256 sHA256 = SHA256.Create();
            byte[] hash = sHA256.ComputeHash(Encoding.ASCII.GetBytes(data));  // byte[]형식의 해시값으로 변환
            
            string hashString = Convert.ToBase64String(hash);

            return hashString;
        }
    }
}

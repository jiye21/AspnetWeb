using AspnetWeb.ViewModel;
using Microsoft.AspNetCore.Mvc;
using Google.Apis.Auth.OAuth2.Flows;
using System.Security.Claims;
using System.IdentityModel.Tokens.Jwt;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using AspnetWeb.Models;
using Microsoft.Extensions.Caching.Distributed;


namespace AspnetWeb.Controllers
{
	public class AccountController : Controller
	{
		private readonly IAuthService _authService;
		private readonly GoogleAuthorizationCodeFlow _flow;
		private readonly IConfiguration _config;
		private readonly IDistributedCache _redisCache;

		// 생성자 주입을 통한 DI
		public AccountController(IAuthService authService, GoogleAuthorizationCodeFlow flow,
			IConfiguration config, IDistributedCache redisCache)
		{
			_authService = authService;
			_flow = flow;
			_config = config;
			_redisCache = redisCache;
		}


		/// <summary>
		/// 로그인
		/// </summary>
		/// <returns></returns>
		[HttpGet]
		public IActionResult Login()
		{
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
					_authService.GenerateSession(user.MUID);
					// Server side RedirectToAction will only work if you start the request from your browser's location bar. 
					return RedirectToAction("MemberIndex", "Home");
				}
			}

			// 로그인에 실패했을 때 - 회원가입으로 넘기게 하기
			ViewData["LoginFailed"] = string.Empty;
			return View(model);
		}


		/// <summary>
		/// 로그인 후 세션이 아닌 JWT발급
		/// </summary>
		/// <param name="model"></param>
		/// <returns></returns>
		[HttpPost]
		public async Task<IActionResult> LoginWithJWT([FromBody] LoginViewModel model)
		{
			if (ModelState.IsValid)
			{
				var user = await _authService.LoginUserAsync(model);

				// JWT 발급 - 로그인에 성공했을 때
				if (user != null)
				{
					GenerateJWT(user);

					return Ok();
				}
			}

			// 로그인 실패
			return Unauthorized();
		}

		public void GenerateJWT(AspnetUser user)
		{
			// 비밀키 생성 후 Signature 필드 생성
			var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_config["JWT:SecretKey"]));
			var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);
			// claim에 MUID와 난수값 넣음
			var claims = new List<Claim>
			{
				new Claim(ClaimTypes.NameIdentifier, user.MUID.ToString()),  // UID
				new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),  // jwt 고유 식별자. 토큰 중복 방지를 위함
            };

			var token = new JwtSecurityToken(
				issuer: _config["JWT:Issuer"],
				audience: _config["JWT:Audience"],
				claims: claims,
				expires: DateTime.Now.AddMinutes(2),
				signingCredentials: credentials
			);

			// jwt 토큰 생성
			string tokenString = new JwtSecurityTokenHandler().WriteToken(token);

			// 쿠키에 토큰을 담아 전달
			var cookieOptions = new CookieOptions
			{
				HttpOnly = true, // HTTPOnly 속성을 설정하여 클라이언트 측 JavaScript에서 접근할 수 없게 함
				Secure = true, // HTTPS에서만 쿠키 전송을 허용 (SSL/TLS를 사용해야 함)
			};

			HttpContext.Response.Cookies.Append("JWT", tokenString, cookieOptions);
		}

		[Route("/api/Logout")]
		public IActionResult Logout()
		{
			long? userMUID = null;

			string sessionKey = HttpContext.Request.Cookies["SESSION_KEY"];
			// 세션 로그인일 경우
			// (Google 로그인도 세션을 발급받아 마이페이지 접근하므로 로그아웃 시 세션로그인과 동일하게 처리)
            if (!string.IsNullOrEmpty(sessionKey))
            {
				// api 미들웨어에서 세션 검증완료, 바로 세션 삭제만 진행
				var sessionValue = _redisCache.Get(sessionKey);
				if (sessionValue != null)
				{
					userMUID = BitConverter.ToInt64(sessionValue);
				}
				HttpContext.Response.Cookies.Delete("SESSION_KEY"); // 클라이언트에게 해당 세션 키를 지우도록 쿠키를 전송
				_redisCache.Remove(sessionKey);   // redis에서도 세션키 삭제

				if(userMUID.HasValue)
				{
					_redisCache.Remove("shoppinglist_" + userMUID.ToString()); // 캐싱된 shoppinglist 데이터 삭제
					_redisCache.Remove("friendlist_" + userMUID.ToString());
					_redisCache.Remove("friend_hearts_" + userMUID.ToString());
				}
				return RedirectToAction("Index", "Home");
            }



			// 세션 로그인이 아니라면 JWT 로그인, 캐싱된 shoppinglist 데이터만 삭제
			string token = HttpContext.Request.Cookies["JWT"];
			var tokenHandler = new JwtSecurityTokenHandler();
			var accessToken = (JwtSecurityToken)tokenHandler.ReadToken(token);

			userMUID = Convert.ToInt64(accessToken.Claims.
				FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?.Value);
			if (userMUID.HasValue)
			{
				_redisCache.Remove("shoppinglist_" + userMUID.ToString());
				_redisCache.Remove("friendlist_" + userMUID.ToString());
				_redisCache.Remove("friend_hearts_" + userMUID.ToString());
			}
			return RedirectToAction("Index", "Home");
		}



		/// <summary>
		/// 회원가입
		/// </summary>
		/// <returns></returns>
		public IActionResult Register()
		{
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

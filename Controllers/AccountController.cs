using AspnetWeb.ViewModel;
using Microsoft.AspNetCore.Mvc;
using Google.Apis.Auth.OAuth2.Flows;
using System.Security.Claims;
using System.IdentityModel.Tokens.Jwt;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using AspnetWeb.Models;
using Microsoft.Extensions.Caching.StackExchangeRedis;


namespace AspnetWeb.Controllers
{
	public class AccountController : Controller
	{
		private readonly IAuthService _authService;
		private readonly GoogleAuthorizationCodeFlow _flow;
		private readonly IConfiguration _config;

		// 생성자 주입을 통한 DI
		public AccountController(IAuthService authService, GoogleAuthorizationCodeFlow flow,
			IConfiguration config)
		{
			_authService = authService;
			_flow = flow;
			_config = config;
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
		public async Task<IActionResult> Login([FromBody] LoginViewModel model)
		{
			if (ModelState.IsValid)
			{
				var user = await _authService.LoginUserAsync(model);

				// 로그인에 성공했을 때 - 세션 생성
				if (user != null)
				{
					_authService.GenerateSession(user.MUID);
					return Redirect("https://localhost:44396/api/LoginSuccess");
				}
			}

			// 로그인에 실패했을 때 - 회원가입으로 넘기게 하기
			ViewData["LoginPage"] = true;
			ViewData["LoginFailed"] = true;
			return View(model);
		}


		/// <summary>
		/// 로그인 후 세션이 아닌 JWT발급
		/// </summary>
		/// <param name="model"></param>
		/// <returns></returns>
		[Route("/Account/JWTLogin")]
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
                    return Redirect("https://localhost:44396/api/Home/MemberIndex");
                }
			}

			// 로그인 실패
			ViewData["LoginPage"] = true;
			ViewData["LoginFailed"] = true;
			return View(model);
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

			var cookieOptions = new CookieOptions
			{
				HttpOnly = true, // HTTPOnly 속성을 설정하여 클라이언트 측 JavaScript에서 접근할 수 없게 함
				Secure = true, // HTTPS에서만 쿠키 전송을 허용 (SSL/TLS를 사용해야 함)
			};

            HttpContext.Response.Cookies.Append("AccessToken", tokenString, cookieOptions);

			

            // 토큰 생성 후 JSON 응답
            //IActionResult response = Ok(new { token = tokenString });
            //return response;
        }

		[Route("/api/Logout")]
		public IActionResult Logout()
		{
			string sessionKey = HttpContext.Request.Cookies["SESSION_KEY"];  // 클라이언트의 쿠키를 받아옴
            if (!string.IsNullOrEmpty(sessionKey))
            {
                _authService.RemoveSession(sessionKey);  // api 미들웨어에서 세션 검증완료, 바로 세션 삭제만 진행
                return RedirectToAction("Index", "Home");
            }


			// 구글 로그인일 경우 추후 구현
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

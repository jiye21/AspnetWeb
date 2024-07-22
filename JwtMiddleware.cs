using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace AspnetWeb
{
	// 인증이 필요한 컨트롤러 요청시 JWT 유효성 체크
	// 예) 라우터 경로 ‘/api/’로 시작되는 요청 -> Program.cs에 명시되어 있음
	public class JwtMiddleware
	{
		private readonly RequestDelegate _next;
		private readonly IConfiguration _configuration;
		//private const string APIKEYNAME = "Authorization"; // Authorization: HTTP 요청에서 인증 정보를 전달하기 위해 예약된 헤더

		public JwtMiddleware(RequestDelegate next, IConfiguration configuration)
		{
			_next = next;
			_configuration = configuration;
		}

		public async Task InvokeAsync(HttpContext context)
		{
			string accessToken = context.Request.Cookies["ACCESS_TOKEN"];
			if (string.IsNullOrEmpty(accessToken))
			{
				context.Response.StatusCode = StatusCodes.Status401Unauthorized;
				await context.Response.WriteAsync("Api Key was not provided. (Using ApiKeyMiddleware)");
				return;
			}

			if (this.ValidateToken(accessToken) == false)
			{
				context.Response.StatusCode = StatusCodes.Status401Unauthorized;
				await context.Response.WriteAsync("jwt token validation failed");
				return;
			}
			await _next(context);
		}

		private bool ValidateToken(string token)
		{
			if (string.IsNullOrWhiteSpace(token))
				return false;

			try
			{
				var tokenHandler = new JwtSecurityTokenHandler();
				var authSigningKey = Encoding.UTF8.GetBytes(_configuration["JWT:SecretKey"]);
				tokenHandler.ValidateToken(token, new TokenValidationParameters
				{
					ValidateIssuerSigningKey = true,
					IssuerSigningKey = new SymmetricSecurityKey(authSigningKey),
					ValidateIssuer = false,
					ValidateAudience = false,
					// set clockskew to zero so tokens expire exactly at token expiration time (instead of 5 minutes later)
					ClockSkew = TimeSpan.Zero
				}, out SecurityToken validatedToken);

				// 만약 위 tokenHandler.ValidateToken에서 예외가 발생하지 않았다면 true 리턴
				return true;
			}
			catch (Exception ex)
			{
				return false;
			}
		}

	}
}

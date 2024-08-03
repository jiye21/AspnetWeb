using AspnetWeb.DataContext;
using Azure.Core;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using Newtonsoft.Json;
using StackExchange.Redis;
using System;
using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Text;

namespace AspnetWeb
{
	public class RequestAuthMiddleware
	{
		private readonly RequestDelegate _next;
		private readonly IDistributedCache _redisCache;
		private readonly IConfiguration _configuration;


		public RequestAuthMiddleware(RequestDelegate next, IDistributedCache redisCache, 
			IConfiguration configuration)
		{
			_next = next;
			_redisCache = redisCache;
			_configuration = configuration;
		}

		public async Task InvokeAsync(HttpContext context, IAuthService authService)
		{
			// 세션 검증 후 업데이트
			string sessionKey = context.Request.Cookies["SESSION_KEY"];

			if (!string.IsNullOrEmpty(sessionKey))
			{
				var sessionValue = _redisCache.Get(sessionKey);
				if (sessionValue != null)
				{
					// 세션 업데이트
					authService.UpdateSessionAndCookie(sessionKey, sessionValue);
					// 내비게이션바 변경 데이터 추가?

					await _next(context);
					return;
				}
			}



			// 세션이 아닐경우 JWT 검증			
			string accessToken = context.Request.Cookies["JWT"];

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

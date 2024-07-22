using Azure.Core;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using Newtonsoft.Json;
using StackExchange.Redis;
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



		public RequestAuthMiddleware(RequestDelegate next, IDistributedCache redisCache, IConfiguration configuration)
		{
			_next = next;
			_redisCache = redisCache;
			_configuration = configuration;

		}

		public async Task InvokeAsync(HttpContext context)
		{
			// 세션 검증
			string sessionKey = context.Request.Cookies["SESSION_KEY"];
			/*
			if (string.IsNullOrEmpty(sessionKey))
			{
				context.Response.StatusCode = StatusCodes.Status401Unauthorized;
				await context.Response.WriteAsync("sessionkey not found");
				return;
			}

			var sessionValue = _redisCache.Get(sessionKey);
			if (sessionValue == null)
			{
				//Session = true;
				context.Response.StatusCode = StatusCodes.Status401Unauthorized;
				await context.Response.WriteAsync("Invalid sessionkey");
				return;
			}
			Session = false;
			*/

			if (!string.IsNullOrEmpty(sessionKey))
			{
				var sessionValue = _redisCache.Get(sessionKey);
				if (sessionValue != null)
				{
					await _next(context);
					return;
				}
			}



			// 세션이 아닐경우 JWT 검증
			string accessToken = context.Request.Cookies["AccessToken"];
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



			/*
            if (!context.Request.Headers.TryGetValue("Authorization", out var extractedApiKey))
			{
				context.Response.StatusCode = StatusCodes.Status401Unauthorized;
				await context.Response.WriteAsync("Api Key was not provided. (Using ApiKeyMiddleware)");
				return;
			}

			// authorization bearer 형식의 헤더 키 값으로 넘어옴
			string accessToken = context.Request.Headers["Authorization"].FirstOrDefault()?.Split(" ").Last();
			if (this.ValidateToken(accessToken) == false)
			{
				context.Response.StatusCode = StatusCodes.Status401Unauthorized;
				await context.Response.WriteAsync("jwt token validation failed");
				return;
			}
			*/

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

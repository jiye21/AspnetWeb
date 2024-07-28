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
	public class CheckLoginMiddleware
	{
		private readonly RequestDelegate _next;
		private readonly IDistributedCache _redisCache;
		private readonly IConfiguration _configuration;



		public CheckLoginMiddleware(RequestDelegate next, IDistributedCache redisCache, IConfiguration configuration)
		{
			_next = next;
			_redisCache = redisCache;
			_configuration = configuration;

		}

		public async Task InvokeAsync(HttpContext context)
		{
			// 세션 검증
			string sessionKey = context.Request.Cookies["SESSION_KEY"];

			if (!string.IsNullOrEmpty(sessionKey))
			{
				context.Response.StatusCode = StatusCodes.Status401Unauthorized;
				await context.Response.WriteAsync("You are already Loggined in");
				return;
			}


			// 세션이 아닐경우 JWT 검증
			/*
			string accessToken = context.Request.Cookies["AccessToken"];
            if (!string.IsNullOrEmpty(accessToken))
            {
                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                await context.Response.WriteAsync("You are already Loggined in");
                return;
            }
			*/
			if (context.Request.Headers.TryGetValue("Authorization", out var extractedApiKey))
			{
				context.Response.StatusCode = StatusCodes.Status401Unauthorized;
				await context.Response.WriteAsync("You are already Loggined in");
				return;
			}

			await _next(context);
		}

				
	}

}

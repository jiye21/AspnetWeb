using Microsoft.Extensions.Caching.Distributed;

namespace AspnetWeb
{
	public class RequestAuthMiddleware
	{
		private readonly RequestDelegate _next;
		private readonly IDistributedCache _redisCache;
        public static bool Session { get; set; }

        public RequestAuthMiddleware(RequestDelegate next, IDistributedCache redisCache)
		{
			_next = next;
			_redisCache = redisCache;
		}

		public async Task InvokeAsync(HttpContext context)
		{
			// 세션 검증
			string sessionKey = context.Request.Cookies["SESSION_KEY"];
			if (!string.IsNullOrEmpty(sessionKey))
			{
				var sessionValue = _redisCache.Get(sessionKey);

				if (sessionValue != null)
				{
					Session = true;
					await _next(context);
				}
			}

			Session = false;
			await _next(context);
		}

	}

	public static class RequestAuthMiddlewareExtensions
	{
		public static IApplicationBuilder UseRequestAuth(
			this IApplicationBuilder builder)
		{
			return builder.UseMiddleware<RequestAuthMiddleware>();
		}

	}
}

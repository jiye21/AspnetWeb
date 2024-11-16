using System.Diagnostics;

namespace AspnetWeb
{
    public class QueryTimingMiddleware
    {
        private readonly RequestDelegate _next;

        public QueryTimingMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            var stopwatch = Stopwatch.StartNew();

            await _next(context);

            stopwatch.Stop();
            Console.WriteLine($"전체 요청 처리 시간: {stopwatch.ElapsedMilliseconds} ms");
        }
    }
}

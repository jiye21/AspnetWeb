using AspnetWeb;
using Google.Apis.Auth.AspNetCore3;
using Google.Apis.Auth.OAuth2.Flows;
using Google.Apis.Auth.OAuth2;
using Microsoft.AspNetCore.Authentication.Cookies;
using Google.Apis.Oauth2.v2;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// DI, 의존성 주입
builder.Services.AddControllersWithViews();
// Session을 서비스에 등록
builder.Services.AddSession();

builder.Services.AddStackExchangeRedisCache(options =>                  // 분산캐시 서비스를 추가
{
    options.Configuration = "127.0.0.1";                                // redis 주소
});

builder.Services.AddTransient<IAuthService, AuthService>();

// controller가 아닌 곳에서 httpcontext를 받아오기 위함
builder.Services.AddHttpContextAccessor();

builder.Services
    .AddSingleton<GoogleAuthorizationCodeFlow>(provider =>
    {
        using (var stream = new FileStream("client_secrets.json", FileMode.Open, FileAccess.Read))
        {
            var clientSecrets = GoogleClientSecrets.FromStream(stream).Secrets;
            var initializer = new GoogleAuthorizationCodeFlow.Initializer
            {
                ClientSecrets = clientSecrets,
                Scopes = [Oauth2Service.Scope.Openid, Oauth2Service.Scope.UserinfoEmail
                ,Oauth2Service.Scope.UserinfoProfile]   // scope: 사용자에게 요청할 정보 범위
            };
            return new GoogleAuthorizationCodeFlow(initializer);   // =GoogleAuthorizationCodeFlow
        }
    });


var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

// application에서 session을 사용하겠다. 
app.UseSession();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();

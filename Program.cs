using AspnetWeb;
using Google.Apis.Auth.OAuth2.Flows;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Oauth2.v2;
using AspnetWeb.DataContext;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;

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

// Authentication정보 서비스 주입을 등록해서 jwt스키마를 사용하도록 설정
// 인증 스키마(Authentication Scheme)란, 애플리케이션에서 사용자 인증을 처리하는 방법과
// 프로토콜을 정의하는 체계를 말한다. 
builder.Services.AddAuthentication(options =>
{
	options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
	options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
	options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
})
	.AddJwtBearer(options =>
{
	options.RequireHttpsMetadata = false;
	options.SaveToken = true;
	options.TokenValidationParameters = new TokenValidationParameters
	{
		ValidateIssuer = true,    // Token의 발행자
		ValidateAudience = true,  // Token을 받을 대상, 일반적으로 JWT인증을 수행하는 도메인.
		ValidateIssuerSigningKey = true,
		ValidIssuer = builder.Configuration["JWT:Issuer"],
		ValidAudience = builder.Configuration["JWT:Audience"],
		// set clockskew to zero so tokens expire exactly at token expiration time (instead of 5 minutes later)
		ClockSkew = TimeSpan.Zero,
		IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(builder.Configuration["JWT:SecretKey"])),
	};
});

// controller에서 dbcontext를 생성자 주입하기 위함
builder.Services.AddDbContext<AspnetNoteDbContext>();

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

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();


// 세션, JWT 인증 체크 Middleware 추가, [api] 컨트롤러만 적용
app.UseWhen(context => context.Request.Path.StartsWithSegments("/api"), appBuilder =>
{
	// 세션, JWT 체크 미들웨어
	appBuilder.UseMiddleware<RequestAuthMiddleware>(); // 사용자 정의 미들웨어
});



app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();

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
// DI, ������ ����
builder.Services.AddControllersWithViews();
// Session�� ���񽺿� ���
builder.Services.AddSession();

builder.Services.AddStackExchangeRedisCache(options =>                  // �л�ĳ�� ���񽺸� �߰�
{
    options.Configuration = "127.0.0.1";                                // redis �ּ�
});

builder.Services.AddTransient<IAuthService, AuthService>();

// controller�� �ƴ� ������ httpcontext�� �޾ƿ��� ����
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
                ,Oauth2Service.Scope.UserinfoProfile]   // scope: ����ڿ��� ��û�� ���� ����
            };
            return new GoogleAuthorizationCodeFlow(initializer);   // =GoogleAuthorizationCodeFlow
        }
    });

// Authentication���� ���� ������ ����ؼ� jwt��Ű���� ����ϵ��� ����
// ���� ��Ű��(Authentication Scheme)��, ���ø����̼ǿ��� ����� ������ ó���ϴ� �����
// ���������� �����ϴ� ü�踦 ���Ѵ�. 
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
		ValidateIssuer = true,    // Token�� ������
		ValidateAudience = true,  // Token�� ���� ���, �Ϲ������� JWT������ �����ϴ� ������.
		ValidateIssuerSigningKey = true,
		ValidIssuer = builder.Configuration["JWT:Issuer"],
		ValidAudience = builder.Configuration["JWT:Audience"],
		// set clockskew to zero so tokens expire exactly at token expiration time (instead of 5 minutes later)
		ClockSkew = TimeSpan.Zero,
		IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(builder.Configuration["JWT:SecretKey"])),
	};
});

// controller���� dbcontext�� ������ �����ϱ� ����
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


// ����, JWT ���� üũ Middleware �߰�, [api] ��Ʈ�ѷ��� ����
app.UseWhen(context => context.Request.Path.StartsWithSegments("/api"), appBuilder =>
{
	// ����, JWT üũ �̵����
	appBuilder.UseMiddleware<RequestAuthMiddleware>(); // ����� ���� �̵����
});



app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();

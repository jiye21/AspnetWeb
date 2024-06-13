using AspnetWeb;
using Google.Apis.Auth.AspNetCore3;
using Google.Apis.Auth.OAuth2.Flows;
using Google.Apis.Auth.OAuth2;
using Microsoft.AspNetCore.Authentication.Cookies;
using Google.Apis.Oauth2.v2;

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

// application���� session�� ����ϰڴ�. 
app.UseSession();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();

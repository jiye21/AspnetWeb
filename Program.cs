var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();
// DI ������ ����
// Session�� ���񽺿� ���
builder.Services.AddSession();

builder.Services.AddStackExchangeRedisCache(options =>                  // �л�ĳ�� ���񽺸� �߰�
{
    options.Configuration = "127.0.0.1";                                // redis �ּ�
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

app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();

using Flipped_Classroom.Data;
using Flipped_Classroom.Services.Implementations;
using Flipped_Classroom.Services.Interfaces;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;
var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorPages();
builder.Services.AddDbContext<Swp391NihongoContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddAuthentication(options =>
{
    // Cookie là scheme chính — mọi request authenticated đều dùng cookie
    options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
    // KHÔNG set DefaultChallengeScheme — để GoogleLogin page tự gọi Challenge(Google)
})
.AddCookie(options =>
{
    options.LoginPath = "/Authentication/Login";
    options.LogoutPath = "/Authentication/Logout";
    options.AccessDeniedPath = "/Authentication/AccessDenied";
    options.ExpireTimeSpan = TimeSpan.FromHours(2);
    options.SlidingExpiration = true;
})
.AddGoogle(options =>
{
    options.ClientId = builder.Configuration["Authentication:Google:ClientId"]!;
    options.ClientSecret = builder.Configuration["Authentication:Google:ClientSecret"]!;

    // Middleware tự xử lý /signin-google, sau đó redirect về RedirectUri
    // mà ta truyền vào lúc Challenge() — là /Authentication/GoogleCallback
    options.CallbackPath = "/signin-google";

    options.Scope.Add("email");
    options.Scope.Add("profile");
    options.SaveTokens = true;

    // Bắt lỗi cancel/deny từ Google trước khi middleware throw exception.
    // Khi user bấm Cancel, Google redirect về /signin-google?error=access_denied
    // — OnRemoteFailure intercept và redirect về Login thay vì crash.
    options.Events.OnRemoteFailure = context =>
    {
        context.Response.Redirect("/Authentication/Login?cancelled=true");
        context.HandleResponse();
        return Task.CompletedTask;
    };
});

builder.Services.AddAuthorization();
var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthorization();

app.MapRazorPages();

app.Run();

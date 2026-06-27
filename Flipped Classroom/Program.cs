using System.Security.Claims;
using Flipped_Classroom.Data;
using Flipped_Classroom.Services.Implementation;
using Flipped_Classroom.Services.Implementations;
using Flipped_Classroom.Services.Interfaces;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorPages();
builder.Services.AddDbContext<Swp391NihongoContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection"))
);

builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<ICurriculumService, CurriculumService>();
builder.Services.AddScoped<IFileStorageService, FileStorageService>();
builder
    .Services.AddAuthentication(options =>
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

        options.CallbackPath = "/signin-google";

        options.AccessType = "offline"; // Quan trọng: Để lấy Refresh Token
        options.Scope.Add("https://www.googleapis.com/auth/gmail.send");

        options.Scope.Add("email");
        options.Scope.Add("profile");
        options.SaveTokens = true;

        options.Events.OnRemoteFailure = context =>
        {
            context.Response.Redirect("/Authentication/Login?cancelled=true");
            context.HandleResponse();
            return Task.CompletedTask;
        };
    });

builder.Services.AddScoped<IQuestionService, QuestionService>();
builder.Services.AddScoped<IQuizService, QuizService>();
builder.Services.AddScoped<IAssignmentService, AssignmentService>();
builder.Services.AddScoped<ISubmissionService, SubmissionService>();
builder.Services.AddScoped<ILessonService, LessonService>();
builder.Services.AddScoped<IVocabularyService, VocabularyService>();
builder.Services.AddScoped<IScheduleService, ScheduleService>();
builder.Services.AddScoped<IWordExcelImportService, WordExcelImportService>();

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

app.UseAuthentication();
app.UseAuthorization();

app.Use(
    async (context, next) =>
    {
        var path = context.Request.Path;
        var isStudent =
            context.User.Identity?.IsAuthenticated == true && context.User.IsInRole("Student");
        var isAllowedPath =
            path.StartsWithSegments("/Quizzes/DailyReview")
            || path.StartsWithSegments("/Authentication")
            || path.StartsWithSegments("/Users/Profile");

        if (isStudent && !isAllowedPath)
        {
            var userIdValue = context.User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (int.TryParse(userIdValue, out var studentId))
            {
                using var scope = context.RequestServices.CreateScope();
                var quizService = scope.ServiceProvider.GetRequiredService<IQuizService>();

                if (await quizService.IsDailyReviewRequiredAsync(studentId))
                {
                    context.Response.Redirect("/Quizzes/DailyReview");
                    return;
                }
            }
        }

        await next();
    }
);

app.MapRazorPages();

app.Run();

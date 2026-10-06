using ContentCartel.API.Services;
using Microsoft.AspNetCore.Authentication.Cookies;

namespace ContentCartel
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            builder.Services.AddControllersWithViews();
            builder.Services.AddControllers();

            builder.Services.AddHttpClient("ContentCartelAPI", client =>
            {
                client.BaseAddress = new Uri(
                    "https://web-application-content-cartel.onrender.com/"
                );
            });

            builder.Services
                .AddAuthentication(
                    CookieAuthenticationDefaults.AuthenticationScheme
                )
                .AddCookie(options =>
                {
                    options.LoginPath = "/Account/Login";
                    options.LogoutPath = "/Account/Logout";
                    options.AccessDeniedPath = "/Account/Login";
                    options.ExpireTimeSpan = TimeSpan.FromHours(8);
                    options.SlidingExpiration = true;
                });

            builder.Services.AddScoped<FirebaseService>();
            builder.Services.AddScoped<RecaptchaService>();

            builder.Services.AddCors(options =>
            {
                options.AddPolicy("AllowSameOrigin", policy =>
                {
                    policy
                        .AllowAnyHeader()
                        .AllowAnyMethod()
                        .SetIsOriginAllowed(_ => true);
                });
            });

            var app = builder.Build();

            if (!app.Environment.IsDevelopment())
            {
                app.UseExceptionHandler("/Home/Error");
                app.UseHsts();
            }

            app.UseHttpsRedirection();
            app.UseStaticFiles();

            app.UseRouting();

            app.UseCors("AllowSameOrigin");

            app.UseAuthentication();
            app.UseAuthorization();

            app.MapControllers();

            app.MapControllerRoute(
                name: "default",
                pattern: "{controller=Home}/{action=Index}/{id?}"
            );

            app.Run();
        }
    }
}
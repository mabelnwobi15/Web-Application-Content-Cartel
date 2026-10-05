using Microsoft.AspNetCore.Authentication.Cookies;

namespace ContentCartel
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder =
                WebApplication.CreateBuilder(args);

            builder.Services
                .AddControllersWithViews();

            builder.Services.AddHttpClient(
                "ContentCartelAPI",
                client =>
                {
                    client.BaseAddress =
                        new Uri(
                            "http://localhost:5090/"
                        );
                }
            );

            // Authentication cookie
            builder.Services
                .AddAuthentication(
                    CookieAuthenticationDefaults
                        .AuthenticationScheme
                )
                .AddCookie(options =>
                {
                    options.LoginPath =
                        "/Account/Login";

                    options.LogoutPath =
                        "/Account/Logout";

                    options.AccessDeniedPath =
                        "/Account/Login";

                    options.ExpireTimeSpan =
                        TimeSpan.FromHours(8);

                    options.SlidingExpiration =
                        true;
                });

            var app =
                builder.Build();

            if (!app.Environment.IsDevelopment())
            {
                app.UseExceptionHandler(
                    "/Home/Error"
                );

                app.UseHsts();
            }

            app.UseHttpsRedirection();

            app.UseStaticFiles();

            app.UseRouting();

            app.UseAuthentication();

            app.UseAuthorization();

            app.MapControllerRoute(
                name: "default",
                pattern:
                    "{controller=Home}/{action=Index}/{id?}"
            );

            app.Run();
        }
    }
}
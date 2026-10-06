using ContentCartel.API.Services;
using Microsoft.AspNetCore.Authentication.Cookies;

namespace ContentCartel
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // ============================================================
            // MVC + API CONTROLLERS
            // ============================================================

            builder.Services.AddControllersWithViews();

            builder.Services.AddControllers();


            // ============================================================
            // HTTP CLIENT
            // Used by Firebase / reCAPTCHA / Auth API
            // ============================================================

            builder.Services.AddHttpClient();


            // ============================================================
            // AUTHENTICATION
            // ============================================================

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


            // ============================================================
            // CONTENT CARTEL API SERVICES
            // ============================================================

            builder.Services.AddScoped<FirebaseService>();

            builder.Services.AddScoped<RecaptchaService>();


            // ============================================================
            // CORS
            // ============================================================

            builder.Services.AddCors(options =>
            {
                options.AddPolicy(
                    "AllowSameOrigin",
                    policy =>
                    {
                        policy
                            .AllowAnyHeader()
                            .AllowAnyMethod()
                            .SetIsOriginAllowed(_ => true);
                    }
                );
            });


            // ============================================================
            // BUILD APPLICATION
            // ============================================================

            var app = builder.Build();


            // ============================================================
            // PRODUCTION ERROR HANDLING
            // ============================================================

            if (!app.Environment.IsDevelopment())
            {
                app.UseExceptionHandler(
                    "/Home/Error"
                );

                app.UseHsts();
            }


            // ============================================================
            // MIDDLEWARE
            // ============================================================

            app.UseHttpsRedirection();

            app.UseStaticFiles();

            app.UseRouting();

            app.UseCors("AllowSameOrigin");

            app.UseAuthentication();

            app.UseAuthorization();


            // ============================================================
            // API ROUTES
            //
            // Examples:
            // /api/auth/login
            // /api/auth/register
            // /api/bookings
            // /api/invoices
            // /api/quotes
            // /api/services
            // /api/admin/staff
            // /api/firebase/test
            // ============================================================

            app.MapControllers();


            // ============================================================
            // MVC ROUTES
            // ============================================================

            app.MapControllerRoute(
                name: "default",
                pattern:
                    "{controller=Home}/{action=Index}/{id?}"
            );


            // ============================================================
            // RUN
            // ============================================================

            app.Run();
        }
    }
}
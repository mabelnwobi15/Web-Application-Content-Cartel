<<<<<<< HEAD
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.HttpOverrides;
=======
using ContentCartel.API.Services;
using Microsoft.AspNetCore.Authentication.Cookies;
>>>>>>> bde5a1049f558c8d0a1af69dd1a9dda38a07dd99

namespace ContentCartel
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            builder.Services.AddControllersWithViews();
<<<<<<< HEAD

            // API base URL comes from config so it can differ per environment.
            // On Render set the env var:  Api__BaseUrl = https://<your-api>.onrender.com/
            var apiBaseUrl =
                builder.Configuration["Api:BaseUrl"]
                ?? "https://web-application-content-cartel.onrender.com/";

            if (!apiBaseUrl.EndsWith('/'))
            {
                apiBaseUrl += "/";
            }

            builder.Services.AddHttpClient("ContentCartelAPI", client =>
            {
                client.BaseAddress = new Uri(apiBaseUrl);
                // Free-tier Render services can take ~1 minute to wake up.
                client.Timeout = TimeSpan.FromSeconds(120);
            });

            builder.Services
                .AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
=======
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
>>>>>>> bde5a1049f558c8d0a1af69dd1a9dda38a07dd99
                .AddCookie(options =>
                {
                    options.LoginPath = "/Account/Login";
                    options.LogoutPath = "/Account/Logout";
                    options.AccessDeniedPath = "/Account/Login";
                    options.ExpireTimeSpan = TimeSpan.FromHours(8);
                    options.SlidingExpiration = true;
                });

<<<<<<< HEAD
            // Render terminates TLS and forwards plain HTTP to the container.
            builder.Services.Configure<ForwardedHeadersOptions>(options =>
            {
                options.ForwardedHeaders =
                    ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
                options.KnownNetworks.Clear();
                options.KnownProxies.Clear();
=======
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
>>>>>>> bde5a1049f558c8d0a1af69dd1a9dda38a07dd99
            });

            var app = builder.Build();

<<<<<<< HEAD
            app.UseForwardedHeaders();

            if (!app.Environment.IsDevelopment())
            {
                app.UseExceptionHandler("/Home/Error");
            }
            else
            {
                app.UseHttpsRedirection();
            }

            app.UseStaticFiles();
            app.UseRouting();

            app.UseAuthentication();
            app.UseAuthorization();

=======
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

>>>>>>> bde5a1049f558c8d0a1af69dd1a9dda38a07dd99
            app.MapControllerRoute(
                name: "default",
                pattern: "{controller=Home}/{action=Index}/{id?}"
            );

            app.Run();
        }
    }
}
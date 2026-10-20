using ContentCartel.API.Services;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.HttpOverrides;

namespace ContentCartel
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            builder.Services.AddControllersWithViews();

            // ------------------------------------------------------------
            // API BASE URL (THE FIX)
            // Previously this silently fell back to http://localhost:{PORT}/,
            // which on Render is the MVC site itself, so "api/quotes" 404'd.
            // Now a missing setting fails loudly with a clear message.
            //
            //   Render (MVC service) env var:
            //       Api__BaseUrl = https://<your-api-service>.onrender.com/
            //
            //   Local (appsettings.Development.json in the MVC project):
            //       { "Api": { "BaseUrl": "https://localhost:<api-https-port>/" } }
            // ------------------------------------------------------------
            var apiBaseUrl =
                builder.Configuration["Api:BaseUrl"]
                ?? throw new InvalidOperationException(
                    "Api:BaseUrl is not configured. Set the Api__BaseUrl " +
                    "environment variable (Render) or Api:BaseUrl in " +
                    "appsettings.Development.json (local).");

            if (!apiBaseUrl.EndsWith('/'))
            {
                apiBaseUrl += "/";
            }

            Console.WriteLine($"[Startup] ContentCartelAPI base address: {apiBaseUrl}");

            builder.Services.AddHttpClient("ContentCartelAPI", client =>
            {
                client.BaseAddress = new Uri(apiBaseUrl);
                // Free-tier Render services can take ~1 minute to wake up.
                client.Timeout = TimeSpan.FromSeconds(120);
            });

            builder.Services
                .AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
                .AddCookie(options =>
                {
                    options.LoginPath = "/Account/Login";
                    options.LogoutPath = "/Account/Logout";
                    options.AccessDeniedPath = "/Account/Login";
                    options.ExpireTimeSpan = TimeSpan.FromHours(8);
                    options.SlidingExpiration = true;
                });

            // Render terminates TLS and forwards plain HTTP to the container.
            builder.Services.Configure<ForwardedHeadersOptions>(options =>
            {
                options.ForwardedHeaders =
                    ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
                options.KnownIPNetworks.Clear();
                options.KnownProxies.Clear();
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

            app.UseForwardedHeaders();

            if (!app.Environment.IsDevelopment())
            {
                app.UseExceptionHandler("/Home/Error");
                app.UseHsts();
            }
            else
            {
                // Only redirect locally; Render's proxy already handles HTTPS.
                app.UseHttpsRedirection();
            }

            app.UseStaticFiles();
            app.UseRouting();

            app.UseCors("AllowSameOrigin");

            app.UseAuthentication();
            app.UseAuthorization();

            app.MapControllerRoute(
                name: "default",
                pattern: "{controller=Home}/{action=Index}/{id?}"
            );

            app.Run();
        }
    }
}
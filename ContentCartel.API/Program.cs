using ContentCartel.API.Services;
using FirebaseAdmin;
using Google.Apis.Auth.OAuth2;
using Microsoft.AspNetCore.HttpOverrides;

namespace ContentCartel.API
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            builder.Services.AddControllers();
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddHttpClient();
            builder.Services.AddSwaggerGen();

            builder.Services.AddCors(options =>
            {
                options.AddPolicy("AllowAll", policy =>
                {
                    policy
                        .AllowAnyOrigin()
                        .AllowAnyHeader()
                        .AllowAnyMethod();
                });
            });

            // Render terminates TLS and forwards plain HTTP to the container.
            builder.Services.Configure<ForwardedHeadersOptions>(options =>
            {
                options.ForwardedHeaders =
                    ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
                options.KnownNetworks.Clear();
                options.KnownProxies.Clear();
            });

            // The credential file comes from a Render Secret File
            // (copied to /app by docker-entrypoint.sh).
            var firebaseCredentialPath = Path.Combine(
                builder.Environment.ContentRootPath,
                "firebase-service-account.json"
            );

            if (!File.Exists(firebaseCredentialPath))
            {
                throw new FileNotFoundException(
                    "Firebase credential not found at: " + firebaseCredentialPath +
                    ". On Render, add a Secret File named " +
                    "'firebase-service-account.json' to this service."
                );
            }

            FirebaseApp.Create(new AppOptions
            {
                Credential = GoogleCredential.FromFile(firebaseCredentialPath),
                ProjectId = "contentcartel-62294"
            });

            builder.Services.AddScoped<FirebaseService>();
            builder.Services.AddScoped<RecaptchaService>();

            var app = builder.Build();

            app.UseForwardedHeaders();
            app.UseCors("AllowAll");

            // Swagger on in every environment so the deployed URL shows something.
            app.UseSwagger();
            app.UseSwaggerUI();

            // Only redirect locally; on Render the proxy already handles HTTPS
            // and the container only listens on HTTP.
            if (app.Environment.IsDevelopment())
            {
                app.UseHttpsRedirection();
            }

            app.UseAuthorization();

            // Root + health endpoints (no more blank 404 at the API URL).
            app.MapGet("/", () => Results.Redirect("/swagger"));
            app.MapGet("/health", () => Results.Ok(new { status = "healthy" }));

            app.MapControllers();

            app.Run();
        }
    }
}
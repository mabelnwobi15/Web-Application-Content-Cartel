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
            builder.Services.AddSwaggerGen(options =>
            {
                // Full type names as schema ids, so two classes with the same
                // short name (e.g. PaymentRequest) can never break swagger.json.
                options.CustomSchemaIds(type => type.FullName!.Replace("+", "."));
            });

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
                options.KnownIPNetworks.Clear();
                options.KnownProxies.Clear();
            });

            // The credential file comes from a Render Secret File
            // (copied to /app by docker-entrypoint.sh). Locally, put it in the
            // project folder; FirebaseService also looks next to the DLL.
            var firebaseCredentialPath = new[]
            {
                Path.Combine(builder.Environment.ContentRootPath, "firebase-service-account.json"),
                Path.Combine(AppContext.BaseDirectory, "firebase-service-account.json")
            }.FirstOrDefault(File.Exists);

            if (firebaseCredentialPath != null)
            {
                FirebaseApp.Create(new AppOptions
                {
                    Credential = GoogleCredential.FromFile(firebaseCredentialPath),
                    ProjectId = "contentcartel-62294"
                });
            }
            else if (builder.Environment.IsDevelopment())
            {
                // Lets Swagger load without the secret file; endpoints that
                // touch Firebase will fail until the file is added.
                Console.WriteLine(
                    "WARNING: firebase-service-account.json not found. " +
                    "Swagger will load, but Firebase-backed endpoints will fail.");
            }
            else
            {
                throw new FileNotFoundException(
                    "firebase-service-account.json not found. On Render, add a " +
                    "Secret File named 'firebase-service-account.json' to this service.");
            }

            builder.Services.AddScoped<FirebaseService>();
            builder.Services.AddScoped<RecaptchaService>();

            var app = builder.Build();

            app.UseForwardedHeaders();
            app.UseCors("AllowAll");

            // Swagger on in every environment so the deployed URL shows something.
            app.UseSwagger();
            app.UseSwaggerUI(options =>
            {
                options.SwaggerEndpoint("/swagger/v1/swagger.json", "ContentCartel API v1");
            });

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
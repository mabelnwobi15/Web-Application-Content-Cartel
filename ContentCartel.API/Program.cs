using ContentCartel.API.Services;
using FirebaseAdmin;
using Google.Apis.Auth.OAuth2;

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

            var firebaseCredentialPath = Path.Combine(
                builder.Environment.ContentRootPath,
                "firebase-service-account.json"
            );

            if (!File.Exists(firebaseCredentialPath))
            {
                throw new FileNotFoundException(
                    $"Firebase credential not found at: {firebaseCredentialPath}"
                );
            }

            FirebaseApp.Create(new AppOptions
            {
                Credential = GoogleCredential.FromFile(
                    firebaseCredentialPath
                ),
                ProjectId = "contentcartel-62294"
            });

            builder.Services.AddScoped<FirebaseService>();
            builder.Services.AddScoped<RecaptchaService>();

            var app = builder.Build();

            app.UseCors("AllowAll");

            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI();
            }

            app.UseHttpsRedirection();

            app.UseAuthorization();

            app.MapControllers();

            app.Run();
        }
    }
}
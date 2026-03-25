using Kotoba.Components;
using Kotoba.Modules.Domain.DTOs;
using Kotoba.Modules.Domain.Entities;
using Kotoba.Modules.Domain.Interfaces;
using Kotoba.Modules.Infrastructure.Data;
using Kotoba.Modules.Infrastructure.Repositories;
using Kotoba.Modules.Infrastructure.Services.Conversations;
using Kotoba.Modules.Infrastructure.Services.Attachments;
using Kotoba.Modules.Infrastructure.Services.Identity;
using Kotoba.Modules.Infrastructure.Services.Reactions;
using Kotoba.Modules.Infrastructure.Services.Social;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Kotoba.Modules.Hubs;
using Kotoba.Modules.Application.Mappings;
using AutoMapper;

namespace Kotoba
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Add services to the container.
            builder.Services.AddRazorComponents()
                .AddInteractiveServerComponents();

            builder.Services.AddHttpClient();

            builder.Services.AddSignalR(options =>
            {
                options.EnableDetailedErrors = true; 
            });


            builder.Services.AddDbContextFactory<KotobaDbContext>(options =>
                options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

            builder.Services.AddHttpContextAccessor();
            builder.Services.AddAuthentication(IdentityConstants.ApplicationScheme)
                .AddIdentityCookies();
            builder.Services.AddAuthorization();
            builder.Services.AddCascadingAuthenticationState();

            builder.Services.AddIdentityCore<User>(options =>
                {
                    options.User.RequireUniqueEmail = true;
                    options.Password.RequiredLength = 3;
                    options.Password.RequireDigit = false;
                    options.Password.RequireLowercase = false;
                    options.Password.RequireUppercase = false;
                    options.Password.RequireNonAlphanumeric = false;
                    options.Password.RequiredUniqueChars = 1;
                })
                .AddSignInManager()
                .AddEntityFrameworkStores<KotobaDbContext>()
                .AddDefaultTokenProviders();

            builder.Services.ConfigureApplicationCookie(options =>
            {
                options.LoginPath = "/login";
                options.LogoutPath = "/auth/logout";
                options.AccessDeniedPath = "/login";
            });


            builder.Services.AddAutoMapper(cfg => cfg.AddMaps(typeof(Program).Assembly));

            builder.Services.AddScoped<IReactionService, ReactionService>();
            builder.Services.AddScoped<IAttachmentService, AttachmentService>();
            builder.Services.AddScoped<IUserService, UserService>();
            builder.Services.AddSingleton<IPresenceService, PresenceService>();
            builder.Services.AddScoped<IPresenceBroadcastService, PresenceBroadcastService>();
            builder.Services.AddScoped<IConversationService, ConversationService>();
            builder.Services.AddScoped<IStoryRepository, StoryRepository>();
            builder.Services.AddScoped<ConversationParticipantRepository>();
            builder.Services.AddScoped<ConversationRepository>();
            builder.Services.AddScoped<UserProfileRepository>();
            builder.Services.AddScoped<MessageRepository>();
            builder.Services.AddScoped<IStoryService, StoryService>();
            builder.Services.AddScoped<ICurrentThoughtService, CurrentThoughtService>();

            var app = builder.Build();

            using (var scope = app.Services.CreateScope())
            {
                var dbContext = scope.ServiceProvider.GetRequiredService<KotobaDbContext>();
                dbContext.Database.Migrate();
            }

            // Configure the HTTP request pipeline.
            if (!app.Environment.IsDevelopment())
            {
                app.UseExceptionHandler("/Error");
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                app.UseHsts();
            }

            app.UseHttpsRedirection();

            app.UseStaticFiles();

            var uploadsPath = builder.Configuration["Attachments:UploadPath"] ?? "uploads";
            if (!Directory.Exists(uploadsPath))
                Directory.CreateDirectory(uploadsPath);

            app.UseStaticFiles(new StaticFileOptions
            {
                FileProvider = new Microsoft.Extensions.FileProviders.PhysicalFileProvider(
                    Path.Combine(Directory.GetCurrentDirectory(), uploadsPath)
                ),
                RequestPath = "/uploads"
            });

            app.UseAntiforgery();
            app.UseAuthentication();
            app.UseAuthorization();

            app.MapPost("/auth/register", async ([FromForm] RegisterRequest request, IUserService userService) =>
            {
                var registrationResult = await userService.RegisterAsync(request);
                if (registrationResult.Succeeded)
                {
                    return Results.LocalRedirect("/");
                }

                var errors = registrationResult.Errors.Count > 0
                    ? registrationResult.Errors
                    : new List<string> { "Registration failed. Please check your input and try again." };

                var encodedErrors = Uri.EscapeDataString(string.Join("||", errors));
                return Results.LocalRedirect($"/register?errors={encodedErrors}");
            })
            .AllowAnonymous()
            .DisableAntiforgery();

            app.MapPost("/auth/login", async ([FromForm] LoginRequest request, IUserService userService) =>
            {
                var isLoggedIn = await userService.LoginAsync(request);
                return isLoggedIn
                    ? Results.LocalRedirect("/")
                    : Results.LocalRedirect("/login?error=1");
            })
            .AllowAnonymous()
            .DisableAntiforgery();

            app.MapPost("/auth/logout", async (HttpContext httpContext) =>
            {
                await httpContext.SignOutAsync(IdentityConstants.ApplicationScheme);
                return Results.LocalRedirect("/");
            });

            app.MapRazorComponents<App>()
                .AddInteractiveServerRenderMode();


            app.MapHub<ChatHub>("/chathub");

            app.Run();
        }
    }
}

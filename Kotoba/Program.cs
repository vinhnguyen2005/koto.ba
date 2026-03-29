using AutoMapper;
using Kotoba.Components;
using Kotoba.Modules.Application.Mappings;
using Kotoba.Modules.Domain.Constants;
using Kotoba.Modules.Domain.DTOs;
using Kotoba.Modules.Domain.Entities;
using Kotoba.Modules.Domain.Enums;
using Kotoba.Modules.Domain.Interfaces;
using Kotoba.Modules.Hubs;
using Kotoba.Modules.Infrastructure.Data;
using Kotoba.Modules.Infrastructure.Repositories;
using Kotoba.Modules.Infrastructure.Services.Attachments;
using Kotoba.Modules.Infrastructure.Services.Conversations;
using Kotoba.Modules.Infrastructure.Services.Identity;
using Kotoba.Modules.Infrastructure.Services.Messages;
using Kotoba.Modules.Infrastructure.Services.Notifications;
using Kotoba.Modules.Infrastructure.Services.Reactions;
using Kotoba.Modules.Infrastructure.Services.Reports;
using Kotoba.Modules.Infrastructure.Services.Settings;
using Kotoba.Modules.Infrastructure.Services.Social;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

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
                .AddRoles<IdentityRole>()
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
            builder.Services.AddScoped<IAdminAuditService, AdminAuditService>();
            builder.Services.AddSingleton<IPresenceService, PresenceService>();
            builder.Services.AddScoped<IPresenceBroadcastService, PresenceBroadcastService>();
            builder.Services.AddScoped<IConversationService, ConversationService>();
            builder.Services.AddScoped<IStoryRepository, StoryRepository>();
            builder.Services.AddScoped<ConversationParticipantRepository>();
            builder.Services.AddScoped<ConversationRepository>();
            builder.Services.AddScoped<UserProfileRepository>();
            builder.Services.AddScoped<MessageRepository>();
            builder.Services.AddScoped<IStoryService, StoryService>();
            builder.Services.AddScoped<ICurrentThoughtRepository, CurrentThoughtRepository>();
            builder.Services.AddScoped<ICurrentThoughtService, CurrentThoughtService>();
            builder.Services.AddScoped<INotificationSettingsService, NotificationSettingsService>();
            builder.Services.AddScoped<NotificationRepository>();
            builder.Services.AddScoped<INotificationService, Modules.Infrastructure.Services.Notifications.NotificationService>();
            builder.Services.AddScoped<GlobalNotificationService>();
            builder.Services.AddScoped<CircuitCookieService>();
            builder.Services.AddSingleton<ChatNotificationState>();
            builder.Services.AddScoped<IMessageRepository, MessageRepository>();
            builder.Services.AddScoped<IMessageService, MessageService>();
            builder.Services.AddScoped<ReportRepository>();
            builder.Services.AddScoped<IReportService, ReportService>();
            builder.Services.AddScoped<IFollowService, FollowService>();
            builder.Services.AddScoped<IFollowRepository, FollowRepository>();
            builder.Services.AddScoped<IGroupAdminService, GroupAdminService>();
            builder.Services.AddSingleton<GlobalNotificationState>();

            var app = builder.Build();

            using (var scope = app.Services.CreateScope())
            {
                var dbContext = scope.ServiceProvider.GetRequiredService<KotobaDbContext>();
                dbContext.Database.Migrate();
            }

            SeedRootAdminAsync(app.Services, app.Configuration).GetAwaiter().GetResult();
            ClearStaleOnlinePresenceAsync(app.Services).GetAwaiter().GetResult();

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

            app.Use(async (context, next) =>
            {
                var user = context.User;
                if (user?.Identity?.IsAuthenticated == true && IsAnyAdmin(user) && IsNormalUserRoute(context.Request.Path))
                {
                    context.Response.Redirect(GetAdminDashboardPath(user));
                    return;
                }

                await next();
            });

            app.Use(async (context, next) =>
            {
                // Only capture for the Blazor hub negotiate or page requests
                var cookieSvc = context.RequestServices.GetRequiredService<CircuitCookieService>();
                cookieSvc.CookieHeader = context.Request.Headers["Cookie"].ToString();
                await next();
            });

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
                var accountStatus = await userService.GetAccountStatusByEmailAsync(request.Email);
                if (accountStatus == AccountStatus.Banned)
                {
                    var reason = await userService.GetLatestBanReasonByEmailAsync(request.Email);
                    var encodedReason = string.IsNullOrWhiteSpace(reason)
                        ? string.Empty
                        : $"&reason={Uri.EscapeDataString(reason)}";
                    return Results.LocalRedirect($"/login?banned=1{encodedReason}");
                }

                var isLoggedIn = await userService.LoginAsync(request);
                return isLoggedIn
                    ? Results.LocalRedirect("/")
                    : Results.LocalRedirect("/login?error=1");
            })
            .AllowAnonymous()
            .DisableAntiforgery();

            app.MapPost("/auth/admin/login", async ([FromForm] LoginRequest request, IUserService userService) =>
            {
                var redirectPath = await userService.LoginAdminAsync(request);
                return !string.IsNullOrWhiteSpace(redirectPath)
                    ? Results.LocalRedirect(redirectPath)
                    : Results.LocalRedirect("/admin/login?error=1");
            })
            .AllowAnonymous()
            .DisableAntiforgery();

            app.MapGet("/admin", (HttpContext httpContext) =>
            {
                var user = httpContext.User;
                if (user?.Identity?.IsAuthenticated != true)
                {
                    return Results.LocalRedirect("/admin/login");
                }

                if (IsAnyAdmin(user))
                {
                    return Results.LocalRedirect(GetAdminDashboardPath(user));
                }

                return Results.LocalRedirect("/admin/login");
            });

            app.MapPost("/admin/system/admins/create", async (
                [FromForm] CreateBusinessAdminRequest request,
                IUserService userService,
                HttpContext httpContext) =>
            {
                var performedByAdminId = httpContext.User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (string.IsNullOrWhiteSpace(performedByAdminId))
                {
                    return Results.Forbid();
                }

                var createResult = await userService.CreateBusinessAdminAsync(
                    request,
                    performedByAdminId,
                    httpContext.Connection.RemoteIpAddress?.ToString(),
                    httpContext.TraceIdentifier,
                    httpContext.RequestAborted);

                if (createResult.Succeeded)
                {
                    return Results.LocalRedirect("/admin/system/admins/create?created=1");
                }

                var encodedErrors = Uri.EscapeDataString(string.Join("||", createResult.Errors));
                return Results.LocalRedirect($"/admin/system/admins/create?errors={encodedErrors}");
            })
            .RequireAuthorization(new AuthorizeAttribute { Roles = AdminRoles.SystemAdmin })
            .DisableAntiforgery();

            app.MapPost("/admin/system/admins/{adminId}/disable", async (
                string adminId,
                IUserService userService,
                HttpContext httpContext) =>
            {
                var performedByAdminId = httpContext.User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (string.IsNullOrWhiteSpace(performedByAdminId))
                {
                    return Results.Forbid();
                }

                var disableResult = await userService.DisableAdminAsync(
                    adminId,
                    performedByAdminId,
                    httpContext.Connection.RemoteIpAddress?.ToString(),
                    httpContext.TraceIdentifier,
                    httpContext.RequestAborted);

                if (disableResult.Succeeded)
                {
                    return Results.Json(new { success = true });
                }

                return Results.Json(new { success = false, errors = disableResult.Errors }, statusCode: 400);
            })
            .RequireAuthorization(new AuthorizeAttribute { Roles = AdminRoles.SystemAdmin })
            .WithName("DisableAdmin");

            app.MapPost("/admin/business/users/{userId}/deactivate", async (
                string userId,
                IUserService userService,
                HttpContext httpContext) =>
            {
                var performedByAdminId = httpContext.User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (string.IsNullOrWhiteSpace(performedByAdminId))
                {
                    return Results.Forbid();
                }

                var result = await userService.DeactivateUserByAdminAsync(
                    userId: userId,
                    performedByAdminId: performedByAdminId,
                    reason: null,
                    sourceIp: httpContext.Connection.RemoteIpAddress?.ToString(),
                    correlationId: httpContext.TraceIdentifier,
                    cancellationToken: httpContext.RequestAborted);

                return result.Succeeded
                    ? Results.Json(new { success = true })
                    : Results.Json(new { success = false, errors = result.Errors }, statusCode: 400);
            })
            .RequireAuthorization(new AuthorizeAttribute { Roles = AdminRoles.BusinessAdmin });

            app.MapPost("/admin/business/users/{userId}/reactivate", async (
                string userId,
                IUserService userService,
                HttpContext httpContext) =>
            {
                var performedByAdminId = httpContext.User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (string.IsNullOrWhiteSpace(performedByAdminId))
                {
                    return Results.Forbid();
                }

                var result = await userService.ReactivateUserByAdminAsync(
                    userId: userId,
                    performedByAdminId: performedByAdminId,
                    reason: null,
                    sourceIp: httpContext.Connection.RemoteIpAddress?.ToString(),
                    correlationId: httpContext.TraceIdentifier,
                    cancellationToken: httpContext.RequestAborted);

                return result.Succeeded
                    ? Results.Json(new { success = true })
                    : Results.Json(new { success = false, errors = result.Errors }, statusCode: 400);
            })
            .RequireAuthorization(new AuthorizeAttribute { Roles = AdminRoles.BusinessAdmin });

            app.MapPost("/admin/business/users/{userId}/ban", async (
                string userId,
                IUserService userService,
                HttpContext httpContext) =>
            {
                var performedByAdminId = httpContext.User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (string.IsNullOrWhiteSpace(performedByAdminId))
                {
                    return Results.Forbid();
                }

                var result = await userService.BanUserByAdminAsync(
                    userId: userId,
                    performedByAdminId: performedByAdminId,
                    reason: null,
                    sourceIp: httpContext.Connection.RemoteIpAddress?.ToString(),
                    correlationId: httpContext.TraceIdentifier,
                    cancellationToken: httpContext.RequestAborted);

                return result.Succeeded
                    ? Results.Json(new { success = true })
                    : Results.Json(new { success = false, errors = result.Errors }, statusCode: 400);
            })
            .RequireAuthorization(new AuthorizeAttribute { Roles = AdminRoles.BusinessAdmin });

            app.MapPost("/admin/business/users/{userId}/unban", async (
                string userId,
                IUserService userService,
                HttpContext httpContext) =>
            {
                var performedByAdminId = httpContext.User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (string.IsNullOrWhiteSpace(performedByAdminId))
                {
                    return Results.Forbid();
                }

                var result = await userService.UnbanUserByAdminAsync(
                    userId: userId,
                    performedByAdminId: performedByAdminId,
                    reason: null,
                    sourceIp: httpContext.Connection.RemoteIpAddress?.ToString(),
                    correlationId: httpContext.TraceIdentifier,
                    cancellationToken: httpContext.RequestAborted);

                return result.Succeeded
                    ? Results.Json(new { success = true })
                    : Results.Json(new { success = false, errors = result.Errors }, statusCode: 400);
            })
            .RequireAuthorization(new AuthorizeAttribute { Roles = AdminRoles.BusinessAdmin });

            app.MapPost("/auth/logout", async (HttpContext httpContext) =>
            {
                await httpContext.SignOutAsync(IdentityConstants.ApplicationScheme);
                return Results.LocalRedirect("/");
            })
            .DisableAntiforgery();

            app.MapPost("/auth/admin/logout", async (HttpContext httpContext) =>
            {
                await httpContext.SignOutAsync(IdentityConstants.ApplicationScheme);
                return Results.LocalRedirect("/admin/login");
            })
            .RequireAuthorization(new AuthorizeAttribute { Roles = AdminRoles.AnyAdmin })
            .DisableAntiforgery();

            app.MapGet("/auth/logout", async (HttpContext httpContext, [FromQuery] string? returnUrl) =>
            {
                await httpContext.SignOutAsync(IdentityConstants.ApplicationScheme);
                var target = string.IsNullOrWhiteSpace(returnUrl) ? "/" : returnUrl;
                return Results.LocalRedirect(target);
            });

            app.MapGet("/auth/admin/logout", async (HttpContext httpContext) =>
            {
                await httpContext.SignOutAsync(IdentityConstants.ApplicationScheme);
                return Results.LocalRedirect("/admin/login");
            })
            .RequireAuthorization(new AuthorizeAttribute { Roles = AdminRoles.AnyAdmin });

            app.MapRazorComponents<App>()
                .AddInteractiveServerRenderMode();

            app.MapHub<Kotoba.Modules.Hubs.ChatHub>("/chathub");
            app.MapHub<NotificationHub>("/notificationhub");

            app.Run();
        }

        private static async Task SeedRootAdminAsync(IServiceProvider services, IConfiguration configuration)
        {
            using var scope = services.CreateScope();
            var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<User>>();
            var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();

            var rootSection = configuration.GetSection("RootAdmin");
            if (!rootSection.Exists())
            {
                logger.LogInformation("RootAdmin section is missing. Root admin seeding skipped.");
                return;
            }

            var email = rootSection["Email"]?.Trim();
            var password = rootSection["Password"];
            var displayName = rootSection["DisplayName"]?.Trim();

            if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
            {
                logger.LogWarning("RootAdmin.Email or RootAdmin.Password is missing. Root admin seeding skipped.");
                return;
            }

            if (!await roleManager.RoleExistsAsync(AdminRoles.SystemAdmin))
            {
                var createRoleResult = await roleManager.CreateAsync(new IdentityRole(AdminRoles.SystemAdmin));
                if (!createRoleResult.Succeeded)
                {
                    logger.LogError("Failed to create {Role}: {Errors}",
                        AdminRoles.SystemAdmin,
                        string.Join("; ", createRoleResult.Errors.Select(e => e.Description)));
                    return;
                }
            }

            if (!await roleManager.RoleExistsAsync(AdminRoles.BusinessAdmin))
            {
                var createBusinessRoleResult = await roleManager.CreateAsync(new IdentityRole(AdminRoles.BusinessAdmin));
                if (!createBusinessRoleResult.Succeeded)
                {
                    logger.LogError("Failed to create {Role}: {Errors}",
                        AdminRoles.BusinessAdmin,
                        string.Join("; ", createBusinessRoleResult.Errors.Select(e => e.Description)));
                    return;
                }
            }

            var rootUser = await userManager.FindByEmailAsync(email);
            if (rootUser is null)
            {
                rootUser = new User
                {
                    UserName = email,
                    Email = email,
                    DisplayName = string.IsNullOrWhiteSpace(displayName) ? "System Root" : displayName,
                    AccountStatus = AccountStatus.Active,
                    CreatedAt = DateTime.UtcNow,
                    IsOnline = false,
                };

                var createUserResult = await userManager.CreateAsync(rootUser, password);
                if (!createUserResult.Succeeded)
                {
                    logger.LogError("Failed to create root admin user {Email}: {Errors}",
                        email,
                        string.Join("; ", createUserResult.Errors.Select(e => e.Description)));
                    return;
                }
            }

            if (!await userManager.IsInRoleAsync(rootUser, AdminRoles.SystemAdmin))
            {
                var addToRoleResult = await userManager.AddToRoleAsync(rootUser, AdminRoles.SystemAdmin);
                if (!addToRoleResult.Succeeded)
                {
                    logger.LogError("Failed to assign {Role} to {Email}: {Errors}",
                        AdminRoles.SystemAdmin,
                        email,
                        string.Join("; ", addToRoleResult.Errors.Select(e => e.Description)));
                    return;
                }
            }

            var systemAdmins = await userManager.GetUsersInRoleAsync(AdminRoles.SystemAdmin);
            if (systemAdmins.Count > 1)
            {
                logger.LogWarning("Multiple SystemAdmin accounts detected ({Count}). Root account is expected to be singular.", systemAdmins.Count);
            }

            logger.LogInformation("Root admin is ready: {Email}", email);
        }

        private static async Task ClearStaleOnlinePresenceAsync(IServiceProvider services)
        {
            using var scope = services.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<KotobaDbContext>();
            var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();

            var usersMarkedOnline = await dbContext.Users
                .Where(user => user.IsOnline)
                .ToListAsync();

            if (usersMarkedOnline.Count == 0)
            {
                return;
            }

            var now = DateTime.UtcNow;
            foreach (var user in usersMarkedOnline)
            {
                user.IsOnline = false;
                user.LastSeenAt ??= now;
            }

            await dbContext.SaveChangesAsync();
            logger.LogInformation("Presence startup reconciliation marked {Count} users offline.", usersMarkedOnline.Count);
        }

        private static bool IsAnyAdmin(ClaimsPrincipal user)
        {
            return user.IsInRole(AdminRoles.SystemAdmin) || user.IsInRole(AdminRoles.BusinessAdmin);
        }

        private static string GetAdminDashboardPath(ClaimsPrincipal user)
        {
            if (user.IsInRole(AdminRoles.SystemAdmin))
            {
                return "/admin/system/dashboard";
            }

            if (user.IsInRole(AdminRoles.BusinessAdmin))
            {
                return "/admin/business/dashboard";
            }

            return "/admin/login";
        }

        private static bool IsNormalUserRoute(PathString path)
        {
            if (path == "/" || path == "/login")
            {
                return true;
            }

            return path.StartsWithSegments("/chat")
                || path.StartsWithSegments("/profile")
                || path.StartsWithSegments("/settings")
                || path.StartsWithSegments("/story")
                || path.StartsWithSegments("/notifications");
        }
    }
}

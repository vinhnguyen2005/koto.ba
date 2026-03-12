using Kotoba.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using Kotoba.Infrastructure.Data;
using Kotoba.Core.Interfaces;
using Kotoba.Infrastructure.Services.Conversations;
using Kotoba.Infrastructure.Services.Messages;
using Kotoba.Infrastructure.Services.Identity;
using Kotoba.Infrastructure.Services.Social;
using Kotoba.Infrastructure.Configuration;
using Kotoba.Server.Hubs;
using Kotoba.Domain.Interfaces;
using Kotoba.Infrastructure.Repositories;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddControllersWithViews();
builder.Services.AddRazorPages();

builder.Services.AddCors(options =>
{
    options.AddPolicy("ClientCors", policy =>
        policy.WithOrigins("https://localhost:5001", "http://localhost:5001")
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials());
});
// Database Context
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("DefaultConnection")));

// ASP.NET Core Identity
builder.Services.AddIdentity<User, IdentityRole>(options =>
{
    options.SignIn.RequireConfirmedAccount = false;
    options.Password.RequireDigit = true;
    options.Password.RequiredLength = 6;
    options.Password.RequireNonAlphanumeric = false;
    options.Password.RequireUppercase = false;
    options.Password.RequireLowercase = false;
})
.AddEntityFrameworkStores<ApplicationDbContext>()
.AddDefaultTokenProviders();

// Repositories
builder.Services.AddScoped<IConversationRepository, ConversationRepository>();
builder.Services.AddScoped<IConversationParticipantRepository, ConversationParticipantRepository>();
builder.Services.AddScoped<IMessageRepository, MessageRepository>();

// Services
builder.Services.AddScoped<IConversationService, ConversationService>();
builder.Services.AddScoped<IMessageService, MessageService>();
builder.Services.AddScoped<IPresenceService, PresenceService>();
builder.Services.AddScoped<IPresenceBroadcastService, PresenceBroadcastService>();
builder.Services.AddScoped<IUserService, UserService>();

// AI Services
builder.Services.Configure<GoogleGeminiOptions>(
    builder.Configuration.GetSection(GoogleGeminiOptions.SectionName));
builder.Services.AddScoped<IAIReplyService, AIReplyService>();
builder.Services.AddHttpClient(); // Required for AI service HTTP calls

// SignalR
builder.Services.AddSignalR();

// Memory Cache (for presence tracking, typing indicators)
builder.Services.AddMemoryCache();

// Swagger/OpenAPI
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Configure cookie authentication
builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/Account/Login";
    options.LogoutPath = "/Account/Logout";
    options.AccessDeniedPath = "/Account/AccessDenied";
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
} else {
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseBlazorFrameworkFiles();  // ← THÊM: serve file WASM từ Client
app.UseStaticFiles();           // ← THÊM: serve wwwroot
app.UseRouting();
app.UseCors("ClientCors");

// Authentication & Authorization
app.UseAuthentication();
app.UseAuthorization();
app.MapRazorPages();
app.MapControllers();

app.MapHub<ChatHub>("/chathub");
app.MapFallbackToFile("index.html");
// Health check endpoint
app.MapGet("/health", () => Results.Ok(new { status = "healthy" }));

// Root redirect to Swagger
app.MapGet("/", context =>
{
    context.Response.Redirect("/swagger", permanent: false);
    return Task.CompletedTask;
});

app.Run();

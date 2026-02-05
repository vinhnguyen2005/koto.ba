# koto.ba - Local Development Setup

## Prerequisites

Before you begin, ensure you have the following installed:

- ✅ **.NET 8 SDK** - [Download here](https://dotnet.microsoft.com/download/dotnet/8.0)
- ✅ **SQL Server** - Local instance or access to WORKBOX server
- ✅ **Git** - [Download here](https://git-scm.com/downloads)
- ✅ **Visual Studio 2022** or **VS Code** (optional but recommended)

---

## Getting Started

### 1. Clone the Repository

```powershell
git clone <repository-url>
cd koto.ba
```

### 2. Configure Database Connection

Open `src/Kotoba.Web/appsettings.json` and update the connection string:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=YOUR_SERVER_NAME;Database=KotobaDb;User Id=sa;Password=YOUR_PASSWORD;TrustServerCertificate=true;MultipleActiveResultSets=true"
  }
}
```

**Examples:**
- **Local SQL Server:** `Server=localhost;Database=KotobaDb;...`
- **Named Instance:** `Server=WORKBOX;Database=KotobaDb;...`
- **LocalDB:** `Server=(localdb)\\mssqllocaldb;Database=KotobaDb;Trusted_Connection=true;...`

**⚠️ Security Note:** Never commit your actual password to Git. Use User Secrets (see below).

### 3. Restore NuGet Packages

```powershell
dotnet restore
```

### 4. Apply Database Migrations

This will create the database and all necessary tables:

```powershell
dotnet ef database update --project src/Kotoba.Infrastructure/Kotoba.Infrastructure.csproj --startup-project src/Kotoba.Web/Kotoba.Web.csproj
```

### 5. Build the Solution

```powershell
dotnet build
```

### 6. Run the Application

```powershell
dotnet run --project src/Kotoba.Web/Kotoba.Web.csproj
```

The application will be available at:
- **HTTP:** `http://localhost:5025` (or port shown in terminal)
- **HTTPS:** `https://localhost:7xxx` (if configured)

---

## Using User Secrets (Recommended for Security)

Instead of storing passwords in `appsettings.json`, use User Secrets:

### Initialize User Secrets

```powershell
dotnet user-secrets init --project src/Kotoba.Web/Kotoba.Web.csproj
```

### Set Connection String

```powershell
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Server=YOUR_SERVER;Database=KotobaDb;User Id=sa;Password=YOUR_PASSWORD;TrustServerCertificate=true;MultipleActiveResultSets=true" --project src/Kotoba.Web/Kotoba.Web.csproj
```

Now you can safely commit `appsettings.json` without exposing credentials.

---

## Development Workflow

### Creating Your Feature Branch

```powershell
# Pull latest changes
git checkout develop
git pull origin develop

# Create your feature branch
git checkout -b feature/your-subsystem-name
```

**Branch naming conventions:**
- `feature/identity-subsystem` - Identity & User Management
- `feature/chat-subsystem` - Chat & Messaging
- `feature/reactions-subsystem` - Reactions & Attachments
- `feature/ai-social-subsystem` - AI & Social Features

### Daily Development Routine

**1. Start your day - sync with develop:**
```powershell
git checkout develop
git pull origin develop
git checkout feature/your-subsystem
git merge develop
```

**2. Make your changes**

**3. Test your changes:**
```powershell
dotnet build
dotnet run --project src/Kotoba.Web/Kotoba.Web.csproj
```

**4. Commit and push:**
```powershell
git add .
git commit -m "Description of your changes"
git push origin feature/your-subsystem
```

**5. Create Pull Request:**
- PR from `feature/your-subsystem` → `develop`
- Request review from at least 1 team member
- Wait for approval before merging

---

## Working with Database Migrations

### Creating a New Migration (After Adding/Modifying Entities)

```powershell
dotnet ef migrations add YourMigrationName --project src/Kotoba.Infrastructure/Kotoba.Infrastructure.csproj --startup-project src/Kotoba.Web/Kotoba.Web.csproj
```

### Applying Migrations

```powershell
dotnet ef database update --project src/Kotoba.Infrastructure/Kotoba.Infrastructure.csproj --startup-project src/Kotoba.Web/Kotoba.Web.csproj
```

### Viewing Migration History

```powershell
dotnet ef migrations list --project src/Kotoba.Infrastructure/Kotoba.Infrastructure.csproj --startup-project src/Kotoba.Web/Kotoba.Web.csproj
```

### Rolling Back Last Migration (if not pushed)

```powershell
dotnet ef migrations remove --project src/Kotoba.Infrastructure/Kotoba.Infrastructure.csproj --startup-project src/Kotoba.Web/Kotoba.Web.csproj
```

---

## Project Structure

```
koto.ba/
├── src/
│   ├── Kotoba.Web/                    (Blazor Server UI - Your entry point)
│   ├── Kotoba.Application/            (Service interfaces & DTOs)
│   ├── Kotoba.Domain/                 (Entities, Enums, Events)
│   ├── Kotoba.Infrastructure/         (EF Core, SignalR, Identity)
│   └── Kotoba.Infrastructure.AI/      (AI service implementation)
├── tests/
└── Kotoba.sln
```

### Where to Work Based on Your Subsystem

| Subsystem | Your Primary Folders |
|-----------|---------------------|
| **Identity & User** | `Infrastructure/Identity/`<br>`Application/Interfaces/IUserService.cs`<br>`Domain/Entities/User.cs` |
| **Chat & Messaging** | `Infrastructure/Chat/`<br>`Application/Interfaces/IMessageService.cs`<br>`Domain/Entities/Message.cs` |
| **Reactions & Attachments** | `Infrastructure/Reactions/`<br>`Application/Interfaces/IReactionService.cs`<br>`Domain/Entities/Reaction.cs` |
| **AI & Social** | `Infrastructure.AI/`<br>`Application/Interfaces/IAIReplyService.cs` |

---

## Common Commands

### Build & Run

```powershell
# Build entire solution
dotnet build

# Run the web application
dotnet run --project src/Kotoba.Web/Kotoba.Web.csproj

# Clean build artifacts
dotnet clean

# Restore packages
dotnet restore
```

### Git Commands

```powershell
# Check current branch and status
git status

# View all branches
git branch -a

# Switch branches
git checkout branch-name

# Pull latest from develop
git pull origin develop

# Push your changes
git push origin your-branch-name

# View commit history
git log --oneline
```

---

## Troubleshooting

### ❌ Cannot connect to database

**Check:**
1. Is SQL Server running?
2. Is the connection string correct in `appsettings.json`?
3. Can you connect using SQL Server Management Studio?
4. Is the database `KotobaDb` created? (If not, run `dotnet ef database update`)

**Solution:**
```powershell
# Try recreating the database
dotnet ef database drop --project src/Kotoba.Infrastructure/Kotoba.Infrastructure.csproj --startup-project src/Kotoba.Web/Kotoba.Web.csproj
dotnet ef database update --project src/Kotoba.Infrastructure/Kotoba.Infrastructure.csproj --startup-project src/Kotoba.Web/Kotoba.Web.csproj
```

### ❌ Build errors about missing packages

**Solution:**
```powershell
dotnet restore
dotnet clean
dotnet build
```

### ❌ Migration errors

**Error:** "The migration has already been applied"

**Solution:**
```powershell
# Remove the migration (if you haven't pushed it)
dotnet ef migrations remove --project src/Kotoba.Infrastructure/Kotoba.Infrastructure.csproj --startup-project src/Kotoba.Web/Kotoba.Web.csproj
```

**Error:** "Unable to create an object of type 'ApplicationDbContext'"

**Solution:**
- Ensure connection string is correct in `appsettings.json`
- Verify you're running commands from the solution root directory

### ❌ Port already in use

**Error:** "Failed to bind to address http://localhost:5025"

**Solution:**
```powershell
# Kill the process using the port (Windows)
netstat -ano | findstr :5025
taskkill /PID <process_id> /F

# Or just change the port in launchSettings.json
```

---

## Team Collaboration Rules

### ✅ DO:
- Work only in your assigned subsystem folders
- Create feature branches from `develop`
- Commit frequently with clear messages
- Pull from `develop` daily to stay updated
- Request code reviews before merging
- Test your changes before pushing

### ❌ DON'T:
- Edit another subsystem's implementation files
- Change service interfaces without team approval
- Commit directly to `develop` or `main`
- Push database passwords to Git
- Merge without approval

### Interface Changes (CRITICAL)

**Before changing ANY interface:**
1. Discuss with the entire team
2. Create a separate branch: `interface/description-of-change`
3. Get approval from ALL subsystem owners
4. Merge interface changes FIRST
5. Then each team member updates their implementation

---

## Tech Stack Reference

- **Backend:** ASP.NET Core (.NET 8), MVC, SignalR, EF Core, Identity
- **Frontend:** Blazor Server, Razor Components
- **Database:** SQL Server
- **Real-time:** SignalR (WebSockets)
- **AI:** External API integration via HttpClient
- **Caching:** IMemoryCache

---

## Getting Help

### Documentation
- **.NET 8:** https://learn.microsoft.com/en-us/aspnet/core/
- **Entity Framework Core:** https://learn.microsoft.com/en-us/ef/core/
- **Blazor:** https://learn.microsoft.com/en-us/aspnet/core/blazor/
- **SignalR:** https://learn.microsoft.com/en-us/aspnet/core/signalr/

### Team Communication
- **Questions?** Ask in the team chat
- **Bugs?** Create an issue in the repository
- **Conflicts?** Reach out to your team lead

---

## Quick Start Checklist

- [ ] Clone the repository
- [ ] Install .NET 8 SDK
- [ ] Configure database connection in `appsettings.json` or User Secrets
- [ ] Run `dotnet restore`
- [ ] Run `dotnet ef database update` (to create database)
- [ ] Run `dotnet build` (verify everything compiles)
- [ ] Run `dotnet run --project src/Kotoba.Web/Kotoba.Web.csproj`
- [ ] Open browser to `http://localhost:5025`
- [ ] Create your feature branch
- [ ] Start coding! 🚀

---

**"Happy" Coding!** ☠️☠️

# Implementation Guide for koto.ba Team

## Folder Structure for Team Implementation

Each team member should place their service implementations in designated folders under `src/Kotoba.Infrastructure/Services/`:

### Service Folders (with .gitkeep placeholders ready)

```
Kotoba.Infrastructure/Services/
├── Identity/                 (Dũng — User Management)
│   ├── UserService.cs               ✓ Complete
│   └── PresenceService.cs           ✓ Complete
│
├── Conversations/            (Nga — Conversation Management)
│   └── ConversationService.cs       ✓ Started
│
├── Messages/                 (Vinh — Message Persistence)
│   └── MessageService.cs            ✓ Complete
│
├── Reactions/                (Vinh — Reactions)
│   ├── .gitkeep                      Ready for implementation
│   ├── ReactionService.cs            (Implement IReactionService)
│   └── [optional] ReactionRepository.cs
│
├── Attachments/              (Vinh — File Management)
│   ├── .gitkeep                      Ready for implementation
│   ├── AttachmentService.cs          (Implement IAttachmentService)
│   └── [optional] FileStorageHelper.cs
│
├── Realtime/                 (Nga — SignalR Support)
│   ├── .gitkeep                      Ready for implementation
│   └── RealtimeChatService.cs        (Implement IRealtimeChatService)
│
└── Social/                   (Hoàn — AI & Social Features)
    ├── .gitkeep                      Ready for implementation
    ├── AIReplyService.cs             (Implement IAIReplyService)
    ├── StoryService.cs               (Implement IStoryService)
    └── CurrentThoughtService.cs      (Implement ICurrentThoughtService)
```

### Repository Pattern (Optional Data Access Layer)

```
Kotoba.Infrastructure/Repositories/
├── .gitkeep
├── [optional] UserRepository.cs
├── [optional] ConversationRepository.cs
├── [optional] MessageRepository.cs
└── [optional] IRepository.cs (generic interface)
```

---

## Implementation Checklist

### When Starting a New Service

1. **Check the Interface Contract**
   ```
   Kotoba.Core/Interfaces/IYourService.cs
   ```
   - Review all method signatures
   - Understand required DTOs and return types

2. **Create Implementation File**
   ```
   Kotoba.Infrastructure/Services/{Subsystem}/YourService.cs
   ```
   - Inherit: `public class YourService : IYourService`
   - Use constructor injection for dependencies:
     ```csharp
     private readonly ApplicationDbContext _context;
     private readonly ILogger<YourService> _logger;

     public YourService(ApplicationDbContext context, ILogger<YourService> logger)
     {
         _context = context;
         _logger = logger;
     }
     ```

3. **Reference Correct Namespaces**
   ```csharp
   using Kotoba.Core.Interfaces;          // Interface reference
   using Kotoba.Domain.Entities;          // Domain models
   using Kotoba.Domain.DTOs;              // Data contracts
   using Kotoba.Domain.Enums;             // Enumerations
   using Kotoba.Infrastructure.Data;      // DbContext
   ```

4. **Register Service in Program.cs**
   ```csharp
   // src/Kotoba.Server/Program.cs
   builder.Services.AddScoped<IYourService, YourService>();
   ```

5. **Add to Project References (if needed)**
   - Services only reference: Core, Domain, Infrastructure.Data
   - Do NOT reference Server, Client, or other service folders

---

## Dependency Injection Pattern

### Constructor Injection (Recommended)

```csharp
public class MyService : IMyService
{
    private readonly ApplicationDbContext _context;
    private readonly IUserService _userService;

    public MyService(ApplicationDbContext context, IUserService userService)
    {
        _context = context;
        _userService = userService;
    }

    public async Task<MyDto> GetDataAsync(string userId)
    {
        // Use injected dependencies
        var user = await _userService.GetUserProfileAsync(userId);
        // ...
    }
}
```

### Do NOT Create New DbContext Instances

❌ **Wrong:**
```csharp
var context = new ApplicationDbContext();
```

✓ **Right:**
```csharp
// Constructor injection
public MyService(ApplicationDbContext context) { ... }
```

---

## Testing Your Implementation

### Before Pushing

```powershell
# 1. Build entire solution
dotnet build

# 2. Run server to test DI registration
dotnet run --project src/Kotoba.Server/Kotoba.Server.csproj

# 3. Create feature branch and test
git checkout -b feature/your-subsystem-implement
git add .
git commit -m "feat: Implement YourService"
git push origin feature/your-subsystem-implement
```

### Code Review Points

- [ ] All interface methods implemented
- [ ] No compilation errors
- [ ] Proper error handling (try-catch or Result pattern)
- [ ] Async/await used for I/O operations
- [ ] DTOs used for all cross-layer boundaries
- [ ] Registered in Program.cs
- [ ] Documentation/XML comments added

---

## Subsystem Ownership & Communication

| Team Member | Owns | Depends On | Contact |
|-------------|------|-----------|---------|
| **Dũng** | Identity, User Mgmt | None | - |
| **Nga** | Conversations, Realtime | Identity | - |
| **Vinh** | Messages, Reactions, Attachments | Identity, Conversations | - |
| **Hoàn** | AI/Social, Blazor UI | All backend services | - |

**Important:** Changes to interface signatures require team discussion before implementation.

---

## Common Gotchas

### 1. Forgetting async/await
```csharp
❌ var users = _context.Users.ToList();  // Blocking
✓ var users = await _context.Users.ToListAsync();  // Non-blocking
```

### 2. Not closing database connections
```csharp
✓ Use DbContext injection - it's disposed automatically
❌ Don't create new DbContext instances
```

### 3. Mixing DTOs and Entities
```csharp
❌ public async Task<User> GetUserAsync(string userId) { ... }
✓ public async Task<UserProfile> GetUserProfileAsync(string userId) { ... }
```

### 4. Circular dependencies
```csharp
❌ IUserService → IConversationService → IUserService
✓ Keep dependencies unidirectional
```

---

## Questions?

1. **About interfaces**: Check `Kotoba.Core/Interfaces/`
2. **About entities**: Check `Kotoba.Domain/Entities/`
3. **About DTOs**: Check `Kotoba.Domain/DTOs/` and `Kotoba.Shared/DTOs/`
4. **About implementation patterns**: See existing services in `Services/{Subsystem}/`

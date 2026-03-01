# koto.ba
**Real-time Social Chat Application with AI Reply Suggestions**

---

## I. Technology Stack

### Backend
- ASP.NET Core (.NET)
- MVC / Minimal API
- SignalR
- Entity Framework Core
- ASP.NET Core Identity
- IMemoryCache

### Frontend
- Blazor WebAssembly (WASM)
- Razor Components
- SignalR client

### Database
- SQL Server

### Realtime
- SignalR (WebSocket + fallback)

### AI
- Server-side integration with AI content generation API

### Attachments
- File upload (images, PDF)
- Local file storage

### Security
- ASP.NET Core Identity
- HTTPS

---

## II. Core Features

### Chat & Messaging
- Real-time 1–1 chat
- Group chat
- Message history persistence
- Typing indicator
- Online / offline presence detection

### Message Interaction
- Message reactions (1 reaction per user per message)
- Image and document upload with preview

### Social Features
- Simple Stories (text/image, auto-delete after 24 hours)
- **Current Thought**: one short status line per user, auto-expiring

### AI Assistance
- Optional AI reply suggestions
- Multiple tones (Polite, Friendly, Confident)
- AI suggestions are not auto-sent

### Performance
- In-memory cache for frequently used data
- Async, non-blocking processing

### Explicitly Out of Scope
- Voice / video calls
- Auto-reply bots
- End-to-end encryption
- Native mobile apps

### MVP Scope
Web application with:
- Authentication
- Real-time 1–1 & group chat
- Message persistence
- Image upload
- AI reply suggestions

---

## III. System Architecture & Overall Design

The system is divided into independent subsystems, each responsible for a clearly defined set of features. Subsystems communicate via:

- Shared interfaces
- Shared DTOs
- Realtime events (SignalR)

Tight coupling is avoided to enable parallel development.

### Project Layers

```
┌───────────────────────────────────────────────────────┐
│  Kotoba.Client        (Blazor WebAssembly UI)         │
│  Kotoba.Server        (Web API + SignalR entry point) │
├───────────────────────────────────────────────────────┤
│  Kotoba.Infrastructure (Service implementations,      │
│                         EF Core, Identity, SignalR)   │
├───────────────────────────────────────────────────────┤
│  Kotoba.Core          (Service interfaces only)       │
├───────────────────────────────────────────────────────┤
│  Kotoba.Domain        (Entities, Enums, DTOs)         │
└───────────────────────────────────────────────────────┘
```

**Kotoba.Shared** holds DTOs used by both client and server (independent of the above layers).

Dependency rules:
- `Kotoba.Core` → `Kotoba.Domain` only
- `Kotoba.Infrastructure` → `Kotoba.Core` + `Kotoba.Domain`
- `Kotoba.Server` → `Kotoba.Core` + `Kotoba.Domain` + `Kotoba.Infrastructure`
- No layer may reference a layer above it

### Design Goals
- Easy task assignment
- Reduced merge conflicts
- Clear individual contribution evaluation
- Clean, extensible architecture

---

## IV. Functional Subsystems

---

### 1. Identity & User Management

#### Responsibilities
- User registration and login
- Account and profile management
- Online / offline presence tracking

#### Includes
- ASP.NET Core Identity
- User profile (DisplayName, Avatar)
- Realtime presence status

#### Technologies
- ASP.NET Core Identity
- Entity Framework Core
- IMemoryCache
- SignalR (presence updates)

#### Dependencies
- Shared domain models

#### Independent From
- Chat
- AI
- Attachments

---

### 2. Messaging Core (Realtime Chat)

#### Responsibilities
- Realtime messaging
- 1–1 and group conversations
- Message persistence
- Typing indicator

#### Technologies
- SignalR (ChatHub)
- Entity Framework Core
- SQL Server / SQLite

#### Depends On
- Identity (UserId)

---

### 2.1 Conversation Management

#### Responsibilities
- Manage 1–1 conversations
- Manage group conversations
- Conversation membership

#### Includes
- Direct chat
- Group chat
- Conversation / Room
- Participant management

#### Technologies
- Entity Framework Core
- SQL Server

#### Depends On
- Identity (UserId)

#### Independent From
- Messages
- SignalR
- Typing
- AI
- Reactions
- Attachments

---

### 2.2 Message Persistence & History

#### Responsibilities
- Send text messages
- Persist messages
- Retrieve message history
- Pagination

#### Includes
- Message entity
- Message history
- Paging

#### Technologies
- Entity Framework Core
- SQL Server

#### Depends On
- Identity (UserId)
- Conversation (ConversationId)

#### Independent From
- SignalR
- Typing
- AI
- Reactions
- Attachments

---

### 2.3 Realtime Interaction & Typing (SignalR Layer)

#### Responsibilities
- Broadcast messages in realtime
- Typing indicators
- Manage realtime connections per conversation

#### Includes
- SignalR ChatHub
- Message broadcast
- Typing events

#### Technologies
- SignalR (WebSocket + fallback)

#### Depends On
- Message DTO
- ConversationId
- UserId

#### Independent From
- Entity Framework Core
- Database
- AI
- Reactions
- Attachments

---

### 3. Reactions & Attachments

#### Responsibilities
- Message reactions
- File upload and management

#### Includes
- Fixed reactions
- One reaction per user per message
- Realtime reaction updates
- Image (PNG, JPG) and PDF upload
- Preview and download links

#### Technologies
- Entity Framework Core
- SignalR (reaction updates)
- `System.IO.Stream` (file upload, no ASP.NET Core dependency in Domain)
- Local file storage

#### Depends On
- MessageId
- UserId

#### Independent From
- AI
- UI logic

---

### 4. AI & Social Features

#### Responsibilities
- AI reply suggestions
- Stories and Current Thought

#### Includes
- AI reply suggestions (manual send)
- Tone selection
- Stories (auto-expire after 24h)
- Current Thought (one per user)

#### Technologies
- HttpClient
- AI generation API (server-side)
- Entity Framework Core
- Worker Service
- IMemoryCache

#### Depends On
- Identity
- Message content

#### Independent From
- UI
- Main SignalR Hub

---

## V. UI & Integration Layer

### Responsibilities
- User interface
- Orchestrating subsystem interactions

The UI runs as a Blazor WebAssembly client and communicates with the server through REST APIs and SignalR hubs.

### Includes
- Chat UI
- Reaction UI
- Story feed
- SignalR client integration

### Technologies
- Blazor WebAssembly (WASM)
- Razor Components
- SignalR client

### Principles
- No business logic
- Only call services via interfaces

---

## VI. Interfaces & Integration Contracts

### 1. Goals
- Define clear responsibility boundaries
- Standardize data exchanged between subsystems
- Prevent inconsistent logic and data models
- Reduce coupling and enable independent grading

Subsystems may only communicate via defined interfaces.

---

### 2. General Rules
- No subsystem accesses another subsystem’s database
- No direct implementation references, only interfaces
- All IDs are globally unique
- DTOs are used for communication, not entities

---

### 3. Identity & User Management

#### Interfaces

**IUserService**
- RegisterAsync(RegisterRequest)
- LoginAsync(LoginRequest)
- GetUserProfileAsync(UserId)
- UpdateUserProfileAsync(UserId, UpdateProfileRequest)

**IPresenceService**
- SetOnline(UserId)
- SetOffline(UserId)
- GetUserPresence(UserId)

#### Data Contracts

**UserProfile**
- UserId
- DisplayName
- AvatarUrl
- IsOnline

**RegisterRequest**
- Email
- Password
- DisplayName

**LoginRequest**
- Email
- Password

**UpdateProfileRequest**
- DisplayName
- AvatarUrl

#### Guarantees
- UserId is immutable
- Presence is updated in realtime
- No chat or AI logic

---

### 4. Messaging Core

#### Interfaces

**IConversationService**
- CreateDirectConversation(UserId, UserId)
- CreateGroupConversation(CreateGroupRequest)
- GetUserConversations(UserId)
- GetConversationDetail(ConversationId)

**IMessageService**
- SendMessageAsync(SendMessageRequest)
- GetMessages(ConversationId, PagingRequest)

**ITypingService**
- SetTyping(UserId, ConversationId, IsTyping)

**IRealtimeChatService**
- BroadcastMessage(MessageDto)
- BroadcastTyping(TypingStatusDto)

> IRealtimeChatService only broadcasts events. No persistence or business logic.

---

### Data Contracts

**Conversation**
- ConversationId
- Type (Direct | Group)
- ParticipantIds[]
- CreatedAt

**Message**
- MessageId
- ConversationId
- SenderId
- Content
- CreatedAt

**SendMessageRequest**
- ConversationId
- SenderId
- Content

**MessageDto**
- MessageId
- ConversationId
- SenderId
- Content
- CreatedAt

**TypingStatusDto**
- UserId
- ConversationId
- IsTyping

---

### Realtime Events
- MessageSent
- TypingStatusChanged

---

### Scope Limitations
- Messages contain text only
- No reactions, attachments, or AI
- No cross-database access
- SignalR Hub contains no business logic

---

### 5. Reactions & Attachments

#### Interfaces

**IReactionService**
- AddOrUpdateReaction(UserId, MessageId, ReactionType)
- RemoveReaction(UserId, MessageId)
- GetReactions(MessageId)

**IAttachmentService**
- UploadAttachmentAsync(UploadAttachmentRequest)
- GetAttachments(MessageId)

#### Data Contracts

**Reaction**
- MessageId
- UserId
- ReactionType

**Attachment**
- AttachmentId
- MessageId
- FileName
- FileType
- FileUrl

**UploadAttachmentRequest**
- MessageId
- FileStream (`System.IO.Stream`)
- FileName
- ContentType

#### Constraints
- One reaction per user per message
- One message per attachment
- Local file storage only

---

### 6. AI & Social Features

#### Interfaces

**IAIReplyService**
- GenerateSuggestionAsync(AIReplyRequest)

**IStoryService**
- CreateStoryAsync(CreateStoryRequest)
- GetActiveStories()

**ICurrentThoughtService**
- SetThought(UserId, Content)
- GetThought(UserId)

#### Data Contracts

**AIReplyRequest**
- UserId
- OriginalMessage
- Tone (Polite | Friendly | Confident)

**Story**
- StoryId
- UserId
- Content
- MediaUrl
- ExpiresAt

#### Rules
- AI never auto-sends messages
- Stories auto-delete after 24 hours
- One Current Thought per user

---

### 7. Shared Contracts

#### Shared Types
- UserId
- ConversationId
- MessageId

**PagingRequest**
- Page
- PageSize

#### Forbidden Dependencies
- Identity → Chat ❌
- Chat → AI ❌
- AI → SignalR Hub ❌
- Reaction → UI ❌

---

## VII. Team Work Allocation (4 Members)

### Dũng
**Identity & User Management**
**Shared Contracts & System Integration**

- Authentication and user management
- User profile management
- System-wide ID standardization
- DTO, interface, and contract ownership
- End-to-end flow design
- System integration and testing
- Architecture consistency

---

### Nga
**Conversation Management**
**Realtime Backend Foundation (SignalR)**

- 1–1 and group conversation management
- Conversation membership
- Conversation listing
- IConversationService implementation
- SignalR ChatHub (server-side)
- Connection lifecycle handling
- SignalR group management
- Realtime broadcast foundation

---

### Vinh
**Message Persistence & History**
**Reactions & Attachments**

- Message sending and storage
- Message history and pagination
- IMessageService implementation
- Message reactions
- Attachment upload and management
- IReactionService & IAttachmentService

---

### Hoàn
**Realtime Client Integration**
**UI & AI / Social Features**

- Blazor WebAssembly UI
- SignalR client integration
- Realtime UI updates
- Typing & presence display
- AI reply suggestion integration
- Story & Current Thought features
- UI contains no business logic

---

Each member:
- Fully owns assigned subsystems
- Must not change interfaces without agreement
- Must update documentation when extending features

**See Also:** [docs/implementing-features.md](implementing-features.md) for folder structure and implementation guidelines.

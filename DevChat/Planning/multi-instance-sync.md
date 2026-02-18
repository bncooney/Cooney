# Plan: Change Log + Named Pipe IPC for Multi-Instance Sync

## Context

Two DevChat instances sharing the same SQLite database currently have no way to stay in sync. One instance's conversation list and messages go stale the moment the other writes. This plan adds a **change log table** (so mutations are observable) and **named pipe IPC** (so peers learn about changes promptly without blind polling).

---

## New Files

| File | Purpose |
|------|---------|
| `DevChat\Models\ChangeLogEntry.cs` | Entity + `ChangeType` enum |
| `DevChat\Services\ISyncService.cs` | Abstraction for IPC (injectable, mockable) |
| `DevChat\Services\NamedPipeSyncService.cs` | Named pipe server + client with peer registry and debouncing |
| `DevChat\Services\StubSyncService.cs` | No-op implementation for `--test` mode |
| `DevChat\Services\ChangeLogPoller.cs` | Polls change log table, tracks `lastSeenId`, filters out self |

## Modified Files

| File | Changes |
|------|---------|
| `DevChat\Data\ChatDbContext.cs` | Add `DbSet<ChangeLogEntry>`, configure entity in `OnModelCreating` |
| `DevChat\App.xaml.cs` | Enable WAL mode, `CREATE TABLE IF NOT EXISTS` for existing DBs, DI registration, poller init, listener startup, dispose on exit |
| `DevChat\ViewModels\ChatViewModel.cs` | Accept `ISyncService`, write change log entries in `SendAsync`, add `ApplyExternalMessageAsync()` |
| `DevChat\ViewModels\MainViewModel.cs` | Accept `ISyncService` + `ChangeLogPoller`, write change log in `DeleteConversationAsync`, add `ApplyExternalChangesAsync()` with handlers for each change type |

---

## Implementation Steps

### 1. ChangeLogEntry entity

New model in `DevChat\Models\ChangeLogEntry.cs`:

```
ChangeType enum: ConversationCreated, ConversationDeleted, ConversationTitleUpdated, MessageAdded

ChangeLogEntry:
  Id              long (auto-increment PK — simple WHERE Id > lastSeenId polling)
  ChangeType      ChangeType
  ConversationId  Guid
  MessageId       int?         (populated for MessageAdded)
  NewTitle        string?      (populated for TitleUpdated — avoids extra round-trip)
  OriginInstanceId Guid        (so each instance skips its own entries)
  CreatedAt       DateTime
```

### 2. ChatDbContext update

- Add `DbSet<ChangeLogEntry> ChangeLog`
- Configure in `OnModelCreating`: PK on `Id`, `ValueGeneratedOnAdd`
- Since the project uses `EnsureCreatedAsync()` (no-op on existing DBs), also run `CREATE TABLE IF NOT EXISTS ChangeLog (...)` raw SQL in App startup as a fallback

### 3. Enable WAL mode

In `App.xaml.cs`, after `EnsureCreatedAsync()`:
```csharp
await db.Database.ExecuteSqlRawAsync("PRAGMA journal_mode=WAL;");
```
Idempotent — SQLite just returns "wal" if already enabled.

### 4. ISyncService interface

```csharp
interface ISyncService : IAsyncDisposable
{
    Guid InstanceId { get; }
    Task NotifyPeersAsync();                    // fire-and-forget broadcast
    Task StartListeningAsync(                   // background listener
        Func<Task> onChangesAvailable,
        CancellationToken ct);
}
```

### 5. NamedPipeSyncService

**Pipe naming:** SHA256 hash of DB path (lowercased) → `DevChat_Sync_{hash16}_{instanceId}`. Each instance gets a unique pipe name.

**Peer discovery:** A shared registry file at `{dbFolder}/.sync_peers` with lines of `instanceId|pipeName|processId`. On startup, register self and prune entries where the process is no longer running.

**Server side:** Background loop — create `NamedPipeServerStream`, `WaitForConnectionAsync`, read one byte (the ping), disconnect, repeat. On ping received, reset a 150ms debounce timer. When the timer fires, marshal the callback to the dispatcher thread.

**Client side (`NotifyPeersAsync`):** Read the peer registry, connect to each peer pipe (skipping self), write one byte, disconnect. Swallow `TimeoutException`/`IOException` — fire-and-forget.

### 6. StubSyncService

No-op implementation for `--test` mode. All methods return immediately.

### 7. ChangeLogPoller

- `InitializeAsync()` — sets `lastSeenId` to current max (skip history on startup)
- `PollAsync()` — `WHERE Id > lastSeenId AND OriginInstanceId != self`, returns entries in order, advances `lastSeenId`

### 8. Write change log entries at mutation sites

**ChatViewModel.SendAsync() — user message save:**
```
using (var db = ...)
{
    if (!_persisted)
    {
        db.Conversations.Add(...);
        // entry: ConversationCreated
        // entry: ConversationTitleUpdated (with NewTitle)
    }
    db.ChatMessages.Add(userEntity);
    await db.SaveChangesAsync();  // userEntity.Id now populated

    // entry: MessageAdded (with MessageId = userEntity.Id)
    await db.SaveChangesAsync();
}
await _syncService.NotifyPeersAsync();
```

**ChatViewModel.SendAsync() — assistant message save (in finally block):**
```
db.ChatMessages.Add(assistantEntity);
await db.SaveChangesAsync();  // assistantEntity.Id populated
// entry: MessageAdded
await db.SaveChangesAsync();
await _syncService.NotifyPeersAsync();
```

**MainViewModel.DeleteConversationAsync():**
```
db.Conversations.Remove(entity);
// entry: ConversationDeleted
await db.SaveChangesAsync();
await _syncService.NotifyPeersAsync();
```

### 9. Reconciliation handlers in MainViewModel

`ApplyExternalChangesAsync()` — called by the pipe listener callback:
1. Polls `ChangeLogPoller` for new entries
2. For each entry, dispatches to a handler:

| ChangeType | Handler |
|---|---|
| `ConversationCreated` | Load entity from DB, create ViewModels, insert into `Conversations` collection (skip if already exists) |
| `ConversationDeleted` | Remove from `Conversations`, dispose ChatViewModel. If user was viewing it, navigate away |
| `ConversationTitleUpdated` | Find existing ConversationViewModel, update `Title` |
| `MessageAdded` | Find ChatViewModel, call `ApplyExternalMessageAsync(messageId)` |

`ChatViewModel.ApplyExternalMessageAsync(int messageId)`:
- If messages not yet loaded, skip (they'll load on demand)
- If message already in collection (by ID), skip
- Load entity from DB, append to `Messages`

### 10. DI wiring in App.xaml.cs

```csharp
// After IChatService registration:
if (isTestMode)
    services.AddSingleton<ISyncService, StubSyncService>();
else
    services.AddSingleton<ISyncService>(new NamedPipeSyncService(dbPath));
services.AddSingleton<ChangeLogPoller>();
```

After DB init:
```csharp
var poller = _host.Services.GetRequiredService<ChangeLogPoller>();
await poller.InitializeAsync();
// ... load conversations, show window ...
var syncService = _host.Services.GetRequiredService<ISyncService>();
await syncService.StartListeningAsync(
    () => mainVm.ApplyExternalChangesAsync(),
    appLifetime.ApplicationStopping);
```

On exit: dispose `ISyncService` before `MainViewModel`.

### 11. Change log pruning (startup)

Delete entries older than 24 hours on each startup to keep the table small.

---

## Verification

1. **Build** — `dotnet build` passes with 0 errors
2. **Existing tests** — `dotnet test` passes (DevChat.Test UI tests still green)
3. **Manual two-instance test:**
   - Launch instance A, create a conversation, send a message
   - Launch instance B — it should show the conversation
   - In instance B, send a message → instance A should show it live
   - In instance A, delete the conversation → instance B should navigate away
   - Create conversation in B → appears in A's nav pane
4. **Unit tests for ChangeLogPoller:**
   - Auto-increment ID works
   - Poll returns only entries since `lastSeenId`
   - Poll filters out entries from the same instance
   - Poll advances `lastSeenId`

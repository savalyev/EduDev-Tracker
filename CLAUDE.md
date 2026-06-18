# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project

**EduDev Tracker** is a cross-platform .NET MAUI app (targets `net10.0-android`, `net10.0-ios`, `net10.0-maccatalyst`, `net10.0-windows`) — a personal productivity tracker covering habits, tasks, notes, a Pomodoro timer, unit/data converters, and analytics. All data is local-only in SQLite; there is no backend. The UI language is Russian (code identifiers are English).

## Build & Run

The solution is a single project under `EduDev Tracker/EduDev Tracker.csproj`. Note the space in the directory name — quote paths.

```bash
# Restore + build for a single platform (fastest; avoids building all TFMs)
dotnet build "EduDev Tracker/EduDev Tracker.csproj" -f net10.0-windows10.0.19041.0
dotnet build "EduDev Tracker/EduDev Tracker.csproj" -f net10.0-android

# Run on Windows
dotnet build "EduDev Tracker/EduDev Tracker.csproj" -t:Run -f net10.0-windows10.0.19041.0
```

There is no test project, linter config, or CI in the repo — "verify" means it compiles and runs.

## Architecture

Feature-first MVVM. Each feature under `Features/<Name>/` owns its `Views/` (XAML pages) and `ViewModels/`. Shared infrastructure lives outside features:

- `Data/` — SQLite layer: `Models/` (sqlite-net entities), `Repositories/Implementations/`, `DatabaseService`, `Seed/`.
- `Services/` — business logic, one folder per domain (`Habits`, `Tasks`, `Notes`, `Pomodoro`, `Converters`, `Analytics`, `Auth`, `Navigation`, `Notification`, `Audio`, `Export`). Each typically has an `IXxxService` + `XxxService` pair.
- `Core/` — `Base/BaseViewModel`, `Converters/` (XAML value converters), `Helpers/`.

### Layering

`View` → `ViewModel` → `Service (IXxxService)` → `Repository : BaseRepository<T>` → `DatabaseService` (one shared `SQLiteAsyncConnection`). ViewModels never touch repositories directly; services wrap repositories and hold the validation/business rules. Repositories are thin SQL/CRUD.

- `BaseRepository<T>` ([Data/Repositories/Implementations/BaseRepository.cs](EduDev%20Tracker/Data/Repositories/Implementations/BaseRepository.cs)) gives `GetAllAsync`/`GetByIdAsync`/`SaveAsync`/`DeleteAsync`. `SaveAsync` decides insert-vs-update via reflection on an `Id` property (0 = insert). Concrete repos add domain queries and use SQLiteNetExtensions (`GetWithChildrenAsync`, `SaveWithChildrenAsync`) for entities with relationships.
- `BaseViewModel` ([Core/Base/BaseViewModel.cs](EduDev%20Tracker/Core/Base/BaseViewModel.cs)) is a CommunityToolkit.Mvvm `ObservableObject` with `IsBusy`/`IsNotBusy`/`Title` and a virtual `InitializeAsync`. Use `[ObservableProperty]` and `[RelayCommand]` source generators throughout.

### Dependency injection

Everything is registered in [MauiProgram.cs](EduDev%20Tracker/MauiProgram.cs). Repositories and services are **singletons**; Pages and ViewModels are **transient**. When adding a feature, register both the Page and ViewModel here. Pages receive their ViewModel via constructor injection and set `BindingContext` in the constructor (see [HabitsPage.xaml.cs](EduDev%20Tracker/Features/Habits/Views/HabitsPage.xaml.cs)).

### Navigation

Shell-based. The flyout menu (top-level destinations: Dashboard, Habits, Tasks, Notes, Pomodoro, Converters, Analytics, Profile) is declared in [AppShell.xaml](EduDev%20Tracker/AppShell.xaml). Detail/modal pages are registered as routes in [AppShell.xaml.cs](EduDev%20Tracker/AppShell.xaml.cs) via `Routing.RegisterRoute`. Navigate through `INavigationService` ([Services/Navigation/NavigationService.cs](EduDev%20Tracker/Services/Navigation/NavigationService.cs)) — `GoToAsync(route, parameters)`, `GoBackAsync`, `PushModalAsync`, and `SwitchToModuleAsync` for flyout switching — not by calling `Shell.Current` from ViewModels.

ViewModels generally load data in the Page's `OnAppearing` by executing a load `[RelayCommand]`, not in the constructor.

### Startup & sessions

[App.xaml.cs](EduDev%20Tracker/App.xaml.cs) runs `DatabaseService.InitAsync()` on launch, then checks `SessionService.GetProfileId()` ([Core/Helpers/SessionService.cs](EduDev%20Tracker/Core/Helpers/SessionService.cs), backed by MAUI `Preferences`). If no active profile → `AuthPage`; otherwise → `AppShell`. The active profile id scopes nearly all data (`ProfileId` foreign key on most queries). Auth uses `BCrypt.Net-Next` for password hashing; profiles also support a passwordless "local mode".

### Database

[DatabaseService.cs](EduDev%20Tracker/Data/DatabaseService.cs) owns the single connection and creates all tables in `InitAsync` (idempotent, guarded by a `SemaphoreSlim`). Notes have a raw-SQL FTS5 virtual table (`notes_fts`) kept in sync by triggers, and `habit_logs` is created with raw SQL for its `ON CONFLICT(HabitId, LogDate)` upsert behavior — most other tables use `CreateTableAsync<T>()`. Schema versioning is via `PRAGMA user_version` compared against `Constants.CurrentSchemaVersion`; bump it and extend `MigrateAsync` for schema changes. The DB file is `edudev-tracker.db3` in `FileSystem.AppDataDirectory`.

## Conventions

- Root namespace is `EduDev_Tracker` (underscore), but the project/folder is "EduDev Tracker" (space).
- Nullable reference types and implicit usings are enabled.
- XAML uses source-generated code-behind (`<MauiXamlInflator>SourceGen</MauiXamlInflator>`). New XAML pages need a `<MauiXaml Update=... Generator="MSBuild:Compile">` entry in the `.csproj` (existing entries show the pattern).
- User-facing strings and many code comments are in Russian — match the surrounding language when editing.
- Platform-specific UI tweaks are guarded with `#if WINDOWS` / `#if DEBUG`.

# Aunties Recipe (portfolio)

Demo juice shop inspired by a Mexican-style stand in the Aubrey, TX area. **Not affiliated** with any real business unless you add explicit permission in writing.

## Prerequisites

- [.NET 9 SDK](https://dotnet.microsoft.com/download) (or retarget projects to net8.0)

## Run

```bash
cd src/AuntiesRecipe.Web
dotnet run
```

## Architecture (Clean Architecture)

| Project | Responsibility |
|--------|----------------|
| **Domain** | Entities and domain rules only. No UI, no EF, no web. |
| **Application** | Use cases, DTOs, interfaces (`IMenuService`). Depends on Domain. |
| **Infrastructure** | EF Core, SQLite, seed data. Implements Application interfaces. |
| **Web** | Blazor host, DI composition, UI. References Application + Infrastructure. |

**Why:** Dependencies point **inward**. The database and Blazor can change without rewriting business concepts in Domain.

---

## Step-by-step (what we build and why)

### Step 1 — Solution and layers

**What:** One `.sln` and four projects: `Domain`, `Application`, `Infrastructure`, `Web`.

**Why:** *Clean Architecture* keeps the **core business model** (`Domain`) independent from **Blazor** and **SQL**. Recruiters can open `Domain` and understand your model without wading through UI.

**Commands (run on your machine with .NET SDK installed):**

```bash
mkdir AuntiesRecipe && cd AuntiesRecipe
dotnet new sln -n AuntiesRecipe

dotnet new classlib -n AuntiesRecipe.Domain -o src/AuntiesRecipe.Domain -f net9.0
dotnet new classlib -n AuntiesRecipe.Application -o src/AuntiesRecipe.Application -f net9.0
dotnet new classlib -n AuntiesRecipe.Infrastructure -o src/AuntiesRecipe.Infrastructure -f net9.0
dotnet new blazor -n AuntiesRecipe.Web -o src/AuntiesRecipe.Web -f net9.0 --interactivity Server

dotnet sln add src/AuntiesRecipe.Domain/AuntiesRecipe.Domain.csproj
dotnet sln add src/AuntiesRecipe.Application/AuntiesRecipe.Application.csproj
dotnet sln add src/AuntiesRecipe.Infrastructure/AuntiesRecipe.Infrastructure.csproj
dotnet sln add src/AuntiesRecipe.Web/AuntiesRecipe.Web.csproj

dotnet add src/AuntiesRecipe.Application/AuntiesRecipe.Application.csproj reference src/AuntiesRecipe.Domain/AuntiesRecipe.Domain.csproj
dotnet add src/AuntiesRecipe.Infrastructure/AuntiesRecipe.Infrastructure.csproj reference src/AuntiesRecipe.Application/AuntiesRecipe.Application.csproj
dotnet add src/AuntiesRecipe.Infrastructure/AuntiesRecipe.Infrastructure.csproj reference src/AuntiesRecipe.Domain/AuntiesRecipe.Domain.csproj
dotnet add src/AuntiesRecipe.Web/AuntiesRecipe.Web.csproj reference src/AuntiesRecipe.Application/AuntiesRecipe.Application.csproj
dotnet add src/AuntiesRecipe.Web/AuntiesRecipe.Web.csproj reference src/AuntiesRecipe.Infrastructure/AuntiesRecipe.Infrastructure.csproj
```

Delete default `Class1.cs` files in the three class libraries.

### Step 2 — Domain entities

**What:** Types like `Category`, `MenuItem` (pure C# properties, minimal behavior at first).

**Why:** This is your **ubiquitous language** for the juice shop. Everything else (Blazor, EF) maps to these types.

### Step 3 — Application contracts

**What:** DTOs (e.g. `MenuItemDto`) and interfaces (e.g. `IMenuService`).

**Why:** The UI depends on **stable contracts**, not on EF or SQL. Easier to unit test and swap storage later.

### Step 4 — Infrastructure (EF Core)

**What:** `DbContext`, Fluent API or conventions, migrations, **SQLite** for local dev (optional PostgreSQL later).

**Why:** Database details stay in one project. `Web` only calls `AddInfrastructure()` and never references `DbContext` directly from pages (prefer injecting `IMenuService`).

### Step 5 — Blazor `Web` composition

**What:** `Program.cs` registers services, `builder.Services.AddInfrastructure(builder.Configuration)`.

**Why:** The **composition root** is the only place that wires interfaces to implementations—clear for GitHub readers.

### Migrations (after model changes)

```bash
export PATH="$HOME/.dotnet:$HOME/.dotnet/tools:$PATH"
export DOTNET_ROOT="$HOME/.dotnet"
dotnet tool install dotnet-ef --global --version 9.0.0   # once
dotnet ef migrations add NameOfMigration --project src/AuntiesRecipe.Infrastructure --startup-project src/AuntiesRecipe.Web
```

### Next steps (Identity, cart, admin, CI)

Follow the plan in `.cursor/plans/juice_shop_blazor_portfolio_*.plan.md`: Identity + roles, cart/checkout, admin CRUD, tests, GitHub Actions.

## How to test logins + cart/checkout (for GitHub reviewers)

### 1. Start the app

```bash
cd src/AuntiesRecipe.Web
# (or: src/AuntiesRecipe.Web)
dotnet run
```

### 2. Create the first Admin/Owner (manual-create)

1. Open `src/AuntiesRecipe.Web/appsettings.json`.
2. Set `AdminBootstrap:Code` to any value you want.
3. Visit: `/admin/bootstrap`.
4. Enter the bootstrap code, plus an admin email + password.
5. After creation, go to `/login` and sign in as the admin.

### 3. Create a Customer account

- Visit `/register`.
- Register with any email/password.
- Your account is automatically assigned the `Customer` role.

### 4. Use cart + checkout

- Go to `/` and click `Add to cart` on any item.
- Review in `/cart`.
- Place an order in `/checkout` (demo checkout stores an order in SQLite; no real payment).

### 5. View orders as Admin

- Sign in as the admin/owner.
- Go to `/admin/orders` to see placed orders.

# Architecture Notes

## Enterprise Layered Architecture

The project follows a strict layered architecture pattern where each layer has a clear, isolated responsibility.

```
Blazor Pages / API Controllers
        |
Application Services (use-case orchestration)
        |
Repository Interfaces (data access contracts)
        |
EF Core Repositories (persistence implementation)
        |
SQLite Database
```

## Layer Responsibilities

- `AuntiesRecipe.Domain`
  - business entities, enums, and value objects
  - no infrastructure or framework dependencies
- `AuntiesRecipe.Application`
  - service interfaces (`ICartService`, `IMenuService`, `IOrderService`, `IBusinessProfileService`)
  - repository interfaces (`ICartRepository`, `IMenuRepository`, `IOrderRepository`, `IBusinessProfileRepository`)
  - application service implementations (`CartAppService`, `MenuAppService`, `OrderAppService`, `BusinessProfileAppService`)
  - DTOs for data transfer between layers
- `AuntiesRecipe.Infrastructure`
  - EF Core DbContexts, migrations, and seed data
  - repository implementations backed by EF Core
  - Identity infrastructure
- `AuntiesRecipe.Web`
  - Blazor UI components/pages (inject application service interfaces)
  - API controllers (REST surface for external consumers)
  - auth endpoints, composition root (`Program.cs`)

## Data Model Overview

- Catalog: `Category` -> many `MenuItem`
- Cart: `Cart` -> many `CartItem`
- Orders: `Order` -> many `OrderItem` (daily unique token index)
- Business profile: singleton-style `BusinessProfile` row

## Service Split

- `CartAppService` -- cart mutation + checkout orchestration via repositories
- `OrderAppService` -- admin order queries, filtered history with paging, status updates
- `MenuAppService` -- menu/category admin CRUD operations
- `BusinessProfileAppService` -- profile persistence with config fallback defaults

## API Surface

Controllers under `Web/Controllers/` provide REST endpoints:
- `AuthController` -- login/logout with cookie-based auth
- `MenuController` -- public menu + admin category/item management
- `CartController` -- cart CRUD + checkout
- `OrdersController` -- order lookup + admin operations
- `BusinessProfileController` -- profile read/update

## Reliability Strategy

- EF Core migrations auto-run on startup for demo simplicity
- Tests cover:
  - checkout creates order and clears cart (via app service + repositories)
  - history filter behavior with paging
  - order status transitions
  - auth endpoint redirect/safety behavior
  - hosted app integration for admin order operations

## Query Optimization

Indexes added for admin-heavy operations:
- `Order.Status`
- `Order.CreatedAtUtc`
- `(Order.PickupName, Order.PickupPhone)`
- existing unique token index retained

# Architecture Notes

## Layer Responsibilities

- `AuntiesRecipe.Domain`
  - business entities and enums
  - no infrastructure dependencies
- `AuntiesRecipe.Application`
  - DTOs and service contracts
  - interface boundary for UI and infrastructure
- `AuntiesRecipe.Infrastructure`
  - EF Core DbContexts, migrations, seed data
  - concrete services implementing application contracts
- `AuntiesRecipe.Web`
  - Blazor UI components/pages
  - auth endpoints and composition root (`Program.cs`)

## Data Model Overview

- Catalog:
  - `Category` -> many `MenuItem`
- Cart:
  - `Cart` -> many `CartItem`
- Orders:
  - `Order` -> many `OrderItem`
  - daily unique token index: `(TokenDateUtc, DailyTokenNumber)`
- Business profile:
  - singleton-style `BusinessProfile` row used by admin-managed homepage content

## Service Split

- `CartService`
  - cart mutation and checkout behavior
- `OrderService`
  - admin order queries, filtering, paging, and status updates
- `MenuService`
  - category/item admin operations and menu retrieval
- `BusinessProfileService`
  - business profile persistence and fallback defaults

## Reliability Strategy

- EF Core migrations auto-run on startup for demo simplicity.
- Tests cover:
  - checkout creates order and clears cart
  - history filter behavior with paging
  - order status transitions

## Query Optimization

Additional indexes added for admin-heavy operations:

- `Order.Status`
- `Order.CreatedAtUtc`
- `(Order.PickupName, Order.PickupPhone)`
- existing unique token index retained

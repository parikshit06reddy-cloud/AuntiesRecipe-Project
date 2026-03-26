# ADR-001: Service Boundary Split

## Status
Accepted

## Context
Cart and admin order responsibilities previously lived in one service, which increased coupling and made testing/reviewing changes harder.

## Decision
Keep `CartService` focused on cart mutation + checkout, and move admin order querying/status operations into `OrderService`.

## Consequences
- Cleaner single-responsibility boundaries.
- Simpler test strategy (cart tests vs admin-order tests).
- Lower risk of regressions when evolving admin history/status features.

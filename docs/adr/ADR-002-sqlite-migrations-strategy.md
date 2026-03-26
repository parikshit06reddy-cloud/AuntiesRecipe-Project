# ADR-002: SQLite + Auto-Migration Strategy

## Status
Accepted

## Context
This project is a portfolio application that needs low-friction local setup, easy hosting, and deterministic schema evolution.

## Decision
Use SQLite for application persistence and run EF Core migrations on startup for both app and identity contexts.

## Consequences
- One-command onboarding and demo startup.
- Reliable schema updates across local/deploy environments.
- Trade-off: not intended for high write-concurrency workloads; acceptable for portfolio/demo scope.

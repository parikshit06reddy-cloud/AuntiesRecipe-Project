# ADR-003: Auth Endpoint Flow for Cookie Writes

## Status
Accepted

## Context
Blazor interactive event handlers can fail when trying to modify auth headers/cookies after response start.

## Decision
Use dedicated server endpoints (`/auth/login`, `/auth/logout`) for sign-in/sign-out, and submit login via regular form post.

## Consequences
- Reliable cookie handling in server request pipeline.
- Clear separation between UI rendering and auth side effects.
- Easier endpoint-level integration testing for success/failure and redirect safety.

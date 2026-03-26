# Engineering Notes

## Why This Structure

- The app uses a layered design to show separation of concerns expected in maintainable .NET applications.
- Service interfaces in `Application` allow UI concerns to remain independent from EF Core details.
- Admin workflows were implemented with server-side filtering and status updates to avoid client-only logic drift.

## Hardening Completed

- Upload validation includes:
  - file-size limit
  - extension allowlist
  - MIME allowlist
  - image signature sniffing
- Admin destructive actions require confirmation prompts.
- Runtime upload directories are excluded from git.
- CI quality workflow added for format/build/test/publish.

## Performance and Scale Considerations

- Read-heavy order queries use `AsNoTracking`.
- Projection-based DTO mapping avoids unnecessary entity materialization in list paths.
- Admin history supports paging controls and bounded page-size.
- Order filtering indexes were added for status/date/pickup lookups.

## Future Improvements

- Add total-count metadata for paged history.
- Add optimistic concurrency tokens for admin edits.
- Add API-level endpoint tests for auth and admin flows.
- Add smoke tests for deployed environment checks.

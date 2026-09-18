# Real visitor demo

The original API and PostgreSQL back the visitor demo. Vercel hosts the React frontend; a Docker-capable host runs this .NET service. No shared Alice/Bob credentials are enabled in Production.

Set `VisitorDemo__Enabled=true` and a randomly generated `VisitorDemo__SigningKey` of at least 32 bytes. Supply `ConnectionStrings__ConnectionString` for a dedicated disposable PostgreSQL database and `Cors__Origins__0` for the frontend's exact origin. Never point this mode at an existing production database: it creates its own schema on an empty database. Schema upgrades need reviewed migrations; EnsureCreated does not migrate an existing schema.

Each POST to /demo/session issues a unique subject and signed token valid for one hour. Validation checks the database session as well as the signature and expiry. DELETE /demo/session revokes access and deletes that visitor's records in one transaction. A background worker removes expired sessions and their data every five minutes. All existing owner checks still apply. Sessions are rate limited, authenticated visitor requests are rate limited, and request bodies are limited to 128 KiB. Use synthetic information only.

## Local production-mode verification

Set `VISITOR_DEMO_SIGNING_KEY` in your shell to a newly generated random value, without checking it into Git. Run:

```sh
docker compose -f compose.visitor.yaml up --build -d
python3 tests/test_visitor.py
```

The isolated database belongs to the case-reviews-visitor Compose project. API: http://127.0.0.1:5210. In the frontend checkout, run `corepack yarn test:e2e:visitor`; it starts the actual frontend on port 5211 and exercises browser/API/database writes.

## Hosting

Vercel container images are the selected deployment path. See VERCEL.md. The Neon free_v3 database integration requires owner acceptance of its marketplace terms before provisioning. No Render account is required.

Build the frontend with `VITE_VISITOR_DEMO=true`, `VITE_BACKEND_API` set to this service's HTTPS URL, and `VITE_PORTFOLIO_DEMO=false`. Deploy the resulting dist directory to the existing Vercel demo project only after running the live visitor tests. The old static demo remains live until this integration is verified.

Required hosted checks: session isolation, reload persistence, reset/revocation, expired-session cleanup, CORS, cold start, and full review/report workflow. Rendering the homepage alone is insufficient.

Verification completed locally: production Docker build, five HTTP ownership/session scenarios, physical deletion on reset and expiry, and the original React browser review/report workflow against PostgreSQL. Hosted verification is still pending Vercel container deployment and a dedicated Neon database.

# Vercel container deployment

Vercel now supports custom container images in beta on all plans: https://vercel.com/docs/functions/container-images . This project uses Dockerfile.vercel with an explicit container service and catch-all route in vercel.json to run the original .NET API. Automatic detection without the service produced a static 404 deployment. Set PORT=8080 in Vercel because the unprivileged .NET container listens on that port.

Required server-only variables: VisitorDemo__Enabled=true, VisitorDemo__SigningKey (random secret), ConnectionStrings__ConnectionString (dedicated PostgreSQL database), Cors__Origins__0=https://case-reviews-demo.vercel.app, ASPNETCORE_ENVIRONMENT=Production, PORT=8080, AllowedHosts=case-reviews-demo-api.vercel.app;*.vercel.app;localhost;127.0.0.1. Never pass the signing key or database credentials to the frontend.

Deployed on the personal Vercel Hobby project case-reviews-demo-api with the Neon free_v3 integration case-reviews-demo-db. The container service and database are verified by all five hosted API tests. Public API: https://case-reviews-demo-api.vercel.app . No Render service is used.

After linking the dedicated project, run `python3 scripts/deploy_vercel.py` with Node 24 and an authenticated Vercel CLI. The script uploads only tracked source from a temporary directory outside Git. It never uploads local environment files, build outputs, or Git history. Run `VISITOR_API_URL=https://case-reviews-demo-api.vercel.app python3 tests/test_visitor.py` after deployment.

Containers scale down when idle. Expired tokens are rejected against the database immediately on a request; the cleanup worker runs at startup and every five minutes while a container is running. Physical deletion of expired records can therefore be delayed until the next startup after an idle period. Do not promise wall-clock deletion while a free container is asleep. No genuine case information may be entered.

Schema creation is intended only for an empty disposable database. A PostgreSQL advisory lock serializes initialization across simultaneous cold starts. Use a reviewed migration process for later schema changes.

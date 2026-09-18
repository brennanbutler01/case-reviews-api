# Vercel container deployment

Vercel now supports custom container images in beta on all plans: https://vercel.com/docs/functions/container-images . This project uses Dockerfile.vercel to run the original .NET API. Set PORT=8080 in Vercel because the unprivileged .NET container listens on that port.

Required server-only variables: VisitorDemo__Enabled=true, VisitorDemo__SigningKey (random secret), ConnectionStrings__ConnectionString (dedicated PostgreSQL database), Cors__Origins__0=https://case-reviews-demo.vercel.app, ASPNETCORE_ENVIRONMENT=Production, PORT=8080. Never pass the signing key or database credentials to the frontend.

The personal Vercel Hobby project and Neon free_v3 integration are the intended deployment surfaces. The container beta and account access must be verified before claiming deployment. Render is not selected.

Containers scale down when idle. Expired tokens are rejected against the database immediately on a request; the cleanup worker runs at startup and every five minutes while a container is running. Physical deletion of expired records can therefore be delayed until the next startup after an idle period. Do not promise wall-clock deletion while a free container is asleep. No genuine case information may be entered.

Schema creation is intended only for an empty disposable database. A PostgreSQL advisory lock serializes initialization across simultaneous cold starts. Use a reviewed migration process for later schema changes.

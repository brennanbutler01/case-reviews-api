# Case Reviews API

C# / .NET 10 API with Entity Framework Core and PostgreSQL, used by the [companion frontend](https://github.com/brennanbutler01/case-reviews-portfolio).

## Local synthetic demo

```sh
docker compose up -d
python3 tests/test_api.py
```

The API listens at http://127.0.0.1:5190. `/health` reports readiness. Docker creates a dedicated database with invented demo credentials; no hosted credentials are required. First startup restores packages and creates the demo schema. Restart the API with `docker compose restart api` after code changes.

In development demo mode only, `/dev/token/alice` and `/dev/token/bob` issue short-lived test tokens. These identities are intentionally public. Do not expose this stack publicly or enter real case records. Startup rejects demo authentication outside the Development environment.

Staff and review queries are scoped to the authenticated subject. The server assigns ownership; input cannot transfer records between users. Reviews require an owned staff record. Staff with reviews cannot be deleted until those reviews are removed. Review updates replace child records within a database save transaction.

## Verification

```sh
docker compose exec api dotnet build --no-restore
python3 tests/test_api.py
docker compose exec api dotnet list package --vulnerable --include-transitive
```

HTTP tests exercise real PostgreSQL persistence, anonymous access, owner isolation, forged ownership, route IDs, invalid inputs, child replacement, and staff deletion constraints.

## Hosted visitor demo

The portfolio frontend at https://case-reviews-demo.vercel.app uses this API at https://case-reviews-demo-api.vercel.app. Both run in Brennan's personal Vercel Hobby scope, with a dedicated Neon free database. Visitors receive isolated one-hour sessions without signing up; reset revokes the token and deletes that visitor's records. Use invented information only.

```sh
VISITOR_API_URL=https://case-reviews-demo-api.vercel.app python3 tests/test_visitor.py
```

All five hosted ownership/session scenarios pass, including anonymous rejection, visitor isolation, linked-record validation, reset revocation, and rejection of local demo tokens. The companion frontend's desktop and mobile workflows also pass against this API. See [VISITOR-DEMO.md](VISITOR-DEMO.md) for behavioral limits and [VERCEL.md](VERCEL.md) for the personal-project deployment boundary.

## Authenticated hosted configuration

Supply through your deployment environment, never committed configuration:

- `ConnectionStrings__ConnectionString`: your PostgreSQL connection string.
- `Auth__Authority`: your Auth0 issuer URL.
- `Auth__Audience`: your API identifier.
- `Cors__Origins__0`: your frontend origin.
- `AllowedHosts`: your API hostname.
- `ASPNETCORE_ENVIRONMENT=Production`; leave `Demo__Enabled` unset or false.

Real Auth0 integration and migration of an existing database have not been verified during local recovery. Automatic schema creation is limited to the disposable demo. Review and test migrations before using an existing database. This is not a production deployment runbook.

## Source hygiene

This public edition starts from the recovered source. Historical screenshots and configuration images were removed before publication. The original repository remains private because GitHub may retain previously exposed commits outside branch history. Provider revocation is separate from source removal; never reuse historical credentials.

# Deployment and recovery

The container persists its SQLite database in `/app/data` and uploaded images
in `/app/uploads`. Back up both volumes before deploying any version that
contains an EF Core migration.

## Deploying

1. Set `JWT_KEY` to a random value of at least 32 bytes.
2. Set `ALLOWED_ORIGINS` to the public application URL.
3. If requests pass through a proxy, set `TRUSTED_PROXY_NETWORKS` to its
   semicolon-separated IP/CIDR range. Leave it empty for direct access.
4. Run `docker compose pull` followed by `docker compose up -d`.
5. Confirm `http://localhost:8080/health/ready` responds successfully.

Migrations run when the API starts. A failed migration prevents readiness, so
do not route traffic until the readiness check succeeds.

## Backup and rollback

Back up both named volumes before deployment. To roll back application code,
deploy the previous image only after confirming its EF Core model can read the
current schema. Recent migrations are additive, but restoring a database backup
is the safe rollback for a destructive future migration.

If a migration fails, stop the new container, preserve the volumes for
investigation, restore both the database and uploads from the same backup when
needed, then start the known-good image.

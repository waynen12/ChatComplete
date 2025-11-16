# Task for Claude Code in Cloud

## Issue: Docker Compose Command Not Found in GitHub Actions

**Workflow:** `.github/workflows/docker-build.yml`
**Error:** `docker-compose: command not found` on ubuntu-latest runner
**Exit Code:** 127

---

## Root Cause

GitHub's `ubuntu-latest` runners use Docker Compose V2, which uses the command `docker compose` (space, not hyphen).

Our workflow uses `docker-compose` (V1 syntax with hyphen), which doesn't exist on these runners.

---

## Fix Required

Update `.github/workflows/docker-build.yml` to use `docker compose` instead of `docker-compose`.

**Line to change:**
```yaml
# Current (line 29):
docker-compose -f docker-compose.qdrant.yml up -d

# Should be:
docker compose -f docker-compose.qdrant.yml up -d
```

---

## Files to Check and Update

### 1. `.github/workflows/docker-build.yml` (MUST FIX)
- Line 29: Change `docker-compose` to `docker compose`

### 2. `.github/workflows/deploy-self.yml` (CHECK - probably OK)
- Line 35: Uses `docker-compose`
- **BUT** this runs on `self-hosted` runner, not `ubuntu-latest`
- Self-hosted runner likely has `docker-compose` installed
- **Only change if you find evidence it's failing too**

---

## Testing Strategy

After fixing:
1. Push changes to trigger workflow
2. Monitor GitHub Actions workflow run
3. Verify test step completes successfully
4. Confirm Docker build proceeds after tests pass

---

## Additional Context

**Docker Compose V1 vs V2:**
- V1: `docker-compose` (standalone binary)
- V2: `docker compose` (Docker CLI plugin)
- Ubuntu-latest runners: Only have V2 installed

**Why deploy-self.yml might be OK:**
- Runs on self-hosted runner (not GitHub-hosted ubuntu-latest)
- Self-hosted runner may have both V1 and V2 installed
- Check recent workflow runs to confirm

---

## Success Criteria

- ✅ Workflow runs without "command not found" error
- ✅ Qdrant starts successfully
- ✅ Tests execute (396 tests)
- ✅ Docker build proceeds (if tests pass)

---

## Related Documentation

- [Docker Compose V2 Migration](https://docs.docker.com/compose/migrate/)
- [GitHub Actions Runner Images](https://github.com/actions/runner-images/blob/main/images/ubuntu/Ubuntu2204-Readme.md)
- [FAILFAST_CI_IMPLEMENTATION.md](../documentation/FAILFAST_CI_IMPLEMENTATION.md) - Context on recent changes

---

## If You Need More Context

Read these files in order:
1. `.github/workflows/docker-build.yml` - The failing workflow
2. `documentation/FAILFAST_CI_IMPLEMENTATION.md` - What we just implemented
3. `.github/workflows/deploy-self.yml` - Similar workflow (self-hosted)

---

**Priority:** HIGH - Blocks Docker image publishing
**Estimated Fix Time:** 5 minutes
**Difficulty:** Easy (one-line change)

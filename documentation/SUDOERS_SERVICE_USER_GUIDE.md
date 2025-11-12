# Sudoers Configuration Guide for Service Users

Complete guide for configuring passwordless sudo permissions for service accounts (e.g., CI/CD runners, deployment automation).

---

## Table of Contents
1. [Overview](#overview)
2. [Prerequisites](#prerequisites)
3. [Step-by-Step Configuration](#step-by-step-configuration)
4. [Common Gotchas](#common-gotchas)
5. [Testing](#testing)
6. [Troubleshooting](#troubleshooting)
7. [Security Best Practices](#security-best-practices)

---

## Overview

Service users (like GitHub Actions runners) need passwordless sudo access to manage systemd services without interactive prompts. This guide shows how to configure sudoers correctly for automated deployments.

**Use Case Example:**
- GitHub Actions runner running as `chatapi` user
- Needs to restart systemd services: `knowledge-api.service`, `knowledge-mcp.service`
- Must work in non-interactive CI/CD environment

---

## Prerequisites

- [ ] Root or sudo access to the target machine
- [ ] Service user account created (e.g., `chatapi`)
- [ ] Service user has nologin shell (security best practice)
- [ ] Know exact commands that need passwordless sudo

**Verify service user exists:**
```bash
id chatapi
# Output: uid=995(chatapi) gid=985(chatapi) groups=985(chatapi)
```

---

## Step-by-Step Configuration

### Step 1: Create Sudoers File for Service User

**Always use `visudo` to edit sudoers files** - it validates syntax before saving.

```bash
sudo visudo -f /etc/sudoers.d/chatapi
```

### Step 2: Add Sudoers Entry

**Template:**
```
<username> ALL=(root) NOPASSWD: <command1>, \
                                <command2>, \
                                <command3>
```

**Real Example:**
```
# Allow GitHub-runner user to manage services without password
chatapi ALL=(root) NOPASSWD: /usr/bin/systemctl restart knowledge-api.service, \
                              /usr/bin/systemctl --no-pager status knowledge-api.service, \
                              /usr/bin/systemctl restart knowledge-mcp.service, \
                              /usr/bin/systemctl --no-pager status knowledge-mcp.service, \
                              /usr/bin/journalctl
```

**Key Points:**
- Use **absolute paths** (`/usr/bin/systemctl`, not `systemctl`)
- Commands must match **exactly** including flags (see Gotcha #1)
- Use backslashes `\` for multi-line entries (improves readability)
- No trailing backslash on the last line

### Step 3: Set Correct File Permissions

**This is critical!** Sudoers files with wrong permissions are silently ignored.

```bash
sudo chmod 0440 /etc/sudoers.d/chatapi
```

**Verify ownership:**
```bash
ls -la /etc/sudoers.d/chatapi
# Should show: -r--r----- 1 root root <size> <date> /etc/sudoers.d/chatapi
```

### Step 4: Validate Syntax

```bash
sudo visudo -c
```

**Expected output:**
```
/etc/sudoers: parsed OK
/etc/sudoers.d/chatapi: parsed OK
```

**Bad output (example):**
```
/etc/sudoers.d/chatapi: bad permissions, should be mode 0440  ❌
/etc/sudoers.d/chatapi: syntax error near line 5                ❌
```

### Step 5: Test Configuration

**Test as the service user:**
```bash
sudo -u chatapi bash -c "sudo -n systemctl restart knowledge-api.service && echo SUCCESS"
```

**Expected:** `SUCCESS` printed with no password prompt

**If it fails:** See [Troubleshooting](#troubleshooting) section

---

## Common Gotchas

### Gotcha #1: Exact Command Matching ⚠️

Sudoers requires **exact command matching** including all flags and arguments.

**Problem Example:**
```bash
# Sudoers entry:
chatapi ALL=(root) NOPASSWD: /usr/bin/systemctl status knowledge-api.service

# This works:
sudo systemctl status knowledge-api.service  ✅

# This FAILS (--no-pager flag not permitted):
sudo systemctl --no-pager status knowledge-api.service  ❌
```

**Solution:** Add every command variation you need:
```bash
chatapi ALL=(root) NOPASSWD: /usr/bin/systemctl status knowledge-api.service, \
                              /usr/bin/systemctl --no-pager status knowledge-api.service
```

### Gotcha #2: File Permissions (Mode 0440) ⚠️

**Problem:** Sudoers files with incorrect permissions are **silently ignored**.

**Wrong permissions:**
```bash
-rwxr-xr-x 1 root root 256 Nov 11 20:00 /etc/sudoers.d/chatapi  ❌ (mode 755)
-rw-r--r-- 1 root root 256 Nov 11 20:00 /etc/sudoers.d/chatapi  ❌ (mode 644)
```

**Correct permissions:**
```bash
-r--r----- 1 root root 256 Nov 11 20:00 /etc/sudoers.d/chatapi  ✅ (mode 0440)
```

**Fix it:**
```bash
sudo chmod 0440 /etc/sudoers.d/chatapi
sudo visudo -c  # Verify it's now "parsed OK"
```

### Gotcha #3: Relative vs Absolute Paths ⚠️

**Problem:** Sudoers requires absolute paths to commands.

**Wrong:**
```bash
chatapi ALL=(root) NOPASSWD: systemctl restart knowledge-api.service  ❌
```

**Correct:**
```bash
chatapi ALL=(root) NOPASSWD: /usr/bin/systemctl restart knowledge-api.service  ✅
```

**Find absolute path:**
```bash
which systemctl
# Output: /usr/bin/systemctl
```

### Gotcha #4: Target User Specification ⚠️

**Problem:** Using `(root)` vs `(ALL)` can affect behavior.

**Most restrictive (recommended):**
```bash
chatapi ALL=(root) NOPASSWD: /usr/bin/systemctl restart knowledge-api.service
```

**More permissive:**
```bash
chatapi ALL=(ALL) NOPASSWD: /usr/bin/systemctl restart knowledge-api.service
```

**Key difference:**
- `(root)` - Only allows `sudo command` (defaults to root)
- `(ALL)` - Allows `sudo -u anyuser command`

For service management, `(root)` is sufficient and more secure.

### Gotcha #5: Wildcards and Arguments ⚠️

**Problem:** Wildcards can be tricky in sudoers.

**This might not work as expected:**
```bash
chatapi ALL=(root) NOPASSWD: /usr/bin/journalctl -u knowledge-api.service *  ❌
```

**Better approach:**
```bash
# Allow entire journalctl command (it's read-only anyway)
chatapi ALL=(root) NOPASSWD: /usr/bin/journalctl  ✅
```

### Gotcha #6: Environment Variables ⚠️

**Problem:** Service users might not have the same PATH or environment.

**Check service user's environment:**
```bash
sudo -u chatapi bash -c 'echo $PATH'
# Might differ from your user's PATH
```

**Solution:** Always use absolute paths in sudoers entries.

### Gotcha #7: Runner Service Caching ⚠️

**Problem:** GitHub Actions runner might cache the old sudoers configuration.

**Symptoms:**
- Manual test works: `sudo -u chatapi bash -c "sudo -n systemctl restart service"`
- GitHub Actions fails: `sudo: a password is required`

**Solution:** Restart the runner service after changing sudoers:
```bash
sudo systemctl restart actions.runner.*
# Or reboot the machine
```

---

## Testing

### Pre-Deployment Testing Checklist

- [ ] **Step 1:** Validate sudoers syntax
  ```bash
  sudo visudo -c
  ```

- [ ] **Step 2:** Check file permissions
  ```bash
  ls -la /etc/sudoers.d/chatapi
  # Should be: -r--r----- 1 root root
  ```

- [ ] **Step 3:** Test as service user (interactive)
  ```bash
  sudo -u chatapi bash
  sudo -n systemctl restart knowledge-api.service
  exit
  ```

- [ ] **Step 4:** Test as service user (single command)
  ```bash
  sudo -u chatapi bash -c "sudo -n systemctl restart knowledge-api.service && echo SUCCESS"
  ```

- [ ] **Step 5:** Test all commands in your sudoers entry
  ```bash
  sudo -u chatapi bash -c "sudo -n systemctl restart knowledge-api.service"
  sudo -u chatapi bash -c "sudo -n systemctl --no-pager status knowledge-api.service"
  sudo -u chatapi bash -c "sudo -n systemctl restart knowledge-mcp.service"
  sudo -u chatapi bash -c "sudo -n systemctl --no-pager status knowledge-mcp.service"
  ```

- [ ] **Step 6:** Verify no password prompts appear

- [ ] **Step 7:** Check exit codes (should be 0)
  ```bash
  sudo -u chatapi bash -c "sudo -n systemctl restart knowledge-api.service"
  echo $?  # Should print: 0
  ```

### CI/CD Testing

**Add debug step to GitHub Actions workflow:**
```yaml
- name: Debug sudo configuration
  run: |
    echo "Current user: $(whoami)"
    echo "Testing passwordless sudo:"
    sudo -n systemctl --no-pager status knowledge-api.service || echo "FAILED: Sudo requires password"
```

**Monitor the workflow output** to verify passwordless sudo works.

---

## Troubleshooting

### Problem: "sudo: a password is required"

**Diagnosis Steps:**

1. **Check file exists:**
   ```bash
   ls -la /etc/sudoers.d/chatapi
   ```

2. **Check file permissions:**
   ```bash
   ls -la /etc/sudoers.d/chatapi
   # Should be: -r--r----- (mode 0440)
   ```

   **Fix if wrong:**
   ```bash
   sudo chmod 0440 /etc/sudoers.d/chatapi
   ```

3. **Validate syntax:**
   ```bash
   sudo visudo -c
   ```

   **If syntax error:** Fix with `sudo visudo -f /etc/sudoers.d/chatapi`

4. **Check command matching:**
   ```bash
   # View your sudoers entry:
   sudo cat /etc/sudoers.d/chatapi

   # Compare with actual command being run
   # They must match EXACTLY including flags
   ```

5. **Test with verbose output:**
   ```bash
   sudo -u chatapi sudo -n -v systemctl restart knowledge-api.service
   ```

### Problem: "bad permissions, should be mode 0440"

**Cause:** File permissions are too permissive.

**Fix:**
```bash
sudo chmod 0440 /etc/sudoers.d/chatapi
sudo chown root:root /etc/sudoers.d/chatapi
sudo visudo -c  # Should now show "parsed OK"
```

### Problem: "No sudoers entries found"

**Cause:** File doesn't exist or name doesn't match username.

**Check:**
```bash
ls -la /etc/sudoers.d/
# Look for your file

# Check content:
sudo cat /etc/sudoers.d/chatapi
```

### Problem: Works manually but fails in CI/CD

**Possible causes:**

1. **Runner service needs restart:**
   ```bash
   sudo systemctl restart actions.runner.*
   ```

2. **Different environment variables:**
   ```bash
   # Add debug to workflow:
   - name: Debug environment
     run: |
       echo "PATH=$PATH"
       echo "USER=$USER"
       which systemctl
   ```

3. **Missing `-n` flag in workflow:**
   ```yaml
   # Wrong (tries interactive prompt):
   - run: sudo systemctl restart service

   # Correct (non-interactive):
   - run: sudo -n systemctl restart service
   ```

### Problem: "systemctl: command not found"

**Cause:** Using relative path instead of absolute path.

**Fix:** Find absolute path and use it:
```bash
which systemctl
# Output: /usr/bin/systemctl

# Update sudoers to use: /usr/bin/systemctl
```

---

## Security Best Practices

### ✅ DO:

1. **Use separate sudoers files per user**
   ```bash
   /etc/sudoers.d/chatapi
   /etc/sudoers.d/deploy-user
   ```

2. **Grant minimal permissions**
   ```bash
   # Only specific commands needed
   chatapi ALL=(root) NOPASSWD: /usr/bin/systemctl restart knowledge-api.service
   ```

3. **Use absolute paths**
   ```bash
   /usr/bin/systemctl (not systemctl)
   ```

4. **Document the purpose**
   ```bash
   # Allow GitHub Actions runner to manage services
   chatapi ALL=(root) NOPASSWD: ...
   ```

5. **Set correct permissions (0440)**
   ```bash
   sudo chmod 0440 /etc/sudoers.d/chatapi
   ```

6. **Always use visudo to edit**
   ```bash
   sudo visudo -f /etc/sudoers.d/chatapi
   ```

7. **Test thoroughly before production**

### ❌ DON'T:

1. **Don't use wildcards carelessly**
   ```bash
   # Too permissive - allows ANY systemctl command:
   chatapi ALL=(root) NOPASSWD: /usr/bin/systemctl *  ❌
   ```

2. **Don't grant full sudo access**
   ```bash
   # NEVER do this for service accounts:
   chatapi ALL=(ALL) NOPASSWD: ALL  ❌
   ```

3. **Don't edit /etc/sudoers directly**
   ```bash
   # Keep custom entries in /etc/sudoers.d/ for easier management
   ```

4. **Don't skip syntax validation**
   ```bash
   # Always run after changes:
   sudo visudo -c
   ```

5. **Don't use wrong permissions**
   ```bash
   # Mode 0755, 0644, etc. will be ignored
   ```

---

## Quick Reference

### Common Sudoers Template for Service Management

```bash
# File: /etc/sudoers.d/<username>
# Permissions: 0440 (sudo chmod 0440 /etc/sudoers.d/<username>)

# Allow service user to manage systemd services
<username> ALL=(root) NOPASSWD: /usr/bin/systemctl restart <service1>.service, \
                                 /usr/bin/systemctl --no-pager status <service1>.service, \
                                 /usr/bin/systemctl restart <service2>.service, \
                                 /usr/bin/systemctl --no-pager status <service2>.service, \
                                 /usr/bin/journalctl
```

### Validation Checklist

```bash
# 1. Syntax check
sudo visudo -c

# 2. Permissions check
ls -la /etc/sudoers.d/<username>
# Should be: -r--r----- 1 root root

# 3. Test passwordless sudo
sudo -u <username> bash -c "sudo -n systemctl restart <service> && echo SUCCESS"

# 4. Verify in CI/CD
# Add to GitHub Actions workflow:
- run: sudo -n systemctl restart <service>
```

---

## Real-World Example: GitHub Actions Runner

**Scenario:**
- GitHub Actions runner running as `chatapi` user
- Needs to deploy two services: `knowledge-api.service` and `knowledge-mcp.service`
- Must restart services and check status
- Must view logs for debugging

**Complete Configuration:**

```bash
# Create sudoers file
sudo visudo -f /etc/sudoers.d/chatapi
```

**Content:**
```bash
# Allow GitHub Actions runner to manage Knowledge Manager services
chatapi ALL=(root) NOPASSWD: /usr/bin/systemctl restart knowledge-api.service, \
                              /usr/bin/systemctl --no-pager status knowledge-api.service, \
                              /usr/bin/systemctl restart knowledge-mcp.service, \
                              /usr/bin/systemctl --no-pager status knowledge-mcp.service, \
                              /usr/bin/journalctl
```

**Set permissions:**
```bash
sudo chmod 0440 /etc/sudoers.d/chatapi
sudo chown root:root /etc/sudoers.d/chatapi
```

**Validate:**
```bash
sudo visudo -c
# All files should show "parsed OK"
```

**Test:**
```bash
sudo -u chatapi bash -c "sudo -n systemctl restart knowledge-api.service && echo API_OK"
sudo -u chatapi bash -c "sudo -n systemctl restart knowledge-mcp.service && echo MCP_OK"
```

**GitHub Actions workflow:**
```yaml
- name: Restart services
  run: |
    sudo -n systemctl restart knowledge-api.service
    sudo -n systemctl --no-pager status knowledge-api.service
    sudo -n systemctl restart knowledge-mcp.service
    sudo -n systemctl --no-pager status knowledge-mcp.service
```

---

## Additional Resources

- **Official Sudoers Manual:** `man sudoers`
- **Sudoers Examples:** `man 5 sudoers`
- **visudo Manual:** `man visudo`
- **Security Considerations:** Search "sudoers security best practices"

---

## Summary

**Key Takeaways:**

1. ✅ Always use `visudo` to edit sudoers files
2. ✅ Set permissions to `0440` (critical!)
3. ✅ Use absolute paths for commands
4. ✅ Commands must match exactly (including flags)
5. ✅ Test as the service user before deploying
6. ✅ Use `-n` flag in CI/CD workflows
7. ✅ Validate syntax with `visudo -c`
8. ✅ Grant minimal permissions (specific commands only)

**Most Common Mistake:** Wrong file permissions (not 0440) - file is silently ignored!

**Most Common Fix:** `sudo chmod 0440 /etc/sudoers.d/<username>`

---

*Document Version: 1.0*
*Last Updated: 2025-11-12*
*Tested On: Linux Mint, Ubuntu 22.04, Debian 12*

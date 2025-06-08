# Technical Specification: deploy_to_droplet copy.ps1

## 1. Introduction

This document provides a technical specification for the `deploy_to_droplet copy.ps1` PowerShell script. The script automates the deployment process of a web application consisting of a Flask backend and a React frontend to a Linux server (specifically targeting a DigitalOcean Droplet, but adaptable). It handles environment configuration, user confirmation, local building, version control checkout, server backup, file synchronization via WSL/rsync, optional database migrations, and service restarts.

## 2. Prerequisites

### 2.1. Local Machine (Running the Script)

*   **Operating System:** Windows with PowerShell v5.1 or later.
*   **WSL (Windows Subsystem for Linux):** Installed and configured with a Linux distribution (e.g., Ubuntu).
*   **Git:** Installed and accessible from the PowerShell environment.
*   **Node.js & npm:** Installed for building the React frontend locally.
*   **SSH Client:** Accessible from PowerShell (usually built-in).
*   **SSH Key for WSL:** An SSH key pair configured within the specified WSL user's home directory (`~/.ssh/`) for passwordless SSH access *from WSL* to the target server. The private key path must be specified in the configuration.
*   **SSH Key for PowerShell:** SSH key pair configured for passwordless SSH access *from Windows/PowerShell* to the target server (can be the same key as WSL, managed via ssh-agent or default `~/.ssh/id_rsa`).
*   **Project Source Code:** Available locally at the path specified in the configuration (`DEPLOYMENT_LOCAL_BASE_DIR`).

### 2.2. Target Server

*   **Operating System:** Linux (tested primarily with Ubuntu/Debian derivatives).
*   **SSH Server:** Running and configured to allow key-based authentication for the deployment user.
*   **Deployment User:** A dedicated user account (specified by `DEPLOYMENT_SERVER_USER`) with:
    *   Passwordless `sudo` privileges for specific commands (`systemctl restart <flask_service>`, `systemctl restart nginx`).
    *   Write permissions to the deployment directories (`DEPLOYMENT_SERVER_BASE_DIR`, `DEPLOYMENT_BACKUP_DIR`).
    *   Permissions to execute `mysqldump` and connect to the target database (ideally configured via `~/.my.cnf` for passwordless access).
*   **Required Software:**
    *   `rsync`
    *   `git` (optional, but good practice)
    *   `mysql-client` (for `mysqldump`)
    *   Python 3.x
    *   `pip` and `venv` (or equivalent Python package management)
    *   Flask, Flask-Migrate, and other Python dependencies (installed within the project's virtual environment).
    *   Nginx (or other web server configured as a reverse proxy).
    *   `systemctl` (or equivalent service manager).
    *   Standard Linux utilities (`mkdir`, `cp`, `rm`, `find`, `sort`, `cut`, `echo`, `sed`, `wc`, `head`, `dirname`, `test`).

### 2.3. Configuration Files

*   **`.deployment_env`:** Located in the same directory as the script. Contains deployment-specific settings, including server details, paths, service names, and environment-specific keys.
*   **`.env`:** Located within the local React application's source directory (`<DEPLOYMENT_LOCAL_BASE_DIR>/satisfactory_tracker/`). Contains local build-time environment variables (e.g., `REACT_APP_RUN_MODE`, `REACT_APP_FLASK_ENV`).

## 3. Configuration Management

*   **Loading:** Configuration is loaded by the `Initialize-DeploymentConfiguration` function at the start of the script.
*   **Files:** It reads `.deployment_env` first, then uses `DEPLOYMENT_LOCAL_BASE_DIR` from it to locate and read the project's `.env` file.
*   **Environment Specificity:** Keys in `.deployment_env` ending with `_PROD`, `_QAS`, or `_DEV` (e.g., `DEPLOYMENT_TARGET_PROD`) are selected based on the `-Environment` parameter provided to the script. The base key name (e.g., `DEPLOYMENT_TARGET`) is then assigned the corresponding value in the script's scope.
*   **Scope:** Loaded configuration values are stored as script-scoped variables (e.g., `$DEPLOYMENT_SERVER_IP`, `$DEPLOYMENT_TARGET`) for global access within the script run.
*   **Validation:** The function checks for the existence of required common keys and environment-specific keys, failing fatally if any are missing.

## 4. Script Parameters

*   **`-Environment`** (`[string]`, Mandatory): Specifies the target deployment environment.
    *   Validation: Must be one of `PROD`, `QAS`, or `DEV` (case-insensitive).
    *   Purpose: Determines which environment-specific variables are loaded from `.deployment_env`.
*   **`-RunDBMigration`** (`[string]`, Mandatory): Controls whether database migrations are executed.
    *   Validation: Must be `y` or `n` (case-insensitive).
    *   Purpose: Enables/disables the database migration step (`Invoke-DatabaseMigration`).
*   **`-Version`** (`[string]`, Mandatory): Specifies the Git tag representing the version to deploy.
    *   Validation: Must match the pattern `^v\d+\.\d+\.\d+$` (e.g., `v1.2.3`).
    *   Purpose: Used to check out the correct code version before building and for logging/backup naming.

## 5. Core Logic / Workflow

The script executes the following steps sequentially:

1.  **Initialization & Logging Setup:**
    *   Determines script root path (`$PSScriptRoot`).
    *   Creates a timestamped/versioned log file in `./build_logs/`.
    *   Calls `Initialize-DeploymentConfiguration` to load settings and set script variables.
    *   Constructs derived variables (backup paths, server paths, WSL paths).
2.  **Environment Check & Confirmation (`Confirm-DeploymentEnvironment`):**
    *   Compares local `.env` settings (`REACT_APP_RUN_MODE`, `REACT_APP_FLASK_ENV`) against target deployment settings (`DEPLOYMENT_TARGET`, `DEPLOYMENT_FLASK_ENV`). Fails fatally on mismatch.
    *   Prompts the user to confirm the target environment, version, and server IP before proceeding. Exits if confirmation is not 'y'.
3.  **Checkout & Build (`Invoke-ReactBuild`):**
    *   Navigates to the local Git repository (`$DEPLOYMENT_LOCAL_BASE_DIR`).
    *   Executes `git fetch --tags origin --force`.
    *   Executes `git checkout $Version`. Fails fatally if checkout fails.
    *   Navigates to the local React frontend directory (`$localFrontendDir`).
    *   Executes `npm run build`. Standard error is redirected to standard output.
    *   Output is logged to the main build log and a separate `npm_build_errors*.log`.
    *   Fails fatally if `npm run build` exits with a non-zero code.
4.  **Server State Backup (`Backup-ServerState`):**
    *   Creates timestamped backup directories on the server within `$DEPLOYMENT_BACKUP_DIR`.
    *   Uses `Invoke-SshCommand` helper for SSH operations.
    *   **Flask Backup:** Copies contents of `$ServerFlaskDir` to `$BackupDirFlask` using `cp -a`. Warns if source doesn't exist. Calls `Remove-OldBackups` for `flask_` prefix.
    *   **Frontend Backup:** Copies `$ServerFrontendBuildDir` to `$BackupDirFrontend` using `cp -a`, then removes the original `$ServerFrontendBuildDir` (`rm -rf`) if the copy succeeds. Warns if source doesn't exist. Calls `Remove-OldBackups` for `frontend_` prefix.
    *   **Database Backup:** Executes `mysqldump` for `$DEPLOYMENT_DB_NAME`, redirecting output to `$BackupDirDB`. Assumes passwordless access via `.my.cnf`. Sets up a cleanup command (`rm -f $BackupDirDB`) to run if the dump fails. Calls `Remove-OldBackups` for `db_backup_` prefix and `.sql` suffix.
5.  **File Synchronization (`Sync-FilesToServer`):**
    *   Uses `Invoke-SshMkdir` to ensure target directories exist on the server.
    *   Uses `Invoke-WslRsync` helper for file transfers.
    *   **React Sync:** Syncs the contents of the local React build directory (`$WslLocalFrontendDirBuild/`) to the server's frontend build directory (`$ServerFrontendBuildDir/`) using `rsync -avz --delete`.
    *   **Flask Sync:** Syncs the contents of the local Flask app directory (`$WslLocalFlaskDirApp/`) to the server's Flask app directory (`$ServerFlaskAppDir/`) using `rsync -avz --delete`, excluding patterns like `__pycache__`, `logs`, `.git*`, etc.
6.  **Database Migration (`Invoke-DatabaseMigration`):**
    *   Skips if `-RunDBMigration` is not 'y'.
    *   Checks if the `migrations` directory exists on the server using `ssh ... "test -d ..."`.
    *   If not found, calls `Invoke-SshFlaskCommand` to run `flask db init`.
    *   Calls `Invoke-SshFlaskCommand` to run `flask db migrate -m '...'` to generate the migration script.
    *   **Pauses execution** and prompts the user to manually SSH into the server, review the generated script, and confirm (`y/n`) to proceed with applying it.
    *   If the user cancels (`n`), prompts whether to delete the generated (unapplied) migration script file. Finds the latest file using `ls -t | head -n 1` and removes it via SSH if confirmed. Fails fatally after cancellation.
    *   If the user confirms (`y`), calls `Invoke-SshFlaskCommand` to run `flask db upgrade`.
7.  **Service Restart (`Restart-Services`):**
    *   Uses `Invoke-SshServiceRestart` helper.
    *   Restarts the Flask service (`$DEPLOYMENT_FLASK_SERVICE_NAME`) using `sudo systemctl restart`. Treats failure as **fatal**.
    *   Restarts the Nginx service (`nginx`) using `sudo systemctl restart`. Treats failure as **non-fatal** (warning only).
8.  **Completion:**
    *   Prints success messages to the console and log file, including the log file path and the likely application URL.

## 6. Helper Functions

*   **`Import-EnvFile`:** Reads a `.env` style file, parses `KEY=VALUE` pairs (ignoring comments and blank lines), and returns a hashtable.
*   **`Convert-WindowsPathToWslPath`:** Converts a Windows path (e.g., `C:\Users\Me`) to its WSL equivalent (e.g., `/mnt/c/Users/Me`).
*   **`Initialize-DeploymentConfiguration`:** (See Section 3)
*   **`Confirm-DeploymentEnvironment`:** (See Section 5, Step 2)
*   **`Invoke-ReactBuild`:** (See Section 5, Step 3)
*   **`Backup-ServerState`:** (See Section 5, Step 4)
*   **`Sync-FilesToServer`:** (See Section 5, Step 5)
*   **`Invoke-DatabaseMigration`:** (See Section 5, Step 6)
*   **`Restart-Services`:** (See Section 5, Step 7)
*   **`Invoke-SshCommand` (Simplified version in script):** Executes a command via SSH (`ssh user@host "command"`). Checks `$LASTEXITCODE` and fails fatally on non-zero, optionally running a cleanup command first. *Note: Needs refactoring to align with the more robust proposed version.*
*   **`Invoke-SshMkdir`:** A specific wrapper around `ssh` to execute `mkdir -p`. Includes error checking.
*   **`Invoke-WslRsync`:** Constructs and executes an `rsync` command via `wsl -u <user> ...`.
    *   Uses `rsync -avz --delete`.
    *   Constructs the `-e 'ssh -i <key_path>'` option for rsync to use the specified WSL SSH key.
    *   Handles `--exclude` patterns.
    *   Uses `Invoke-Expression` to handle the complex command string with nested quotes.
    *   Includes detailed error checking and reporting.
*   **`Invoke-SshFlaskCommand`:** Executes Flask commands (`flask db init`, `migrate`, `upgrade`) on the server via SSH.
    *   Constructs a command sequence: `cd <flask_dir> && source <venv_path>/bin/activate && <flask_command>`.
    *   Uses `Invoke-Expression` to execute the `ssh user@host 'command sequence'` structure.
    *   Includes error checking.
*   **`Invoke-SshServiceRestart`:** A specific wrapper around `ssh` to execute `sudo systemctl restart <service>`. Includes error checking and a criticality flag (`-IsCritical`) to determine if failure is fatal or just a warning.
*   **`Remove-OldBackups`:** Executes a multi-line shell script via SSH to clean up old backups.
    *   Uses `find` to list items matching a prefix/suffix in a parent directory, sorted by modification time (oldest first).
    *   Calculates the number of items to delete based on `$MaxBackupsToKeep`.
    *   Iterates through the oldest items using `head` and `while read`, deleting directories (`rm -rf`) or files (`rm -f`) appropriately.
    *   Logs output from the remote script. Treats failure as non-fatal (warning).

## 7. Error Handling

*   **Fatal Errors:** Uses `Write-Error -ErrorAction Stop` for critical failures (configuration loading, confirmation mismatch, build failure, essential SSH command failures, rsync failures, critical service restart failures, user cancellation during migration). This terminates the script immediately.
*   **Non-Fatal Errors/Warnings:** Uses `Write-Warning` for less critical issues (backup cleanup failure, non-critical service restart failure) allowing the script to continue but alerting the user.
*   **Exit Codes:** Checks `$LASTEXITCODE` after external commands (`ssh`, `npm`, `git`, `wsl`, `rsync`) to determine success or failure.
*   **Try/Catch:** Used around blocks prone to exceptions (file I/O in `Import-EnvFile`, `Push-Location`/`Pop-Location`, configuration loading).
*   **Logging:** All errors and warnings are logged to the console and the build log file using `Tee-Object`. Specific logs are created for `npm build` errors.

## 8. Logging

*   **Mechanism:** Uses `Tee-Object -FilePath $BuildLog -Append` extensively to write script output (status messages, confirmations, warnings, errors) to both the console and a log file.
*   **Log File:** A unique log file is created for each run in the `build_logs` subdirectory, named `build_<Version>_<Timestamp>.log`.
*   **Specific Logs:** `npm run build` output (including errors) is also captured in a separate `npm_build_errors_<Version>_<Timestamp>.log` file.
*   **Verbosity:** Logs include executed commands (SSH, rsync), status updates, confirmation prompts/responses, and detailed error messages.

## 9. Security Considerations

*   **SSH Keys:** Relies heavily on passwordless SSH key authentication for both PowerShell->Server and WSL->Server connections. Secure management of these private keys is crucial. Keys should have strong passphrases (managed via ssh-agent).
*   **Passwordless Sudo:** Requires the deployment user on the server to have passwordless `sudo` access for specific `systemctl restart` commands. This should be configured carefully (e.g., via `/etc/sudoers.d/`) to limit access only to the necessary commands.
*   **WSL User Permissions:** The WSL user specified (`DEPLOYMENT_WSL_SSH_USER`) needs read access to the SSH private key (`DEPLOYMENT_WSL_SSH_KEY_PATH`) and execute permissions for `rsync` and `ssh`.
*   **Configuration File Security:** The `.deployment_env` file may contain sensitive information (server IP, user, paths). Access to this file should be restricted.
*   **Database Credentials:** The script assumes `mysqldump` can run without a password, typically configured via a `~/.my.cnf` file on the server for the deployment user. This file should have restricted permissions (e.g., `chmod 600`).

## 10. Future Enhancements (Based on TODOs)

*   Make configuration file paths (`.deployment_env`, project `.env`) configurable via script parameters or within `.deployment_env` itself.
*   Move hardcoded values like `$MaxBackups`, service names (`nginx`), service types (`Flask`, `Nginx`), and paths (`/bin/systemctl`) into the `.deployment_env` configuration file.
*   Refactor SSH command execution to use a single, more robust helper function (like the proposed `Invoke-SshCommand` detailed in previous discussions) consistently across the script.
*   Add parameter for specifying the SSH key path for direct PowerShell SSH connections if not using ssh-agent or default keys.


# QA Testing Guide: Automated Deployment Script (`deploy_to_droplet copy.ps1`)

## 1. Introduction

This guide outlines how to test the `deploy_to_droplet copy.ps1` PowerShell script. The script's purpose is to automate the process of deploying new versions of the Flask/React application to different server environments (like Development, QAS, or Production).

As a QA Tester, your goal is to ensure this script reliably deploys the correct version of the application, handles various scenarios gracefully (including errors), and leaves the server in a correct and functional state.

## 2. Prerequisites for Testing

Before you start testing the script, make sure you have the following:

*   **Access to the Script:** The latest version of `deploy_to_droplet copy.ps1`.
*   **Configuration Files:**
    *   `.deployment_env`: Contains server connection details and paths. You'll need the version relevant to the environment you are testing against (likely QAS or DEV). **Ensure you are NOT using Production settings for testing.**
    *   `.env`: Located in the local `satisfactory_tracker` project folder. Contains settings for the *local* build process.
*   **Local Environment:**
    *   Windows PC with PowerShell.
    *   WSL (Windows Subsystem for Linux) installed.
    *   Git installed.
    *   Node.js/npm installed.
    *   The application's source code checked out locally.
*   **Server Access:**
    *   SSH access details (Server IP, Deployment Username) for the target test environment (QAS/DEV).
    *   Confirmation that your SSH keys are set up correctly for passwordless login from both PowerShell and WSL to the test server.
*   **Application Knowledge:** Basic understanding of the application's functionality to verify successful deployment.
*   **Git Familiarity:** Ability to create and push Git tags (e.g., `v0.9.1`).

## 3. Understanding Configuration & Parameters

*   **`.deployment_env`:** This file tells the script *where* to deploy (Server IP, user, paths) and *which* services to manage. It has sections for different environments (PROD, QAS, DEV).
*   **`.env`:** This file tells the *local build process* which mode it's building for. **Crucially,** the settings here (`REACT_APP_RUN_MODE`, `REACT_APP_FLASK_ENV`) **must match** the target environment specified when running the script.
*   **Script Parameters:** When running the script, you **must** provide:
    *   `-Environment`: `QAS` or `DEV` (Use the environment you intend to test against). **NEVER use `PROD` unless specifically instructed for Production validation.**
    *   `-RunDBMigration`: `y` or `n`. Controls if database changes should be applied.
    *   `-Version`: The Git tag you want to deploy (e.g., `v0.9.1`). This tag **must exist** in the Git repository.

## 4. Test Scenarios

Here are key scenarios to test:

### 4.1. Happy Path - Full Deployment (with DB Migration)

*   **Goal:** Verify a standard deployment including database updates works correctly.
*   **Steps:**
    1.  Ensure the local `.env` file matches the target `-Environment` (e.g., both set for QAS).
    2.  Create and push a new Git tag (e.g., `v0.9.1`).
    3.  Run the script: `./deploy_to_droplet copy.ps1 -Environment QAS -RunDBMigration y -Version v0.9.1`
    4.  Answer `y` to the confirmation prompt.
    5.  Observe the script output for any errors.
    6.  If database migrations are generated, the script will pause. SSH into the server as instructed, **pretend** to review the migration script in the specified directory.
    7.  Answer `y` to the prompt asking if you want to apply the migration.
    8.  Wait for the script to complete.
*   **Expected Outcome:**
    *   Script finishes with "Deployment... completed successfully!" messages.
    *   No `FATAL` errors in the console output or the log file (`build_logs/build_*.log`).
    *   The application is accessible at its QAS/DEV URL and reflects the changes included in `v0.9.1`.
    *   Database changes (if any were generated) are applied correctly.
    *   Timestamped backups (Flask, Frontend, DB) exist in the backup directory on the server.
    *   If enough previous backups existed, older ones should be automatically deleted (keeping the configured maximum).

### 4.2. Happy Path - Deployment (No DB Migration)

*   **Goal:** Verify deployment works correctly when no database changes are needed.
*   **Steps:**
    1.  Follow steps 1-3 from Scenario 4.1, but run the script with: `./deploy_to_droplet copy.ps1 -Environment QAS -RunDBMigration n -Version v0.9.1`
    2.  Answer `y` to the confirmation prompt.
    3.  Wait for the script to complete.
*   **Expected Outcome:**
    *   Script finishes successfully.
    *   No `FATAL` errors.
    *   The application is accessible and updated to `v0.9.1`.
    *   **No** database migration steps are performed or prompted.
    *   Backups are created/managed as expected.

### 4.3. Error Handling - User Cancellation (Initial Prompt)

*   **Goal:** Verify the script stops safely if the user cancels at the start.
*   **Steps:**
    1.  Run the script with valid parameters (e.g., `./deploy_to_droplet copy.ps1 -Environment QAS -RunDBMigration n -Version v0.9.1`).
    2.  Answer `n` to the initial confirmation prompt.
*   **Expected Outcome:**
    *   Script immediately stops with a "Deployment cancelled by user" message.
    *   No build, backup, or deployment actions are performed.

### 4.4. Error Handling - Environment Mismatch

*   **Goal:** Verify the script prevents deployment if local settings don't match the target.
*   **Steps:**
    1.  Edit the local `.env` file. Change `REACT_APP_RUN_MODE` to something different from the `-Environment` you will use (e.g., set `REACT_APP_RUN_MODE=DEV` in the file).
    2.  Run the script targeting a different environment: `./deploy_to_droplet copy.ps1 -Environment QAS -RunDBMigration n -Version v0.9.1`
*   **Expected Outcome:**
    *   Script fails very early with a `FATAL` error message clearly stating the mismatch between local `.env` variables and the target environment.
    *   No build, backup, or deployment actions are performed.

### 4.5. Error Handling - Invalid Git Tag

*   **Goal:** Verify the script fails if the specified version tag doesn't exist.
*   **Steps:**
    1.  Run the script with a non-existent version tag: `./deploy_to_droplet copy.ps1 -Environment QAS -RunDBMigration n -Version v9.9.9`
*   **Expected Outcome:**
    *   Script fails during the "Checking out version..." step with a `FATAL` error related to `git checkout`.
    *   No build, backup, or deployment actions beyond the failed checkout are performed.

### 4.6. Error Handling - Build Failure

*   **Goal:** Verify the script stops if the local React build fails.
*   **Steps:**
    1.  Temporarily introduce a syntax error into the React application code (e.g., in `src/App.js`).
    2.  Run the script with valid parameters.
*   **Expected Outcome:**
    *   Script fails during the "Run React Build Locally" step with a `FATAL` error.
    *   The console output and the `npm_build_errors*.log` file should show the build error details.
    *   No backup or deployment actions are performed.
    *   *(Remember to fix the syntax error afterwards!)*

### 4.7. Error Handling - DB Migration Cancellation (Review Prompt)

*   **Goal:** Verify the script stops safely if the user cancels during the DB migration review.
*   **Steps:**
    1.  Run the script for a full deployment (Scenario 4.1).
    2.  When the script pauses for migration review, answer `n` to the prompt "Have you reviewed... and want to apply it?".
    3.  The script will then ask if you want to delete the generated script file. Test both scenarios:
        *   Answer `y` (delete).
        *   Answer `n` (keep).
*   **Expected Outcome:**
    *   Script halts with a "Deployment halted by user..." message.
    *   No database `upgrade` command is run.
    *   If 'y' was chosen for deletion, the script attempts to delete the latest file in the `migrations/versions` directory on the server and logs the outcome (success or warning).
    *   If 'n' was chosen for deletion, the generated script file remains on the server.

### 4.8. Backup Verification

*   **Goal:** Verify backups are created and old ones are removed correctly.
*   **Steps:**
    1.  After one or more successful deployments, SSH into the test server.
    2.  Navigate to the backup directory specified by `DEPLOYMENT_BACKUP_DIR` in `.deployment_env`.
    3.  List the contents (`ls -l`). Check for timestamped directories/files matching the prefixes (`flask_`, `frontend_`, `db_backup_`).
    4.  Run the deployment script several more times (e.g., 6-7 times) using different version tags.
    5.  After the last run, check the backup directory again.
*   **Expected Outcome:**
    *   Timestamped backups corresponding to the recent deployments exist.
    *   The total number of backups for each type (Flask, Frontend, DB) should not exceed the configured limit (`$MaxBackups`, currently hardcoded as 5 in the script - see TODO). The oldest backups beyond this limit should have been deleted.

### 4.9. Service Restart Verification

*   **Goal:** Verify the application services are restarted correctly.
*   **Steps:**
    1.  After a successful deployment, SSH into the test server.
    2.  Check the status of the Flask service: `sudo systemctl status <flask_service_name_from_config>`
    3.  Check the status of Nginx: `sudo systemctl status nginx`
*   **Expected Outcome:**
    *   Both commands should show the service is `active (running)`. Note that a failure to restart Nginx only produces a *warning* in the script log, while a Flask service failure is *fatal*.

## 5. How to Verify a Successful Deployment

*   **Check Script Output:** The script should end with success messages and no `FATAL` errors.
*   **Check Log File:** Review the `build_logs/build_*.log` file for details and ensure no unexpected warnings or errors occurred.
*   **Access Application:** Open the application URL for the tested environment (QAS/DEV).
*   **Verify Version:** Check if the deployed application shows the correct version (e.g., in a footer, about page, or by verifying a specific feature/fix known to be in that version).
*   **Verify DB Changes (if applicable):** If migrations were run, check the database directly or via the application's UI to confirm the expected changes were applied.
*   **Verify Backups:** Briefly check the backup directory on the server (see Scenario 4.8).

## 6. Troubleshooting Tips

*   **Read the Logs:** The `build_*.log` file is your best friend. Look for `FATAL` or `Warning` messages. Check the `npm_build_errors*.log` for build issues.
*   **Check SSH:** Can you manually SSH into the server from PowerShell? Can you manually SSH *from WSL* (`wsl ssh user@host`)? Key issues are common.
*   **Check Paths:** Double-check paths in `.deployment_env`. A typo can cause `rsync` or backup failures.
*   **Check Permissions:** If `rsync`, backups, or service restarts fail, the deployment user on the server might lack necessary permissions (write access to directories, sudo rights for `systemctl`).
*   **Check Service Status:** If the app doesn't respond after deployment, manually check the service status on the server (Scenario 4.9).

## 7. Reporting Issues

When reporting a bug found while testing this script:

*   **Clearly state the scenario** you were testing.
*   Provide the **exact command** you used to run the script (including parameters).
*   Describe the **expected result** and the **actual result**.
*   **Attach the full build log file** (`build_logs/build_*.log`).
*   If the build failed, also attach the `npm_build_errors*.log` file.
*   Include any relevant screenshots of console output or application behavior.


# 📦 Section 2 – New System Installation  
[🔝 Back to top](#table-of-contents)

---

## 1. Install Node.js  
➡️ https://nodejs.org/en/download/prebuilt-installer

---

## 2. Install MySQL (Community Version)  
➡️ https://dev.mysql.com/downloads/mysql/

---

## 3. Install MySQL Workbench  
➡️ https://dev.mysql.com/downloads/workbench/

---

## 4. Install Python (if necessary)  
➡️ https://www.python.org/downloads/windows/

> **Note:** If you're installing Node.js, it may also install Python depending on the selected options.

---

## 5. Set Up a Virtual Environment  


## 5.1. Create the virtual environment

```bash
python -m venv venv
```

---

## 5.2. Activate the virtual environment

### 🐧 Linux

```bash
source venv/bin/activate
```

### 🪟 Windows (PowerShell)

```powershell
.env\Scripts\Activate.ps1
```

### ⚠️ Windows PowerShell – Script Execution Error

If you see this error:

```
.env\Scripts\Activate.ps1 : File ... cannot be loaded because running scripts is disabled on this system.
```

It means your system’s PowerShell execution policy is blocking the activation script. To fix it, run this command:

1. **Allow PowerShell to run local scripts**:
```powershell
Set-ExecutionPolicy -Scope CurrentUser -ExecutionPolicy RemoteSigned
```

> This only needs to be done once per user.

This allows local scripts (like this one) to run while keeping protections against unsigned scripts from the internet.  

Safe for development use and only needs to be done once per user.

To check your current policy at any time:

```powershell
Get-ExecutionPolicy -List
```

---

## 6. Install Required pip Packages

```bash
pip install -r pip_requirements.txt
```

---

## 7. Install Required npm Packages
Change directory to the react code:

    cd .\satisfactory_tracker\

### 🪟 Windows (PowerShell)

```powershell
Get-Content -Path npm_requirements.txt | ForEach-Object {npm install $_}
```

### 🐧 Linux

```bash
xargs -a npm_requirements.txt npm install
```

---

That’s it! Your system is now ready to run the project.

This guide explains how to use the `start` scripts on both **Linux/macOS** and **Windows**.

---

## 🐧 Linux/macOS: `start.sh`

## First-time setup on Linux/macOS:
1. **Make the script executable** (only once per machine):

I have added an index to the file in git to mark it as executable but if it doesn't hold run the following command once:
```bash
chmod +x start.sh
```
Going forward, you just need to use this:

2. **Run it**:
```bash
./start.sh
```

---

## 🪟 Windows: `start.ps1`

## ✅ First-time setup
1. **Allow PowerShell to run local scripts**:
```powershell
Set-ExecutionPolicy -Scope CurrentUser -ExecutionPolicy RemoteSigned
```

> This only needs to be done once per user.

2. **Run the script**:
```powershell
.\start.ps1
```

---

## 🌐 Universal Script: `start`

Use the included `start` script to automatically detect the operating system and run the correct script.

## First-time setup on Linux/macOS:
1. **Make the script executable** (only once per machine):

I have added an index to the file in git to mark it as executable but if it doesn't hold run the following command once:

```bash
chmod +x start
```

Going forward, you just need to use this:

```bash
./start
```

On Windows, just run 
```powershell
start.ps1
```

---

## 📝 Summary

| Script        | Platform       | Setup Required?                      | Run with                |
|---------------|----------------|--------------------------------------|-------------------------|
| `start.sh`    | Linux/macOS    | `chmod +x start.sh` (once)           | `./start.sh`            |
| `start.ps1`   | Windows         | Set execution policy (once)          | `.\start.ps1`           |
| `start`       | Auto-detects   | `chmod +x start` (Linux/macOS only)  | `./start`               |

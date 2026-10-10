# AI_MIGRATION_GUIDE.md
# INSTRUCTIONS FOR AI AGENT ON NEW LAPTOP

> **NOTICE FOR AI AGENT:**
> The user wants you to completely set up and prepare this Unity 3D FPS Game on their new laptop automatically. Follow the steps below carefully.

---

## 1. Project Specifications
- **Project Name:** 3D GAME FPS
- **Engine Version:** `Unity 6000.3.6f1` (Unity 6)
- **Target Unity Project Folder:** `./3D GAME FPS` (The root repository contains project documentation, assets, and the inner Unity project directory `3D GAME FPS/` which contains `Assets`, `Packages`, and `ProjectSettings`).

---

## 2. Automated Setup Checklist for the AI

When the user asks you to set up this game on their machine, run the following steps:

### Step 1: Detect Unity Hub & Installed Editors
1. Check if Unity Hub CLI or installed Unity Editors exist:
   - Typical Windows Unity Hub path:
     `C:\Program Files\Unity Hub\Unity Hub.exe`
   - Check installed editors:
     ```powershell
     & "C:\Program Files\Unity Hub\Unity Hub.exe" -- --headless editors --installed
     ```
2. If `6000.3.6f1` is NOT installed:
   - Alert the user or install via Unity Hub CLI:
     ```powershell
     & "C:\Program Files\Unity Hub\Unity Hub.exe" -- --headless install --version 6000.3.6f1 --changeset bbb010bdb8a3
     ```
   - (Or advise the user to install Unity 6000.3.6f1 via Unity Hub GUI if headless mode is not permitted).

### Step 2: Register Project in Unity Hub
1. Register the project directory to Unity Hub:
   ```powershell
   $projectPath = (Resolve-Path ".\3D GAME FPS").Path
   & "C:\Program Files\Unity Hub\Unity Hub.exe" -- --headless project --add $projectPath
   ```

### Step 3: Verify Assets & Project Integrity
1. Confirm that `3D GAME FPS/Assets`, `3D GAME FPS/Packages/manifest.json`, and `3D GAME FPS/ProjectSettings/ProjectVersion.txt` are intact.
2. Verify Unity version in [ProjectVersion.txt](file:///ProjectSettings/ProjectVersion.txt):
   - It should state `m_EditorVersion: 6000.3.6f1`.

### Step 4: First-Time Project Build / Cache Warmup (Optional/Headless)
To compile scripts and let Unity generate the initial `Library` folder without hanging the UI:
```powershell
$unityExe = "C:\Program Files\Unity\Hub\Editor\6000.3.6f1\Editor\Unity.exe"
if (Test-Path $unityExe) {
    & $unityExe -batchmode -quit -projectPath "$projectPath" -logFile "$projectPath\Editor.log"
}
```

### Step 5: Report Status to User
Confirm to the user:
- Unity editor version is verified.
- Project is added to Unity Hub.
- Instructions on how to launch the project or open it directly in Unity Editor / Antigravity Unity MCP tools.

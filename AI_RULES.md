# AI_RULES.md
# 📜 Development & Coding Rules for AI Agents

> **MANDATORY RULES FOR ALL AI AGENTS WORKING ON THIS REPOSITORY**
> Violation of these rules leads to compile breaks, meta file corruptions, and broken prefab linkages.

---

## ⚠️ Rule 1: NEVER Break Unity Meta Files
1. Whenever creating a new `.cs`, `.prefab`, `.mat`, or asset file, Unity generates a corresponding `.meta` file containing a GUID.
2. **Never rename, delete, or move files** directly using raw shell commands without moving their corresponding `.meta` file, or do it within Unity Editor / Unity MCP.
3. If writing code via an external AI without Unity running, do not delete existing `.meta` files, or Unity will reassign new GUIDs and break all scene references!

---

## Rule 2: Strictly Respect the New Input System
- **DO NOT USE** legacy input (`Input.GetKey`, `Input.GetAxis`, `Input.GetMouseButtonDown`).
- **USE ONLY** the Unity New Input System (`UnityEngine.InputSystem`) and the centralized `InputManager.cs` / `PlayerController.cs`.

---

## Rule 3: Maintain Code Modular & Decoupled Architecture
- Avoid tight coupling (`GetComponent` in tight `Update()` loops is strictly prohibited).
- Cache references in `Awake()` or `Start()`.
- Use `CombatEvents.cs` or C# Actions/Events for decoupled communication across systems (e.g., Player health change -> HUD update).
- Singletons are centralized in `Assets/_Project/Scripts/Utils/Singleton.cs`. Do not invent conflicting singleton patterns.

---

## Rule 4: Folder Placement Rules
- All newly created game assets MUST be placed inside `Assets/_Project/<Category>/`.
- Do NOT dump files directly into `Assets/` or `Assets/Scenes/`.
- Large binary 3D assets (FBX, OBJ, blend) must be added with Git LFS if committing.

---

## Rule 5: Keep Git Clean & Avoid Committing Garbage Folders
- Never commit `Library/`, `Temp/`, `Logs/`, `Builds/`, or `UserSettings/`.
- Ensure `.gitignore` is always respected.
- Before committing any `.fbx` or large files, ensure Git LFS tracking is intact:
  ```powershell
  git lfs track "*.fbx"
  ```

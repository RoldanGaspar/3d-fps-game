# Contributing to 3D GAME FPS

Thank you for your interest in contributing! Here's how to get involved.

## 🛠️ Development Setup

1. Install **Unity 6** (6000.3.6f1) via Unity Hub.
2. Clone the repository and open the project in Unity.
3. Let Unity import all packages and compile scripts before making changes.

## 📐 Project Conventions

### Folder Structure

Organize assets under `Assets/` using this structure:

```
Assets/
├── Art/
│   ├── Materials/
│   ├── Models/
│   ├── Textures/
│   └── Animations/
├── Audio/
│   ├── Music/
│   └── SFX/
├── Prefabs/
│   ├── Player/
│   ├── Enemies/
│   ├── Weapons/
│   └── Environment/
├── Scenes/
├── Scripts/
│   ├── Player/
│   ├── Enemies/
│   ├── Weapons/
│   ├── UI/
│   └── Managers/
├── Settings/
└── UI/
    ├── Fonts/
    └── Sprites/
```

### Coding Standards

- **Language:** C# (Unity scripting)
- **Naming:**
  - Classes & methods: `PascalCase`
  - Private fields: `_camelCase` with underscore prefix
  - Public fields / properties: `PascalCase`
  - Local variables & parameters: `camelCase`
  - Constants: `UPPER_SNAKE_CASE`
- **One class per file**, filename matches the class name.
- **Use `[SerializeField]`** instead of public fields for Inspector-exposed variables.
- Add **XML documentation comments** (`///`) to all public methods.

### Scene & Prefab Rules

- Don't modify scenes other people are actively working on.
- Use **prefabs** for reusable objects; avoid duplicating GameObjects in scenes.
- Keep scene hierarchy organized with empty GameObjects as folders (e.g., `--- Environment ---`).

### Commit Messages

Use clear, descriptive commit messages:

```
feat: add player movement controller
fix: resolve camera jitter on low FPS
art: import enemy soldier model
audio: add gunshot sound effects
refactor: extract health system into base class
docs: update README with build instructions
```

## 🐛 Reporting Issues

When reporting a bug, include:

1. **Steps to reproduce** the issue
2. **Expected behavior** vs. **actual behavior**
3. **Unity console errors** (if any)
4. **Screenshots or video** (if visual)

## 🔀 Pull Request Process

1. Create a feature branch from `main`: `git checkout -b feature/your-feature`
2. Make your changes and test in the Unity Editor.
3. Ensure no new console errors or warnings.
4. Submit a PR with a clear description of changes.

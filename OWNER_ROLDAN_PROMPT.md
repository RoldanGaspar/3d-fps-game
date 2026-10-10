# OWNER_ROLDAN_PROMPT.md
# 📌 Cheat Sheet & Prompt Templates para kay Boss Roldan

> **PAANO GAMITIN:**
> Kapag binuksan mo ang project sa iyong bagong laptop gamit ang AI (Antigravity, Cursor, o ChatGPT/Claude), **kopyahin at i-paste mo lang ang isa sa mga prompt sa ibaba** depende sa gusto mong ipagawa.

---

## 🚀 Prompt 1: Initial Setup sa Bagong Laptop (Unang Pagbukas)
*Kopyahin at i-send ito agad sa AI pagka-clone ng repository sa bagong laptop:*

```text
Basahin mo muna ang PROJECT_CONTEXT.md, AI_RULES.md, at AI_MIGRATION_GUIDE.md. I-setup at i-verify mo ang Unity 6 project sa laptop na ito ayon sa guide (Unity 6000.3.6f1, Unity Hub registration, at cache warmup). Huwag mag-hallucinate ng bagong systems dahil kumpleto na ang core architecture natin.
```

---

## 🛠️ Prompt 2: Pagpapatuloy ng Development / Feature Request
*Gamitin ito kapag may ipapadagdag kang bagong feature sa laro:*

```text
Sumangguni sa ROADMAP.md, PROJECT_CONTEXT.md, at AI_RULES.md. Gusto kong ipagpatuloy ang pag-develop ng game. Para sa susunod nating task, gusto kong gawin: [ILAGAY DITO ANG GUSTO MONG GAWIN, hal. i-tweak ang weapon recoil / maglagay ng bagong enemy state]. Siguraduhing sundin ang New Input System, Assets/_Project/ folder rules, at huwag gagalawin ang .meta files nang walang pahintulot.
```

---

## 🔍 Prompt 3: Codebase Health & Integrity Check
*Gamitin ito kung matagal bago mo nabuksan o gusto mong i-check ng AI kung may error:*

```text
I-audit mo ang kasalukuyang state ng project batay sa PROJECT_CONTEXT.md. I-check kung may sirang scripts, missing meta files, o compilation errors bago tayo magpatuloy.
```

---

## 📦 Prompt 4: Git Push & Backup Reminder
*Gamitin ito bago mo i-off o i-close ang laptop para ma-save sa GitHub:*

```text
I-save at i-commit mo lahat ng bagong updates natin. Siguraduhing naka-track sa Git LFS ang mga bagong 3D models/FBX at i-push ito sa main branch ng GitHub repo natin.
```

---

## 📋 Mahahalagang Detalye ng Project (Reference mo lang)
- **GitHub Repo:** `https://github.com/RoldanGaspar/3d-fps-game.git`
- **Unity Version:** `Unity 6 (6000.3.6f1)`
- **Project Folder:** `3D GAME FPS`

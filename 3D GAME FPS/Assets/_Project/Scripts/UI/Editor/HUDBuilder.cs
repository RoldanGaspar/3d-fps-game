#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Project.UI;

public static class HUDBuilder
{
    [MenuItem("FPS Mobile/Rebuild Procedural Vector HUD")]
    public static void BuildHUD()
    {
        // 1. Locate or create Canvas
        var canvasObj = GameObject.Find("HUD_Canvas");
        if (canvasObj == null)
        {
            canvasObj = new GameObject("HUD_Canvas");
            var c = canvasObj.AddComponent<Canvas>();
            c.renderMode = RenderMode.ScreenSpaceCamera;
            c.worldCamera = Camera.main;
            c.planeDistance = 0.5f;
            c.sortingOrder = 100;
            var cs = canvasObj.AddComponent<CanvasScaler>();
            cs.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            cs.referenceResolution = new Vector2(1920, 1080);
            cs.matchWidthOrHeight = 0.5f;
            canvasObj.AddComponent<GraphicRaycaster>();
        }
        else
        {
            var c = canvasObj.GetComponent<Canvas>();
            c.renderMode = RenderMode.ScreenSpaceCamera;
            c.worldCamera = Camera.main;
            c.planeDistance = 0.5f;
            c.sortingOrder = 100;
        }

        // Clear existing children
        for (int i = canvasObj.transform.childCount - 1; i >= 0; i--)
        {
            Object.DestroyImmediate(canvasObj.transform.GetChild(i).gameObject);
        }

        // Load TMP font
        var fontAsset = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF.asset");

        // Palette
        Color navyBg = new Color(0.04f, 0.08f, 0.18f, 0.92f);       // Deep Dark Navy Blue
        Color neonCyan = new Color(0.0f, 0.95f, 1.0f, 1.0f);        // Electric Neon Cyan
        Color neonBlue = new Color(0.0f, 0.45f, 1.0f, 1.0f);        // Electric Neon Blue
        Color neonGreen = new Color(0.0f, 1.0f, 0.55f, 1.0f);       // Neon Emerald Green
        Color neonMagenta = new Color(0.85f, 0.0f, 1.0f, 1.0f);     // Neon Magenta
        Color neonRed = new Color(1.0f, 0.18f, 0.32f, 1.0f);        // Neon Crimson Red
        Color neonOrange = new Color(1.0f, 0.55f, 0.0f, 1.0f);      // Neon Orange
        Color neonGold = new Color(1.0f, 0.88f, 0.15f, 1.0f);       // Neon Gold
        Color neonIceWhite = new Color(0.95f, 0.98f, 1.0f, 1.0f);   // Neon Ice White

        // ----------------------------------------------------
        // 0. MOBILE CONTROLS ROOT GROUP (With CanvasGroup for Smart Auto-Fade)
        // ----------------------------------------------------
        var mobileGroupObj = new GameObject("MobileControlsGroup");
        mobileGroupObj.transform.SetParent(canvasObj.transform, false);
        var mgRt = mobileGroupObj.AddComponent<RectTransform>();
        SetRect(mgRt, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
        var mgCanvasGroup = mobileGroupObj.AddComponent<CanvasGroup>();
        mgCanvasGroup.alpha = 1.0f;
        mgCanvasGroup.interactable = true;
        mgCanvasGroup.blocksRaycasts = true;

        // ----------------------------------------------------
        // 0. TOUCH INPUT SECTORS (Move on Left, Look on Right)
        // ----------------------------------------------------
        // Left Sector: MoveTouchZone (X: 0.0 -> 0.40)
        var moveZoneObj = new GameObject("MoveTouchZone");
        moveZoneObj.transform.SetParent(mobileGroupObj.transform, false);
        var mzRt = moveZoneObj.AddComponent<RectTransform>();
        SetRect(mzRt, new Vector2(0f, 0f), new Vector2(0.40f, 0.90f), new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
        var mzImage = moveZoneObj.AddComponent<Image>();
        mzImage.color = Color.clear;
        var mtz = moveZoneObj.AddComponent<MoveTouchZone>();

        // Right Sector: TouchLookZone (X: 0.40 -> 1.0)
        var lookZoneObj = new GameObject("TouchLookZone");
        lookZoneObj.transform.SetParent(mobileGroupObj.transform, false);
        var lzRt = lookZoneObj.AddComponent<RectTransform>();
        SetRect(lzRt, new Vector2(0.40f, 0f), new Vector2(1f, 1f), new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
        var lzImage = lookZoneObj.AddComponent<Image>();
        lzImage.color = Color.clear;
        var tlz = lookZoneObj.AddComponent<TouchLookZone>();

        // ----------------------------------------------------
        // A. RETICLE / VECTOR CROSSHAIR (CENTER - Universal HUD)
        // ----------------------------------------------------
        var crosshairObj = new GameObject("VectorCrosshair");
        crosshairObj.transform.SetParent(canvasObj.transform, false);
        var chRt = crosshairObj.AddComponent<RectTransform>();
        SetRect(chRt, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(100, 100));
        var crosshairComp = crosshairObj.AddComponent<VectorCrosshair>();
        crosshairComp.color = neonCyan;
        crosshairComp.gap = 12f;
        crosshairComp.length = 18f;
        crosshairComp.thickness = 2.8f;
        crosshairComp.showCenterDot = true;
        crosshairComp.dotSize = 3.5f;

        // ----------------------------------------------------
        // B. TOP-LEFT: HEALTH PANEL (Universal HUD)
        // ----------------------------------------------------
        var healthPanel = new GameObject("HealthPanel");
        healthPanel.transform.SetParent(canvasObj.transform, false);
        var hpRt = healthPanel.AddComponent<RectTransform>();
        SetRect(hpRt, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(50, -45), new Vector2(410, 74));

        var hpVPanel = healthPanel.AddComponent<VectorPanel>();
        hpVPanel.backgroundColor = navyBg;
        hpVPanel.borderGradientStart = neonCyan;
        hpVPanel.borderGradientEnd = neonGreen;
        hpVPanel.borderWidth = 3.5f;
        hpVPanel.cornerCutSize = 14f;

        // Solid medical cross icon
        var iconCross = new GameObject("HealthIcon");
        iconCross.transform.SetParent(healthPanel.transform, false);
        var icRt = iconCross.AddComponent<RectTransform>();
        SetRect(icRt, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(30, 0), new Vector2(28, 28));
        var vi = iconCross.AddComponent<VectorSolidIcon>();
        vi.iconType = VectorSolidIcon.IconType.SolidMedicalCross;
        vi.color = neonGreen;

        // Health text ("100")
        var hpTextObj = new GameObject("HealthText");
        hpTextObj.transform.SetParent(healthPanel.transform, false);
        var htRt = hpTextObj.AddComponent<RectTransform>();
        SetRect(htRt, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(55, 0), new Vector2(90, 40));
        var htTMP = hpTextObj.AddComponent<TextMeshProUGUI>();
        if (fontAsset != null) htTMP.font = fontAsset;
        htTMP.text = "+100";
        htTMP.fontSize = 24;
        htTMP.fontStyle = FontStyles.Bold;
        htTMP.color = Color.white;
        htTMP.alignment = TextAlignmentOptions.MidlineLeft;

        // 10-Segmented Health Bar
        var hpBarObj = new GameObject("HealthBar");
        hpBarObj.transform.SetParent(healthPanel.transform, false);
        var hbRt = hpBarObj.AddComponent<RectTransform>();
        SetRect(hbRt, new Vector2(0f, 0.5f), new Vector2(1f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(65, 0), new Vector2(-155, 24));
        var hbComp = hpBarObj.AddComponent<VectorSegmentedBar>();
        hbComp.totalSegments = 10;
        hbComp.color = neonGreen;
        hbComp.fillGradientEnd = neonCyan;
        hbComp.emptyColor = new Color(0.04f, 0.08f, 0.18f, 0.85f);
        hbComp.fillPercent = 1f;
        hbComp.slantAngle = 14f;
        hbComp.spacing = 4f;

        // ----------------------------------------------------
        // C. TOP-RIGHT: AMMO PANEL (Universal HUD)
        // ----------------------------------------------------
        var ammoPanel = new GameObject("AmmoPanel");
        ammoPanel.transform.SetParent(canvasObj.transform, false);
        var amRt = ammoPanel.AddComponent<RectTransform>();
        SetRect(amRt, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-50, -45), new Vector2(295, 74));

        var amVPanel = ammoPanel.AddComponent<VectorPanel>();
        amVPanel.backgroundColor = navyBg;
        amVPanel.borderGradientStart = neonCyan;
        amVPanel.borderGradientEnd = neonMagenta;
        amVPanel.borderWidth = 3.5f;
        amVPanel.cornerCutSize = 14f;

        var ammoTextObj = new GameObject("AmmoText");
        ammoTextObj.transform.SetParent(ammoPanel.transform, false);
        var atRt = ammoTextObj.AddComponent<RectTransform>();
        SetRect(atRt, new Vector2(0.5f, 0.65f), new Vector2(0.5f, 0.65f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(260, 36));
        var atTMP = ammoTextObj.AddComponent<TextMeshProUGUI>();
        if (fontAsset != null) atTMP.font = fontAsset;
        atTMP.text = "30 / 90";
        atTMP.fontSize = 24;
        atTMP.fontStyle = FontStyles.Bold;
        atTMP.characterSpacing = 8f;
        atTMP.color = Color.white;
        atTMP.alignment = TextAlignmentOptions.Center;

        var subAmmoObj = new GameObject("WeaponSub");
        subAmmoObj.transform.SetParent(ammoPanel.transform, false);
        var subRt = subAmmoObj.AddComponent<RectTransform>();
        SetRect(subRt, new Vector2(0.5f, 0.26f), new Vector2(0.5f, 0.26f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(260, 18));
        var subTMP = subAmmoObj.AddComponent<TextMeshProUGUI>();
        if (fontAsset != null) subTMP.font = fontAsset;
        subTMP.text = "MK 14 II FULL AUTO";
        subTMP.fontSize = 10;
        subTMP.fontStyle = FontStyles.Bold;
        subTMP.color = neonCyan;
        subTMP.alignment = TextAlignmentOptions.Center;

        // ----------------------------------------------------
        // D. BOTTOM-CENTER: PARKOUR SPEED & STATE BAR (Universal HUD)
        // ----------------------------------------------------
        var parkourPanel = new GameObject("ParkourPanel");
        parkourPanel.transform.SetParent(canvasObj.transform, false);
        var pkRt = parkourPanel.AddComponent<RectTransform>();
        SetRect(pkRt, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0, 35), new Vector2(400, 50));

        var pkVPanel = parkourPanel.AddComponent<VectorPanel>();
        pkVPanel.backgroundColor = navyBg;
        pkVPanel.borderGradientStart = neonCyan;
        pkVPanel.borderGradientEnd = neonBlue;
        pkVPanel.borderWidth = 3f;
        pkVPanel.cornerCutSize = 10f;

        var stateTextObj = new GameObject("StateText");
        stateTextObj.transform.SetParent(parkourPanel.transform, false);
        var stRt = stateTextObj.AddComponent<RectTransform>();
        SetRect(stRt, new Vector2(0.5f, 0.74f), new Vector2(0.5f, 0.74f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(300, 24));
        var stTMP = stateTextObj.AddComponent<TextMeshProUGUI>();
        if (fontAsset != null) stTMP.font = fontAsset;
        stTMP.text = "IDLE";
        stTMP.fontSize = 17;
        stTMP.fontStyle = FontStyles.Bold;
        stTMP.characterSpacing = 14f;
        stTMP.color = neonCyan;
        stTMP.alignment = TextAlignmentOptions.Center;

        var speedBarObj = new GameObject("SpeedBar");
        speedBarObj.transform.SetParent(parkourPanel.transform, false);
        var spRt = speedBarObj.AddComponent<RectTransform>();
        SetRect(spRt, new Vector2(0.5f, 0.28f), new Vector2(0.5f, 0.28f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(380, 10));
        var sbComp = speedBarObj.AddComponent<VectorSegmentedBar>();
        sbComp.totalSegments = 24;
        sbComp.color = neonCyan;
        sbComp.fillGradientEnd = neonGreen;
        sbComp.emptyColor = new Color(0.02f, 0.04f, 0.10f, 0.85f);
        sbComp.fillPercent = 0.5f;
        sbComp.slantAngle = 0f;
        sbComp.spacing = 3f;

        // ----------------------------------------------------
        // E. BOTTOM-LEFT: ENHANCED MOBILE JOYSTICK (Inside MobileControlsGroup)
        // ----------------------------------------------------
        var joystickRoot = new GameObject("MobileJoystick");
        joystickRoot.transform.SetParent(mobileGroupObj.transform, false);
        var jsRt = joystickRoot.AddComponent<RectTransform>();
        SetRect(jsRt, new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(0.5f, 0.5f), new Vector2(175, 175), new Vector2(210, 210));

        // Outer Ring Disc
        var joyBaseDisc = joystickRoot.AddComponent<VectorCircleButton>();
        joyBaseDisc.backgroundColor = new Color(0.04f, 0.08f, 0.18f, 0.75f);
        joyBaseDisc.borderGradientTop = neonCyan;
        joyBaseDisc.borderGradientBottom = neonBlue;
        joyBaseDisc.borderThickness = 4.5f;

        // Inner Handle Knob
        var joyKnobObj = new GameObject("Handle");
        joyKnobObj.transform.SetParent(joystickRoot.transform, false);
        var jkRt = joyKnobObj.AddComponent<RectTransform>();
        SetRect(jkRt, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(88, 88));
        var joyKnobDisc = joyKnobObj.AddComponent<VectorCircleButton>();
        joyKnobDisc.backgroundColor = navyBg;
        joyKnobDisc.borderGradientTop = neonCyan;
        joyKnobDisc.borderGradientBottom = neonCyan;
        joyKnobDisc.borderThickness = 4f;
        joyKnobDisc.raycastTarget = false;

        // Center Solid Dot
        var joyDotObj = new GameObject("CenterDot");
        joyDotObj.transform.SetParent(joyKnobObj.transform, false);
        var jdRt = joyDotObj.AddComponent<RectTransform>();
        SetRect(jdRt, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(24, 24));
        var joyDot = joyDotObj.AddComponent<VectorRing>();
        joyDot.isFilled = true;
        joyDot.color = neonCyan;
        joyDot.raycastTarget = false;

        // Sprint Lock Node Badge (Vertical extension above joystick)
        var sprintNodeObj = new GameObject("SprintLockNode");
        sprintNodeObj.transform.SetParent(joystickRoot.transform, false);
        var snRt = sprintNodeObj.AddComponent<RectTransform>();
        SetRect(snRt, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, 120), new Vector2(50, 56));

        var snPanel = sprintNodeObj.AddComponent<VectorPanel>();
        snPanel.backgroundColor = navyBg;
        snPanel.borderGradientStart = neonGreen;
        snPanel.borderGradientEnd = neonCyan;
        snPanel.borderWidth = 3f;
        snPanel.cornerCutSize = 8f;
        snPanel.raycastTarget = false;

        // Solid sprint arrow icon inside node
        var snIcoObj = new GameObject("SprintIcon");
        snIcoObj.transform.SetParent(sprintNodeObj.transform, false);
        var sniRt = snIcoObj.AddComponent<RectTransform>();
        SetRect(sniRt, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, 5), new Vector2(26, 26));
        var snVi = snIcoObj.AddComponent<VectorSolidIcon>();
        snVi.iconType = VectorSolidIcon.IconType.SolidArrowUp;
        snVi.color = neonGreen;
        snVi.raycastTarget = false;

        // "LOCK" text
        var snLblObj = new GameObject("LockLabel");
        snLblObj.transform.SetParent(sprintNodeObj.transform, false);
        var snlRt = snLblObj.AddComponent<RectTransform>();
        SetRect(snlRt, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, -16), new Vector2(50, 14));
        var snLbl = snLblObj.AddComponent<TextMeshProUGUI>();
        if (fontAsset != null) snLbl.font = fontAsset;
        snLbl.text = "LOCK";
        snLbl.fontSize = 9;
        snLbl.fontStyle = FontStyles.Bold;
        snLbl.color = neonGreen;
        snLbl.alignment = TextAlignmentOptions.Center;
        snLbl.raycastTarget = false;

        // Attach FloatingJoystick script
        var fj = joystickRoot.AddComponent<FloatingJoystick>();
        var fjSO = new SerializedObject(fj);
        fjSO.FindProperty("_background").objectReferenceValue = jsRt;
        fjSO.FindProperty("_handle").objectReferenceValue = jkRt;
        fjSO.FindProperty("_sprintLockNode").objectReferenceValue = snRt;
        fjSO.FindProperty("_handleRange").floatValue = 70f;
        fjSO.ApplyModifiedProperties();

        // Wire MoveTouchZone
        if (mtz != null)
        {
            var mtzSO = new SerializedObject(mtz);
            mtzSO.FindProperty("_joystick").objectReferenceValue = fj;
            mtzSO.ApplyModifiedProperties();
        }

        // ----------------------------------------------------
        // F. BOTTOM-RIGHT: MOBILE ACTION BUTTONS (Inside MobileControlsGroup)
        // ----------------------------------------------------
        // 1. FIRE Button: 125x125, Gradient Neon Red -> Orange
        CreateButton(mobileGroupObj, fontAsset, "Btn_Fire", new Vector2(-140, 215), new Vector2(125, 125),
            navyBg, neonRed, neonOrange, 5.5f, "FIRE", VectorSolidIcon.IconType.SolidFireTarget, neonRed, 0.55f);

        // 2. JUMP Button: 100x100, Gradient Neon Cyan -> Blue
        CreateButton(mobileGroupObj, fontAsset, "Btn_Jump", new Vector2(-280, 225), new Vector2(100, 100),
            navyBg, neonCyan, neonBlue, 4.5f, "JUMP", VectorSolidIcon.IconType.SolidArrowUp, neonCyan, 0.52f);

        // 3. SLIDE Button: 95x95, Gradient Neon Gold -> Orange
        CreateButton(mobileGroupObj, fontAsset, "Btn_Slide", new Vector2(-280, 105), new Vector2(95, 95),
            navyBg, neonGold, neonOrange, 4.5f, "SLIDE", VectorSolidIcon.IconType.SolidArrowDown, neonGold, 0.52f);

        // 4. RELOAD Button: 90x90, Gradient Neon Ice White -> Blue
        CreateButton(mobileGroupObj, fontAsset, "Btn_Reload", new Vector2(-140, 80), new Vector2(90, 90),
            navyBg, neonIceWhite, neonBlue, 4.5f, "RELOAD", VectorSolidIcon.IconType.SolidReload, neonIceWhite, 0.50f);

        // 5. VIEW (FPS/TPS Switch) Button: 78x78, Gradient Neon Cyan -> Gold
        CreateButton(mobileGroupObj, fontAsset, "Btn_View", new Vector2(-60, 340), new Vector2(78, 78),
            navyBg, neonCyan, neonGold, 4.0f, "VIEW", VectorSolidIcon.IconType.SolidCameraView, neonCyan, 0.45f);

        // ----------------------------------------------------
        // G. HOOK UP REFERENCES & WIRE EVERYTHING
        // ----------------------------------------------------
        var hudManager = canvasObj.GetComponent<HUDManager>() ?? canvasObj.AddComponent<HUDManager>();
        var playerObj = GameObject.Find("Player");
        var playerHealth = playerObj != null ? playerObj.GetComponent<PlayerHealth>() : null;
        var playerMotor = playerObj != null ? playerObj.GetComponent<PlayerMotor>() : null;
        var playerLook = playerObj != null ? playerObj.GetComponent<PlayerLook>() : null;
        var playerController = playerObj != null ? playerObj.GetComponent<PlayerController>() : null;
        var playerStateMachine = playerObj != null ? playerObj.GetComponent<ParkourStateMachine>() : null;

        var hmSO = new SerializedObject(hudManager);
        hmSO.FindProperty("_crosshair").objectReferenceValue = crosshairComp;
        hmSO.FindProperty("_healthBar").objectReferenceValue = hbComp;
        hmSO.FindProperty("_speedBar").objectReferenceValue = sbComp;
        hmSO.FindProperty("_healthText").objectReferenceValue = htTMP;
        hmSO.FindProperty("_ammoText").objectReferenceValue = atTMP;
        var wsProp = hmSO.FindProperty("_weaponSubText");
        if (wsProp != null) wsProp.objectReferenceValue = subTMP;
        hmSO.FindProperty("_stateText").objectReferenceValue = stTMP;
        hmSO.FindProperty("_playerHealth").objectReferenceValue = playerHealth;
        hmSO.FindProperty("_playerMotor").objectReferenceValue = playerMotor;
        var psProp = hmSO.FindProperty("_parkourStateMachine");
        if (psProp != null) psProp.objectReferenceValue = playerStateMachine;
        hmSO.ApplyModifiedProperties();

        // ----------------------------------------------------
        // H. SMART INPUT MANAGER & ADAPTIVE HUD CONTROLLER
        // ----------------------------------------------------
        var inputMgrObj = GameObject.Find("InputManager");
        if (inputMgrObj == null)
        {
            inputMgrObj = new GameObject("InputManager");
        }
        var inputMgr = inputMgrObj.GetComponent<InputManager>() ?? inputMgrObj.AddComponent<InputManager>();
        var imSO = new SerializedObject(inputMgr);
        imSO.FindProperty("_virtualJoystick").objectReferenceValue = fj;
        imSO.ApplyModifiedProperties();

        // Adaptive HUD Controller on HUD_Canvas
        var adaptiveHud = canvasObj.GetComponent<AdaptiveHUDController>() ?? canvasObj.AddComponent<AdaptiveHUDController>();
        adaptiveHud.SetMobileControlsGroup(mgCanvasGroup);
        var ahSO = new SerializedObject(adaptiveHud);
        ahSO.FindProperty("_mobileControlsGroup").objectReferenceValue = mgCanvasGroup;
        ahSO.FindProperty("_desktopAlpha").floatValue = 0.0f; // Clean Auto-Fade
        ahSO.FindProperty("_touchAlpha").floatValue = 1.0f;
        ahSO.FindProperty("_fadeDuration").floatValue = 0.2f;
        ahSO.ApplyModifiedProperties();

        // Wire touch look zone
        if (playerLook != null && tlz != null)
        {
            var tlzSO = new SerializedObject(tlz);
            tlzSO.FindProperty("_playerLook").objectReferenceValue = playerLook;
            tlzSO.ApplyModifiedProperties();
        }

        // Wire player's joystick reference
        if (playerController != null)
        {
            var pcSO = new SerializedObject(playerController);
            pcSO.FindProperty("_joystick").objectReferenceValue = fj;
            var wm = playerObj != null ? playerObj.GetComponentInChildren<WeaponManager>() : null;
            if (wm != null)
            {
                var wmProp = pcSO.FindProperty("_weaponManager");
                if (wmProp != null) wmProp.objectReferenceValue = wm;
            }
            pcSO.ApplyModifiedProperties();

            // Wire Action Buttons (Pressed & Released)
            WireButton(GameObject.Find("Btn_Jump"), playerController.OnJumpPressed, playerController.OnJumpReleased);
            WireButton(GameObject.Find("Btn_Fire"), playerController.OnFirePressed, playerController.OnFireReleased);
            WireButton(GameObject.Find("Btn_Slide"), playerController.OnSlidePressed, playerController.OnSlideReleased);
            WireButton(GameObject.Find("Btn_Reload"), playerController.OnReloadPressed, playerController.OnReloadReleased);
            WireButton(GameObject.Find("Btn_View"), playerController.OnCameraTogglePressed, null);
        }

        // Wire Global Volume profile if missing
        var gv = GameObject.Find("Global Volume");
        if (gv != null)
        {
            var vol = gv.GetComponent<UnityEngine.Rendering.Volume>();
            if (vol != null && vol.sharedProfile == null)
            {
                var profile = AssetDatabase.LoadAssetAtPath<UnityEngine.Rendering.VolumeProfile>("Assets/_Project/Art/Materials/Environment/TestArenaVolumeProfile.asset");
                if (profile != null)
                {
                    vol.sharedProfile = profile;
                    EditorUtility.SetDirty(gv);
                }
            }
        }

        EditorUtility.SetDirty(canvasObj);
        Debug.Log("Procedural Vector HUD (with Smart Adaptive Dual Input) Built Successfully!");
    }

    private static void SetRect(RectTransform rt, Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot, Vector2 anchoredPos, Vector2 sizeDelta)
    {
        rt.anchorMin = anchorMin;
        rt.anchorMax = anchorMax;
        rt.pivot = pivot;
        rt.anchoredPosition = anchoredPos;
        rt.sizeDelta = sizeDelta;
    }

    private static GameObject CreateButton(GameObject parent, TMP_FontAsset fontAsset, string name, Vector2 anchoredPos, Vector2 size,
        Color navyBg, Color gradTop, Color gradBottom, float borderThick, string label, VectorSolidIcon.IconType iconType, Color iconColor, float iconScale)
    {
        var btnObj = new GameObject(name);
        btnObj.transform.SetParent(parent.transform, false);
        var bRt = btnObj.AddComponent<RectTransform>();
        SetRect(bRt, new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(0.5f, 0.5f), anchoredPos, size);

        var circleBtn = btnObj.AddComponent<VectorCircleButton>();
        circleBtn.backgroundColor = navyBg;
        circleBtn.borderGradientTop = gradTop;
        circleBtn.borderGradientBottom = gradBottom;
        circleBtn.borderThickness = borderThick;

        var icoObj = new GameObject("Icon");
        icoObj.transform.SetParent(btnObj.transform, false);
        var iRt = icoObj.AddComponent<RectTransform>();
        SetRect(iRt, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, 7), size * iconScale);
        var viComp = icoObj.AddComponent<VectorSolidIcon>();
        viComp.iconType = iconType;
        viComp.color = iconColor;
        viComp.raycastTarget = false;

        var lblObj = new GameObject("Label");
        lblObj.transform.SetParent(btnObj.transform, false);
        var lRt = lblObj.AddComponent<RectTransform>();
        SetRect(lRt, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, -size.y * 0.28f), new Vector2(size.x, 20));
        var lblTMP = lblObj.AddComponent<TextMeshProUGUI>();
        if (fontAsset != null) lblTMP.font = fontAsset;
        lblTMP.text = label;
        lblTMP.fontSize = 11;
        lblTMP.fontStyle = FontStyles.Bold;
        lblTMP.color = gradTop;
        lblTMP.alignment = TextAlignmentOptions.Center;
        lblTMP.raycastTarget = false;

        btnObj.AddComponent<TouchButton>();
        return btnObj;
    }

    private static void WireButton(GameObject btnObj, UnityEngine.Events.UnityAction actionPressed, UnityEngine.Events.UnityAction actionReleased = null)
    {
        if (btnObj == null) return;
        var tb = btnObj.GetComponent<TouchButton>();
        if (tb != null)
        {
            if (actionPressed != null)
            {
                UnityEditor.Events.UnityEventTools.RemovePersistentListener(tb.OnPressed, actionPressed);
                UnityEditor.Events.UnityEventTools.AddPersistentListener(tb.OnPressed, actionPressed);
            }
            if (actionReleased != null)
            {
                UnityEditor.Events.UnityEventTools.RemovePersistentListener(tb.OnReleased, actionReleased);
                UnityEditor.Events.UnityEventTools.AddPersistentListener(tb.OnReleased, actionReleased);
            }
        }
    }
}
#endif

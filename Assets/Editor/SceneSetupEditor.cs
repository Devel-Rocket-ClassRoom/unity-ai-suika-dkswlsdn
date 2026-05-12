using UnityEngine;
using UnityEditor;
using UnityEditor.Events;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using TMPro;

public static class SceneSetupEditor
{
    // ── 레이아웃 상수 ─────────────────────────────────────────────────────────
    // CAM_SIZE=7, 11:16(720×1280 ref) 기준:
    //   월드 높이=14, HUD 120px = 1.3125 월드 → HUD 하단 Y=5.69
    //   DROP_Y=5.0 → 화면 183px (HUD 120px 아래 ✓)
    const float CAM_SIZE   = 7f;
    const float BOX_HALF_W = 2.6f;
    const float FLOOR_Y    = -5.5f;
    const float WALL_THICK = 0.25f;
    const float GAMEOVER_Y = 4.5f;
    const float DROP_Y     = 5.0f;

    static readonly Color COL_WALL      = new Color(0.60f, 0.45f, 0.30f);
    static readonly Color COL_FLOOR     = new Color(0.55f, 0.40f, 0.27f);
    static readonly Color COL_LINE      = new Color(0.93f, 0.33f, 0.31f, 0.85f);
    static readonly Color COL_CREAM     = new Color(1.00f, 0.98f, 0.94f);
    static readonly Color COL_ORANGE    = new Color(1.00f, 0.80f, 0.44f, 0.95f);
    static readonly Color COL_BTN       = new Color(0.93f, 0.33f, 0.31f);
    static readonly Color COL_TXT_DARK  = new Color(0.18f, 0.14f, 0.10f);
    static readonly Color COL_TXT_BROWN = new Color(0.50f, 0.32f, 0.08f);

    [MenuItem("SuikaGame/① Setup Scene")]
    public static void SetupScene()
    {
        SetupCamera();
        CreateWalls();
        CreateGameOverLine();
        CreateGameManager();
        var dc = CreateDropController();
        CreateUI(dc);
        EnsureEventSystem();
        Debug.Log("[SuikaGame] 씬 배치 완료!");
    }

    // ── 카메라 ────────────────────────────────────────────────────────────────

    static void SetupCamera()
    {
        var camGO = GameObject.FindWithTag("MainCamera");
        if (camGO == null) { Debug.LogWarning("[SuikaGame] MainCamera 없음"); return; }

        var cam = camGO.GetComponent<Camera>();
        Undo.RecordObject(cam,              "Setup Camera");
        Undo.RecordObject(camGO.transform,  "Setup Camera Transform");

        cam.orthographic     = true;
        cam.orthographicSize = CAM_SIZE;
        cam.clearFlags       = CameraClearFlags.SolidColor;
        cam.backgroundColor  = COL_CREAM;
        camGO.transform.position = new Vector3(0f, 0f, -10f);
    }

    // ── 박스 벽면 ─────────────────────────────────────────────────────────────

    static void CreateWalls()
    {
        float h   = (GAMEOVER_Y - FLOOR_Y) + WALL_THICK * 2f;
        float midY = (GAMEOVER_Y + FLOOR_Y) * 0.5f;

        WallBox("Floor",
            new Vector3(0f, FLOOR_Y - WALL_THICK * 0.5f, 0f),
            new Vector3(BOX_HALF_W * 2f + WALL_THICK * 2f, WALL_THICK, 1f),
            COL_FLOOR);

        WallBox("Wall_Left",
            new Vector3(-(BOX_HALF_W + WALL_THICK * 0.5f), midY, 0f),
            new Vector3(WALL_THICK, h, 1f),
            COL_WALL);

        WallBox("Wall_Right",
            new Vector3(BOX_HALF_W + WALL_THICK * 0.5f, midY, 0f),
            new Vector3(WALL_THICK, h, 1f),
            COL_WALL);
    }

    static void WallBox(string name, Vector3 pos, Vector3 scale, Color color)
    {
        Kill(name);
        var go = new GameObject(name);
        go.transform.position   = pos;
        go.transform.localScale = scale;

        // Simple 모드: 스케일이 월드 크기를 결정 (Sliced 불필요)
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = WhiteSprite();
        sr.color  = color;

        go.AddComponent<BoxCollider2D>(); // size 기본값 (1,1) → scale 이 실제 크기

        Undo.RegisterCreatedObjectUndo(go, "Wall " + name);
    }

    // ── 게임오버 기준선 ───────────────────────────────────────────────────────

    static void CreateGameOverLine()
    {
        Kill("GameOverLine");
        var go = new GameObject("GameOverLine");
        go.transform.position   = new Vector3(0f, GAMEOVER_Y, 0f);
        go.transform.localScale = new Vector3(BOX_HALF_W * 2f, 0.04f, 1f);

        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite       = WhiteSprite();
        sr.color        = COL_LINE;
        sr.sortingOrder = 5;

        Undo.RegisterCreatedObjectUndo(go, "GameOverLine");
    }

    // ── GameManager ───────────────────────────────────────────────────────────

    static void CreateGameManager()
    {
        Kill("GameManager");
        var go = new GameObject("GameManager");
        var gm = go.AddComponent<GameManager>();

        var fp = new GameObject("FruitParent");
        fp.transform.SetParent(go.transform);
        gm.fruitParent         = fp.transform;
        gm.gameOverLineY       = GAMEOVER_Y;
        gm.gameOverGracePeriod = 1.5f;

        AutoDB(gm);
        Undo.RegisterCreatedObjectUndo(go, "GameManager");
    }

    // ── DropController ────────────────────────────────────────────────────────

    static DropController CreateDropController()
    {
        Kill("DropController");
        var go = new GameObject("DropController");
        var dc = go.AddComponent<DropController>();

        dc.dropY        = DROP_Y;
        dc.minX         = -(BOX_HALF_W - 0.05f);
        dc.maxX         =   BOX_HALF_W - 0.05f;
        dc.dropCooldown = 0.5f;
        dc.nextFruitIcon = null;

        // 드롭 인디케이터 (현재 과일 스프라이트)
        var ind = new GameObject("DropIndicator");
        ind.transform.SetParent(go.transform);
        ind.transform.position = new Vector3(0f, DROP_Y, 0f);
        var indSr = ind.AddComponent<SpriteRenderer>();
        indSr.sprite       = WhiteSprite();
        indSr.color        = new Color(1f, 1f, 1f, 0.75f);
        indSr.sortingOrder = 10;
        dc.dropIndicator   = ind.transform;

        // 조준 점선
        var line = new GameObject("AimLine");
        line.transform.SetParent(ind.transform);
        line.transform.localPosition = Vector3.zero;
        line.transform.localScale    = new Vector3(0.04f, 30f, 1f);
        var lineSr = line.AddComponent<SpriteRenderer>();
        lineSr.sprite       = WhiteSprite();
        lineSr.color        = new Color(0.93f, 0.33f, 0.31f, 0.28f);
        lineSr.sortingOrder = 9;

        AutoDB(dc);
        Undo.RegisterCreatedObjectUndo(go, "DropController");
        return dc;
    }

    // ── UI Canvas ─────────────────────────────────────────────────────────────
    //
    //  720×1280 기준 레이아웃
    //  ┌───────────────────────────────────────┐ Y=0
    //  │  SCORE(0~432px)  BEST(432~576px)  NEXT(576~720px)  │ 120px
    //  └───────────────────────────────────────┘ Y=120
    //                  게임 영역
    //            GameOverLine  Y=4.5 → 228px
    //                DROP_Y=5.0 → 183px  ← HUD 아래
    //  └───────────────────────────────────────┘ Y=1280

    static void CreateUI(DropController dc)
    {
        Kill("GameCanvas");

        // Canvas
        var root   = new GameObject("GameCanvas");
        var canvas = root.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;

        var scaler = root.AddComponent<CanvasScaler>();
        scaler.uiScaleMode         = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(720f, 1280f);
        scaler.matchWidthOrHeight  = 0.5f;

        root.AddComponent<GraphicRaycaster>();
        var uiMgr = root.AddComponent<UIManager>();

        // ── HUD 띠 (상단 전체 너비 × 120px) ──────────────────────────────────
        var hud = Img(root.transform, "HUD", COL_ORANGE);
        SetRect(hud,
            ancMin: new Vector2(0f, 1f), ancMax: new Vector2(1f, 1f),
            pivot:  new Vector2(0.5f, 1f),
            pos:    Vector2.zero,
            size:   new Vector2(0f, 120f));

        // SCORE 영역 (좌측 0~60%)
        var scoreArea = Img(hud.transform, "ScoreArea", Color.clear);
        SetRect(scoreArea,
            ancMin: new Vector2(0f, 0f), ancMax: new Vector2(0.6f, 1f),
            pivot:  new Vector2(0.5f, 0.5f), pos: Vector2.zero, size: Vector2.zero);

        TMP(scoreArea.transform, "ScoreLabel",
            new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
            new Vector2(0f, -10f), new Vector2(260f, 24f),
            "SCORE", 15f, new Color(1f, 1f, 1f, 0.8f), false);

        var scoreTxt = TMP(scoreArea.transform, "ScoreText",
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            new Vector2(0f, 6f), new Vector2(260f, 64f),
            "0", 46f, Color.white, true);
        uiMgr.scoreText = scoreTxt;

        // BEST 영역 (중간 60~78%)
        var bestArea = Img(hud.transform, "BestArea", Color.clear);
        SetRect(bestArea,
            ancMin: new Vector2(0.60f, 0f), ancMax: new Vector2(0.78f, 1f),
            pivot:  new Vector2(0.5f, 0.5f), pos: Vector2.zero, size: Vector2.zero);

        TMP(bestArea.transform, "BestLabel",
            new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
            new Vector2(0f, -12f), new Vector2(120f, 22f),
            "BEST", 13f, new Color(1f, 1f, 1f, 0.75f), false);

        var bestTxt = TMP(bestArea.transform, "BestText",
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            new Vector2(0f, 6f), new Vector2(120f, 36f),
            "0", 24f, Color.white, true);
        uiMgr.bestScoreText = bestTxt;

        // NEXT 영역 (우측 78~100%) + 배경 강조
        var nextArea = Img(hud.transform, "NextArea", new Color(0f, 0f, 0f, 0.12f));
        SetRect(nextArea,
            ancMin: new Vector2(0.78f, 0f), ancMax: new Vector2(1f, 1f),
            pivot:  new Vector2(0.5f, 0.5f), pos: Vector2.zero, size: Vector2.zero);

        TMP(nextArea.transform, "NextLabel",
            new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
            new Vector2(0f, -8f), new Vector2(140f, 20f),
            "NEXT", 13f, new Color(1f, 1f, 1f, 0.85f), true);

        var nextImgGO = new GameObject("NextFruitImage");
        nextImgGO.transform.SetParent(nextArea.transform, false);
        SetRect(nextImgGO,
            ancMin: new Vector2(0.1f, 0.06f), ancMax: new Vector2(0.9f, 0.70f),
            pivot:  new Vector2(0.5f, 0.5f), pos: Vector2.zero, size: Vector2.zero);
        var nextImg = nextImgGO.AddComponent<Image>();
        nextImg.preserveAspect = true;
        nextImg.raycastTarget  = false;
        if (dc != null) dc.nextFruitUIImage = nextImg;

        // ── 게임오버 패널 (중앙) ──────────────────────────────────────────────
        var pop = Img(root.transform, "GameOverPanel",
            new Color(1f, 0.976f, 0.941f, 0.97f));
        SetRect(pop,
            ancMin: new Vector2(0.5f, 0.5f), ancMax: new Vector2(0.5f, 0.5f),
            pivot:  new Vector2(0.5f, 0.5f),
            pos:    Vector2.zero, size: new Vector2(520f, 400f));
        pop.SetActive(false);
        uiMgr.gameOverPanel = pop;

        TMP(pop.transform, "GOTitle",
            new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
            new Vector2(0f, -44f), new Vector2(480f, 64f),
            "게임 오버", 44f, COL_BTN, true);

        var goScore = TMP(pop.transform, "GOScore",
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            new Vector2(0f, 52f), new Vector2(480f, 52f),
            "점수: 0", 30f, COL_TXT_DARK, false);
        uiMgr.gameOverScoreText = goScore;

        var goBest = TMP(pop.transform, "GOBest",
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            new Vector2(0f, -4f), new Vector2(480f, 44f),
            "최고 점수: 0", 24f, COL_TXT_BROWN, false);
        uiMgr.gameOverBestText = goBest;

        RestartBtn(pop.transform, uiMgr);

        Undo.RegisterCreatedObjectUndo(root, "GameCanvas");
    }

    // ── EventSystem ───────────────────────────────────────────────────────────

    static void EnsureEventSystem()
    {
        // FindFirstObjectByType: Unity 6 권장 API
        if (Object.FindFirstObjectByType<EventSystem>() != null) return;

        var go = new GameObject("EventSystem");
        go.AddComponent<EventSystem>();
        go.AddComponent<InputSystemUIInputModule>();
        Undo.RegisterCreatedObjectUndo(go, "EventSystem");
    }

    // ── 헬퍼 ─────────────────────────────────────────────────────────────────

    static GameObject Img(Transform parent, string name, Color color)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.AddComponent<RectTransform>();
        var img = go.AddComponent<Image>();
        img.color         = color;
        img.raycastTarget = false;
        return go;
    }

    static void SetRect(GameObject go,
        Vector2 ancMin, Vector2 ancMax, Vector2 pivot, Vector2 pos, Vector2 size)
    {
        var rt = go.GetComponent<RectTransform>();
        if (rt == null) rt = go.AddComponent<RectTransform>();
        rt.anchorMin        = ancMin;
        rt.anchorMax        = ancMax;
        rt.pivot            = pivot;
        rt.anchoredPosition = pos;
        rt.sizeDelta        = size;
    }

    static TextMeshProUGUI TMP(Transform parent, string name,
        Vector2 ancMin, Vector2 pivot, Vector2 pos, Vector2 size,
        string text, float fontSize, Color color, bool bold)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        SetRect(go, ancMin, ancMin, pivot, pos, size);
        var t = go.AddComponent<TextMeshProUGUI>();
        t.text          = text;
        t.fontSize      = fontSize;
        t.color         = color;
        t.fontStyle     = bold ? FontStyles.Bold : FontStyles.Normal;
        t.alignment     = TextAlignmentOptions.Center;
        t.raycastTarget = false;
        t.overflowMode  = TextOverflowModes.Ellipsis;
        return t;
    }

    static void RestartBtn(Transform parent, UIManager uiMgr)
    {
        var go = new GameObject("RestartButton");
        go.transform.SetParent(parent, false);
        SetRect(go,
            new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
            new Vector2(0.5f, 0f),
            new Vector2(0f, 40f), new Vector2(300f, 70f));
        var img = go.AddComponent<Image>();
        img.color = COL_BTN;

        var btn = go.AddComponent<Button>();
        var nav = btn.navigation;
        nav.mode       = Navigation.Mode.None;
        btn.navigation = nav;
        var cb = btn.colors;
        cb.normalColor      = COL_BTN;
        cb.highlightedColor = new Color(0.97f, 0.47f, 0.45f);
        cb.pressedColor     = new Color(0.78f, 0.22f, 0.20f);
        btn.colors = cb;

        var lblGO = new GameObject("Label");
        lblGO.transform.SetParent(go.transform, false);
        SetRect(lblGO,
            Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f),
            Vector2.zero, Vector2.zero);
        var t = lblGO.AddComponent<TextMeshProUGUI>();
        t.text      = "다시 시작";
        t.fontSize  = 28f;
        t.color     = Color.white;
        t.fontStyle = FontStyles.Bold;
        t.alignment = TextAlignmentOptions.Center;

        UnityEventTools.AddPersistentListener(btn.onClick, uiMgr.OnRestartButton);
    }

    static Sprite WhiteSprite() =>
        AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");

    static void Kill(string name)
    {
        var go = GameObject.Find(name);
        if (go != null) Object.DestroyImmediate(go);
    }

    static void AutoDB(GameManager gm)
    {
        var guids = AssetDatabase.FindAssets("t:FruitDatabase");
        if (guids.Length == 0) return;
        gm.fruitDatabase = AssetDatabase.LoadAssetAtPath<FruitDatabase>(
            AssetDatabase.GUIDToAssetPath(guids[0]));
    }

    static void AutoDB(DropController dc)
    {
        var guids = AssetDatabase.FindAssets("t:FruitDatabase");
        if (guids.Length == 0) return;
        dc.fruitDatabase = AssetDatabase.LoadAssetAtPath<FruitDatabase>(
            AssetDatabase.GUIDToAssetPath(guids[0]));
    }
}

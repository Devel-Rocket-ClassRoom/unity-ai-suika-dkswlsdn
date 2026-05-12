using UnityEngine;
using UnityEditor;
using UnityEditor.Events;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using TMPro;

/// <summary>
/// 에디터 메뉴 "SuikaGame/① Setup Scene" 을 실행하면
/// SampleScene에 맵(박스 벽)과 UI를 한 번에 배치합니다.
/// </summary>
public static class SceneSetupEditor
{
    // ── 레이아웃 상수 ─────────────────────────────────────────────────────────
    // CAM_SIZE=7 기준 11:16 화면:
    //   월드 Y ±7 (높이 14), X ±4.8125 (11:16)
    //   HUD 120px / 1280px * 14 = 1.3125 월드 유닛 → HUD 하단 Y = 5.6875
    //   DROP_Y=5.0 → 화면 상단에서 183px (HUD 120px 아래 ✓)
    const float CAM_SIZE   = 7f;
    const float BOX_HALF_W = 2.6f;
    const float FLOOR_Y    = -5.5f;
    const float WALL_THICK = 0.25f;
    const float GAMEOVER_Y = 4.5f;
    const float DROP_Y     = 5.0f;

    // 팔레트
    static readonly Color COL_WALL      = new Color(0.60f, 0.45f, 0.30f, 1f);
    static readonly Color COL_FLOOR     = new Color(0.55f, 0.40f, 0.27f, 1f);
    static readonly Color COL_LINE      = new Color(0.93f, 0.33f, 0.31f, 0.85f);
    static readonly Color COL_CREAM     = new Color(1.00f, 0.98f, 0.94f, 1f);
    static readonly Color COL_ORANGE    = new Color(1.00f, 0.80f, 0.44f, 0.95f);
    static readonly Color COL_BTN       = new Color(0.93f, 0.33f, 0.31f, 1f);
    static readonly Color COL_TXT_DARK  = new Color(0.18f, 0.14f, 0.10f, 1f);
    static readonly Color COL_TXT_BROWN = new Color(0.50f, 0.32f, 0.08f, 1f);

    // ─────────────────────────────────────────────────────────────────────────

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
        if (camGO == null) { Debug.LogWarning("MainCamera 없음"); return; }

        var cam = camGO.GetComponent<Camera>();
        cam.orthographic     = true;
        cam.orthographicSize = CAM_SIZE;
        cam.clearFlags       = CameraClearFlags.SolidColor;
        cam.backgroundColor  = COL_CREAM;
        camGO.transform.position = new Vector3(0, 0, -10);

        Undo.RecordObject(cam, "Setup Camera");
        Undo.RecordObject(camGO.transform, "Setup Camera Transform");
    }

    // ── 박스 벽면 ─────────────────────────────────────────────────────────────

    static void CreateWalls()
    {
        float wallHeight = (GAMEOVER_Y - FLOOR_Y) + WALL_THICK * 2f;
        float wallMidY   = (GAMEOVER_Y + FLOOR_Y) / 2f;

        MakeWallBox("Floor",
            pos:   new Vector3(0, FLOOR_Y - WALL_THICK * 0.5f, 0),
            scale: new Vector3(BOX_HALF_W * 2f + WALL_THICK * 2f, WALL_THICK, 1),
            color: COL_FLOOR);

        MakeWallBox("Wall_Left",
            pos:   new Vector3(-(BOX_HALF_W + WALL_THICK * 0.5f), wallMidY, 0),
            scale: new Vector3(WALL_THICK, wallHeight, 1),
            color: COL_WALL);

        MakeWallBox("Wall_Right",
            pos:   new Vector3(BOX_HALF_W + WALL_THICK * 0.5f, wallMidY, 0),
            scale: new Vector3(WALL_THICK, wallHeight, 1),
            color: COL_WALL);
    }

    static void MakeWallBox(string name, Vector3 pos, Vector3 scale, Color color)
    {
        DestroyExisting(name);
        var go = new GameObject(name);
        go.transform.position   = pos;
        go.transform.localScale = scale;

        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite   = WhiteSprite();
        sr.color    = color;
        sr.drawMode = SpriteDrawMode.Sliced;
        sr.size     = Vector2.one;

        var col = go.AddComponent<BoxCollider2D>();
        col.size = Vector2.one;

        Undo.RegisterCreatedObjectUndo(go, $"Create {name}");
    }

    // ── 게임오버 기준선 ───────────────────────────────────────────────────────

    static void CreateGameOverLine()
    {
        DestroyExisting("GameOverLine");
        var go = new GameObject("GameOverLine");
        go.transform.position   = new Vector3(0, GAMEOVER_Y, 0);
        go.transform.localScale = new Vector3(BOX_HALF_W * 2f, 0.04f, 1);

        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite       = WhiteSprite();
        sr.color        = COL_LINE;
        sr.sortingOrder = 5;

        Undo.RegisterCreatedObjectUndo(go, "Create GameOverLine");
    }

    // ── GameManager ───────────────────────────────────────────────────────────

    static void CreateGameManager()
    {
        DestroyExisting("GameManager");
        var go = new GameObject("GameManager");
        var gm = go.AddComponent<GameManager>();

        var fruitParent = new GameObject("FruitParent");
        fruitParent.transform.SetParent(go.transform);
        gm.fruitParent         = fruitParent.transform;
        gm.gameOverLineY       = GAMEOVER_Y;
        gm.gameOverGracePeriod = 1.5f;

        TryAssignFruitDatabase(gm);
        Undo.RegisterCreatedObjectUndo(go, "Create GameManager");
    }

    // ── DropController ────────────────────────────────────────────────────────

    static DropController CreateDropController()
    {
        DestroyExisting("DropController");
        var go = new GameObject("DropController");
        var dc = go.AddComponent<DropController>();

        dc.dropY        = DROP_Y;
        dc.minX         = -BOX_HALF_W + 0.05f;
        dc.maxX         =  BOX_HALF_W - 0.05f;
        dc.dropCooldown = 0.5f;
        dc.nextFruitIcon = null;   // UI Image 방식 사용, 월드 아이콘 미사용

        // DropIndicator: 현재 과일 스프라이트 + 조준선
        var indGO = new GameObject("DropIndicator");
        indGO.transform.SetParent(go.transform);
        indGO.transform.position = new Vector3(0, DROP_Y, 0);
        var indSr = indGO.AddComponent<SpriteRenderer>();
        indSr.sprite       = WhiteSprite();
        indSr.color        = new Color(1, 1, 1, 0.75f);
        indSr.sortingOrder = 10;
        dc.dropIndicator   = indGO.transform;

        // 조준 점선
        var lineGO = new GameObject("AimLine");
        lineGO.transform.SetParent(indGO.transform);
        lineGO.transform.localPosition = Vector3.zero;
        lineGO.transform.localScale    = new Vector3(0.04f, 30f, 1);
        var lineSr = lineGO.AddComponent<SpriteRenderer>();
        lineSr.sprite       = WhiteSprite();
        lineSr.color        = new Color(0.93f, 0.33f, 0.31f, 0.30f);
        lineSr.sortingOrder = 9;

        TryAssignFruitDatabase(dc);
        Undo.RegisterCreatedObjectUndo(go, "Create DropController");
        return dc;
    }

    // ── UI Canvas ─────────────────────────────────────────────────────────────
    //
    // 레이아웃 (720×1280 기준):
    //
    //  ┌──────────────────────────────────────────────────┐ ← 0px
    //  │  SCORE          BEST          NEXT               │
    //  │  [  0  ]       [  0 ]        [🍒]   120px HUD   │
    //  └──────────────────────────────────────────────────┘ ← 120px (Y=5.69)
    //  │                                                  │
    //  │               ( 게임 영역 )                       │  DROP_Y=5.0 → 183px
    //  │               GameOverLine Y=4.5 → 228px        │
    //  │                                                  │
    //  └──────────────────────────────────────────────────┘
    //
    //  ┌───────────────────────────────────────────────────┐  게임오버 팝업 (중앙)
    //  │  게임 오버   점수: 0   최고: 0   [다시 시작]        │
    //  └───────────────────────────────────────────────────┘

    static void CreateUI(DropController dc)
    {
        DestroyExisting("GameCanvas");

        var canvasGO = new GameObject("GameCanvas");
        var canvas   = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;

        var scaler = canvasGO.AddComponent<CanvasScaler>();
        scaler.uiScaleMode         = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(720, 1280);
        scaler.matchWidthOrHeight  = 0.5f;

        canvasGO.AddComponent<GraphicRaycaster>();
        var uiMgr = canvasGO.AddComponent<UIManager>();

        // ── 상단 HUD (전체 너비 × 120px) ──────────────────────────────────────
        // anchorMin/Max (0,1)~(1,1): 화면 상단에 가로로 꽉 찬 띠
        var topPanel = MakeStretchPanel(canvasGO.transform, "TopPanel", COL_ORANGE,
            ancMin: new Vector2(0, 1), ancMax: new Vector2(1, 1),
            pivot:  new Vector2(0.5f, 1),
            height: 120);

        // 왼쪽 60% — SCORE
        var scoreSection = MakeStretchPanel(topPanel.transform, "ScoreSection",
            new Color(0, 0, 0, 0),
            ancMin: new Vector2(0, 0), ancMax: new Vector2(0.6f, 1),
            pivot: new Vector2(0, 0.5f), height: 0);

        MakeTMP(scoreSection.transform, "ScoreLabel",
            anchor: new Vector2(0.5f, 1), pivot: new Vector2(0.5f, 1),
            aPos: new Vector2(0, -10), size: new Vector2(300, 26),
            text: "SCORE", fontSize: 16, color: new Color(1, 1, 1, 0.8f));

        var scoreText = MakeTMP(scoreSection.transform, "ScoreText",
            anchor: new Vector2(0.5f, 0.5f), pivot: new Vector2(0.5f, 0.5f),
            aPos: new Vector2(0, 4), size: new Vector2(300, 64),
            text: "0", fontSize: 48, color: Color.white, bold: true);
        uiMgr.scoreText = scoreText;

        // 중간 20% — BEST
        var bestSection = MakeStretchPanel(topPanel.transform, "BestSection",
            new Color(0, 0, 0, 0),
            ancMin: new Vector2(0.55f, 0), ancMax: new Vector2(0.75f, 1),
            pivot: new Vector2(0.5f, 0.5f), height: 0);

        MakeTMP(bestSection.transform, "BestLabel",
            anchor: new Vector2(0.5f, 1), pivot: new Vector2(0.5f, 1),
            aPos: new Vector2(0, -12), size: new Vector2(130, 22),
            text: "BEST", fontSize: 14, color: new Color(1, 1, 1, 0.75f));

        var bestText = MakeTMP(bestSection.transform, "BestScoreText",
            anchor: new Vector2(0.5f, 0.5f), pivot: new Vector2(0.5f, 0.5f),
            aPos: new Vector2(0, 4), size: new Vector2(130, 40),
            text: "0", fontSize: 26, color: Color.white, bold: true);
        uiMgr.bestScoreText = bestText;

        // 오른쪽 25% — NEXT (레이블 + 과일 이미지)
        var nextSection = MakeStretchPanel(topPanel.transform, "NextSection",
            new Color(0, 0, 0, 0.12f),
            ancMin: new Vector2(0.75f, 0), ancMax: new Vector2(1f, 1),
            pivot: new Vector2(0.5f, 0.5f), height: 0);

        MakeTMP(nextSection.transform, "NextLabel",
            anchor: new Vector2(0.5f, 1), pivot: new Vector2(0.5f, 1),
            aPos: new Vector2(0, -10), size: new Vector2(100, 28),
            text: "NEXT", fontSize: 18, color: new Color(1,1,1,0.85f), bold: true,
            align: TextAlignmentOptions.Center);

        // NEXT 패널 내부 과일 이미지 (#4)
        var nextImgGO = new GameObject("NextFruitImage");
        nextImgGO.transform.SetParent(nextPanel.transform, false);
        var nextImgRt = nextImgGO.AddComponent<RectTransform>();
        nextImgRt.anchorMin        = new Vector2(0.1f, 0.08f);
        nextImgRt.anchorMax        = new Vector2(0.9f, 0.72f);
        nextImgRt.offsetMin        = Vector2.zero;
        nextImgRt.offsetMax        = Vector2.zero;
        var nextImg = nextImgGO.AddComponent<Image>();
        nextImg.preserveAspect     = true;
        nextImg.raycastTarget      = false;
        if (dc != null) dc.nextFruitUIImage = nextImg;

        // ── 게임오버 패널 ───────────────────────────────────────────────────
        var goPanel = MakeImage(canvasGO.transform, "GameOverPanel", COL_CREAM,
            ancMin: new Vector2(0.5f, 0.5f), ancMax: new Vector2(0.5f, 0.5f),
            pivot:  new Vector2(0.5f, 0.5f),
            aPos: Vector2.zero, size: new Vector2(520, 400));

        uiMgr.gameOverPanel = goPanel;
        goPanel.SetActive(false);

        MakeTMP(goPanel.transform, "GOTitle",
            anchor: new Vector2(0.5f, 1), pivot: new Vector2(0.5f, 1),
            aPos: new Vector2(0, -44), size: new Vector2(480, 64),
            text: "게임 오버", fontSize: 44, color: COL_BTN, bold: true);

        var goScore = MakeTMP(goPanel.transform, "GOScoreText",
            anchor: new Vector2(0.5f, 0.5f), pivot: new Vector2(0.5f, 0.5f),
            aPos: new Vector2(0, 50), size: new Vector2(480, 54),
            text: "점수: 0", fontSize: 30, color: COL_TXT_DARK);
        uiMgr.gameOverScoreText = goScore;

        var goBest = MakeTMP(goPanel.transform, "GOBestText",
            anchor: new Vector2(0.5f, 0.5f), pivot: new Vector2(0.5f, 0.5f),
            aPos: new Vector2(0, -6), size: new Vector2(480, 44),
            text: "최고 점수: 0", fontSize: 24, color: COL_TXT_BROWN);
        uiMgr.gameOverBestText = goBest;

        MakeRestartButton(goPanel.transform, uiMgr);

        Undo.RegisterCreatedObjectUndo(canvasGO, "Create UI Canvas");
    }

    // ── EventSystem ───────────────────────────────────────────────────────────

    static void EnsureEventSystem()
    {
        if (Object.FindObjectOfType<EventSystem>() != null) return;
        var go = new GameObject("EventSystem");
        go.AddComponent<EventSystem>();
        go.AddComponent<InputSystemUIInputModule>();
        Undo.RegisterCreatedObjectUndo(go, "Create EventSystem");
    }

    // ── UI 헬퍼 ───────────────────────────────────────────────────────────────

    // 앵커 스트레치 패널 (sizeDelta.x=0 → 앵커 범위로 너비 결정, height=0이면 높이도 스트레치)
    static GameObject MakeStretchPanel(Transform parent, string name, Color color,
        Vector2 ancMin, Vector2 ancMax, Vector2 pivot, float height)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        var rt = go.AddComponent<RectTransform>();
        rt.anchorMin        = ancMin;
        rt.anchorMax        = ancMax;
        rt.pivot            = pivot;
        rt.anchoredPosition = Vector2.zero;
        rt.sizeDelta        = height > 0 ? new Vector2(0, height) : Vector2.zero;
        var img = go.AddComponent<Image>();
        img.color         = color;
        img.raycastTarget = false;
        return go;
    }

    static GameObject MakeImage(Transform parent, string name, Color color,
        Vector2 ancMin, Vector2 ancMax, Vector2 pivot, Vector2 aPos, Vector2 size)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        var rt = go.AddComponent<RectTransform>();
        rt.anchorMin        = ancMin;
        rt.anchorMax        = ancMax;
        rt.pivot            = pivot;
        rt.anchoredPosition = aPos;
        rt.sizeDelta        = size;
        var img = go.AddComponent<Image>();
        img.color         = color;
        img.raycastTarget = false;
        return go;
    }

    static TextMeshProUGUI MakeTMP(Transform parent, string name,
        Vector2 anchor, Vector2 pivot, Vector2 aPos, Vector2 size,
        string text, float fontSize, Color color, bool bold = false,
        TextAlignmentOptions align = TextAlignmentOptions.Center)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        var rt = go.AddComponent<RectTransform>();
        rt.anchorMin        = anchor;
        rt.anchorMax        = anchor;
        rt.pivot            = pivot;
        rt.anchoredPosition = aPos;
        rt.sizeDelta        = size;
        var tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.text         = text;
        tmp.fontSize     = fontSize;
        tmp.color        = color;
        tmp.fontStyle    = bold ? FontStyles.Bold : FontStyles.Normal;
        tmp.alignment    = align;
        tmp.raycastTarget = false;
        tmp.overflowMode = TextOverflowModes.Ellipsis;
        return tmp;
    }

    static void MakeRestartButton(Transform parent, UIManager uiMgr)
    {
        var go = new GameObject("RestartButton");
        go.transform.SetParent(parent, false);
        var rt = go.AddComponent<RectTransform>();
        rt.anchorMin        = new Vector2(0.5f, 0);
        rt.anchorMax        = new Vector2(0.5f, 0);
        rt.pivot            = new Vector2(0.5f, 0);
        rt.anchoredPosition = new Vector2(0, 40);
        rt.sizeDelta        = new Vector2(300, 70);
        var img = go.AddComponent<Image>();
        img.color = COL_BTN;
        var btn = go.AddComponent<Button>();
        var nav = btn.navigation;
        nav.mode       = Navigation.Mode.None;
        btn.navigation = nav;
        var cb = btn.colors;
        cb.normalColor      = COL_BTN;
        cb.highlightedColor = new Color(0.97f, 0.47f, 0.45f, 1f);
        cb.pressedColor     = new Color(0.78f, 0.22f, 0.20f, 1f);
        btn.colors          = cb;

        var txtGO = new GameObject("Text");
        txtGO.transform.SetParent(go.transform, false);
        var txtRt = txtGO.AddComponent<RectTransform>();
        txtRt.anchorMin = Vector2.zero;
        txtRt.anchorMax = Vector2.one;
        txtRt.offsetMin = Vector2.zero;
        txtRt.offsetMax = Vector2.zero;
        var tmp = txtGO.AddComponent<TextMeshProUGUI>();
        tmp.text      = "다시 시작";
        tmp.fontSize  = 28;
        tmp.color     = Color.white;
        tmp.fontStyle = FontStyles.Bold;
        tmp.alignment = TextAlignmentOptions.Center;

        UnityEventTools.AddPersistentListener(btn.onClick, uiMgr.OnRestartButton);
    }

    // ── 유틸 ─────────────────────────────────────────────────────────────────

    static Sprite WhiteSprite() =>
        AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");

    static void DestroyExisting(string name)
    {
        var go = GameObject.Find(name);
        if (go != null) Object.DestroyImmediate(go);
    }

    static void TryAssignFruitDatabase(GameManager gm)
    {
        var guids = AssetDatabase.FindAssets("t:FruitDatabase");
        if (guids.Length == 0) return;
        gm.fruitDatabase = AssetDatabase.LoadAssetAtPath<FruitDatabase>(
            AssetDatabase.GUIDToAssetPath(guids[0]));
    }

    static void TryAssignFruitDatabase(DropController dc)
    {
        var guids = AssetDatabase.FindAssets("t:FruitDatabase");
        if (guids.Length == 0) return;
        dc.fruitDatabase = AssetDatabase.LoadAssetAtPath<FruitDatabase>(
            AssetDatabase.GUIDToAssetPath(guids[0]));
    }
}

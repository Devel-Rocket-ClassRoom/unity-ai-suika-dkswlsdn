using System.IO;
using UnityEditor;
using UnityEngine;

/// <summary>
/// 에디터 메뉴 "SuikaGame/② Generate Fruit Assets" 실행 시
/// Assets/Fruits/Textures/ 에 원형 스프라이트를,
/// Assets/Fruits/Data/ 에 FruitData ScriptableObject 11개와
/// FruitDatabase 를 자동 생성합니다.
/// </summary>
public static class FruitAssetGenerator
{
    const int TEX  = 128;           // 텍스처 해상도
    const int PPU  = 100;           // Pixels Per Unit

    // ── 과일 정의 (이름, 스테이지, 반지름, 점수, 낙하가능, 메인색, 테두리색) ──────
    static readonly FruitDef[] Defs =
    {
        new("Cherry",      1, 0.20f,  1, true,
            new Color32(255, 120, 140, 255), new Color32(200,  50,  75, 255)),
        new("Strawberry",  2, 0.30f,  3, true,
            new Color32(240,  60,  90, 255), new Color32(180,  20,  50, 255)),
        new("Grapes",      3, 0.40f,  6, true,
            new Color32(150,  80, 200, 255), new Color32( 90,  30, 140, 255)),
        new("Dekopon",     4, 0.52f, 10, true,
            new Color32(255, 165,  30, 255), new Color32(210, 110,   0, 255)),
        new("Persimmon",   5, 0.64f, 15, true,
            new Color32(255, 110,  50, 255), new Color32(200,  65,  15, 255)),
        new("Apple",       6, 0.78f, 21, false,
            new Color32(220,  55,  60, 255), new Color32(160,  20,  25, 255)),
        new("Pear",        7, 0.93f, 28, false,
            new Color32(185, 220, 130, 255), new Color32(110, 160,  60, 255)),
        new("Peach",       8, 1.08f, 36, false,
            new Color32(255, 185, 155, 255), new Color32(220, 120,  90, 255)),
        new("Pineapple",   9, 1.24f, 45, false,
            new Color32(255, 215,  50, 255), new Color32(190, 150,   0, 255)),
        new("Melon",      10, 1.42f, 55, false,
            new Color32(120, 200, 120, 255), new Color32( 50, 130,  50, 255)),
        new("Watermelon", 11, 1.62f, 66, false,
            new Color32( 80, 175,  80, 255), new Color32( 30, 110,  30, 255)),
    };

    // ─────────────────────────────────────────────────────────────────────────

    [MenuItem("SuikaGame/② Generate Fruit Assets")]
    public static void GenerateAll()
    {
        EnsureFolder("Assets/Fruits");
        EnsureFolder("Assets/Fruits/Textures");
        EnsureFolder("Assets/Fruits/Data");

        var dataAssets = new FruitData[Defs.Length];
        for (int i = 0; i < Defs.Length; i++)
            dataAssets[i] = CreateFruitAsset(Defs[i]);

        CreateDatabase(dataAssets);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("[SuikaGame] 과일 에셋 11개 + FruitDatabase 생성 완료!");
    }

    // ── 개별 과일 생성 ────────────────────────────────────────────────────────

    static FruitData CreateFruitAsset(FruitDef def)
    {
        // 1. 스프라이트 생성
        var sprite = GenerateSprite(def);

        // 2. FruitData ScriptableObject
        string dataPath = $"Assets/Fruits/Data/Fruit_{def.Stage:D2}_{def.Name}.asset";
        var existing = AssetDatabase.LoadAssetAtPath<FruitData>(dataPath);
        FruitData data = existing != null ? existing : ScriptableObject.CreateInstance<FruitData>();

        data.fruitName  = def.Name;
        data.stage      = def.Stage;
        data.radius     = def.Radius;
        data.mergeScore = def.Score;
        data.canDrop    = def.CanDrop;
        data.color      = def.Main;
        data.sprite     = sprite;

        if (existing == null)
            AssetDatabase.CreateAsset(data, dataPath);
        else
            EditorUtility.SetDirty(data);

        return data;
    }

    // ── 원형 스프라이트 생성 ──────────────────────────────────────────────────

    static Sprite GenerateSprite(FruitDef def)
    {
        string texPath = $"Assets/Fruits/Textures/Fruit_{def.Stage:D2}_{def.Name}.png";

        // 텍스처 그리기
        var tex  = new Texture2D(TEX, TEX, TextureFormat.RGBA32, false);
        var pixels = new Color32[TEX * TEX];

        float cx     = TEX / 2f - 0.5f;
        float cy     = TEX / 2f - 0.5f;
        float outerR = TEX / 2f - 2f;           // 바깥 반지름
        float borderW = outerR * 0.085f;         // 테두리 두께
        float innerR  = outerR - borderW;

        for (int y = 0; y < TEX; y++)
        {
            for (int x = 0; x < TEX; x++)
            {
                float dx   = x - cx;
                float dy   = y - cy;
                float dist = Mathf.Sqrt(dx * dx + dy * dy);

                Color32 col = new Color32(0, 0, 0, 0); // 투명

                if (dist < outerR + 0.5f)
                {
                    float alpha = Mathf.Clamp01(outerR + 0.5f - dist); // 안티앨리어싱

                    if (dist < innerR)
                    {
                        // 내부: 중심은 살짝 밝게 (셰이딩)
                        float brightness = Mathf.Lerp(1.18f, 0.88f, dist / innerR);
                        col = Multiply(def.Main, brightness);
                    }
                    else
                    {
                        // 테두리
                        float t = (dist - innerR) / borderW;
                        col = LerpColor32(def.Main, def.Border, t);
                    }

                    col.a = (byte)(alpha * 255);
                }

                // 수박: 세로 줄무늬 오버레이
                if (def.Stage == 11 && dist < innerR * 0.96f)
                    col = WatermelonStripe(x, y, cx, cy, dist, innerR, col);

                pixels[y * TEX + x] = col;
            }
        }

        // 눈 그리기
        DrawEye(pixels, cx - outerR * 0.28f, cy + outerR * 0.22f, outerR * 0.09f, def.Border);
        DrawEye(pixels, cx + outerR * 0.28f, cy + outerR * 0.22f, outerR * 0.09f, def.Border);

        // 웃음 그리기
        DrawSmile(pixels, cx, cy - outerR * 0.12f, outerR * 0.30f, outerR * 0.07f, def.Border);

        tex.SetPixels32(pixels);
        tex.Apply();

        // 파일로 저장
        string absPath = Path.Combine(Application.dataPath,
            $"Fruits/Textures/Fruit_{def.Stage:D2}_{def.Name}.png");
        File.WriteAllBytes(absPath, tex.EncodeToPNG());
        Object.DestroyImmediate(tex);

        AssetDatabase.ImportAsset(texPath);

        // 임포터 설정: Sprite 모드
        var importer = (TextureImporter)AssetImporter.GetAtPath(texPath);
        importer.textureType         = TextureImporterType.Sprite;
        importer.spriteImportMode    = SpriteImportMode.Single;
        importer.spritePivot         = new Vector2(0.5f, 0.5f);
        importer.spritePixelsPerUnit = PPU;
        importer.filterMode          = FilterMode.Bilinear;
        importer.alphaIsTransparency = true;
        importer.maxTextureSize      = 256;
        importer.textureCompression  = TextureImporterCompression.Uncompressed;
        AssetDatabase.ImportAsset(texPath, ImportAssetOptions.ForceUpdate);

        return AssetDatabase.LoadAssetAtPath<Sprite>(texPath);
    }

    // ── 픽셀 드로잉 헬퍼 ─────────────────────────────────────────────────────

    static void DrawEye(Color32[] pixels, float ex, float ey, float r, Color32 color)
    {
        int xMin = Mathf.FloorToInt(ex - r - 1);
        int xMax = Mathf.CeilToInt(ex + r + 1);
        int yMin = Mathf.FloorToInt(ey - r - 1);
        int yMax = Mathf.CeilToInt(ey + r + 1);

        for (int y = yMin; y <= yMax; y++)
        for (int x = xMin; x <= xMax; x++)
        {
            if (x < 0 || x >= TEX || y < 0 || y >= TEX) continue;
            float dx = x - ex, dy = y - ey;
            float alpha = Mathf.Clamp01(r - Mathf.Sqrt(dx * dx + dy * dy) + 0.5f);
            if (alpha <= 0) continue;
            int idx = y * TEX + x;
            pixels[idx] = BlendAlpha(pixels[idx], color, alpha);
        }
    }

    static void DrawSmile(Color32[] pixels, float cx, float cy, float r, float thick, Color32 color)
    {
        // y < cy 쪽의 호(아랫쪽 호 = 미소)를 픽셀로 직접 그림
        for (int angle = 200; angle <= 340; angle += 1)
        {
            float rad = angle * Mathf.Deg2Rad;
            float px  = cx + Mathf.Cos(rad) * r;
            float py  = cy + Mathf.Sin(rad) * r;

            // 두께를 위해 주변 픽셀도 칠함
            for (float tr = -thick; tr <= thick; tr += 0.5f)
            {
                float ex2 = cx + Mathf.Cos(rad) * (r + tr);
                float ey2 = cy + Mathf.Sin(rad) * (r + tr);
                int ix = Mathf.RoundToInt(ex2);
                int iy = Mathf.RoundToInt(ey2);
                if (ix < 0 || ix >= TEX || iy < 0 || iy >= TEX) continue;
                int idx = iy * TEX + ix;
                pixels[idx] = BlendAlpha(pixels[idx], color, 0.85f);
            }
        }
    }

    static Color32 WatermelonStripe(int x, int y, float cx, float cy,
        float dist, float innerR, Color32 base32)
    {
        float angle = Mathf.Atan2(y - cy, x - cx) * Mathf.Rad2Deg;
        if (angle < 0) angle += 360f;
        float stripe = angle % 60f;
        bool isStripe = stripe < 20f;

        // 내부: 빨간 계열, 줄무늬는 진한 초록
        if (dist < innerR * 0.65f)
        {
            Color32 red   = new Color32(230,  60,  60, 255);
            Color32 seed  = new Color32(180,  30,  30, 255);
            return isStripe ? seed : red;
        }
        else
        {
            Color32 mid   = new Color32(100, 190,  80, 255);
            Color32 dark  = new Color32( 40, 120,  40, 255);
            return isStripe ? dark : mid;
        }
    }

    // ── FruitDatabase 생성 ────────────────────────────────────────────────────

    static void CreateDatabase(FruitData[] assets)
    {
        const string path = "Assets/Fruits/Data/FruitDatabase.asset";
        var db = AssetDatabase.LoadAssetAtPath<FruitDatabase>(path);
        if (db == null)
        {
            db = ScriptableObject.CreateInstance<FruitDatabase>();
            AssetDatabase.CreateAsset(db, path);
        }
        db.fruits = assets;
        EditorUtility.SetDirty(db);
    }

    // ── 색상 유틸 ─────────────────────────────────────────────────────────────

    static Color32 Multiply(Color32 c, float t)
    {
        return new Color32(
            (byte)Mathf.Clamp(c.r * t, 0, 255),
            (byte)Mathf.Clamp(c.g * t, 0, 255),
            (byte)Mathf.Clamp(c.b * t, 0, 255),
            c.a);
    }

    static Color32 LerpColor32(Color32 a, Color32 b, float t)
    {
        t = Mathf.Clamp01(t);
        return new Color32(
            (byte)(a.r + (b.r - a.r) * t),
            (byte)(a.g + (b.g - a.g) * t),
            (byte)(a.b + (b.b - a.b) * t),
            (byte)(a.a + (b.a - a.a) * t));
    }

    static Color32 BlendAlpha(Color32 dst, Color32 src, float srcAlpha)
    {
        float a = srcAlpha;
        return new Color32(
            (byte)(dst.r * (1 - a) + src.r * a),
            (byte)(dst.g * (1 - a) + src.g * a),
            (byte)(dst.b * (1 - a) + src.b * a),
            (byte)Mathf.Max(dst.a, src.a * srcAlpha * 255));
    }

    // ── 폴더 유틸 ─────────────────────────────────────────────────────────────

    static void EnsureFolder(string path)
    {
        if (!AssetDatabase.IsValidFolder(path))
        {
            int slash = path.LastIndexOf('/');
            AssetDatabase.CreateFolder(path[..slash], path[(slash + 1)..]);
        }
    }

    // ── 내부 데이터 구조 ──────────────────────────────────────────────────────

    readonly struct FruitDef
    {
        public readonly string  Name;
        public readonly int     Stage;
        public readonly float   Radius;
        public readonly int     Score;
        public readonly bool    CanDrop;
        public readonly Color32 Main;
        public readonly Color32 Border;

        public FruitDef(string name, int stage, float radius, int score, bool canDrop,
                        Color32 main, Color32 border)
        {
            Name = name; Stage = stage; Radius = radius;
            Score = score; CanDrop = canDrop; Main = main; Border = border;
        }
    }
}

# Suika Game — Scene Setup Guide

## 1. Create FruitData Assets (x11)

Right-click in Project → Create → SuikaGame → FruitData.
Create 11 assets, one per fruit. Fill in:

| Asset name        | stage | radius | mergeScore | canDrop | color (hex)   |
|-------------------|-------|--------|------------|---------|---------------|
| Fruit_01_Cherry   | 1     | 0.20   | 1          | true    | #FF4D6D       |
| Fruit_02_Strawberry | 2  | 0.28   | 3          | true    | #FF3366       |
| Fruit_03_Grapes   | 3     | 0.38   | 6          | true    | #7B2D8B       |
| Fruit_04_Dekopon  | 4     | 0.50   | 10         | true    | #FF8C00       |
| Fruit_05_Persimmon| 5     | 0.62   | 15         | true    | #FF6B35       |
| Fruit_06_Apple    | 6     | 0.76   | 21         | false   | #E63946       |
| Fruit_07_Pear     | 7     | 0.90   | 28         | false   | #A8D5A2       |
| Fruit_08_Peach    | 8     | 1.06   | 36         | false   | #FFAD99       |
| Fruit_09_Pineapple| 9     | 1.22   | 45         | false   | #FFD700       |
| Fruit_10_Melon    | 10    | 1.40   | 55         | false   | #80C080       |
| Fruit_11_Watermelon| 11   | 1.60   | 66         | false   | #4CAF50       |

## 2. Create FruitDatabase Asset

Right-click → Create → SuikaGame → FruitDatabase.
Drag all 11 FruitData assets into the `Fruits` array in order (stage 1 first).

## 3. Camera

- Set Camera **Projection** to Orthographic, **Size** to 6.
- Position: (0, 1, -10)

## 4. Box Walls (2D Colliders)

Create three empty GameObjects with `BoxCollider2D`:
- **Wall_Left** : position (-3.1, 0) scale (0.2, 12)
- **Wall_Right**: position ( 3.1, 0) scale (0.2, 12)
- **Floor**     : position (0, -5.2) scale (6.4, 0.2)

Add a `SpriteRenderer` with a white/beige square sprite for visuals if desired.

## 5. GameManager GameObject

Create an empty GO named `GameManager`.
- Add component `GameManager`.
- Assign `FruitDatabase` → the asset created above.
- Create empty child GO `FruitParent` and assign to `Fruit Parent`.
- `Game Over Line Y` = 4.2 (matches camera top minus a little margin).
- `Grace Period` = 1.5

## 6. DropController GameObject

Create an empty GO named `DropController`.
- Add component `DropController`.
- Assign the same `FruitDatabase`.
- `Drop Y` = 5.0 (above game-over line, below camera top ~6).
- `Min X` = -2.7, `Max X` = 2.7.

### Drop Indicator
- Create a child GO `DropIndicator` with a `SpriteRenderer` (use a circle sprite).
- Assign to `Drop Indicator` field.

### Next Fruit Icon
- Create a child GO `NextFruitIcon` with a `SpriteRenderer`.
- Assign to `Next Fruit Icon` field (position it beside the HUD).

## 7. Game-Over Line Visual (optional)

Create a GO with a thin `SpriteRenderer` (red, 1px tall, wide as box) at Y = 4.2.

## 8. UI Canvas

Use a Canvas (Screen Space – Overlay).

### HUD
- `ScoreText` (TextMeshPro) — top-center.
- `BestScoreText` (TextMeshPro) — top-right.

### Game Over Panel
- A Panel covering center screen, initially **inactive**.
- `GameOverScoreText` and `GameOverBestText` inside it.
- A Button with text "다시 시작" → OnClick → `UIManager.OnRestartButton()`.

### UIManager
- Add `UIManager` component to the Canvas or a child GO.
- Wire all text fields and the panel reference.

## 9. Physics Settings

Edit → Project Settings → Physics 2D:
- Gravity: (0, -18) — snappier feel.
- Layer Collision Matrix: make sure Default ↔ Default is checked.

## 10. Play

Press Play. Click in the Game view to drop fruits. Fruits of the same stage that touch will merge into the next stage.

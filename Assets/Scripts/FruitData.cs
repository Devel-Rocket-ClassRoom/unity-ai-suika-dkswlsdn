using UnityEngine;

[CreateAssetMenu(fileName = "FruitData", menuName = "SuikaGame/FruitData")]
public class FruitData : ScriptableObject
{
    public string fruitName;
    public int stage;          // 1–11
    public float radius;       // physics circle radius in world units
    public int mergeScore;
    public bool canDrop;       // true for stages 1–5
    public Color color;
    public Sprite sprite;
}

using UnityEngine;

[CreateAssetMenu(fileName = "FruitDatabase", menuName = "SuikaGame/FruitDatabase")]
public class FruitDatabase : ScriptableObject
{
    [Tooltip("11 fruits in order: stage 1 (Cherry) … stage 11 (Watermelon)")]
    public FruitData[] fruits;

    public FruitData Get(int stage) => fruits[Mathf.Clamp(stage - 1, 0, fruits.Length - 1)];
    public int Count => fruits.Length;
}

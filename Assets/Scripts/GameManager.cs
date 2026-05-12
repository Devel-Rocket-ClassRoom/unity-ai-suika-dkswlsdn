using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("References")]
    public FruitDatabase fruitDatabase;
    public Transform fruitParent;          // empty GameObject to keep hierarchy tidy

    [Header("Game Over")]
    [Tooltip("Y position of the red game-over line")]
    public float gameOverLineY = 4.2f;
    [Tooltip("Seconds a fruit must stay above the line before triggering game over")]
    public float gameOverGracePeriod = 1.5f;

    public bool IsGameOver { get; private set; }

    int _score;
    int _bestScore;

    public int Score => _score;
    public int BestScore => _bestScore;

    // Fruits currently above the game-over line, mapped to how long they've been there
    readonly Dictionary<Fruit, float> _overlineFruits = new();

    // Queue to process merges one at a time (avoids simultaneous merge collisions)
    readonly Queue<(Fruit, Fruit)> _mergeQueue = new();
    bool _processingMerge;

    const string BEST_SCORE_KEY = "BestScore";

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        _bestScore = PlayerPrefs.GetInt(BEST_SCORE_KEY, 0);
    }

    void Update()
    {
        if (IsGameOver) return;
        CheckGameOverLine();
        ProcessMergeQueue();
    }

    // ── Scoring ─────────────────────────────────────────────────────────────

    void AddScore(int pts)
    {
        _score += pts;
        if (_score > _bestScore)
        {
            _bestScore = _score;
            PlayerPrefs.SetInt(BEST_SCORE_KEY, _bestScore);
        }
        UIManager.Instance?.UpdateScore(_score, _bestScore);
    }

    // ── Merge ────────────────────────────────────────────────────────────────

    public void MergeFruits(Fruit a, Fruit b)
    {
        _mergeQueue.Enqueue((a, b));
    }

    void ProcessMergeQueue()
    {
        if (_processingMerge || _mergeQueue.Count == 0) return;
        var (a, b) = _mergeQueue.Dequeue();

        // Fruits may have been destroyed by a previous merge this frame
        if (a == null || b == null) return;

        _processingMerge = true;
        StartCoroutine(DoMerge(a, b));
    }

    IEnumerator DoMerge(Fruit a, Fruit b)
    {
        int stage = a.Data.stage;
        AddScore(a.Data.mergeScore);

        Vector2 midpoint = ((Vector2)a.transform.position + (Vector2)b.transform.position) * 0.5f;

        a.DestroyFruit();
        b.DestroyFruit();

        // Remove from overline tracker
        _overlineFruits.Remove(a);
        _overlineFruits.Remove(b);

        yield return null; // wait one frame so destroys propagate

        if (stage < fruitDatabase.Count)
        {
            // Spawn next stage fruit
            SpawnFruitAt(fruitDatabase.Get(stage + 1), midpoint, settled: true);
        }
        // stage == 11 (Watermelon): both disappear, score already added

        _processingMerge = false;
    }

    // ── Spawning ─────────────────────────────────────────────────────────────

    public Fruit SpawnFruitAt(FruitData data, Vector2 position, bool settled = false)
    {
        var go = new GameObject(data.fruitName);
        go.transform.position = position;
        if (fruitParent != null) go.transform.SetParent(fruitParent);

        var fruit = go.AddComponent<Fruit>();
        fruit.Init(data);

        if (settled)
            fruit.EnableCollisions();

        return fruit;
    }

    // ── Game Over Line ────────────────────────────────────────────────────────

    void CheckGameOverLine()
    {
        if (fruitParent == null) return;

        var fruits = fruitParent.GetComponentsInChildren<Fruit>();
        var toRemove = new List<Fruit>();

        foreach (var f in fruits)
        {
            if (f == null) continue;
            float top = f.transform.position.y + f.Data.radius;
            if (top > gameOverLineY)
            {
                if (!_overlineFruits.ContainsKey(f))
                    _overlineFruits[f] = 0f;

                _overlineFruits[f] += Time.deltaTime;

                if (_overlineFruits[f] >= gameOverGracePeriod)
                {
                    TriggerGameOver();
                    return;
                }
            }
            else
            {
                toRemove.Add(f);
            }
        }

        foreach (var f in toRemove)
            _overlineFruits.Remove(f);
    }

    void TriggerGameOver()
    {
        if (IsGameOver) return;
        IsGameOver = true;
        UIManager.Instance?.ShowGameOver(_score, _bestScore);
    }

    // ── Restart ───────────────────────────────────────────────────────────────

    public void RestartGame()
    {
        IsGameOver = false;
        _score = 0;
        _overlineFruits.Clear();
        _mergeQueue.Clear();
        _processingMerge = false;

        // Destroy all fruit objects
        if (fruitParent != null)
        {
            foreach (Transform child in fruitParent)
                Destroy(child.gameObject);
        }

        UIManager.Instance?.UpdateScore(0, _bestScore);
        UIManager.Instance?.HideGameOver();
        DropController.Instance?.StartDrop();
    }
}

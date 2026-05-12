using UnityEngine;
using UnityEngine.UI;

public class DropController : MonoBehaviour
{
    public static DropController Instance { get; private set; }

    [Header("References")]
    public FruitDatabase fruitDatabase;
    public Transform dropIndicator;   // small sprite that follows the cursor horizontally
    public Transform nextFruitIcon;   // (선택) 월드 공간 아이콘 — 사용하지 않아도 됨
    public Image nextFruitUIImage;    // UI Canvas 안 NEXT 패널 이미지

    [Header("Drop Zone")]
    public float dropY = 5f;          // Y position where fruits are released
    public float minX = -2.7f;
    public float maxX = 2.7f;

    [Header("Cooldown")]
    public float dropCooldown = 0.5f;

    FruitData _currentFruitData;
    FruitData _nextFruitData;
    float _cooldownTimer;
    bool _active;

    Camera _cam;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        _cam = Camera.main;
    }

    void Start() => StartDrop();

    public void StartDrop()
    {
        _active = true;
        _cooldownTimer = 0f;
        _nextFruitData = PickRandom();
        AdvanceFruit();
    }

    void Update()
    {
        if (!_active || GameManager.Instance == null || GameManager.Instance.IsGameOver) return;

        _cooldownTimer -= Time.deltaTime;

        float worldX = GetInputX();
        // 현재 과일 반지름만큼 안쪽으로 좁혀 벽 콜라이더 겹침 방지 (#3)
        float r = _currentFruitData != null ? _currentFruitData.radius : 0f;
        worldX = Mathf.Clamp(worldX, minX + r, maxX - r);

        // Move drop indicator
        if (dropIndicator != null)
            dropIndicator.position = new Vector3(worldX, dropY, 0f);

        // Drop on click / tap
        if (_cooldownTimer <= 0f && IsDropInput())
        {
            Drop(worldX);
            _cooldownTimer = dropCooldown;
        }
    }

    float GetInputX()
    {
        // Mouse
        if (Input.mousePresent)
        {
            Vector3 world = _cam.ScreenToWorldPoint(Input.mousePosition);
            return world.x;
        }
        // Touch
        if (Input.touchCount > 0)
        {
            Vector3 world = _cam.ScreenToWorldPoint(Input.GetTouch(0).position);
            return world.x;
        }
        return 0f;
    }

    bool IsDropInput()
    {
        if (Input.GetMouseButtonDown(0)) return true;
        if (Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Began) return true;
        return false;
    }

    void Drop(float x)
    {
        var spawnPos = new Vector2(x, dropY);
        var fruit = GameManager.Instance.SpawnFruitAt(_currentFruitData, spawnPos, settled: false);
        fruit.EnableCollisions();

        AdvanceFruit();
    }

    void AdvanceFruit()
    {
        _currentFruitData = _nextFruitData;
        _nextFruitData = PickRandom();

        // NEXT UI Image 업데이트 (#4)
        if (nextFruitUIImage != null)
        {
            nextFruitUIImage.sprite = _nextFruitData.sprite;
            nextFruitUIImage.color  = _nextFruitData.sprite != null ? Color.white : _nextFruitData.color;
            nextFruitUIImage.preserveAspect = true;
        }

        // (월드 공간 아이콘이 연결된 경우 함께 업데이트)
        if (nextFruitIcon != null)
        {
            var sr = nextFruitIcon.GetComponent<SpriteRenderer>();
            if (sr != null)
            {
                sr.sprite = _nextFruitData.sprite;
                sr.color  = _nextFruitData.color;
            }
            nextFruitIcon.localScale = Vector3.one * _nextFruitData.radius * 2f;
        }

        // Update drop indicator appearance
        if (dropIndicator != null)
        {
            var sr = dropIndicator.GetComponent<SpriteRenderer>();
            if (sr != null)
            {
                sr.sprite = _currentFruitData.sprite;
                sr.color = _currentFruitData.color;
            }
            dropIndicator.localScale = Vector3.one * _currentFruitData.radius * 2f;
        }
    }

    FruitData PickRandom()
    {
        // Only stages 1–5 can be dropped
        int maxDropStage = 5;
        int count = Mathf.Min(maxDropStage, fruitDatabase.Count);
        int idx = Random.Range(0, count);
        return fruitDatabase.Get(idx + 1);
    }
}

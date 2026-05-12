using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D), typeof(CircleCollider2D), typeof(SpriteRenderer))]
public class Fruit : MonoBehaviour
{
    public FruitData Data { get; private set; }

    // Prevent a pair from triggering two merges simultaneously
    bool _merging;
    // Grace period after spawn before collision detection is active
    bool _settled;

    static readonly int FRUIT_LAYER = -1; // resolved at runtime

    public void Init(FruitData data)
    {
        Data = data;

        var col = GetComponent<CircleCollider2D>();
        // 스프라이트는 PPU=TEX 설정으로 자연 크기 1유닛(반지름 0.5).
        // localScale = radius*2 이므로 월드 충돌 반지름 = 0.5 * radius*2 = radius.
        col.radius = 0.5f;

        var sr = GetComponent<SpriteRenderer>();
        if (data.sprite != null)
            sr.sprite = data.sprite;
        else
            sr.color = data.color;

        var rb = GetComponent<Rigidbody2D>();
        rb.gravityScale = 1f;
        // Slightly different mass per stage to give distinct feel
        rb.mass = 0.5f + data.stage * 0.15f;
        rb.linearDamping = 0.3f;
        rb.angularDamping = 0.5f;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

        transform.localScale = Vector3.one * data.radius * 2f;
    }

    // Called by DropController once the fruit has been released
    public void EnableCollisions()
    {
        StartCoroutine(SettleDelay());
    }

    IEnumerator SettleDelay()
    {
        // Brief delay so the fruit clears the dropper before collisions count
        yield return new WaitForSeconds(0.15f);
        _settled = true;
    }

    void OnCollisionEnter2D(Collision2D col)
    {
        if (!_settled || _merging) return;
        if (GameManager.Instance == null || GameManager.Instance.IsGameOver) return;

        Fruit other = col.gameObject.GetComponent<Fruit>();
        if (other == null || other._merging || !other._settled) return;
        if (other.Data.stage != Data.stage) return;

        // Prevent both fruits from triggering the merge
        _merging = true;
        other._merging = true;

        GameManager.Instance.MergeFruits(this, other);
    }

    // Called by GameManager to animate and destroy this fruit
    public void DestroyFruit()
    {
        Destroy(gameObject);
    }
}

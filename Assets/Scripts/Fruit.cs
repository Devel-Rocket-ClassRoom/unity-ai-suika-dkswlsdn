using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D), typeof(CircleCollider2D), typeof(SpriteRenderer))]
public class Fruit : MonoBehaviour
{
    public FruitData Data { get; private set; }

    bool _merging;
    bool _settled;

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
        rb.gravityScale  = 1f;
        rb.mass          = 0.5f + data.stage * 0.15f;
        rb.linearDamping    = 0.3f;
        rb.angularDamping   = 0.5f;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

        transform.localScale = Vector3.one * data.radius * 2f;
    }

    public void EnableCollisions()
    {
        StartCoroutine(SettleDelay());
    }

    IEnumerator SettleDelay()
    {
        yield return new WaitForSeconds(0.15f);
        _settled = true;
    }

    // Enter: 낙하 중 최초 접촉
    void OnCollisionEnter2D(Collision2D col) => TryMerge(col.gameObject);

    // Stay: Enter 시점에 _settled=false 여서 기각된 경우 재검사 (#6)
    // _merging 가드가 있어 중복 실행되지 않음
    void OnCollisionStay2D(Collision2D col) => TryMerge(col.gameObject);

    void TryMerge(GameObject otherGO)
    {
        if (!_settled || _merging) return;
        if (GameManager.Instance == null || GameManager.Instance.IsGameOver) return;

        Fruit other = otherGO.GetComponent<Fruit>();
        if (other == null || other._merging || !other._settled) return;
        if (other.Data.stage != Data.stage) return;

        _merging = true;
        other._merging = true;

        GameManager.Instance.MergeFruits(this, other);
    }

    public void DestroyFruit()
    {
        Destroy(gameObject);
    }
}

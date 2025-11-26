using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(CapsuleCollider))]
public class BlinkMove : MonoBehaviour
{
[Header("顺移高度（单位：米）")]
public float blinkHeight = 3f;

[Header("安全距离（离障碍物保持的最小间隔）")]
public float safeDistance = 0.1f;

[Header("顺移冷却时间（秒）")]
public float blinkCooldown = 1f;

[Header("Gizmo 颜色")]
public Color gizmoColor = new Color(0f, 1f, 1f, 0.25f);

private Rigidbody rb;
private CapsuleCollider capsule;
private bool movedUp = false;
private bool canBlink = true;      // 是否可以顺移
private float cooldownTimer = 0f;  // 冷却计时器
public Vector2 fixedHorizontalPos; //固定水平位置

void Start()
{
    rb = GetComponent<Rigidbody>();
    capsule = GetComponent<CapsuleCollider>();
}

void Update()
{
    // 处理冷却倒计时
    if (!canBlink)
    {
        cooldownTimer -= Time.deltaTime;
        if (cooldownTimer <= 0f)
            canBlink = true;
    }

    // 检测按键触发
    if (Input.GetKeyDown(KeyCode.C) && canBlink)
    {
        Blink();
    }
}

void Blink()
{
    Vector3 direction = movedUp ? Vector3.down : Vector3.up;
    Vector3 currentPos = transform.position;

    float offset = Mathf.Max(0f, capsule.height / 2f - capsule.radius);
    Vector3 top = currentPos + Vector3.up * offset;
    Vector3 bottom = currentPos - Vector3.up * offset;

    float distance = blinkHeight;
    Vector3 targetPos = currentPos + direction * distance;

    // 检测障碍物
    // if (Physics.CapsuleCast(bottom, top, capsule.radius, direction, out RaycastHit hit, distance))
    // {
    //     float allowed = hit.distance - safeDistance;
    //     if (allowed < 0f) allowed = 0f;
    //     targetPos = currentPos + direction * allowed;
    // }

    targetPos.x = fixedHorizontalPos.x;
    targetPos.z = fixedHorizontalPos.y;

    
    rb.position = targetPos;
    rb.velocity = Vector3.zero;
    Physics.SyncTransforms();

    movedUp = !movedUp;

    // 进入冷却
    canBlink = false;
    cooldownTimer = blinkCooldown;
}

// Scene 视图中绘制检测范围
void OnDrawGizmosSelected()
{
    if (!capsule) capsule = GetComponent<CapsuleCollider>();

    Gizmos.color = gizmoColor;

    Vector3 center = transform.position;
    float offset = Mathf.Max(0f, capsule.height / 2f - capsule.radius);
    Vector3 top = center + Vector3.up * offset;
    Vector3 bottom = center - Vector3.up * offset;

    DrawCapsuleGizmo(top, bottom, capsule.radius);

    // 显示顺移方向与距离
    Vector3 dir = movedUp ? Vector3.down : Vector3.up;
    Gizmos.DrawLine(center, center + dir * blinkHeight);
}

// 绘制胶囊体外形
void DrawCapsuleGizmo(Vector3 top, Vector3 bottom, float radius)
{
    Gizmos.DrawWireSphere(top, radius);
    Gizmos.DrawWireSphere(bottom, radius);
    Gizmos.DrawLine(top + Vector3.forward * radius, bottom + Vector3.forward * radius);
    Gizmos.DrawLine(top - Vector3.forward * radius, bottom - Vector3.forward * radius);
    Gizmos.DrawLine(top + Vector3.right * radius, bottom + Vector3.right * radius);
    Gizmos.DrawLine(top - Vector3.right * radius, bottom - Vector3.right * radius);
}

public void SetCheckpoint(Vector3 checkpointPos)
{
    fixedHorizontalPos = new Vector2(checkpointPos.x, checkpointPos.z);
}
}




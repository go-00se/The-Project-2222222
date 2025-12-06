using UnityEngine;



public class Jump : MonoBehaviour
{
    Rigidbody rigidbody;
    public float jumpStrength = 2;
    public event System.Action Jumped;

    [SerializeField, Tooltip("Prevents jumping when the transform is in mid-air.")]
    GroundCheck groundCheck;
    
        [Header("Gravity")]
    public float basegravity = 2f;
    public float maxFallSpeed = -18f;
    public float fallSpeedMultiplier = 1.5f;


    void Reset()
    {
        // Try to get groundCheck.
        groundCheck = GetComponentInChildren<GroundCheck>();
    }

    void Awake()
    {
        rigidbody = GetComponent<Rigidbody>();
        rigidbody.useGravity = false;  // 禁用 Unity 自带重力
    }


    void LateUpdate()
    {
        Gravity();
        // Jump when the Jump button is pressed and we are on the ground.
        if (Input.GetButtonDown("Jump") && (!groundCheck || groundCheck.isGrounded))
        {
            rigidbody.AddForce(Vector3.up * 100 * jumpStrength);
            Jumped?.Invoke();
        }
    }
    

    public void Gravity()
    {
        // 原生重力由 Physics.gravity 决定，此处改成自定义附加重力
        float gravity = Physics.gravity.y * basegravity;

        // 下落时增加重力倍率
        if (rigidbody.velocity.y < 0)
        {
            gravity *= fallSpeedMultiplier;
        }

        // 手动增加重力
        rigidbody.AddForce(new Vector3(0, gravity, 0), ForceMode.Acceleration);

        // 限制最大下落速度
        if (rigidbody.velocity.y < maxFallSpeed)
        {
            rigidbody.velocity = new Vector3(
                rigidbody.velocity.x,
                maxFallSpeed,
                rigidbody.velocity.z
            );
        }
    }

    
}

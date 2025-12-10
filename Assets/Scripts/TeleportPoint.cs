using UnityEngine;

public class TeleportPoint : MonoBehaviour
{
    [Header("传送目标位置")]
    public Transform teleportTarget;  // 传送到的位置

    private void OnTriggerEnter(Collider other)
    {
        // 判断碰撞的物体是否是玩家
        if (other.CompareTag("Player"))
        {
            // 传送玩家
            other.transform.position = teleportTarget.position;

            // 可选：同步旋转
            // other.transform.rotation = teleportTarget.rotation;
        }
    }
}
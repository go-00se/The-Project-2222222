using UnityEngine;

public class TeleportPoint : MonoBehaviour
{
    [Header("传送目标位置（任意物体的 Transform）")]
    public Transform targetPoint;

    [Header("玩家的标签名")]
    public string playerTag = "Player";

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag(playerTag))
        {
            Teleport(other.gameObject);
        }
    }

    private void Teleport(GameObject player)
    {
        if (targetPoint != null)
        {
            player.transform.position = targetPoint.position + Vector3.up * 1.0f;

        }
        else
        {
            Debug.LogWarning("TeleportPoint：未指定传送目标点！");
        }
    }
}
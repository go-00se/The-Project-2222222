using UnityEngine;
using UnityEngine.Video;

public class TeleportPoint : MonoBehaviour
{
    [Header("传送目标位置")]
    public Transform targetPoint;

    [Header("玩家标签")]
    public string playerTag = "Player";

    [Header("可选：该传送器要播放的视频（留空则直接传送）")]
    public VideoClip videoClip;

    [Header("公共视频系统（整个场景只有一套）")]
    public VideoPlayer videoPlayer;       // 场景中唯一 VideoPlayer
    public GameObject videoUI;            // 场景中唯一 UI（显示 RawImage）
    public MonoBehaviour playerController; // 玩家控制脚本

    private bool isTriggered = false;

    private void Start()
    {
        if (videoUI != null)
            videoUI.SetActive(false);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (isTriggered) return;
        if (!other.CompareTag(playerTag)) return;

        GameObject player = other.gameObject;
        isTriggered = true;

        // 🔥 情况 1：没有视频 → 直接传送
        if (videoClip == null || videoPlayer == null || videoUI == null)
        {
            DirectTeleport(player);
            isTriggered = false;
            return;
        }

        // 🔥 情况 2：有视频 → 播放视频
        PlayVideoAndTeleport(player);
    }

    private void DirectTeleport(GameObject player)
    {
        if (targetPoint != null)
        {
            player.transform.position = targetPoint.position;
        }
        else
        {
            Debug.LogWarning("TeleportPoint：未指定传送目标点！");
        }
    }

    private void PlayVideoAndTeleport(GameObject player)
    {
        // 禁用玩家控制
        if (playerController != null)
            playerController.enabled = false;

        // 显示视频 UI
        videoUI.SetActive(true);

        // 播放视频
        videoPlayer.clip = videoClip;

        // 注册事件（防止反复添加）
        videoPlayer.loopPointReached -= OnVideoFinished; 
        videoPlayer.loopPointReached += OnVideoFinished;

        // 使用全局变量存储玩家
        currentPlayer = player;

        videoPlayer.Play();
    }

    private GameObject currentPlayer;

    private void OnVideoFinished(VideoPlayer vp)
    {
        // 传送
        if (currentPlayer != null && targetPoint != null)
            currentPlayer.transform.position = targetPoint.position;

        // 隐藏 UI
        videoUI.SetActive(false);

        // 恢复玩家控制
        if (playerController != null)
            playerController.enabled = true;

        isTriggered = false;
    }
}

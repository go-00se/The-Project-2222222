using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(FirstPersonMovement))]
public class RigidbodyPathRecorder : MonoBehaviour
{
    [Header("Recording Settings")] public float recordDuration = 5f; // 最大录制时长
    public float recordInterval = 0.05f; // 每隔多久记录一次
    public KeyCode recordKey = KeyCode.C;

    [Header("Playback Settings")] public float playbackSpeed = 1f; // 回放速度倍率
    public KeyCode playbackKey = KeyCode.V;

    private Rigidbody rb;
    private FirstPersonMovement moveScript;
    private bool isRecording = false;
    private bool isPlayingBack = false;

    private float recordTimer = 0f;
    private float recordIntervalTimer = 0f;

    // 存储相对位移而不是绝对位置
    private List<Vector3> recordedDisplacements = new List<Vector3>();
    private List<Quaternion> recordedRotations = new List<Quaternion>();
    private Vector3 recordingStartPosition;
    private Quaternion recordingStartRotation;

    private int playbackIndex = 0;
    private float playbackTimer = 0f;
    private Vector3 playbackStartPosition;
    private Quaternion playbackStartRotation;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        moveScript = GetComponent<FirstPersonMovement>();
    }

    void Update()
    {
        // ========= 录制 =========
        if (Input.GetKeyDown(recordKey))
        {
            StartRecording();
        }

        if (isRecording)
        {
            recordTimer += Time.deltaTime;
            recordIntervalTimer += Time.deltaTime;

            if (recordIntervalTimer >= recordInterval)
            {
                RecordFrame();
                recordIntervalTimer = 0f;
            }

            if (recordTimer >= recordDuration || Input.GetKeyUp(recordKey))
            {
                StopRecording();
            }
        }

        // ========= 回放 =========
        if (Input.GetKeyDown(playbackKey) && recordedDisplacements.Count > 1)
        {
            StartPlayback();
        }

        if (isPlayingBack)
        {
            PlaybackFrame();

            if (Input.GetKeyUp(playbackKey))
            {
                StopPlayback();
            }
        }
    }

    // ----------- 录制 -----------
    void StartRecording()
    {
        isRecording = true;
        isPlayingBack = false;
        recordTimer = 0f;
        recordIntervalTimer = 0f;
        recordedDisplacements.Clear();
        recordedRotations.Clear();

        // 记录开始位置和旋转
        recordingStartPosition = transform.position;
        recordingStartRotation = transform.rotation;

        Debug.Log("<color=green>开始录制路径...</color>");
    }

    void RecordFrame()
    {
        // 存储相对于开始位置的位移
        recordedDisplacements.Add(transform.position - recordingStartPosition);
        recordedRotations.Add(transform.rotation);
    }

    void StopRecording()
    {
        isRecording = false;
        Debug.Log($"<color=yellow>录制结束，共 {recordedDisplacements.Count} 个点。</color>");
    }

    // ----------- 回放 -----------
    void StartPlayback()
    {
        if (recordedDisplacements.Count < 2)
        {
            Debug.LogWarning("没有录制到有效路径，无法回放！");
            return;
        }

        isPlayingBack = true;
        playbackIndex = 0;
        playbackTimer = 0f;

        // 记录回放开始的位置和旋转
        playbackStartPosition = transform.position;
        playbackStartRotation = transform.rotation;

        moveScript.enabled = false; // 禁用原始移动控制
        rb.velocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;

        Debug.Log("<color=cyan>开始路径回放...</color>");
    }

    void PlaybackFrame()
    {
        if (playbackIndex >= recordedDisplacements.Count - 1)
        {
            StopPlayback();
            return;
        }

        playbackTimer += Time.deltaTime * playbackSpeed;

        // 计算当前应该在哪个片段
        float segmentDuration = recordInterval / playbackSpeed;
        while (playbackTimer >= segmentDuration && playbackIndex < recordedDisplacements.Count - 1)
        {
            playbackTimer -= segmentDuration;
            playbackIndex++;
        }

        if (playbackIndex >= recordedDisplacements.Count - 1)
        {
            StopPlayback();
            return;
        }

        // 计算当前片段内的插值比例
        float t = playbackTimer / segmentDuration;
        t = Mathf.Clamp01(t);

        // 获取当前片段的位移和旋转
        Vector3 currentDisplacement = recordedDisplacements[playbackIndex];
        Vector3 nextDisplacement = recordedDisplacements[playbackIndex + 1];
        Quaternion currentRotation = recordedRotations[playbackIndex];
        Quaternion nextRotation = recordedRotations[playbackIndex + 1];

        // 计算目标位置和旋转
        Vector3 targetDisplacement = Vector3.Lerp(currentDisplacement, nextDisplacement, t);
        Quaternion targetRotation = Quaternion.Slerp(currentRotation, nextRotation, t);

        // 应用位移和旋转（从回放起点开始）
        Vector3 targetPosition = playbackStartPosition + targetDisplacement;

        // 用物理方式移动（保留碰撞）
        rb.MovePosition(targetPosition);
        rb.MoveRotation(targetRotation);
    }

    void StopPlayback()
    {
        if (isPlayingBack)
            Debug.Log("<color=magenta>路径回放结束。</color>");

        isPlayingBack = false;
        moveScript.enabled = true; // 恢复玩家控制
    }

    // 可视化路径
    void OnDrawGizmos()
    {
        if (recordedDisplacements == null || recordedDisplacements.Count < 2) return;

        Gizmos.color = Color.yellow;

        // 计算当前场景中的路径位置
        Vector3 basePosition = Application.isPlaying
            ? (isPlayingBack ? playbackStartPosition : recordingStartPosition)
            : transform.position;

        for (int i = 0; i < recordedDisplacements.Count - 1; i++)
        {
            Vector3 worldPos1 = basePosition + recordedDisplacements[i];
            Vector3 worldPos2 = basePosition + recordedDisplacements[i + 1];
        }
    }
}
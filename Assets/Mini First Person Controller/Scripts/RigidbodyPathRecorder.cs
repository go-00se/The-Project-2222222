using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(FirstPersonMovement))]
public class RigidbodyPathRecorder : MonoBehaviour
{
    [Header("Recording Settings")]
    public float recordDuration = 5f;        // 最大录制时长
    public float recordInterval = 0.05f;     // 每隔多久记录一次
    public KeyCode recordKey = KeyCode.C;

    [Header("Playback Settings")]
    public float playbackSpeed = 1f;         // 回放速度倍率
    public KeyCode playbackKey = KeyCode.V;

    private Rigidbody rb;
    private FirstPersonMovement moveScript;
    private bool isRecording = false;
    private bool isPlayingBack = false;

    private float recordTimer = 0f;
    private float recordIntervalTimer = 0f;

    private List<Vector3> recordedPositions = new List<Vector3>();
    private List<Quaternion> recordedRotations = new List<Quaternion>();

    private int playbackIndex = 0;
    private float playbackT = 0f;

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
        if (Input.GetKeyDown(playbackKey) && recordedPositions.Count > 1)
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
        recordedPositions.Clear();
        recordedRotations.Clear();

        Debug.Log("<color=green>开始录制路径...</color>");
    }

    void RecordFrame()
    {
        recordedPositions.Add(transform.position);
        recordedRotations.Add(transform.rotation);
    }

    void StopRecording()
    {
        isRecording = false;
        Debug.Log($"<color=yellow>录制结束，共 {recordedPositions.Count} 个点。</color>");
    }

    // ----------- 回放 -----------
    void StartPlayback()
    {
        if (recordedPositions.Count < 2)
        {
            Debug.LogWarning("没有录制到有效路径，无法回放！");
            return;
        }

        isPlayingBack = true;
        playbackIndex = 0;
        playbackT = 0f;
        moveScript.enabled = false; // 禁用原始移动控制
        rb.velocity = Vector3.zero;

        Debug.Log("<color=cyan>开始路径回放...</color>");
    }

    void PlaybackFrame()
    {
        if (playbackIndex >= recordedPositions.Count - 1)
        {
            StopPlayback();
            return;
        }

        playbackT += Time.deltaTime * playbackSpeed / recordInterval;
        Vector3 startPos = recordedPositions[playbackIndex];
        Vector3 endPos = recordedPositions[playbackIndex + 1];
        Quaternion startRot = recordedRotations[playbackIndex];
        Quaternion endRot = recordedRotations[playbackIndex + 1];

        Vector3 targetPos = Vector3.Lerp(startPos, endPos, playbackT);
        Quaternion targetRot = Quaternion.Slerp(startRot, endRot, playbackT);

        // 用物理方式移动（保留碰撞）
        rb.MovePosition(targetPos);
        rb.MoveRotation(targetRot);

        if (playbackT >= 1f)
        {
            playbackIndex++;
            playbackT = 0f;
        }
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
        if (recordedPositions == null || recordedPositions.Count < 2) return;
        Gizmos.color = Color.yellow;
        for (int i = 0; i < recordedPositions.Count - 1; i++)
        {
            Gizmos.DrawLine(recordedPositions[i], recordedPositions[i + 1]);
        }
    }
}

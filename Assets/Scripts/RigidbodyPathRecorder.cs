using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(FirstPersonMovement))]
public class ActionReplay_Final : MonoBehaviour
{
    [Header("录制设置")]
    public float recordDuration = 5f;
    public float recordInterval = 0.05f;
    public KeyCode recordKey = KeyCode.C;

    [Header("回放设置")]
    public float playbackSpeed = 1f;
    public KeyCode playbackKey = KeyCode.V;

    [Header("路径显示")]
    public float previewHeightOffset = 0.5f;  
    public Color previewStartColor = new Color(0, 1, 1, 0.8f);
    public Color previewEndColor = new Color(0, 1, 1, 0.2f);
    public float previewWidth = 0.06f;

    private Rigidbody rb;
    private FirstPersonMovement moveScript;

    // 录制状态
    private bool isRecording = false;
    private float recordTimer = 0f;
    private float recordIntervalTimer = 0f;

    // 回放状态
    private bool isPlayingBack = false;
    private int playbackIndex = 0;
    private float playbackTimer = 0f;

    // 数据存储
    private List<Vector3> recordedDisplacements = new List<Vector3>();
    private List<Quaternion> recordedRotations = new List<Quaternion>();
    private Vector3 recordingStartPosition;
    private Quaternion recordingStartRotation;

    // 回放起点（绝对坐标）
    private Vector3 playbackStartPosition;
    private Quaternion playbackStartRotation;

    // 路径显示
    private LineRenderer previewLine;
    private int previewCutIndex = 0;
    private Vector3[] playbackWorldPositions; // 回放期间锁定的绝对坐标

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        moveScript = GetComponent<FirstPersonMovement>();

        // 初始化 LineRenderer
        previewLine = gameObject.AddComponent<LineRenderer>();
        previewLine.material = new Material(Shader.Find("Sprites/Default"));
        previewLine.widthMultiplier = previewWidth;
        previewLine.startColor = previewStartColor;
        previewLine.endColor = previewEndColor;
        previewLine.positionCount = 0;
    }

    void Update()
    {
        HandleRecording();
        HandlePlayback();
    }

    void LateUpdate()
    {
        UpdatePreviewPath();
    }

    #region 录制
    void HandleRecording()
    {
        if (Input.GetKeyDown(recordKey))
            StartRecording();

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
                StopRecording();
        }
    }

    void StartRecording()
    {
        isRecording = true;
        isPlayingBack = false;
        recordTimer = 0f;
        recordIntervalTimer = 0f;

        recordedDisplacements.Clear();
        recordedRotations.Clear();

        recordingStartPosition = transform.position;
        recordingStartRotation = transform.rotation;

        Debug.Log("<color=green>开始录制路径...</color>");
    }

    void RecordFrame()
    {
        // 本地位移和旋转
        Vector3 worldDelta = transform.position - recordingStartPosition;
        Vector3 localDelta = Quaternion.Inverse(recordingStartRotation) * worldDelta;
        recordedDisplacements.Add(localDelta);

        Quaternion localRot = Quaternion.Inverse(recordingStartRotation) * transform.rotation;
        recordedRotations.Add(localRot);
    }

    void StopRecording()
    {
        isRecording = false;
        previewCutIndex = 0;
        UpdatePreviewPath();

        Debug.Log($"<color=yellow>录制结束，共 {recordedDisplacements.Count} 个点。</color>");
    }
    #endregion

    #region 回放
    void HandlePlayback()
    {
        if (Input.GetKeyDown(playbackKey) && recordedDisplacements.Count > 1)
            StartPlayback();

        if (isPlayingBack)
        {
            PlaybackFrame();

            if (Input.GetKeyUp(playbackKey))
                StopPlayback();
        }
    }

    void StartPlayback()
    {
        isPlayingBack = true;
        playbackIndex = 0;
        playbackTimer = 0f;

        playbackStartPosition = transform.position;  // 锁定回放起点
        playbackStartRotation = transform.rotation;  // 锁定回放朝向

        moveScript.enabled = false;
        rb.velocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;

        // 生成绝对坐标的路径用于回放期间显示
        playbackWorldPositions = new Vector3[recordedDisplacements.Count];
        for (int i = 0; i < recordedDisplacements.Count; i++)
        {
            Vector3 worldPos = playbackStartPosition + playbackStartRotation * recordedDisplacements[i];
            worldPos.y += previewHeightOffset; // 高度偏移
            playbackWorldPositions[i] = worldPos;
        }

        previewCutIndex = 0; // 从头开始吃掉路径
        UpdatePreviewPath();

        Debug.Log("<color=cyan>开始回放路径（绝对坐标）...</color>");
    }

    void PlaybackFrame()
    {
        if (playbackIndex >= recordedDisplacements.Count - 1)
        {
            StopPlayback();
            return;
        }

        playbackTimer += Time.deltaTime * playbackSpeed;
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

        // 插值
        float t = Mathf.Clamp01(playbackTimer / segmentDuration);
        Vector3 currentDisplacement = recordedDisplacements[playbackIndex];
        Vector3 nextDisplacement = recordedDisplacements[playbackIndex + 1];
        Quaternion currentRotation = recordedRotations[playbackIndex];
        Quaternion nextRotation = recordedRotations[playbackIndex + 1];

        Vector3 targetDisplacement = Vector3.Lerp(currentDisplacement, nextDisplacement, t);
        Quaternion targetRotation = Quaternion.Slerp(currentRotation, nextRotation, t);

        Vector3 worldOffset = playbackStartRotation * targetDisplacement;
        Vector3 targetPosition = playbackStartPosition + worldOffset;

        rb.MovePosition(targetPosition);
        rb.MoveRotation(playbackStartRotation * targetRotation);

        // 更新路径裁剪
        previewCutIndex = playbackIndex;
    }

    void StopPlayback()
    {
        if (isPlayingBack)
            Debug.Log("<color=magenta>回放结束</color>");

        isPlayingBack = false;
        moveScript.enabled = true;

        previewCutIndex = 0;
        UpdatePreviewPath();
    }
    #endregion

    #region 路径可视化
    void UpdatePreviewPath()
    {
        if (recordedDisplacements.Count < 2)
        {
            previewLine.positionCount = 0;
            return;
        }

        Vector3[] displayPositions;
        if (isPlayingBack && playbackWorldPositions != null)
        {
            // 回放期间使用绝对坐标路径
            int remainingPoints = playbackWorldPositions.Length - previewCutIndex;
            if (remainingPoints <= 1)
            {
                previewLine.positionCount = 0;
                return;
            }

            displayPositions = new Vector3[remainingPoints];
            for (int i = 0; i < remainingPoints; i++)
                displayPositions[i] = playbackWorldPositions[previewCutIndex + i];
        }
        else
        {
            // 平时预览随玩家朝向旋转
            int remainingPoints = recordedDisplacements.Count - previewCutIndex;
            if (remainingPoints <= 1)
            {
                previewLine.positionCount = 0;
                return;
            }

            displayPositions = new Vector3[remainingPoints];
            for (int i = 0; i < remainingPoints; i++)
            {
                Vector3 worldOffset = transform.rotation * recordedDisplacements[previewCutIndex + i];
                Vector3 worldPos = transform.position + worldOffset;
                worldPos.y += previewHeightOffset;
                displayPositions[i] = worldPos;
            }
        }

        previewLine.positionCount = displayPositions.Length;
        previewLine.SetPositions(displayPositions);
    }
    #endregion
}

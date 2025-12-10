using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(FirstPersonMovement))]
public class ActionReplay_PhysicsSafe : MonoBehaviour
{
    [Header("录制设置")]
    public float recordDuration = 5f;
    public float recordInterval = 0.05f;
    public KeyCode recordKey = KeyCode.C;
    public LayerMask obstacleLayers;

    [Header("玩家体型")]
    public float capsuleHeight = 1.8f;
    public float capsuleRadius = 0.3f;

    [Header("回放设置")]
    public float playbackSpeed = 1f;
    public KeyCode playbackKey = KeyCode.V;

    [Header("路径显示")]
    public float previewHeightOffset = 0.5f;
    public Color previewStartColor = new Color(0, 1, 1, 0.8f);
    public Color previewEndColor = new Color(0, 1, 1, 0.2f);
    public Color previewBlockedColor = new Color(1, 0, 0, 0.8f);
    public float previewWidth = 0.06f;

    [Header("终点圆标记")]
    public float endMarkerRadius = 0.3f;
    public Color endMarkerColor = new Color(0, 1, 1, 0.3f);

    private Rigidbody rb;
    private FirstPersonMovement moveScript;

    // 录制
    private bool isRecording = false;
    private float recordTimer = 0f;
    private float recordIntervalTimer = 0f;
    private bool pathBlocked = false;

    // 回放
    private bool isPlayingBack = false;
    private int playbackIndex = 0;
    private float playbackTimer = 0f;

    // 数据
    private List<Vector3> recordedDisplacements = new List<Vector3>();
    private List<Quaternion> recordedRotations = new List<Quaternion>();
    private Vector3 recordingStartPosition;
    private Quaternion recordingStartRotation;

    private Vector3 playbackStartPosition;
    private Quaternion playbackStartRotation;
    private Vector3[] playbackWorldPositions;

    // 可视化
    private LineRenderer previewLine;
    private int previewCutIndex = 0;
    private GameObject endMarker;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        moveScript = GetComponent<FirstPersonMovement>();

        previewLine = gameObject.AddComponent<LineRenderer>();
        previewLine.material = new Material(Shader.Find("Sprites/Default"));
        previewLine.widthMultiplier = previewWidth;
        previewLine.startColor = previewStartColor;
        previewLine.endColor = previewEndColor;

        endMarker = GameObject.CreatePrimitive(PrimitiveType.Quad);
        endMarker.transform.parent = null;
        endMarker.transform.localScale = new Vector3(endMarkerRadius * 2, endMarkerRadius * 2, 1f);
        endMarker.GetComponent<MeshRenderer>().material = new Material(Shader.Find("Sprites/Default"));
        endMarker.GetComponent<MeshRenderer>().material.color = endMarkerColor;
        Destroy(endMarker.GetComponent<Collider>());
        endMarker.SetActive(false);
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
        pathBlocked = false;

        recordedDisplacements.Clear();
        recordedRotations.Clear();
        recordingStartPosition = transform.position;
        recordingStartRotation = transform.rotation;

        previewCutIndex = 0;
        Debug.Log("<color=green>开始录制路径...</color>");
    }

    void RecordFrame()
    {
        Vector3 localDelta = Quaternion.Inverse(recordingStartRotation) * (transform.position - recordingStartPosition);
        recordedDisplacements.Add(localDelta);

        Quaternion localRot = Quaternion.Inverse(recordingStartRotation) * transform.rotation;
        recordedRotations.Add(localRot);

        // **实时检测路径段是否阻塞**
        if (recordedDisplacements.Count > 1)
        {
            Vector3 prev = recordingStartPosition + recordingStartRotation * recordedDisplacements[recordedDisplacements.Count - 2];
            Vector3 curr = recordingStartPosition + recordingStartRotation * recordedDisplacements[recordedDisplacements.Count - 1];

            Vector3 start = prev + Vector3.up * (capsuleHeight * 0.5f);
            Vector3 end = prev + Vector3.up * capsuleHeight;
            Vector3 dir = curr - prev;
            float dist = dir.magnitude;

            if (Physics.CapsuleCast(start, end, capsuleRadius, dir.normalized, out RaycastHit hit, dist, obstacleLayers))
                pathBlocked = true;
        }
    }

    void StopRecording()
    {
        isRecording = false;
        previewCutIndex = 0;
        UpdatePreviewPath();

        if (pathBlocked)
            Debug.LogWarning("<color=red>路径被障碍物阻挡，回放被禁止！</color>");
        else
            Debug.Log($"<color=yellow>录制结束，共 {recordedDisplacements.Count} 个点，路径有效。</color>");
    }
    #endregion

    #region 回放
    void HandlePlayback()
    {
        if (Input.GetKeyDown(playbackKey))
        {
            if (pathBlocked)
            {
                Debug.LogWarning("<color=red>回放被禁止，因为路径有障碍物阻挡！</color>");
                return;
            }

            if (recordedDisplacements.Count > 1)
                StartPlayback();
        }

        if (isPlayingBack)
            FixedUpdatePlayback();
    }

    void StartPlayback()
    {
        isPlayingBack = true;
        playbackIndex = 0;
        playbackTimer = 0f;

        playbackStartPosition = transform.position;
        playbackStartRotation = transform.rotation;

        moveScript.enabled = false;
        rb.velocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;

        playbackWorldPositions = new Vector3[recordedDisplacements.Count];
        for (int i = 0; i < recordedDisplacements.Count; i++)
        {
            Vector3 worldPos = playbackStartPosition + playbackStartRotation * recordedDisplacements[i];
            worldPos.y += previewHeightOffset;
            playbackWorldPositions[i] = worldPos;
        }

        previewCutIndex = 0;
        UpdatePreviewPath();

        if (playbackWorldPositions.Length > 0)
        {
            endMarker.SetActive(true);
            endMarker.transform.position = playbackWorldPositions[playbackWorldPositions.Length - 1];
            endMarker.transform.rotation = Quaternion.LookRotation(Vector3.up);
        }

        Debug.Log("<color=cyan>开始回放路径（绝对坐标）...</color>");
    }

    void FixedUpdatePlayback()
    {
        if (!isPlayingBack) return;

        if (playbackIndex >= recordedDisplacements.Count - 1)
        {
            StopPlayback();
            return;
        }

        playbackTimer += Time.fixedDeltaTime * playbackSpeed;
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

        float t = Mathf.Clamp01(playbackTimer / segmentDuration);

        Vector3 currentDisplacement = recordedDisplacements[playbackIndex];
        Vector3 nextDisplacement = recordedDisplacements[playbackIndex + 1];
        Quaternion currentRotation = recordedRotations[playbackIndex];
        Quaternion nextRotation = recordedRotations[playbackIndex + 1];

        Vector3 targetDisplacement = Vector3.Lerp(currentDisplacement, nextDisplacement, t);
        Quaternion targetRotation = Quaternion.Slerp(currentRotation, nextRotation, t);

        Vector3 desiredPosition = playbackStartPosition + playbackStartRotation * targetDisplacement;

        // **物理安全移动**
        Vector3 moveDir = desiredPosition - rb.position;
        float moveDist = moveDir.magnitude;

        if (moveDist > 0.001f)
        {
            Vector3 capsuleStart = rb.position + Vector3.up * (capsuleHeight * 0.5f);
            Vector3 capsuleEnd = rb.position + Vector3.up * capsuleHeight;

            if (Physics.CapsuleCast(capsuleStart, capsuleEnd, capsuleRadius, moveDir.normalized, out RaycastHit hit, moveDist, obstacleLayers))
            {
                moveDist = hit.distance;
            }

            Vector3 safeMove = moveDir.normalized * moveDist;
            rb.velocity = safeMove / Time.fixedDeltaTime;
        }
        else
        {
            rb.velocity = Vector3.zero;
        }

        rb.MoveRotation(playbackStartRotation * targetRotation);

        previewCutIndex = playbackIndex;
        UpdatePreviewPath();
    }

    void StopPlayback()
    {
        if (isPlayingBack)
            Debug.Log("<color=magenta>回放结束</color>");

        isPlayingBack = false;
        moveScript.enabled = true;

        rb.velocity = Vector3.zero;

        previewCutIndex = 0;
        UpdatePreviewPath();

        endMarker.SetActive(false);
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
            int remainingPoints = playbackWorldPositions.Length - previewCutIndex;
            if (remainingPoints <= 1)
            {
                previewLine.positionCount = 0;
                return;
            }

            displayPositions = new Vector3[remainingPoints];
            for (int i = 0; i < remainingPoints; i++)
                displayPositions[i] = playbackWorldPositions[previewCutIndex + i];

            previewLine.startColor = previewStartColor;
            previewLine.endColor = previewEndColor;
        }
        else
        {
            displayPositions = new Vector3[recordedDisplacements.Count];
            for (int i = 0; i < recordedDisplacements.Count; i++)
            {
                Vector3 worldPos = transform.position + transform.rotation * recordedDisplacements[i];
                worldPos.y += previewHeightOffset;
                displayPositions[i] = worldPos;
            }

            if (pathBlocked)
            {
                previewLine.startColor = previewBlockedColor;
                previewLine.endColor = previewBlockedColor;
            }
            else
            {
                previewLine.startColor = previewStartColor;
                previewLine.endColor = previewEndColor;
            }
        }

        previewLine.positionCount = displayPositions.Length;
        previewLine.SetPositions(displayPositions);
    }
    #endregion
}

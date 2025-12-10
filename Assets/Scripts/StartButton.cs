using UnityEngine;
using UnityEngine.Events;

public class StartButton : MonoBehaviour
{
    [Tooltip("启动画布，点击开始按钮时会被隐藏")]
    public GameObject canvas;

    [Tooltip("游戏开始时触发的额外行为（可在检视面板添加方法)")]
    public UnityEvent onGameStart;

    bool gameStarted = false;

    void Awake()
    {
        // 确保一开始鼠标可见且不被锁定
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;
    }

    void Update()
    {
        if (gameStarted) return;

        // 持续确保未开始时鼠标可见且未锁定（防止其他脚本改变）
        Cursor.visible = true;
        if (Cursor.lockState != CursorLockMode.None)
            Cursor.lockState = CursorLockMode.None;
    }

    // 在 Button 的 OnClick 中绑定此方法
    public void OnStartButtonClicked()
    {
        if (canvas != null) canvas.SetActive(false);

        gameStarted = true;

        // 隐藏并锁定鼠标以进入游戏模式
        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;

        onGameStart?.Invoke();
    }
}
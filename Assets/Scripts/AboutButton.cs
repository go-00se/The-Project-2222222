using UnityEngine;

public class AboutButton : MonoBehaviour
{
    [Tooltip("点击时打开的网址")]
    public string url = "https://sites.google.com/nyu.edu/cgdd/2025-fall/project-wake-up";

    // 在 Button 的 OnClick 中绑定此方法
    public void OnAboutButtonClicked()
    {
        if (string.IsNullOrEmpty(url)) return;
        Application.OpenURL(url);
    }
}
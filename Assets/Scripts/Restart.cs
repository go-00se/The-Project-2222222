using UnityEngine;
using UnityEngine.SceneManagement;

public class Restart : MonoBehaviour
{
    [Tooltip("将玩家对象拖到这里。若留空，会按 tag=\"Player\" 查找")]
    public GameObject player;

    [Tooltip("重生点（位置和朝向）。若留空，则根据 forceReload 决定是否重载场景")]
    public Transform respawnPoint;

    [Tooltip("若为 true 则强制重载当前场景作为重生方式")]
    public bool forceReload = false;

    // 在 UI Button 的 OnClick 中调用此方法
    public void OnRestartButton()
    {
        if (forceReload || respawnPoint == null)
        {
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
            return;
        }

        if (player == null)
        {
            player = GameObject.FindGameObjectWithTag("Player");
            if (player == null)
            {
                Debug.LogWarning("Restart: 未找到玩家对象，改为重载场景进行重生");
                SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
                return;
            }
        }

        // 传送玩家到重生点并重置常见物理状态
        player.transform.position = respawnPoint.position;
        player.transform.rotation = respawnPoint.rotation;

        var rb = player.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.velocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            Physics.SyncTransforms();
        }

        var cc = player.GetComponent<CharacterController>();
        if (cc != null)
        {
            // CharacterController 需要短暂禁用以确保位置生效
            cc.enabled = false;
            cc.transform.position = respawnPoint.position;
            cc.enabled = true;
        }

        // 若玩家有自定义恢复逻辑（如 Health 脚本），可在对应组件中添加公开方法并在此处调用
    }
}
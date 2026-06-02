using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    // 单例模式：让其他脚本可以直接通过 GameManager.Instance 访问这个类
    public static GameManager Instance { get; private set; }

    private void Awake()
    {
        // 确保场景中只有一个 GameManager
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    // 全局的失败处理方法
    public void GameOver()
    {
        Debug.Log("GameManager Game Over！");
        
        // 这里以后可以加入呼出失败UI的代码
        // 延迟重开
        Invoke("RestartLevel", 1f);
    }

    // 全局的胜利处理方法（提前预留）
    public void LevelClear()
    {
        Debug.Log("GameManager 宣布：关卡完成！");
        // 这里以后可以加入进入下一关的逻辑
    }

    private void RestartLevel()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }
}
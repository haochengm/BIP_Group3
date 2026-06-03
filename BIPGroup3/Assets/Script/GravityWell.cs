using UnityEngine;

public class GravityWell : MonoBehaviour
{
    [Header("延迟设置")]
    [Tooltip("视觉特效立刻产生，但物理引力会延迟多少秒才真正吸人")]
    public float gravityPhysicsDelay = 0.3f; 

    private PointEffector2D effector;
    private ParticleSystem particleSys;

    private void Awake()
    {
        // 自动获取自身的物理引力组件
        effector = GetComponent<PointEffector2D>();
        // 自动获取子物体上的粒子系统
        particleSys = GetComponentInChildren<ParticleSystem>();
    }

    // 每当大管家执行 activeGravityWell.SetActive(true) 时，Unity会自动触发此函数
    private void OnEnable()
    {
        // 1. 核心视觉：让粒子特效没有任何延迟，立刻开始播放/凝聚
        if (particleSys != null)
        {
            particleSys.Clear(); // 清理上一轮残存的粒子
            particleSys.Play();
        }

        // 2. 核心物理：先把物理引力组件关掉，让它现在没有吸力
        if (effector != null)
        {
            effector.enabled = false;
        }

        // 3. 开启延时：在指定秒数后，呼叫激活物理的方法
        Invoke("ActivateGravityPhysics", gravityPhysicsDelay);
    }

    // 每当玩家松开鼠标执行 activeGravityWell.SetActive(false) 时触发
    private void OnDisable()
    {
        // 玩家松开鼠标时，立刻取消所有未完成的延时呼叫，防止逻辑错乱
        CancelInvoke();

        if (effector != null)
        {
            effector.enabled = false;
        }
    }

    // 延时结束后的实际物理激活方法
    private void ActivateGravityPhysics()
    {
        if (effector != null)
        {
            effector.enabled = true;
            // Debug.Log("黑洞凝聚完成，引力开始爆发！");
        }
    }
}
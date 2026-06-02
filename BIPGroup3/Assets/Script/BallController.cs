using UnityEngine;

public class BallController : MonoBehaviour
{
    private Rigidbody2D rb;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    public void Launch(Vector2 launchVelocity)
    {
        // Clean physics state before applying new velocity
        rb.linearVelocity = Vector2.zero; 
        rb.angularVelocity = 0f;
        
        
        // stable application of launch velocity
        rb.linearVelocity = launchVelocity;
    }
}
using UnityEngine;

public class PlayerController : MonoBehaviour
{
    public Animator animator;

    public float speed;

    private Rigidbody2D rb;

    private Vector2 direction;

    private void Awake()
    {
        animator = GetComponent<Animator>();
        rb = GetComponent<Rigidbody2D>();
    }

    private void FixedUpdate()
    {
        float horizontalInput = Input.GetAxisRaw("Horizontal");
        float verticalInput = Input.GetAxisRaw("Vertical");

        //Determine the direction by using the input, normalize to keep the speed the same when moving diagonally.
        direction = new Vector2(horizontalInput, verticalInput).normalized;

        //If the manager is currently timing, use the input to move, otherwise set velocity to zero.
        rb.velocity = PlayModeManager.Instance.timing ? direction * speed : Vector2.zero;

        //Set the moving animation when there is movement input.
        bool moving = direction != Vector2.zero;
        animator.SetBool("moving", moving);
    }
}
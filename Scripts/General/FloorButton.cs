using SaintsField;
using SaintsField.Playa;
using UnityEngine;
using UnityEngine.Events;

public class FloorButton : MonoBehaviour
{
    [LayoutStart("Button", ELayout.Horizontal, marginBottom: 4)]
    [LayoutStart("./Modifiers", ELayout.FoldoutBox)]
    [Tooltip("Locks the button when pressed by user")]
    [SerializeField] public bool pressLocking;
    [LayoutStart("../Statistics", ELayout.FoldoutBox)]
    [Tooltip("Admin locking by other scripts")]
    [SerializeField][ReadOnly] public bool locked;
    [SerializeField][ReadOnly] public bool pressed;
    [LayoutEnd]
    [SerializeField] public UnityEvent onPress;
    Animator anim;

    void Start()
    {
        GetComponent<SpriteRenderer>().sortingOrder = -1 * Mathf.RoundToInt((transform.position.y) * 10);
        anim = GetComponent<Animator>();
    }

    void FixedUpdate()
    {
        anim.SetBool("Pressed", pressed);
    }

    public void ResetButton()
    {
        pressed = false;
    }

    void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.gameObject.tag == "Player" && !pressed)
        {
            pressed = true;
            onPress?.Invoke();
            if (pressLocking) locked = true;
        }
    }
    void OnTriggerExit2D(Collider2D collision)
    {
        if (pressed
            && !locked)
        {
            ResetButton();
        }
    }
}

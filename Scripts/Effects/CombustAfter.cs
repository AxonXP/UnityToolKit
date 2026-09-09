using UnityEngine;

public class CombustAfter : MonoBehaviour
{
    [SerializeField] float time;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        Invoke("Combust", time);
    }

    void Combust()
    {
        Destroy(gameObject);
    }
}

using UnityEngine;

public class StickTo : MonoBehaviour
{
    [SerializeField] Transform ObjectToStickTo;

    void Update()
    {
        transform.position = ObjectToStickTo.position;
    }
}

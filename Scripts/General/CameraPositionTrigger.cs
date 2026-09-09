using UnityEngine;
using UnityEngine.Events;

public class CameraPositionTrigger : MonoBehaviour
{
    [System.Serializable]
    public class CameraStateEvent : UnityEvent<GameManager.CameraState> { }

    [SerializeField] GameManager.CameraState cameraState;
    [SerializeField] CameraStateEvent onTrigger;

    void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.tag == "Player")
        {
            onTrigger?.Invoke(cameraState);
        }
    }
}
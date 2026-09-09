using UnityEngine;

public class ZSortingLayer : MonoBehaviour
{
    [SerializeField] Transform transformReference;
    SpriteRenderer spriteRenderer;
    void Start()
    {
        if (!transformReference) transformReference = transform.root;
        spriteRenderer = GetComponent<SpriteRenderer>();
    }
    void LateUpdate()
    {
        spriteRenderer.sortingOrder = -1 * Mathf.RoundToInt((transformReference.position.y) * 10);
    }
}

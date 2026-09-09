using SaintsField.Playa;
using UnityEngine;

public class Health : MonoBehaviour
{
    [SerializeField] public float health;
    public void TakeDamage(float damage)
    {
        health -= damage;
        if (health <= 0)
        {
            SendMessage("Death");
        }
    }
}
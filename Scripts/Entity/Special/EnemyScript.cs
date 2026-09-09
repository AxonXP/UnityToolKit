using UnityEngine;

public class EnemyScript : MonoBehaviour
{
    public enum Monster
    {
        Chase = 1,
    }

    OverheadMovement movement;
    Entity entity;
    public Monster type;
    Transform player;
    OverheadFakeVertical fakeVertical;
    MoneyManager moneyManager;

    public MoneyManager.Currency moneyCurrency = MoneyManager.Currency.coins;
    public Vector2 moneyRange;

    void Start()
    {
        player = GameObject.FindGameObjectWithTag("Player").transform;
        movement = GetComponent<OverheadMovement>();
        entity = GetComponent<Entity>();
        fakeVertical = GetComponent<OverheadFakeVertical>();

        // roll this enemy's drop once on spawn so identical enemies don't all drop the same amount
        moneyManager = GetComponent<MoneyManager>();
        if (moneyManager != null)
            moneyManager.SetBalance(moneyCurrency, Random.Range(moneyRange.x, moneyRange.y));
    }

    void FixedUpdate()
    {
        switch (type)
        {
            case Monster.Chase:
                if (!fakeVertical.inAir)
                    movement.movementInput = (player.position - transform.position).normalized;
                else
                    movement.movementInput = Vector2.zero;
                break;
        }
    }
}

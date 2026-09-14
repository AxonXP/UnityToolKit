using System.Collections.Generic;
using System.ComponentModel;
using SaintsField;
using SaintsField.Playa;
using TMPro;
using UnityEngine;

[System.Serializable]
public class MonsterSpawn
{
    public GameObject monsterPrefab;
    public int spawnPoints;
    public int firstRoundAppearance;
}

public class GameManager : MonoBehaviour
{
    public enum GameState
    {
        Start,
        Starting,
        StartRound,
        InRound,
        EndRound,
        StoppingForShop,
        Stopped
    }
    public enum CameraState
    {
        Elevator,
        Shop,
        Start
    }
    [LayoutStart("States", ELayout.FoldoutBox)]
    public GameState gameState;
    public CameraState camState;
    [LayoutStart("Camera", ELayout.FoldoutBox)]
    [SaintsField.ReadOnly] public Vector3 currentCamPosition;
    [SaintsDictionary]
    public SaintsDictionary<CameraState, Vector3> camPositions = new();
    [LayoutStart("Stats", ELayout.FoldoutBox)]
    [SaintsField.ReadOnly] public int round;
    [SerializeField] public int shopRound;
    [LayoutStart("Shop", ELayout.FoldoutBox)]
    [SaintsField.Expandable]
    [SerializeField] ItemCollectionSO itemCollection;
    [SerializeField] GameObject shopItemPrefab;
    [SerializeField] List<Vector3> shopItemLocations;
    [SerializeField][SaintsField.ReadOnly] List<GameObject> shopItems = new();
    [LayoutStart("Spawning", ELayout.FoldoutBox)]
    public List<MonsterSpawn> monsterList = new List<MonsterSpawn>();
    public Vector2 spawnOffEdgeVelocity;
    public Vector2 maxSpawnLocation;
    public Vector2 minSpawnLocation;

    [LayoutStart("Management", ELayout.FoldoutBox)]
    public List<GameObject> walls;
    public GameObject outerElevator;
    [Tooltip("Min (x) / max (y) upward launch force applied to entities that touch a wall tagged \"Launching\".")]
    public Vector2 elevatorLaunchRange;
    [Tooltip("Min (x) / max (y) horizontal push force applied away from the wall to entities that touch a wall tagged \"Launching\".")]
    public Vector2 elevatorPushRange;
    public TextMeshProUGUI roundText;
    [SaintsField.ReadOnly] public FloorButton floorButton;
    [LayoutStart("Particles", ELayout.FoldoutBox)]

    public ParticleSystem elevatorNorm;
    public ParticleSystem elevatorStopping;
    public ParticleSystem elevatorStop;

    [LayoutStart("Screen Shake", ELayout.FoldoutBox)]
    [SaintsField.ReadOnly] public float intensity;
    public float baseIntensity;
    public AnimationCurve startingIntensity;
    public AnimationCurve stoppingIntensity;
    [SaintsField.ReadOnly] public float timer;

    void ChangeWallsLaunch(bool launching)
    {
        foreach (GameObject wall in walls)
        {
            wall.tag = launching ? "Launching" : "Untagged";
        }
    }

    void Start()
    {
        floorButton = GameObject.FindAnyObjectByType<FloorButton>();
    }

    public void Update()
    {
        CameraShake();
        GameStateUpdate();
        CameraUpdate();
    }

    void CameraUpdate()
    {
        if (camPositions.TryGetValue(camState, out Vector3 position))
            currentCamPosition = Vector3.Lerp(currentCamPosition, position, 1f / 16f);
    }

    public void CameraStateUpdate(CameraState state)
    {
        camState = state;
    }

    void GameStateUpdate()
    {
        switch (gameState)
        {
            case GameState.Start:
                ChangeWallsLaunch(false);
                if (elevatorNorm.isPlaying) elevatorNorm.Stop();
                if (elevatorStopping.isPlaying) elevatorStopping.Stop();
                if (elevatorStop.isPlaying) elevatorStop.Stop();
                roundText.enabled = false;
                timer = 0;
                break;
            case GameState.Starting:
                ChangeWallsLaunch(false);

                if (elevatorNorm.isPlaying) elevatorNorm.Stop();
                if (!elevatorStopping.isPlaying) elevatorStopping.Play();
                if (elevatorStop.isPlaying) elevatorStop.Stop();
                intensity = startingIntensity.Evaluate(timer);
                timer += Time.deltaTime;
                if (timer >= startingIntensity.length)
                {
                    timer = 0;
                    gameState = GameState.StartRound;
                    round += 1;
                    roundText.enabled = true;
                    roundText.text = "Round:" + round;
                    outerElevator.GetComponent<Animator>().Play("Leave");
                }
                break;
            case GameState.StartRound:
                ChangeWallsLaunch(true);
                if (!elevatorNorm.isPlaying) elevatorNorm.Play();
                if (elevatorStopping.isPlaying) elevatorStopping.Stop();
                if (elevatorStop.isPlaying) elevatorStop.Stop();
                intensity = baseIntensity;
                timer += Time.deltaTime;
                if (timer >= startingIntensity.length)
                {
                    timer = 0;
                    roundText.enabled = false;
                    SpawnWave(round);
                    gameState = GameState.InRound;
                }
                break;
            case GameState.InRound:
                ChangeWallsLaunch(true);
                if (!elevatorNorm.isPlaying) elevatorNorm.Play();
                if (elevatorStopping.isPlaying) elevatorStopping.Stop();
                if (elevatorStop.isPlaying) elevatorStop.Stop();

                if (!CheckMonstersAlive())
                {
                    gameState = GameState.EndRound;
                }

                intensity = baseIntensity;
                timer = 0;
                break;
            case GameState.EndRound:
                ChangeWallsLaunch(true);
                intensity = baseIntensity;
                if (round % shopRound == 0) // to shop
                {
                    gameState = GameState.StoppingForShop;
                }
                else // next round
                {
                    timer = 0;
                    gameState = GameState.StartRound;
                    round += 1;
                    roundText.enabled = true;
                    roundText.text = "Round:" + round;
                }
                break;
            case GameState.StoppingForShop:
                ChangeWallsLaunch(true);
                if (!elevatorNorm.isPlaying) elevatorNorm.Play();
                if (!elevatorStopping.isPlaying) elevatorStopping.Play();
                if (elevatorStop.isPlaying) elevatorStop.Stop();

                intensity = stoppingIntensity.Evaluate(timer);
                timer += Time.deltaTime;
                if (timer >= stoppingIntensity.length * 3 / 4)
                {
                    CreateShopItems();
                    gameState = GameState.Stopped;
                    outerElevator.GetComponent<Animator>().Play("LowerTo");
                    floorButton.ResetButton();
                }
                break;
            case GameState.Stopped:
                ChangeWallsLaunch(false);


                if (timer >= stoppingIntensity.length)
                {
                    intensity = 0;
                    if (elevatorNorm.isPlaying) elevatorNorm.Stop();
                    if (elevatorStopping.isPlaying) elevatorStopping.Stop();
                    elevatorStop.Play();
                    timer = 0;
                }
                else if (timer >= stoppingIntensity.length * 3 / 4)
                {
                    timer += Time.deltaTime;
                }
                break;
        }
    }

    public void SpawnWave(int wave)
    {
        // set up
        int points = wave * 10;
        List<MonsterSpawn> spawnableMonsters = new List<MonsterSpawn>();

        // fill up spawnableMonsters
        foreach (MonsterSpawn x in monsterList)
        {
            if (x.firstRoundAppearance <= wave)
            {
                spawnableMonsters.Add(x);
            }
        }

        // find way to spread points
        while (points > 0)
        {
            MonsterSpawn monsterChosen = spawnableMonsters[Random.Range(0, spawnableMonsters.Count)];
            SpawnMonster(monsterChosen.monsterPrefab, true);
            points -= monsterChosen.spawnPoints;
        }
    }

    void SpawnMonster(GameObject monster, bool dropIn)
    {
        float side = Random.Range(0, 4);
        float spawnX = 0;
        float spawnY = 0;
        Vector2 spawnVelocity;

        // choose a side and set the spawn location + velocity
        if (side <= 1)                  // top
        {
            spawnX = Random.Range(minSpawnLocation.x + 1, maxSpawnLocation.x - 1);
            spawnY = maxSpawnLocation.y;
            spawnVelocity = Vector2.down * Random.Range(spawnOffEdgeVelocity.x, spawnOffEdgeVelocity.y);
        }
        else if (side <= 2 && side > 1) // right
        {
            spawnY = Random.Range(minSpawnLocation.y + 1, maxSpawnLocation.y - 1);
            spawnX = maxSpawnLocation.x;
            spawnVelocity = Vector2.left * Random.Range(spawnOffEdgeVelocity.x, spawnOffEdgeVelocity.y);
        }
        else if (side <= 3 && side > 2) // bottom
        {
            spawnX = Random.Range(minSpawnLocation.x + 1, maxSpawnLocation.x - 1);
            spawnY = minSpawnLocation.y;
            spawnVelocity = Vector2.up * Random.Range(spawnOffEdgeVelocity.x, spawnOffEdgeVelocity.y);
        }
        else                            // left
        {
            spawnY = Random.Range(minSpawnLocation.y + 1, maxSpawnLocation.y - 1);
            spawnX = minSpawnLocation.x;
            spawnVelocity = Vector2.right * Random.Range(spawnOffEdgeVelocity.x, spawnOffEdgeVelocity.y);
        }

        // spawn the monster
        GameObject newMonster = Instantiate(monster, new Vector2(spawnX, spawnY), Quaternion.identity);
        newMonster.GetComponent<Rigidbody2D>().linearVelocity = spawnVelocity;
    }

    bool CheckMonstersAlive()
    {
        if (GameObject.FindObjectsByType<EnemyScript>(FindObjectsSortMode.None).Length <= 0)
            return false;
        else
            return true;
    }

    public void CameraShake()
    {
        Camera.main.transform.position = currentCamPosition + (Random.insideUnitSphere * intensity);
    }

    public void ButtonPressed()
    {
        switch (gameState)
        {
            case GameState.Start:
                ClearShopItems();
                gameState = GameState.Starting;
                break;
            case GameState.Stopped:
                ClearShopItems();
                gameState = GameState.Starting;
                break;
        }
    }

    public void OnDrawGizmos()
    {
        Gizmos.color = Color.green;
        Gizmos.DrawLine(minSpawnLocation, maxSpawnLocation);
    }

    public void CreateShopItems()
    {
        foreach (Vector3 location in shopItemLocations)
        {
            ItemSO chosenItem = itemCollection.GetRandomItem();
            if (chosenItem == null) continue;

            GameObject shopItem = Instantiate(shopItemPrefab, location, Quaternion.identity);
            if (shopItem.TryGetComponent<BuyableItem>(out BuyableItem buyableItem))
                buyableItem.SetItem(chosenItem);

            shopItems.Add(shopItem);
        }
    }

    public void ClearShopItems()
    {
        foreach (GameObject shopItem in shopItems)
        {
            if (shopItem != null) Destroy(shopItem);
        }
        shopItems.Clear();
    }
}
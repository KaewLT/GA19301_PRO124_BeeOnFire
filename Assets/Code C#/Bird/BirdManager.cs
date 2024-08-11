using System.Collections.Generic;
using UnityEngine;

public class BirdManager : MonoBehaviour
{
    //Quản lý cũng như tạo ra Bird
    public static event System.Action<Vector2> OnPlayerPositionUpdated;

    [SerializeField] private BirdSettings birdSettings;
    [SerializeField] private BirdBehavior birdPrefab;
    [SerializeField] private int initialBirdCount = 10;
    [SerializeField] private Transform player;

    private List<BirdBehavior> activeBirds = new List<BirdBehavior>();
    private Queue<BirdBehavior> birdPool = new Queue<BirdBehavior>();



    private void Start()
    {
        InitializeBirdPool();
        SpawnInitialBirds();
    }

    private void Update()
    {
        if (player != null)
        {
            OnPlayerPositionUpdated?.Invoke(player.position);
        }
    }

    private void InitializeBirdPool()
    {
        for (int i = 0; i < initialBirdCount; i++)
        {
            CreateBird();
        }
    }

    private void CreateBird()
    {
        BirdBehavior bird = Instantiate(birdPrefab, transform);
        bird.settings = birdSettings;
        bird.gameObject.SetActive(false);
        birdPool.Enqueue(bird);
    }

    private void SpawnInitialBirds()
    {
        for (int i = 0; i < initialBirdCount; i++)
        {
            SpawnBird();
        }
    }

    public void SpawnBird()
    {
        if (birdPool.Count == 0)
        {
            CreateBird();
        }

        BirdBehavior bird = birdPool.Dequeue();
        bird.transform.position = GetRandomPosition();
        bird.gameObject.SetActive(true);
        activeBirds.Add(bird);
    }

    public void DespawnBird(BirdBehavior bird)
    {
        bird.gameObject.SetActive(false);
        activeBirds.Remove(bird);
        birdPool.Enqueue(bird);
    }

    private Vector3 GetRandomPosition()
    {
        return new Vector3(
            Random.Range(-birdSettings.mapWidth / 2, birdSettings.mapWidth / 2),
            Random.Range(-birdSettings.mapHeight / 2, birdSettings.mapHeight / 2),
            0
        );
    }
}
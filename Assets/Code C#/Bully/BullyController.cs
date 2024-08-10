using Unity.Collections;
using System.Collections.Generic;
using UnityEngine;
using Pathfinding;
using System.Linq;
using Unity.Jobs;

public struct CalculateDistancesJob : IJobParallelFor
{
    [ReadOnly] public NativeArray<Vector2> points;
    [ReadOnly] public Vector2 center;
    [WriteOnly] public NativeArray<float> distances;

    public void Execute(int index)
    {
        distances[index] = Vector2.SqrMagnitude(points[index] - center);
    }
}

public class BullyController : MonoBehaviour
{
    private Seeker seeker;
    private AIPath aiPath;

    [Header("Interface")]
    [SerializeField] Animator animator;
    [SerializeField] private Vector2 movement;
    [SerializeField] Rigidbody2D rb2d;

    [Header("Obstacles")]
    [SerializeField] private LayerMask WhatIsObstacles;
    [SerializeField] private float avoidanceRadius = 1f;

    [Header("Player")]
    [SerializeField] private GameObject player;

    [Header("BullyBehaviour")]
    [Header("BullySpeed")]
    [SerializeField] private float moveSpeed = 5f;

    [Header("BullyChasePlayer")]
    [SerializeField] private float distance; //Khoảng cách giữa Bully và người chơi
    [SerializeField] private float distanceToPlayer; // Ngưỡng khoảng cách để Bully bắt đầu đi theo người chơi
    [SerializeField] private float maxDistance; // Khoảng cách tối đa Bully có thể đi
    [SerializeField] private float maxChaseTime = 5f;
    [SerializeField] private float predictionTime = 1f;

    [Header("BullySearch")]
    [SerializeField] private float maxSearchTime = 30f;
    [SerializeField] private float searchInterval = 1f; // Tìm kiếm mỗi giây
    [SerializeField] private float searchRadius = 10f;
    [SerializeField] private int randomPoints = 5;
    [SerializeField] private int spiralPoints = 10;

    [Header("BullyPatrol")]
    [SerializeField] private float patrolDuration;
    [SerializeField] private float patrolRadius;
    [SerializeField] private float randomnessFactor = 0.2f;

    [Header("BullyState")]
    [SerializeField] private BullyState currentState = BullyState.Idle;

    private Vector2[] patrolPoints;
    private float[,] transitionMatrix;
    private int currentPatrolIndex = 0;
    private Vector2 initialPosition;
    private Vector2 lastKnownPlayerPosition;
    private float searchTime = 0f;
    private List<Vector2> searchPoints = new List<Vector2>();
    private int currentSearchIndex = 0;
    private float timeSinceLastSearch = 0f;
    private float timeSinceLastSeenPlayer = 0f;
    private float patrolTimer = 0f;

    private enum BullyState
    {
        Idle,
        Chasing,
        Searching,
        Patrolling,
        ReturningToStart
    }


    private void Awake()
    {
        player = GameObject.FindGameObjectWithTag("Player");
        seeker = GetComponent<Seeker>();
        aiPath = GetComponent<AIPath>();
        animator = GetComponent<Animator>();
        rb2d = GetComponent<Rigidbody2D>();
    }

    void Start()
    {
        aiPath.enableRotation = false;
        initialPosition = transform.position;
        InitializeSearchPoints();
        InitializePatrolSystem();
    }

    private void FixedUpdate()
    {
        UpdateState();
        ExecuteCurrentState();
        distance = Vector2.Distance(transform.position, player.transform.position);
        movement = AvoidObstacles(movement);
        rb2d.velocity = movement * moveSpeed;
        UpdateAnimation();
    }

    private void UpdateState()
    {
        switch (currentState)
        {
            case BullyState.Idle:
                if (distance < distanceToPlayer && distance <= maxDistance && CanSeePlayer())
                {
                    currentState = BullyState.Chasing;
                }
                break;

            case BullyState.Chasing:
                if (!CanSeePlayer())
                {
                    if (timeSinceLastSeenPlayer > maxChaseTime)
                    {
                        currentState = BullyState.Searching;
                        searchTime = 0f;
                    }
                }
                break;

            case BullyState.Searching:
                searchTime += Time.deltaTime;
                if (distance < distanceToPlayer && distance <= maxDistance && CanSeePlayer())
                {
                    currentState = BullyState.Chasing;
                    searchTime = 0f;
                }
                else if (searchTime >= maxSearchTime && !CanSeePlayer())
                {
                    currentState = BullyState.Patrolling;
                }
                break;

            case BullyState.Patrolling:
                patrolTimer += Time.deltaTime;
                if (distance < distanceToPlayer && distance <= maxDistance && CanSeePlayer())
                {
                    currentState = BullyState.Chasing;
                    patrolTimer = 0f;
                }
                else if (patrolTimer > patrolDuration && aiPath.reachedEndOfPath)
                {
                    currentState = BullyState.ReturningToStart;
                    patrolTimer = 0f;
                }
                break;

            case BullyState.ReturningToStart:
                if (distance < distanceToPlayer && distance <= maxDistance && CanSeePlayer())
                {
                    currentState = BullyState.Chasing;
                    patrolTimer = 0f;
                }
                break;
        }
    }

    private void ExecuteCurrentState()
    {
        switch (currentState)
        {
            case BullyState.Idle:
                SetIdleState();
                break;
            case BullyState.Chasing:
                MoveTowardsPredictedPosition();
                break;
            case BullyState.Searching:
                SearchForPlayer();
                break;
            case BullyState.Patrolling:
                Patrol();
                break;
            case BullyState.ReturningToStart:
                ReturnToInitialPosition();
                break;
        }
    }

    private bool CanSeePlayer()
    {
        Vector2 directionToPlayer = (Vector2)player.transform.position - (Vector2)transform.position;
        float distanceToPlayer = directionToPlayer.magnitude;

        // Kiểm tra nếu người chơi ở trong khoảng cách hợp lệ
        if (distanceToPlayer < distance || distanceToPlayer > maxDistance)
        {
            timeSinceLastSeenPlayer += Time.deltaTime;
            return false;
        }

        // Kiểm tra có vật cản giữa NPC và người chơi
        RaycastHit2D hit = Physics2D.Raycast(transform.position, directionToPlayer, distanceToPlayer, WhatIsObstacles);
        if (hit.collider == null || hit.collider.CompareTag("Player"))
        {
            lastKnownPlayerPosition = player.transform.position;
            timeSinceLastSeenPlayer = 0f;
            return true;
        }

        timeSinceLastSeenPlayer += Time.deltaTime;
        return false;
    }

    private void SetIdleState()
    {
        ResetBully();
    }

    private void MoveTowardsPredictedPosition()
    {
        Vector2 predictedPosition = PredictPlayerPosition();
        seeker.StartPath(transform.position, predictedPosition, OnPathComplete);
        aiPath.maxSpeed = moveSpeed * (1 + Random.Range(-0.1f, 0.1f));
        movement = aiPath.desiredVelocity;
    }

    private Vector2 PredictPlayerPosition()
    {
        Vector2 playerVelocity = (Vector2)player.transform.position - lastKnownPlayerPosition;
        Vector2 predictedPosition = (Vector2)player.transform.position + playerVelocity * predictionTime;

        float maxPredictionDistance = maxDistance;
        if (Vector2.Distance(transform.position, predictedPosition) > maxPredictionDistance)
        {
            predictedPosition = (Vector2)transform.position + (predictedPosition - (Vector2)transform.position).normalized * maxPredictionDistance;
        }

        return predictedPosition;
    }

    private void OnPathComplete(Path p)
    {
        if (!p.error)
        {
            aiPath.canMove = true;
        }
    }

    private void Patrol()
    {
        if (patrolPoints == null || patrolPoints.Length == 0)
        {
            return;
        }

        if (aiPath.reachedEndOfPath)
        {
            int nextPatrolIndex = ChooseNextPatrolPoint();
            Vector2 nextPatrolPoint = patrolPoints[nextPatrolIndex];
            seeker.StartPath(transform.position, nextPatrolPoint, OnPathComplete);
            currentPatrolIndex = nextPatrolIndex;
        }
        movement = aiPath.desiredVelocity;
        UpdateAnimation();
    }

    private int ChooseNextPatrolPoint()
    {
        float randomValue = Random.value;
        if (randomValue < randomnessFactor)
        {
            return Random.Range(0, patrolPoints.Length);
        }
        else
        {
            float[] probabilities = new float[patrolPoints.Length];
            for (int i = 0; i < patrolPoints.Length; i++)
            {
                probabilities[i] = transitionMatrix[currentPatrolIndex, i];
            }

            float total = probabilities.Sum();
            float randomPoint = Random.value * total;

            for (int i = 0; i < probabilities.Length; i++)
            {
                if (randomPoint < probabilities[i])
                {
                    return i;
                }
                randomPoint -= probabilities[i];
            }
            return (currentPatrolIndex + 1) % patrolPoints.Length;
        }
    }

    private void ReturnToInitialPosition()
    {
        Vector2 currentPosition = transform.position;
        float distanceToInitialPosition = Vector2.Distance(currentPosition, initialPosition);

        // Kiểm tra nếu NPC đã gần đến vị trí ban đầu
        if (distanceToInitialPosition > 0.1f)
        {
            // Di chuyển NPC về vị trí ban đầu
            seeker.StartPath(transform.position, initialPosition, OnPathComplete);

            // Cập nhật animation di chuyển
            movement = aiPath.desiredVelocity;
            UpdateAnimation();
        }
        else
        {
            ResetBully();
        }
    }

    private void InitializeSearchPoints()
    {
        searchPoints.Clear();
        GenerateSearchPoints();
    }

    private void SearchForPlayer()
    {
        searchTime += Time.deltaTime;
        timeSinceLastSearch += Time.deltaTime;

        if (searchTime <= maxSearchTime)
        {
            // Tìm kiếm theo khoảng thời gian
            if (timeSinceLastSearch >= searchInterval)
            {
                Vector2 nextSearchPoint = GetNextSearchPoint();
                seeker.StartPath(transform.position, nextSearchPoint, OnPathComplete);
                movement = aiPath.desiredVelocity;
                timeSinceLastSearch = 0f;
            }
        }
        UpdateAnimation();
    }

    private void GenerateSearchPoints()
    {
        // Tạo điểm tìm kiếm theo mô hình xoắn ốc
        float angleStep = 2f * Mathf.PI / spiralPoints;
        for (int i = 0; i < spiralPoints; i++)
        {
            float angle = i * angleStep;
            float radius = Mathf.Lerp(0, searchRadius, (float)i / spiralPoints);
            Vector2 offset = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius;
            searchPoints.Add(offset);
        }

        // Thêm các điểm ngẫu nhiên
        for (int i = 0; i < randomPoints; i++)
        {
            searchPoints.Add(Random.insideUnitCircle * searchRadius);
        }

        // Sử dụng Job System để sắp xếp các điểm (nếu có nhiều điểm)
        SortSearchPointsJob();
    }

    private Vector2 GetNextSearchPoint()
    {
        Vector2 nextPoint = lastKnownPlayerPosition + searchPoints[currentSearchIndex];
        currentSearchIndex = (currentSearchIndex + 1) % searchPoints.Count;
        return nextPoint;
    }

    private void SortSearchPointsJob()
    {
        NativeArray<Vector2> nativePoints = new NativeArray<Vector2>(searchPoints.ToArray(), Allocator.TempJob);
        NativeArray<float> distances = new NativeArray<float>(searchPoints.Count, Allocator.TempJob);

        CalculateDistancesJob job = new CalculateDistancesJob
        {
            points = nativePoints,
            center = transform.position,
            distances = distances
        };

        JobHandle jobHandle = job.Schedule(searchPoints.Count, 64);
        jobHandle.Complete();

        // Sắp xếp các điểm dựa trên khoảng cách
        List<KeyValuePair<int, float>> indexDistancePairs = new List<KeyValuePair<int, float>>();
        for (int i = 0; i < distances.Length; i++)
        {
            indexDistancePairs.Add(new KeyValuePair<int, float>(i, distances[i]));
        }
        indexDistancePairs.Sort((a, b) => a.Value.CompareTo(b.Value));

        List<Vector2> sortedPoints = new List<Vector2>();
        foreach (var pair in indexDistancePairs)
        {
            sortedPoints.Add(searchPoints[pair.Key]);
        }

        searchPoints = sortedPoints;

        nativePoints.Dispose();
        distances.Dispose();
    }


    private Vector2 AvoidObstacles(Vector2 currentMovement)
    {
        Collider2D[] obstacles = Physics2D.OverlapCircleAll(transform.position, avoidanceRadius, WhatIsObstacles);
        Vector2 avoidanceForce = Vector2.zero;

        foreach (Collider2D obstacle in obstacles)
        {
            Vector2 directionToObstacle = obstacle.transform.position - transform.position;
            float distance = directionToObstacle.magnitude;
            avoidanceForce += -directionToObstacle.normalized * (avoidanceRadius / distance);
        }

        return (currentMovement + avoidanceForce).normalized;
    }

    private void InitializePatrolSystem()
    {
        if (patrolPoints == null || patrolPoints.Length == 0)
        {
            patrolPoints = new Vector2[]
            {
                initialPosition + Vector2.right * 5,
                initialPosition + Vector2.up * 5,
                initialPosition + Vector2.left * 5,
                initialPosition + Vector2.down * 5
            };
        }

        int pointCount = patrolPoints.Length;
        transitionMatrix = new float[pointCount, pointCount];

        for (int i = 0; i < pointCount; i++)
        {
            for (int j = 0; j < pointCount; j++)
            {
                if (i == j) continue;
                float distance = Vector2.Distance(patrolPoints[i], patrolPoints[j]);
                transitionMatrix[i, j] = 1f / distance;
            }

            float sum = 0;
            for (int j = 0; j < pointCount; j++)
            {
                sum += transitionMatrix[i, j];
            }
            for (int j = 0; j < pointCount; j++)
            {
                transitionMatrix[i, j] /= sum;
            }
        }
    }

    private void ResetBully()
    {
        // Đảm bảo rằng Bully ngừng di chuyển hoàn toàn
        rb2d.velocity = Vector2.zero;
        rb2d.angularVelocity = 0f;
        rb2d.Sleep(); // Dừng tất cả các hoạt động vật lý trên Rigidbody2D

        // Đặt lại các tham số của animator
        animator.SetFloat("Horizontal", 0f);
        animator.SetFloat("Vertical", 0f);
        animator.SetFloat("Speed", 0f);

        // Đặt lại các trạng thái hoặc biến khác của NPC
        currentState = BullyState.Idle; // Đặt lại trạng thái về Idle
        movement = Vector2.zero; // Đặt lại biến chuyển động
        timeSinceLastSearch = 0f; //Đặt lại thời gian tìm kiếm
        timeSinceLastSeenPlayer = 0f; // Đặt lại biến theo dõi thời gian
        searchTime = 0f; // Đặt lại thời gian tìm kiếm
        patrolTimer = 0f; // Đặt lại thời gian tuần tra
    }

    private void UpdateAnimation()
    {
        animator.SetFloat("Horizontal", movement.x);
        animator.SetFloat("Vertical", movement.y);
        animator.SetFloat("Speed", movement.sqrMagnitude);
    }
}
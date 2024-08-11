using System.Collections;
using UnityEngine;

public class BirdBehavior : MonoBehaviour
{
    public BirdSettings settings;

    [SerializeField] private Animator animator;
    [SerializeField] private AudioSource birdSound;
    [SerializeField] private float smoothTime = 0.3f;
    [SerializeField] private SpriteRenderer spriteRenderer;

    private Vector2 currentVelocity;
    private Vector2 targetPosition;
    private Vector2 velocity;
    private Vector2 playerPosition;
    private float fleeCooldownTime = 0f;

    private enum BirdState { Idle, Flying, Fleeing }
    private BirdState currentState = BirdState.Idle;

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        animator = GetComponent<Animator>();
        birdSound = GetComponent<AudioSource>();
    }

    private void OnEnable()
    {
        StartCoroutine(StateMachine());
        BirdManager.OnPlayerPositionUpdated += UpdatePlayerPosition;
    }

    private void OnDisable()
    {
        BirdManager.OnPlayerPositionUpdated -= UpdatePlayerPosition;
    }

    private void Update()
    {
        float distanceToPlayer = Vector2.Distance(transform.position, playerPosition);
        if (distanceToPlayer < settings.fleeDistance && Time.time > fleeCooldownTime)
        {
            SetState(BirdState.Fleeing);
        }

        if (currentState != BirdState.Idle)
        {
            Move();
        }
    }

    private void Move()
    {
        Vector2 currentPosition = transform.position;
        Vector2 desiredDirection = (targetPosition - currentPosition).normalized;
        Vector2 steering = desiredDirection - velocity;
        velocity = Vector2.ClampMagnitude(velocity + steering, settings.moveSpeed);

        AvoidObstacles();

        Vector2 smoothedPosition = Vector2.SmoothDamp(currentPosition, currentPosition + velocity, ref currentVelocity, smoothTime);

        if (!IsPositionOnAvoidLayer(smoothedPosition))
        {
            transform.position = smoothedPosition;
        }
        else
        {
            SetNewRandomTarget();
        }

        if (velocity != Vector2.zero)
        {
            float angle = Mathf.Atan2(velocity.y, velocity.x) * Mathf.Rad2Deg;
            transform.rotation = Quaternion.Euler(0, 0, angle);

            if (Mathf.Abs(velocity.x) > 0.1f)
            {
                spriteRenderer.flipY = (velocity.x < 0);
            }
        }

        if (Vector2.Distance(transform.position, targetPosition) < 0.1f)
        {
            SetState(BirdState.Idle);
        }

        animator.SetBool("IsFlying", true);
    }

    private bool IsPositionOnAvoidLayer(Vector3 position)
    {
        RaycastHit hit;
        if (Physics.Raycast(position + Vector3.up * 0.1f, Vector3.down, out hit, 0.2f, settings.avoidLayers))
        {
            return true;
        }
        return false;
    }

    private void SetNewRandomTarget()
    {
        Vector2 bias = ((Vector2)transform.position - playerPosition).normalized;
        int maxAttempts = 10;

        for (int i = 0; i < maxAttempts; i++)
        {
            Vector2 potentialTarget = new Vector2(
                Random.Range(-settings.mapWidth / 2, settings.mapWidth / 2) + bias.x * 2f,
                Random.Range(-settings.mapHeight / 2, settings.mapHeight / 2) + bias.y * 2f
            );

            float noiseX = Mathf.PerlinNoise(Time.time * 0.5f, 0) * 2 - 1;
            float noiseY = Mathf.PerlinNoise(0, Time.time * 0.5f) * 2 - 1;
            Vector2 noiseOffset = new Vector2(noiseX, noiseY) * 2f;

            potentialTarget += noiseOffset;

            if (!IsPositionOnAvoidLayer(potentialTarget))
            {
                targetPosition = potentialTarget;
                return;
            }
        }

        targetPosition = transform.position;
    }

    private void FleeFromPlayer()
    {
        birdSound.Play();
        Vector2 fleeDirection = ((Vector2)transform.position - playerPosition).normalized;
        fleeDirection += new Vector2(Random.Range(-0.5f, 0.5f), Random.Range(-0.5f, 0.5f));

        float maxDistance = settings.fleeDistance * 2f; // Tăng khoảng cách bay
        int maxAttempts = 20; // Tăng số lần thử để tìm được điểm xa nhất

        for (int i = 0; i < maxAttempts; i++)
        {
            Vector2 potentialTarget = (Vector2)transform.position + fleeDirection * maxDistance;
            if (!IsPositionOnAvoidLayer(potentialTarget))
            {
                targetPosition = potentialTarget;
                settings.moveSpeed *= settings.fleeSpeedMultiplier;

                StartCoroutine(FleeWithCurvedPath(transform.position, targetPosition, 1f));
                fleeCooldownTime = Time.time + 3f; // Cooldown 3 giây
                return;
            }
            fleeDirection = Quaternion.Euler(0, 0, Random.Range(-30f, 30f)) * fleeDirection; // Giảm góc quay để giữ hướng đi xa
        }

        targetPosition = transform.position;
        settings.moveSpeed *= settings.fleeSpeedMultiplier;
        fleeCooldownTime = Time.time + 3f;
    }

    private IEnumerator FleeWithCurvedPath(Vector2 start, Vector2 end, float time)
    {
        Vector2 control = (start + end) / 2f + Vector2.Perpendicular(end - start) * Random.Range(1f, 3f);
        float elapsedTime = 0;

        while (elapsedTime < time)
        {
            elapsedTime += Time.deltaTime;
            float t = elapsedTime / time;
            Vector2 position = QuadraticBezier(start, control, end, t);
            transform.position = position;

            Vector2 direction = (position - (Vector2)transform.position).normalized;
            float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
            transform.rotation = Quaternion.Euler(0, 0, angle);
            spriteRenderer.flipY = (direction.x < 0);

            yield return null;
        }
    }

    private Vector2 QuadraticBezier(Vector2 p0, Vector2 p1, Vector2 p2, float t)
    {
        float u = 1 - t;
        float tt = t * t;
        float uu = u * u;
        return uu * p0 + 2 * u * t * p1 + tt * p2;
    }

    private void AvoidObstacles()
    {
        RaycastHit2D hit = Physics2D.CircleCast(transform.position, settings.avoidanceRadius, velocity, settings.avoidanceRadius, settings.avoidLayers);
        if (hit.collider != null)
        {
            Vector2 avoidanceForce = Vector2.Reflect(velocity, hit.normal) * 0.5f; // Giảm hệ số nhân để tránh va chạm quá mức
            velocity = Vector2.ClampMagnitude(velocity + avoidanceForce, settings.moveSpeed);
        }
    }

    private void SetState(BirdState newState)
    {
        currentState = newState;
        switch (newState)
        {
            case BirdState.Flying:
                SetNewRandomTarget();
                velocity = Vector3.zero;
                break;
            case BirdState.Fleeing:
                FleeFromPlayer();
                velocity = Vector3.zero;
                break;
            case BirdState.Idle:
                velocity = Vector3.zero;
                animator.SetBool("IsFlying", false);
                settings.moveSpeed /= settings.fleeSpeedMultiplier;
                break;
        }
    }

    private IEnumerator StateMachine()
    {
        while (true)
        {
            switch (currentState)
            {
                case BirdState.Idle:
                    yield return new WaitForSeconds(Random.Range(settings.minIdleTime.x, settings.minIdleTime.y));
                    SetState(BirdState.Flying);
                    break;
                case BirdState.Flying:
                case BirdState.Fleeing:
                    yield return null;
                    break;
            }
        }
    }

    private void UpdatePlayerPosition(Vector2 newPosition)
    {
        playerPosition = newPosition;
    }
}
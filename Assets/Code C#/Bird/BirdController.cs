using UnityEngine;
using System.Collections;

public class BirdController : MonoBehaviour
{
    [SerializeField] public Transform[] landingAreas;
    public float flySpeed = 5f;
    public float avoidanceDistance = 3f;
    public float circlingRadius = 1f;
    public float circlingDuration = 2f;
    public float waveAmplitude = 0.5f;
    public float waveFrequency = 2f;
    public float maxAvoidanceForce = 10f; // Lực tránh né tối đa
    public float avoidanceSmoothTime = 0.5f; // Thời gian làm mượt

    public Animator birdAnimator;
    public AudioClip birdSound;

    
    private bool isFlying = false;
    private Rigidbody2D rb;
    private Vector2 currentDirection;
    private SpriteRenderer spriteRenderer;
    private AudioSource audioSource;
    private float flyTime;
    private Vector2 currentAvoidanceVelocity;
    private Vector2 smoothedAvoidanceForce;

    private void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        audioSource = GetComponent<AudioSource>();

        {
            if (landingAreas == null || landingAreas.Length == 0)
            {
                Debug.LogError("Landing areas not assigned in BirdController. Please assign them in the Inspector.");
                enabled = false; // Disable the script to prevent further errors
                return;
            }
        }
    }


    private void Update()
    {
        if (isFlying)
        {
            AvoidPlayer();
        }
    }

    private void FixedUpdate()
    {
        if (isFlying)
        {
            AvoidPlayer();
            ApplyWaveMotion();
        }
    }
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player") && !isFlying)
        {
            PlayBirdSound();
            FlyAway();
        }
    }

    private void PlayBirdSound()
    {
        
        {
            audioSource.PlayOneShot(birdSound);
           
        }
    }

    private void FlyAway()
    {
        isFlying = true;
        Vector3 randomPosition = GetRandomLandingPosition();
        StartCoroutine(FlyToPosition(randomPosition));
    }

    private Vector3 GetRandomLandingPosition()
    {
        if (landingAreas == null || landingAreas.Length == 0)
        {
            Debug.LogError("No landing areas available in BirdController.");
            return transform.position; // Return current position as fallback
        }

        Transform selectedArea = landingAreas[Random.Range(0, landingAreas.Length)];

        if (selectedArea == null)
        {
            Debug.LogError("Selected landing area is null in BirdController.");
            return transform.position; // Return current position as fallback
        }

        float randomX = Random.Range(-selectedArea.localScale.x / 2, selectedArea.localScale.x / 2);
        float randomY = Random.Range(-selectedArea.localScale.y / 2, selectedArea.localScale.y / 2);

        return selectedArea.position + new Vector3(randomX, randomY, 0);
    }

    private IEnumerator FlyToPosition(Vector3 targetPosition)
    {
        birdAnimator.SetBool("IsFlying", true);
        birdAnimator.SetBool("Idle", false);

        // Fly up
        Vector3 flyUpPosition = transform.position + Vector3.up * 5f;
        yield return StartCoroutine(FlyToIntermediatePosition(flyUpPosition));

        // Move to the target position
        yield return StartCoroutine(FlyToIntermediatePosition(targetPosition));

        // Circle around before landing
        yield return StartCoroutine(CircleAroundPosition(targetPosition));

        // Land
        yield return StartCoroutine(FlyToIntermediatePosition(targetPosition));

        rb.velocity = Vector2.zero;
        isFlying = false;

        birdAnimator.SetBool("IsFlying", false);
        birdAnimator.SetBool("Idle", true);
    }

    private IEnumerator FlyToIntermediatePosition(Vector3 position)
    {
        while (Vector3.Distance(transform.position, position) > 0.1f)
        {
            Vector3 direction = (position - transform.position).normalized;
            Vector2 desiredVelocity = direction * flySpeed;

            // Kết hợp vận tốc mong muốn với lực tránh né
            Vector2 combinedVelocity = desiredVelocity + smoothedAvoidanceForce;

            rb.velocity = combinedVelocity;
            UpdateSpriteDirection(rb.velocity);
            flyTime += Time.deltaTime;
            yield return null;
        }
    }

    private IEnumerator CircleAroundPosition(Vector3 center)
    {
        float startTime = Time.time;
        while (Time.time - startTime < circlingDuration)
        {
            float angle = (Time.time - startTime) * flySpeed;
            Vector3 offset = new Vector3(Mathf.Sin(angle), Mathf.Cos(angle), 0) * circlingRadius;
            Vector3 circlePosition = center + offset;

            Vector3 direction = (circlePosition - transform.position).normalized;
            rb.velocity = direction * flySpeed;
            UpdateSpriteDirection(direction);
            flyTime += Time.deltaTime;

            yield return null;
        }
    }

    private void UpdateSpriteDirection(Vector2 direction)
    {
        currentDirection = direction;
        if (currentDirection.x > 0)
        {
            spriteRenderer.flipX = true; // Flip left
        }
        else if (currentDirection.x < 0)
        {
            spriteRenderer.flipX = false; // Flip right
        }
    }

    private void ApplyWaveMotion()
    {
        float waveOffset = Mathf.Sin(flyTime * waveFrequency) * waveAmplitude;
        Vector2 waveMotion = transform.up * waveOffset;
        rb.velocity += waveMotion;
    }

    private void AvoidPlayer()
    {
        Collider2D[] nearbyObjects = Physics2D.OverlapCircleAll(transform.position, avoidanceDistance);
        Vector2 avoidanceForce = Vector2.zero;

        foreach (Collider2D obj in nearbyObjects)
        {
            if (obj.CompareTag("Player"))
            {
                Vector2 directionAway = (Vector2)transform.position - (Vector2)obj.transform.position;
                float distance = directionAway.magnitude;
                float avoidanceStrength = 1 - (distance / avoidanceDistance);
                avoidanceForce += directionAway.normalized * avoidanceStrength * maxAvoidanceForce;
            }
        }

        // Áp dụng làm mượt cho lực tránh né
        smoothedAvoidanceForce = Vector2.SmoothDamp(smoothedAvoidanceForce, avoidanceForce, ref currentAvoidanceVelocity, avoidanceSmoothTime);

        // Giới hạn lực tránh né tối đa
        smoothedAvoidanceForce = Vector2.ClampMagnitude(smoothedAvoidanceForce, maxAvoidanceForce);

        // Áp dụng lực tránh né
        rb.AddForce(smoothedAvoidanceForce);

        // Cập nhật hướng sprite nếu đang tránh né
        if (smoothedAvoidanceForce.magnitude > 0.1f)
        {
            UpdateSpriteDirection(smoothedAvoidanceForce);
        }
    }



    private void OnDrawGizmosSelected()
    {
        // Vẽ vòng tròn avoidance
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, avoidanceDistance);
        // vòng tròn hạ cánh 

        Gizmos.color = Color.yellow;
        foreach (Transform landingArea in landingAreas)
        {
            Gizmos.DrawWireCube(landingArea.position, landingArea.localScale);
        }
    }
}
using UnityEngine;
using System.Collections;

public class BirdController : MonoBehaviour
{
    // Mảng các Transform đại diện cho các vị trí hạ cánh có thể chỉnh sửa trong Inspector
    [SerializeField] public Transform[] landingAreas;

    // Tốc độ bay của chim
    public float flySpeed = 5f;
    // Khoảng cách mà chim bắt đầu tránh né người chơi
    public float avoidanceDistance = 3f;
    // Bán kính của vòng tròn mà chim bay quanh vị trí mục tiêu
    public float circlingRadius = 1f;
    // Thời gian chim bay vòng quanh vị trí mục tiêu
    public float circlingDuration = 2f;
    // Biên độ của chuyển động sóng khi chim bay
    public float waveAmplitude = 0.5f;
    // Tần số của chuyển động sóng khi chim bay
    public float waveFrequency = 2f;
    // Lực tránh né tối đa mà chim có thể áp dụng
    public float maxAvoidanceForce = 10f;
    // Thời gian làm mượt lực tránh né
    public float avoidanceSmoothTime = 0.5f;
    // Khoảng thời gian tối thiểu giữa các lần tự động bay
    public float minAutoFlyInterval = 15f;
    // Khoảng thời gian tối đa giữa các lần tự động bay
    public float maxAutoFlyInterval = 30f;

    // Thời điểm mà chim sẽ tự động bay tiếp theo
    private float nextAutoFlyTime;

    // Tham chiếu đến component Animator của chim
    public Animator birdAnimator;
    // Âm thanh mà chim sẽ phát ra
    public AudioClip birdSound;

    // Biến kiểm tra xem chim có đang bay hay không
    private bool isFlying = false;
    // Tham chiếu đến component Rigidbody2D của chim
    private Rigidbody2D rb;
    // Hướng di chuyển hiện tại của chim
    private Vector2 currentDirection;
    // Tham chiếu đến component SpriteRenderer của chim
    private SpriteRenderer spriteRenderer;
    // Tham chiếu đến component AudioSource của chim
    private AudioSource audioSource;
    // Thời gian chim đã bay
    private float flyTime;
    // Vận tốc tránh né hiện tại của chim
    private Vector2 currentAvoidanceVelocity;
    // Lực tránh né đã được làm mượt
    private Vector2 smoothedAvoidanceForce;

    private void Start()
    {
        // Lấy component Rigidbody2D từ GameObject hiện tại
        rb = GetComponent<Rigidbody2D>();
        // Lấy component SpriteRenderer từ GameObject hiện tại
        spriteRenderer = GetComponent<SpriteRenderer>();
        // Lấy component AudioSource từ GameObject hiện tại
        audioSource = GetComponent<AudioSource>();

        // Kiểm tra xem mảng landingAreas có được gán giá trị hay không
        if (landingAreas == null || landingAreas.Length == 0)
        {
            // Nếu không, in ra thông báo lỗi và vô hiệu hóa script
            Debug.LogError("Landing areas not assigned in BirdController. Please assign them in the Inspector.");
            enabled = false;
            return;
        }
        // Đặt lại bộ đếm thời gian cho lần bay tự động tiếp theo
        ResetAutoFlyTimer();
    }

    private void Update()
    {
        // Nếu chim đang bay, thực hiện tránh né người chơi
        if (isFlying)
        {
            AvoidPlayer();
        }
        // Nếu chim không bay và đến thời điểm bay tự động, và người chơi không ở gần, thì bay đi
        else if (Time.time >= nextAutoFlyTime && !IsPlayerNearby())
        {
            FlyAway();
        }
    }

    private bool IsPlayerNearby()
    {
        // Tìm tất cả các collider trong phạm vi tránh né
        Collider2D[] nearbyObjects = Physics2D.OverlapCircleAll(transform.position, avoidanceDistance);
        // Kiểm tra từng collider
        foreach (Collider2D obj in nearbyObjects)
        {
            // Nếu có collider nào là của Player, trả về true
            if (obj.CompareTag("Player"))
            {
                return true;
            }
        }
        // Nếu không tìm thấy Player, trả về false
        return false;
    }

    private void ResetAutoFlyTimer()
    {
        // Đặt thời điểm bay tự động tiếp theo là một khoảng thời gian ngẫu nhiên trong tương lai
        nextAutoFlyTime = Time.time + Random.Range(minAutoFlyInterval, maxAutoFlyInterval);
    }

    private void FixedUpdate()
    {
        // Nếu chim đang bay
        if (isFlying)
        {
            // Thực hiện tránh né người chơi
            AvoidPlayer();
            // Áp dụng chuyển động sóng
            ApplyWaveMotion();
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        // Nếu người chơi chạm vào chim và chim đang không bay
        if (other.CompareTag("Player") && !isFlying)
        {
            // Phát âm thanh của chim
            PlayBirdSound();
            // Bắt đầu bay đi
            FlyAway();
        }
    }

    private void PlayBirdSound()
    {
        // Phát âm thanh của chim một lần
        audioSource.PlayOneShot(birdSound);
    }

    private void FlyAway()
    {
        // Đặt lại bộ đếm thời gian cho lần bay tự động tiếp theo
        ResetAutoFlyTimer();
        // Đặt trạng thái bay là true
        isFlying = true;
        // Lấy một vị trí hạ cánh ngẫu nhiên
        Vector3 randomPosition = GetRandomLandingPosition();
        // Bắt đầu quá trình bay đến vị trí đó
        StartCoroutine(FlyToPosition(randomPosition));
    }

    private Vector3 GetRandomLandingPosition()
    {
        // Kiểm tra xem có khu vực hạ cánh nào được định nghĩa không
        if (landingAreas == null || landingAreas.Length == 0)
        {
            Debug.LogError("No landing areas available in BirdController.");
            return transform.position;
        }

        // Chọn ngẫu nhiên một khu vực hạ cánh
        Transform selectedArea = landingAreas[Random.Range(0, landingAreas.Length)];

        // Kiểm tra xem khu vực được chọn có hợp lệ không
        if (selectedArea == null)
        {
            Debug.LogError("Selected landing area is null in BirdController.");
            return transform.position;
        }

        // Chọn một điểm ngẫu nhiên trong phạm vi của khu vực hạ cánh
        float randomX = Random.Range(-selectedArea.localScale.x / 2, selectedArea.localScale.x / 2);
        float randomY = Random.Range(-selectedArea.localScale.y / 2, selectedArea.localScale.y / 2);

        // Trả về vị trí ngẫu nhiên đã chọn
        return selectedArea.position + new Vector3(randomX, randomY, 0);
    }

    private IEnumerator FlyToPosition(Vector3 targetPosition)
    {
        // Bắt đầu animation bay
        birdAnimator.SetBool("IsFlying", true);
        birdAnimator.SetBool("Idle", false);

        // Bay lên
        Vector3 flyUpPosition = transform.position + Vector3.up * 5f;
        yield return StartCoroutine(FlyToIntermediatePosition(flyUpPosition));

        // Bay đến vị trí mục tiêu
        yield return StartCoroutine(FlyToIntermediatePosition(targetPosition));

        // Bay vòng quanh vị trí mục tiêu
        yield return StartCoroutine(CircleAroundPosition(targetPosition));

        // Hạ cánh
        yield return StartCoroutine(FlyToIntermediatePosition(targetPosition));

        // Kết thúc bay
        rb.velocity = Vector2.zero;
        isFlying = false;

        // Chuyển sang animation đứng yên
        birdAnimator.SetBool("IsFlying", false);
        birdAnimator.SetBool("Idle", true);
    }

    private IEnumerator FlyToIntermediatePosition(Vector3 position)
    {
        // Tiếp tục bay cho đến khi đến gần vị trí mục tiêu
        while (Vector3.Distance(transform.position, position) > 0.1f)
        {
            // Tính toán hướng bay
            Vector3 direction = (position - transform.position).normalized;
            Vector2 desiredVelocity = direction * flySpeed;

            // Kết hợp vận tốc mong muốn với lực tránh né
            Vector2 combinedVelocity = desiredVelocity + smoothedAvoidanceForce;

            // Áp dụng vận tốc cho Rigidbody
            rb.velocity = combinedVelocity;
            // Cập nhật hướng của sprite
            UpdateSpriteDirection(rb.velocity);
            // Tăng thời gian bay
            flyTime += Time.deltaTime;
            yield return null;
        }
    }

    private IEnumerator CircleAroundPosition(Vector3 center)
    {
        float startTime = Time.time;
        // Bay vòng tròn trong khoảng thời gian circlingDuration
        while (Time.time - startTime < circlingDuration)
        {
            // Tạo chuyển động bay vòng tròn
            float angle = (Time.time - startTime) * flySpeed;
            Vector3 offset = new Vector3(Mathf.Sin(angle), Mathf.Cos(angle), 0) * circlingRadius;
            Vector3 circlePosition = center + offset;

            // Áp dụng chuyển động vòng tròn
            Vector3 direction = (circlePosition - transform.position).normalized;
            rb.velocity = direction * flySpeed;
            UpdateSpriteDirection(direction);
            flyTime += Time.deltaTime;

            yield return null;
        }
    }

    private void UpdateSpriteDirection(Vector2 direction)
    {
        // Cập nhật hướng di chuyển hiện tại
        currentDirection = direction;
        // Lật sprite theo hướng di chuyển
        if (currentDirection.x > 0)
        {
            spriteRenderer.flipX = true;
        }
        else if (currentDirection.x < 0)
        {
            spriteRenderer.flipX = false;
        }
    }

    private void ApplyWaveMotion()
    {
        // Tạo chuyển động sóng dựa trên thời gian bay
        float waveOffset = Mathf.Sin(flyTime * waveFrequency) * waveAmplitude;
        Vector2 waveMotion = transform.up * waveOffset;
        // Thêm chuyển động sóng vào vận tốc hiện tại
        rb.velocity += waveMotion;
    }

    private void AvoidPlayer()
    {
        // Tìm các đối tượng gần chim
        Collider2D[] nearbyObjects = Physics2D.OverlapCircleAll(transform.position, avoidanceDistance);
        Vector2 avoidanceForce = Vector2.zero;

        foreach (Collider2D obj in nearbyObjects)
        {
            if (obj.CompareTag("Player"))
            {
                // Tính toán lực tránh né
                Vector2 directionAway = (Vector2)transform.position - (Vector2)obj.transform.position;
                float distance = directionAway.magnitude;
                float avoidanceStrength = 1 - (distance / avoidanceDistance);
                avoidanceForce += directionAway.normalized * avoidanceStrength * maxAvoidanceForce;
            }
        }

        // Làm mượt lực tránh né
        smoothedAvoidanceForce = Vector2.SmoothDamp(smoothedAvoidanceForce, avoidanceForce, ref currentAvoidanceVelocity, avoidanceSmoothTime);

        // Giới hạn lực tránh né tối đa
        smoothedAvoidanceForce = Vector2.ClampMagnitude(smoothedAvoidanceForce, maxAvoidanceForce);

        // Áp dụng lực tránh né
        rb.AddForce(smoothedAvoidanceForce);

        // Cập nhật hướng sprite nếu có lực tránh né đáng kể
        if (smoothedAvoidanceForce.magnitude > 0.1f)
        {
            UpdateSpriteDirection(smoothedAvoidanceForce);
        }
    }

    private void OnDrawGizmosSelected()
    {
        // Vẽ Gizmo cho khoảng cách tránh né
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, avoidanceDistance);

        // Vẽ Gizmo cho các khu vực hạ cánh
        Gizmos.color = Color.yellow;
        if (landingAreas != null)
        {
            foreach (Transform landingArea in landingAreas)
            {
                if (landingArea != null)
                {
                    Gizmos.DrawWireCube(landingArea.position, landingArea.localScale);
                }
            }
        }
    }
}
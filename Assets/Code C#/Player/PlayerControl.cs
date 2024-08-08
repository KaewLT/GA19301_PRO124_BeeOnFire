﻿using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections.Generic;
using System.Collections;
using UnityEngine.UI;

public class PlayerControl : MonoBehaviour
{

    [Header("Interface")]
    [SerializeField] private Animator animator;
    [SerializeField] private Rigidbody2D rb2d;
    [SerializeField] private InventoryManager inventoryManager;

    [Header("Movement Settings")]
    private float moveSpeed;
    [SerializeField] private float maxSpeed = 5f;
    [SerializeField] private float acceleration = 50f;
    [SerializeField] private float deceleration = 100f;
    [SerializeField] private float velocityPower = 0.9f;
    [SerializeField] private float frictionAmount = 0.2f;

    [Header("NPC Interaction")]
    [SerializeField] private float interactDistance = 2f; //Khoảng cách để tương tác với npc (nếu cần)

    [Header("Buff Settings")]
    [SerializeField] private Coroutine buffSpeedCoroutine;
    [SerializeField] private Coroutine debuffSpeedCoroutine;
    [SerializeField] private Coroutine blinkCoroutine;
    [SerializeField] float buffDuration = 5f;
    [SerializeField] float blinkDuration = 2f;
    [SerializeField] private float debuffDuration = 5f; // Thời gian giảm tốc độ
    [SerializeField] Image buffSpeedIcon; // Icon cho buff
    [SerializeField] private GameObject shadowPrefab; // Prefab cho bóng mờ

    [Header("Gameplay")]
    [SerializeField] public Door nearbyDoor;


    private Vector2 movement;
    private Vector2 smoothedMovement;

    private void Awake()
    {
        animator = GetComponent<Animator>();
        rb2d = GetComponent<Rigidbody2D>();
    }

    private void Start()
    {
        inventoryManager = FindObjectOfType<InventoryManager>();
        moveSpeed = maxSpeed;
    }

    private void FixedUpdate()
    {
        SmoothMovement();
        Move();
        ApplyFriction();
    }

    private void Update()
    {
        UpdateAnimation();

        if (Input.GetKeyDown(KeyCode.E))
        {
            InventoryManager.Instance.ToggleInventory();
        }

        if (nearbyDoor != null && Input.GetKeyDown(KeyCode.G))
        {
            nearbyDoor.TryOpenDoor();
        }

        if (Input.GetKeyDown(KeyCode.F))
        {
            Item selectedItem = inventoryManager.GetSelectedItem(false);
            if (selectedItem != null)
            {
                if (!(selectedItem is Key))  // Only use items that are not KeyItem
                {
                    selectedItem.Use(this);
                    inventoryManager.GetSelectedItem(true); // Consume the item
                }
            }
        }
    }

    private void OnMove(InputValue value)
    {
        movement = value.Get<Vector2>();
    }

    private void SmoothMovement()
    {
        smoothedMovement = Vector2.Lerp(smoothedMovement, movement, Time.fixedDeltaTime * 10f);
    }

    private void Move()
    {
        // Tính toán vận tốc tối đa
        Vector2 targetVelocity = smoothedMovement * maxSpeed;

        // Tính toán vận tốc hiện tại và tối đa
        Vector2 velocityDiff = targetVelocity - rb2d.velocity;

        // Tính toán lực gia tốc
        Vector2 accelerationForce = velocityDiff * acceleration;

        // Áp dụng lực vào Rigidbody2D
        rb2d.AddForce(accelerationForce);

        // Giới hạn tốc độ tối đa
        rb2d.velocity = Vector2.ClampMagnitude(rb2d.velocity, maxSpeed);
    }

    private void ApplyFriction()
    {
        // Áp dụng ma sát khi người chơi bỏ tay ra khỏi nút bấm
        if (movement.magnitude < 0.01f)
        {
            float frictionForce = Mathf.Min(rb2d.velocity.magnitude, frictionAmount);
            rb2d.AddForce(-rb2d.velocity.normalized * frictionForce, ForceMode2D.Impulse);
        }
    }

    private void UpdateAnimation()
    {
        animator.SetFloat("Horizontal", smoothedMovement.x);
        animator.SetFloat("Vertical", smoothedMovement.y);
        animator.SetFloat("Speed", smoothedMovement.sqrMagnitude);
    }

    public void ApplyBuffSpeed()
    {
        if (buffSpeedCoroutine != null)
        {
            StopCoroutine(buffSpeedCoroutine);
        }

        if (blinkCoroutine != null)
        {
            StopCoroutine(blinkCoroutine);
            blinkCoroutine = null;
        }

        buffSpeedCoroutine = StartCoroutine(BuffSpeedItemCoroutine());
    }

    private IEnumerator BuffSpeedItemCoroutine()
    {
        float buffSpeedIncrease = maxSpeed * 0.5f; // Tăng 50% so với tốc độ gốc
        moveSpeed = maxSpeed + buffSpeedIncrease;

        if (buffSpeedIcon != null)
        {
            buffSpeedIcon.gameObject.SetActive(true); // Hiển thị icon
        }

        StartCoroutine(CreateShadows(buffDuration)); // Bắt đầu tạo bóng mờ

        yield return new WaitForSeconds(buffDuration - blinkDuration); // Chờ buffDuration - blinkDuration giây

        if (buffSpeedIcon != null)
        {
            blinkCoroutine = StartCoroutine(BlinkIcon(buffSpeedIcon, blinkDuration)); // Bắt đầu nhấp nháy
        }

        yield return new WaitForSeconds(blinkDuration); // Chờ thêm blinkDuration giây

        ResetBuffSpeed();
    }

    private IEnumerator BlinkIcon(Image icon, float duration)
    {
        float endTime = Time.time + duration;
        while (Time.time < endTime)
        {
            icon.gameObject.SetActive(!icon.gameObject.activeSelf); // Chuyển đổi trạng thái hiện/ẩn
            yield return new WaitForSeconds(0.2f); // Thời gian nhấp nháy
        }
        icon.gameObject.SetActive(false); // Ẩn icon khi kết thúc
    }

    private void ResetBuffSpeed()
    {
        moveSpeed = maxSpeed; // Khôi phục tốc độ di chuyển ban đầu
        if (buffSpeedIcon != null)
        {
            buffSpeedIcon.gameObject.SetActive(false); // Ẩn icon
        }

        buffSpeedCoroutine = null;
        blinkCoroutine = null;
    }

    private IEnumerator CreateShadows(float duration, float shadowAlpha = 0.5f, float shadowExistTime = 0.25f, float shadowSpawnInterval = 0.1f, Vector3 shadowOffset = default(Vector3))
    {
        float endTime = Time.time + duration;

        // Mảng các màu sắc để chọn ngẫu nhiên
        Color[] shadowColors = new Color[]
        {
        Color.red,
        Color.green,
        Color.blue,
        Color.yellow,
        Color.magenta
        };

        while (Time.time < endTime)
        {
            GameObject shadow = Instantiate(shadowPrefab, transform.position + shadowOffset, transform.rotation);
            SpriteRenderer shadowSprite = shadow.GetComponent<SpriteRenderer>();
            shadowSprite.sprite = GetComponent<SpriteRenderer>().sprite;

            // Chọn ngẫu nhiên một màu từ mảng shadowColors
            Color shadowColor = shadowColors[Random.Range(0, shadowColors.Length)];
            shadowColor.a = shadowAlpha; // Độ trong suốt của bóng mờ
            shadowSprite.color = shadowColor;

            Destroy(shadow, shadowExistTime); // Bóng mờ tồn tại trong shadowExistTime giây

            yield return new WaitForSeconds(shadowSpawnInterval); // Tạo bóng mờ sau mỗi shadowSpawnInterval giây
        }
    }

    public void ApplySick()
    {
        StartCoroutine(PlayerSick());
    }

    private IEnumerator PlayerSick()
    {
        float buffSpeedIncrease = maxSpeed * 0.25f;
        moveSpeed = maxSpeed - buffSpeedIncrease;
        yield return new WaitForSeconds(10f);
        moveSpeed = maxSpeed;
    }
}
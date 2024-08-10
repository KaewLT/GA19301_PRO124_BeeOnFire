//using UnityEngine;
//using System.Collections;

//namespace Pathfinding {
//	/// <summary>
//	/// Simple patrol behavior.
//	/// This will set the destination on the agent so that it moves through the sequence of objects in the <see cref="targets"/> array.
//	/// Upon reaching a target it will wait for <see cref="delay"/> seconds.
//	///
//	/// See: <see cref="Pathfinding.AIDestinationSetter"/>
//	/// See: <see cref="Pathfinding.AIPath"/>
//	/// See: <see cref="Pathfinding.RichAI"/>
//	/// See: <see cref="Pathfinding.AILerp"/>
//	/// </summary>
//	[UniqueComponent(tag = "ai.destination")]
//	[HelpURL("http://arongranberg.com/astar/docs/class_pathfinding_1_1_patrol.php")]
//	public class Patrol : VersionedMonoBehaviour {
//		/// <summary>Target points to move to in order</summary>
//		public Transform[] targets;

//		/// <summary>Time in seconds to wait at each target</summary>
//		public float delay = 0;

//		/// <summary>Current target index</summary>
//		int index;

//		IAstarAI agent;
//		float switchTime = float.PositiveInfinity;

//		protected override void Awake () {
//			base.Awake();
//			agent = GetComponent<IAstarAI>();
//		}

//		/// <summary>Update is called once per frame</summary>
//		void Update () {
//			if (targets.Length == 0) return;

//			bool search = false;

//			// Note: using reachedEndOfPath and pathPending instead of reachedDestination here because
//			// if the destination cannot be reached by the agent, we don't want it to get stuck, we just want it to get as close as possible and then move on.
//			if (agent.reachedEndOfPath && !agent.pathPending && float.IsPositiveInfinity(switchTime)) {
//				switchTime = Time.time + delay;
//			}

//			if (Time.time >= switchTime) {
//				index = index + 1;
//				search = true;
//				switchTime = float.PositiveInfinity;
//			}

//			index = index % targets.Length;
//			agent.destination = targets[index].position;

//			if (search) agent.SearchPath();
//		}
//	}
//}

using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Pathfinding;
using System;

namespace Pathfinding
{
    [UniqueComponent(tag = "ai.destination")]
    [HelpURL("http://arongranberg.com/astar/docs/class_pathfinding_1_1_patrol.php")]
    public class Patrol : VersionedMonoBehaviour
    {
        [SerializeField] public Transform[] waypoints;
        [SerializeField] public float delay = 0;
        [SerializeField] public float detectionDistance = 2f; // Distance to detect cars ahead
        [SerializeField] public LayerMask carLayerMask; // Layer mask for car detection
        [SerializeField] public float slowDownFactor = 0.5f; // Factor to slow down the car when an obstacle is detected
        [SerializeField] public float checkInterval = 0.2f; // Time interval for obstacle checks
        [SerializeField] public float intersectionStopDuration = 1f; // Duration to stop at an intersection
        private bool isWaiting = false;
        [SerializeField] public float entryTime;
        [SerializeField] public float knockbackForce;
        [SerializeField] public float maxSpeed = 10f; // Base max speed
        [SerializeField] public float debuffPercentage = 0.5f; // Speed debuff percentage
        [SerializeField] public float acceleration = 5f; // Acceleration force


        private int index = 0;
        private IAstarAI agent;
        private Seeker seeker;
        private float switchTime = float.PositiveInfinity;
        private bool isSlowedDown = false;
        private float nextCheckTime = 0f;
        private float originalSpeed;
        private bool isStoppingAtIntersection = false;
        private Rigidbody2D rb;
        private PlayerControl player;
        [SerializeField] private LayerMask playerLayer; // Set this to the layer of the player

        [SerializeField] public SpriteSet currentSpriteSet;
        [SerializeField] public AnimationSet currentAnimationSet;
        [SerializeField] public CarColliderConfig leftRightCarColliderConfig;
        [SerializeField] public CarColliderConfig upDownCarColliderConfig;

        private Animator animator;
        private SpriteRenderer spriteRenderer;
        private BoxCollider2D carCollider;
        private Vector3 previousPosition;

        protected override void Awake()
        {
            base.Awake();
            rb = GetComponent<Rigidbody2D>();
            player = GetComponent<PlayerControl>();
            agent = GetComponent<IAstarAI>();
            seeker = GetComponent<Seeker>();
            animator = GetComponent<Animator>();
            spriteRenderer = GetComponent<SpriteRenderer>();
            carCollider = GetComponent<BoxCollider2D>();
            previousPosition = transform.position;
        }

        void Start()
        {
            originalSpeed = agent.maxSpeed;
        }

        void Update()
        {
            if (waypoints.Length == 0) return;

            if (Time.time >= nextCheckTime)
            {
                nextCheckTime = Time.time + checkInterval;

                if (CheckForObstaclesAlongPath())
                {
                    SlowDown();
                }
                else if (isSlowedDown)
                {
                    ResumeNormalSpeed();
                }
            }

            if (isStoppingAtIntersection)
            {
                return; // Skip movement if stopping at intersection
            }

            bool search = false;

            if (agent.reachedEndOfPath && !agent.pathPending && float.IsPositiveInfinity(switchTime))
            {
                switchTime = Time.time + delay;
            }

            if (Time.time >= switchTime)
            {
                NextWaypoint();
                search = true;
                switchTime = float.PositiveInfinity;
            }

            UpdateDirectionAndAnimation();

            agent.destination = waypoints[index].position;

            if (search) agent.SearchPath();

            previousPosition = transform.position;
        }

        void NextWaypoint()
        {
            index = (index + 1) % waypoints.Length;
        }

        void UpdateDirectionAndAnimation()
        {
            Vector3 currentVelocity = (transform.position - previousPosition) / Time.deltaTime;

            if (Mathf.Abs(currentVelocity.x) > Mathf.Abs(currentVelocity.y))
            {
                if (currentVelocity.x > 0)
                {
                    //Debug.Log("Right");
                    UpdateCarState(currentSpriteSet.rightSprite, currentAnimationSet.rightClip.name, leftRightCarColliderConfig);
                }
                else if (currentVelocity.x < 0)
                {
                    //Debug.Log("Left");
                    UpdateCarState(currentSpriteSet.leftSprite, currentAnimationSet.leftClip.name, leftRightCarColliderConfig);
                }
            }
            else
            {
                if (currentVelocity.y > 0)
                {
                    //Debug.Log("Up");
                    UpdateCarState(currentSpriteSet.upSprite, currentAnimationSet.upClip.name, upDownCarColliderConfig);
                }
                else if (currentVelocity.y < 0)
                {
                    //Debug.Log("Down");
                    UpdateCarState(currentSpriteSet.downSprite, currentAnimationSet.downClip.name, upDownCarColliderConfig);
                }
            }
        }
        public void AllowToProceed()
        {
            isWaiting = false;
            agent.isStopped = false;
            // Resume normal movement
        }
        void UpdateCarState(Sprite sprite, string animationClipName, CarColliderConfig colliderConfig)
        {
            spriteRenderer.sprite = sprite;
            animator.Play(animationClipName);
            AdjustCarColliderSizeAndOffset(colliderConfig);
        }

        void AdjustCarColliderSizeAndOffset(CarColliderConfig config)
        {
            if (carCollider != null)
            {
                carCollider.size = config.size;
                carCollider.offset = config.offset;
            }
        }

        bool CheckForObstaclesAlongPath()
        {
            var path = seeker.GetCurrentPath();
            if (path != null && path.vectorPath != null)
            {
                foreach (var point in path.vectorPath)
                {
                    Collider2D hit = Physics2D.OverlapCircle(point, detectionDistance, carLayerMask);
                    if (hit != null && hit.transform != transform)
                    {
                        Debug.Log("Obstacle detected at point: " + point + " Obstacle: " + hit.transform.name);
                        return true;
                    }
                }
            }
            return false;
        }

        void SlowDown()
        {
            if (!isSlowedDown)
            {
                Debug.Log("Slowing down");
                agent.maxSpeed = originalSpeed * slowDownFactor;
                isSlowedDown = true;
            }
        }

        void ResumeNormalSpeed()
        {
            if (isSlowedDown)
            {
                Debug.Log("Resuming normal speed");
                agent.maxSpeed = originalSpeed;
                isSlowedDown = false;
            }
        }

        void OnTriggerEnter2D(Collider2D other)
        {
            if (other.CompareTag("Intersection"))
            {
                StartCoroutine(HandleIntersection());
            }
        }

        IEnumerator HandleIntersection()
        {
            isStoppingAtIntersection = true;
            Debug.Log("Stopped at intersection");

            // Stop for a fixed duration
            yield return new WaitForSeconds(intersectionStopDuration);

            // Check if there are other cars at the intersection
            // This should be your logic to determine who should go first
            // For now, just assume it is okay to continue after stopping
            isStoppingAtIntersection = false;

            Debug.Log("Continuing after intersection");
        }
        public void StopAtIntersection(float waitTime)
        {
            entryTime = Time.time;
            isWaiting = true;
            agent.isStopped = true;
            Invoke("AllowToProceed", waitTime); // Allow the car to proceed after wait time
        }
        //void OnCollisionEnter2D(Collision2D collision)
        //{
        //    if (collision.gameObject.CompareTag("Player"))
        //    {
        //        ApplyKnockback(collision);
        //    }
        //}
        //void ApplyKnockback(Collision2D collision)
        //{
        //    Vector2 knockbackDirection = (rb.position - collision.rigidbody.position).normalized;
        //    rb.AddForce(knockbackDirection * knockbackForce, ForceMode2D.Impulse);

        //    // Also apply knockback to the other car/player
        //    Rigidbody2D otherRb = collision.rigidbody;
        //    if (otherRb != null)
        //    {
        //        otherRb.AddForce(-knockbackDirection * knockbackForce, ForceMode2D.Impulse);
        //    }
        //}
        //private bool IsPlayer(GameObject obj)
        //{
        //    // Check if the object has a Player tag or is in the playerLayer
        //    return obj.CompareTag("Player") || (playerLayer == (playerLayer | (1 << obj.layer)));
        //}
    }
}

[System.Serializable]
public class CarColliderConfig
{
    [SerializeField] public Vector2 size;
    [SerializeField] public Vector2 offset;
}

[System.Serializable]
public class SpriteSet
{
    [SerializeField] public Sprite leftSprite;
    [SerializeField] public Sprite rightSprite;
    [SerializeField] public Sprite upSprite;
    [SerializeField] public Sprite downSprite;
}

[System.Serializable]
public class AnimationSet
{
    [SerializeField] public AnimationClip leftClip;
    [SerializeField] public AnimationClip rightClip;
    [SerializeField] public AnimationClip upClip;
    [SerializeField] public AnimationClip downClip;
}



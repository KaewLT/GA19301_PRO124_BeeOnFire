using UnityEngine;

[CreateAssetMenu(fileName = "BirdSettings", menuName = "Bird/Settings")]
public class BirdSettings : ScriptableObject
{
    public float moveSpeed = 2f;
    public float steeringForce = 10f;
    public float fleeSpeedMultiplier = 1.5f;
    public float fleeDistance = 3f;
    public float mapWidth = 10f;
    public float mapHeight = 5f;
    public float avoidanceRadius = 1f;
    public float smoothTime = 1f;
    public Vector2 minIdleTime = new Vector2(3f, 7f);
    public LayerMask avoidLayers;
}
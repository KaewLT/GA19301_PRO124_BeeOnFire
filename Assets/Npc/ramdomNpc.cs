using UnityEngine;

public class RandomPositionGenerator : MonoBehaviour
{
    public GameObject objectToMove;
    private float minX = -44.9f;
    private float maxX = 105.9f;
    private float minY = 106.6f;
    private float maxY = 3.6f;

    void Start()
    {
        InvokeRepeating("SetRandomPosition", 0f, 10f);
    }

    public void SetRandomPosition()
    {
        float randomX = Random.Range(minX, maxX);
        float randomY = Random.Range(minY, maxY);
        objectToMove.transform.position = new Vector3(randomX, randomY, objectToMove.transform.position.z);
    }
}
using UnityEngine;
using UnityEngine.Rendering.Universal;

public class VisionReductionItem : MonoBehaviour
{
    public float reducedInnerRadius = 1;    // Giá trị inner radius muốn giảm xuống
    public float reducedOuterRadius = 10f;      // Giá trị outer radius muốn giảm xuống
    public Material listMaterial;               // Material mới với Sprite-Lit-Default shader
    private bool isTriggered = false;

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player") && !isTriggered)
        {
            isTriggered = true;
            ChangeMaterialsToLit();
            Light2D playerLight = other.GetComponentInChildren<Light2D>();
            VisionReductionItemManager.Instance.ReduceLightGradually(playerLight, reducedInnerRadius, reducedOuterRadius);
            Destroy(gameObject);
        }
    }

    private void ChangeMaterialsToLit()
    {
        // Lấy tất cả các SpriteRenderer trong cảnh
        SpriteRenderer[] spriteRenderers = FindObjectsOfType<SpriteRenderer>();

        // Thay đổi material của mỗi SpriteRenderer thành material mới
        foreach (SpriteRenderer renderer in spriteRenderers)
        {
            if (renderer.material.shader.name == "Sprites/Default")
            {
                renderer.material = listMaterial;
            }
        }
    }
}
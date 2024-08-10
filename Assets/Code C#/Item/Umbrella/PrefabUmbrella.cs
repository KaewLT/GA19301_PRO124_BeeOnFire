using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PrefabUmbrella : MonoBehaviour
{
    public GameObject umbrellaPrefab;
    private GameObject attachedUmbrella;
    private UnderRain underRain;
    private Coroutine umbrellaCoroutine; // Thêm biến để lưu trữ Coroutine

    void Start()
    {
        underRain = FindObjectOfType<UnderRain>();
    }

    public void AttachUmbrellaToPlayer(GameObject Adam_Player)
    {
        if (attachedUmbrella != null)
        {
            Destroy(attachedUmbrella); // Hủy cây dù hiện có nếu đã tồn tại
            attachedUmbrella = null;
        }

        attachedUmbrella = Instantiate(umbrellaPrefab, Adam_Player.transform);
        attachedUmbrella.transform.localPosition = new Vector3(-0.5f, 0.8f, 0);
        underRain.SetUmbrellaState(true);

        if (umbrellaCoroutine != null)
        {
            StopCoroutine(umbrellaCoroutine);
        }

        // Bắt đầu Coroutine mới
        umbrellaCoroutine = StartCoroutine(RemoveUmbrellaAfterTime());
    }

    IEnumerator RemoveUmbrellaAfterTime()
    {

        float timer = 0f;

        while (timer < 30f)
        {
            yield return null;

            timer += Time.deltaTime;
        }
        RemoveAttachedUmbrella();
    }

    public void RemoveAttachedUmbrella()
    {
        if (attachedUmbrella != null)
        {
            Destroy(attachedUmbrella);
            underRain.SetUmbrellaState(false); // Thiết lập trạng thái không có dù
        }
    }
}

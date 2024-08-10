using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Rendering.Universal;

public class VisionReductionItemManager : MonoBehaviour
{
    private static VisionReductionItemManager instance;
    public static VisionReductionItemManager Instance { get { return instance; } }

    public float reductionDuration = 1f;       // Thời gian giảm tầm nhìn
    public float intensityDuration = 3f;       // Thời gian duy trì cường độ ánh sáng tăng lên
    public float restorationDuration = 3f;     // Thời gian để ánh sáng tăng trở lại từ từ

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(this.gameObject);
        }
        else
        {
            instance = this;
            DontDestroyOnLoad(this.gameObject);
        }
    }

    public void ReduceLightGradually(Light2D playerLight, float reducedInnerRadius, float reducedOuterRadius)
    {
        StartCoroutine(ReduceLightCoroutine(playerLight, reducedInnerRadius, reducedOuterRadius));
    }

    private IEnumerator ReduceLightCoroutine(Light2D playerLight, float reducedInnerRadius, float reducedOuterRadius)
    {
        // Lưu giá trị ban đầu của inner và outer radius
        float originalInnerRadius = playerLight.pointLightInnerRadius;
        float originalOuterRadius = playerLight.pointLightOuterRadius;

        // Giảm inner và outer radius của ánh sáng từ từ
        float elapsedTime = 0f;
        while (elapsedTime < reductionDuration)
        {
            elapsedTime += Time.deltaTime;
            playerLight.pointLightInnerRadius = Mathf.Lerp(originalInnerRadius, reducedInnerRadius, elapsedTime / reductionDuration);
            playerLight.pointLightOuterRadius = Mathf.Lerp(originalOuterRadius, reducedOuterRadius, elapsedTime / reductionDuration);
            yield return null;
        }

        // Đảm bảo rằng giá trị cuối cùng là giá trị giảm tối đa
        playerLight.pointLightInnerRadius = reducedInnerRadius;
        playerLight.pointLightOuterRadius = reducedOuterRadius;


        // Chờ trong thời gian duy trì cường độ ánh sáng tăng lên
        yield return new WaitForSeconds(intensityDuration);

        // Tăng dần inner và outer radius của ánh sáng trở lại giá trị ban đầu
        elapsedTime = 0f;
        while (elapsedTime < restorationDuration)
        {
            elapsedTime += Time.deltaTime;
            playerLight.pointLightInnerRadius = Mathf.Lerp(reducedInnerRadius, originalInnerRadius, elapsedTime / restorationDuration);
            playerLight.pointLightOuterRadius = Mathf.Lerp(reducedOuterRadius, originalOuterRadius, elapsedTime / restorationDuration);
            yield return null;
        }

        // Đảm bảo rằng giá trị cuối cùng là giá trị ban đầu
        playerLight.pointLightInnerRadius = originalInnerRadius;
        playerLight.pointLightOuterRadius = originalOuterRadius;
    }
}
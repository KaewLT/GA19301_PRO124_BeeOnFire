﻿using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEditorInternal.Profiling.Memory.Experimental;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UIElements;

public class AddItemValue : MonoBehaviour
{
    private static readonly string GetItemValueURL = "https://phamduchuan.name.vn/GetValueItems.php";
    // Start is called before the first frame update

    public NewItem neww;
    void Start()
    {
        StartCoroutine(GetItems());
    }
    public ItemValueList itemList;
    IEnumerator GetItems()
    {
        UnityWebRequest www = UnityWebRequest.Get(GetItemValueURL);
        yield return www.SendWebRequest();

        if (www.result != UnityWebRequest.Result.Success)
        {
            Debug.LogError("Error: " + www.error);
        }
        else
        {
            string json = www.downloadHandler.text;
            List<ItemValueList.itemValueList> items = JsonConvert.DeserializeObject<List<ItemValueList.itemValueList>>(json);
            itemList.Itemp = items;
            foreach (ItemValueList.itemValueList item in items)
            {
                Debug.Log("Item ID: " + item.Items_Id + ", Name: " + item.Items_Name + ", Feature: " + item.Feature + ", Duration: " + item.Duration);
            }
            GenerateOrUpdateScriptableObjects();
        }
    }

    public ItemValueList itemValueList;

    [ContextMenu("Generate Or Update ScriptableObjects")]
    void GenerateOrUpdateScriptableObjects()
    {
        // Tạo thư mục lưu trữ các ScriptableObject nếu chưa tồn tại
        string folderPath = "Assets/ItemsData";
        if (!System.IO.Directory.Exists(folderPath))
        {
            System.IO.Directory.CreateDirectory(folderPath);
        }

        foreach (var item in itemValueList.Itemp)
        {
            // Tạo đường dẫn file cho ScriptableObject
            string assetPath = $"{folderPath}/Item_{item.Items_Name}.asset";

            // Kiểm tra xem ScriptableObject đã tồn tại chưa
            NewItem existingItem = AssetDatabase.LoadAssetAtPath<NewItem>(assetPath);
            if (existingItem == null)
            {
                // Nếu không tồn tại, tạo mới
                existingItem = ScriptableObject.CreateInstance<NewItem>();
                AssetDatabase.CreateAsset(existingItem, assetPath);
            }

            // Cập nhật dữ liệu của ScriptableObject
            existingItem.Items_Id = item.Items_Id;
            existingItem.Items_Name = item.Items_Name;
            existingItem.Feature = item.Feature;
            existingItem.Duration = item.Duration;

            // Lưu thay đổi
            EditorUtility.SetDirty(existingItem);
        }

        // Cập nhật tất cả tài sản để đảm bảo các thay đổi được lưu
        AssetDatabase.SaveAssets();
    }
}

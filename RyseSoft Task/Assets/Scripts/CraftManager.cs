// CraftManager.cs - Runtime Only
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;
using System.IO;
using System.Collections.Generic;
using Cinemachine;

public class CraftManager : MonoBehaviour
{
    public static CraftManager Instance;

    [Header("UI References")]
    public Button craftButton;
    public GameObject namingPanel;
    public TMPro.TMP_InputField nameInput;
    public Button confirmButton;

    [Header("Workbench")]
    public Transform workbenchContent;
    public Transform workbenchPlacementCollider;
    public string workbenchTag = "WorkbenchArea";

    [Header("Tracing")]
    public LineRenderer traceLinePrefab;
    public float traceTolerance = 0.4f;

    private List<GameObject> workbenchItems = new List<GameObject>();
    private LineRenderer currentTrace;
    private List<Vector3> targetPath;
    private bool isTracing = false;
    private bool hasStartedTracing = false;

    private void Awake()
    {
        Instance = this;

        craftButton.gameObject.SetActive(false);
        namingPanel.SetActive(false);
        
        workbenchContent.gameObject.GetComponent<BoxCollider>().enabled = false;
        
        craftButton.onClick.AddListener(StartCrafting);
        confirmButton.onClick.AddListener(ConfirmCraft);
    }

    private void Update()
    {
        UpdateCraftButtonVisibility();

        if (!isTracing || currentTrace == null) return;

        if (Input.GetMouseButtonDown(0))
        {
            hasStartedTracing = true;
        }

        if (Input.GetMouseButton(0) && hasStartedTracing)
        {
            Camera cam = GetWorkbenchCamera();
            if (cam == null) return;

            Ray ray = cam.ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(ray, out RaycastHit hit, 100f, ~LayerMask.GetMask("UI")))
            {
                if (hit.collider.CompareTag(workbenchTag))
                {
                    Vector3 mousePos = hit.point + Vector3.up * 0.03f;

                    if (targetPath.Count > 0 && Vector3.Distance(mousePos, targetPath[0]) < 1.25f)
                    {
                        targetPath.RemoveAt(0);
                        currentTrace.positionCount = targetPath.Count;
                        currentTrace.SetPositions(targetPath.ToArray());

                        if (targetPath.Count <= 1)
                            CraftSuccess();
                    }
                }
            }
        }

        if (hasStartedTracing && Input.GetMouseButtonUp(0) && targetPath.Count > 1)
        {
            CraftFailed();
        }
    }

    private void UpdateCraftButtonVisibility()
    {
        int itemCount = 0;
        foreach (Transform child in workbenchContent)
        {
            if (child.gameObject.activeInHierarchy)
                itemCount++;
        }

        bool canCraft = itemCount >= 2;
        if (craftButton.gameObject.activeSelf != canCraft)
        {
            craftButton.gameObject.SetActive(canCraft);
        }
    }

    public void StartCrafting()
    {
        workbenchContent.gameObject.GetComponent<BoxCollider>().enabled = true;
        workbenchPlacementCollider.gameObject.GetComponent<BoxCollider>().enabled = false;
        
        workbenchItems.Clear();
        foreach (Transform child in workbenchContent)
        {
            if (child.gameObject.activeInHierarchy)
                workbenchItems.Add(child.gameObject);
        }

        if (workbenchItems.Count < 2) return;

        GenerateTracePath();
        ShowTraceGuide();
        isTracing = true;
        craftButton.gameObject.SetActive(false);
    }

    private void GenerateTracePath()
    {
        targetPath = new List<Vector3>();
        Vector3 center = GetCenter();
        float traceHeight = center.y + 0.25f;

        int segments = workbenchItems.Count + 2;
        for (int i = 0; i < segments; i++)
        {
            float angle = i * Mathf.PI * 2f / segments;
            float radius = 0.08f + (i % 3) * 0.03f;
            Vector3 offset = new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)) * radius;
            Vector3 pos = center + offset;
            pos.y = traceHeight;
            targetPath.Add(pos);
        }
        targetPath.Add(targetPath[0]);
    }

    private void ShowTraceGuide()
    {
        Vector3 center = GetCenter();
        currentTrace = Instantiate(traceLinePrefab, center, Quaternion.identity, workbenchContent);
    
        currentTrace.useWorldSpace = true;
        currentTrace.positionCount = targetPath.Count;
        currentTrace.SetPositions(targetPath.ToArray());

        var mat = new Material(Shader.Find("HDRP/Lit"));
        mat.SetColor("_BaseColor", new Color(0, 1, 1, 0.9f));
        mat.SetColor("_EmissiveColor", Color.cyan * 12f);
        mat.EnableKeyword("_EMISSIVE_COLOR_MAP");
        currentTrace.material = mat;
    
        currentTrace.startWidth = 0.025f;
        currentTrace.endWidth = 0.025f;
    }

    private void CraftSuccess()
    {
        isTracing = false;
        currentTrace.material.SetColor("_EmissiveColor", Color.green * 15f);

        namingPanel.SetActive(true);
        nameInput.text = "";
        nameInput.Select();
    }

    private void CraftFailed()
    {
        isTracing = false;
        currentTrace.material.SetColor("_EmissiveColor", Color.red * 10f);
        Invoke("ResetCraft", 1.5f);
    }

    private void ResetCraft()
    {
        if (currentTrace) Destroy(currentTrace.gameObject);
        namingPanel.SetActive(false);
        isTracing = false;
        hasStartedTracing = false;
        UpdateCraftButtonVisibility();
    }

    public void ConfirmCraft()
    {
        string name = string.IsNullOrEmpty(nameInput.text) ? "Unnamed Creation" : nameInput.text.Trim();
        CreateNewItemRuntime(name);
        namingPanel.SetActive(false);
        nameInput.text = "";
        
        workbenchContent.gameObject.GetComponent<BoxCollider>().enabled = false;
        workbenchPlacementCollider.gameObject.GetComponent<BoxCollider>().enabled = true;
    }

    private void CreateNewItemRuntime(string itemName)
    {
        Bounds totalBounds = new Bounds(workbenchItems[0].transform.position, Vector3.zero);
        foreach (var item in workbenchItems)
        {
            var rend = item.GetComponentInChildren<Renderer>();
            if (rend) totalBounds.Encapsulate(rend.bounds);
        }
        Vector3 geometricCenter = totalBounds.center;

        GameObject finalGO = new GameObject(itemName);
        finalGO.transform.position = geometricCenter;
        finalGO.transform.rotation = Quaternion.identity;
        finalGO.transform.localScale = Vector3.one;

        var combine = new CombineInstance[workbenchItems.Count];
        Material[] sharedMaterials = null;

        for (int i = 0; i < workbenchItems.Count; i++)
        {
            var mf = workbenchItems[i].GetComponentInChildren<MeshFilter>();
            var mr = workbenchItems[i].GetComponentInChildren<MeshRenderer>();
            if (mf && mr)
            {
                combine[i].mesh = mf.sharedMesh;
                combine[i].transform = finalGO.transform.worldToLocalMatrix * mf.transform.localToWorldMatrix;
                if (sharedMaterials == null) sharedMaterials = mr.sharedMaterials;
            }
        }

        Mesh finalMesh = new Mesh();
        finalMesh.name = itemName + "_Mesh";
        finalMesh.CombineMeshes(combine, true, true);

        finalMesh.RecalculateBounds();
        Vector3 pivotOffset = finalMesh.bounds.center;
        Vector3[] vertices = finalMesh.vertices;
        for (int i = 0; i < vertices.Length; i++)
            vertices[i] -= pivotOffset;
        finalMesh.vertices = vertices;
        finalMesh.RecalculateBounds();

        var filter = finalGO.AddComponent<MeshFilter>();
        filter.sharedMesh = finalMesh;

        var renderer = finalGO.AddComponent<MeshRenderer>();
        renderer.sharedMaterials = sharedMaterials ?? new Material[] { new Material(Shader.Find("HDRP/Lit")) };

        finalGO.transform.position += finalGO.transform.TransformVector(pivotOffset);

        Texture2D iconTex = CaptureIconHDRP(finalGO);
        string description = $"Player-crafted fusion of {workbenchItems.Count} items";

        // Save to runtime system
        RuntimeItemManager.Instance.SavePlayerCreatedItem(itemName, finalMesh, iconTex, description);
        
        // Create runtime item and add to storage
        ItemData runtimeItemData = CreateRuntimeItemData(itemName, finalMesh, iconTex, description);
        AddItemToStorage(runtimeItemData);
        
        // Notify save system
        RuntimeItemManager.Instance.OnItemCrafted(runtimeItemData);

        // Cleanup
        foreach (var item in workbenchItems)
            if (item) Destroy(item.gameObject);
        Destroy(finalGO);
    }

    private ItemData CreateRuntimeItemData(string itemName, Mesh mesh, Texture2D icon, string description)
    {
        GameObject runtimePrefab = new GameObject(itemName);
        MeshFilter filter = runtimePrefab.AddComponent<MeshFilter>();
        MeshRenderer renderer = runtimePrefab.AddComponent<MeshRenderer>();
        
        filter.sharedMesh = mesh;
        
        Material material = new Material(Shader.Find("HDRP/Lit"));
        if (workbenchItems.Count > 0)
        {
            var sourceRenderer = workbenchItems[0].GetComponentInChildren<Renderer>();
            if (sourceRenderer != null && sourceRenderer.sharedMaterial != null)
            {
                material.CopyPropertiesFromMaterial(sourceRenderer.sharedMaterial);
            }
        }
        renderer.sharedMaterial = material;
        
        runtimePrefab.AddComponent<MeshCollider>();
        
        ItemData itemData = ScriptableObject.CreateInstance<ItemData>();
        itemData.itemName = itemName;
        itemData.prefab = runtimePrefab;
        itemData.mesh = mesh;
        
        Sprite iconSprite = Sprite.Create(icon, new Rect(0, 0, icon.width, icon.height), new Vector2(0.5f, 0.5f));
        itemData.icon = iconSprite;
        itemData.description = description;
        
        return itemData;
    }

    private void AddItemToStorage(ItemData itemData)
    {
        bool itemExists = false;
        foreach (var slot in GameManager.Instance.storageBoxData.slots)
        {
            if (slot.item != null && slot.item.itemName == itemData.itemName)
            {
                slot.quantity++;
                itemExists = true;
                break;
            }
        }
        
        if (!itemExists)
        {
            GameManager.Instance.storageBoxData.slots.Add(new InventorySlot(itemData, 1));
        }
        
        MainUIManager.Instance?.RefreshWorkbenchUI();
    }

    private Texture2D CaptureIconHDRP(GameObject target)
    {
        Vector3 originalPos = target.transform.position;
        target.transform.position = Vector3.zero;

        Camera cam = GetWorkbenchCamera();
        if (cam == null) { target.transform.position = originalPos; return Texture2D.whiteTexture; }

        var oldRT = cam.targetTexture;
        var oldClear = cam.clearFlags;
        var oldBG = cam.backgroundColor;
        var oldMask = cam.cullingMask;

        var rt = new RenderTexture(256, 256, 24, RenderTextureFormat.ARGB32);
        rt.antiAliasing = 4;

        cam.targetTexture = rt;
        cam.clearFlags = CameraClearFlags.Color;
        cam.backgroundColor = new Color(0, 0, 0, 0);
        cam.cullingMask = LayerMask.GetMask("Default");

        int originalLayer = target.layer;
        foreach (Transform t in target.GetComponentsInChildren<Transform>(true))
            t.gameObject.layer = LayerMask.NameToLayer("Default");

        cam.Render();

        RenderTexture.active = rt;
        Texture2D tex = new Texture2D(256, 256, TextureFormat.RGBA32, false);
        tex.ReadPixels(new Rect(0, 0, 256, 256), 0, 0);
        tex.Apply();

        cam.targetTexture = oldRT;
        cam.clearFlags = oldClear;
        cam.backgroundColor = oldBG;
        cam.cullingMask = oldMask;
        RenderTexture.active = null;
        rt.Release();
        Destroy(rt);
        
        target.transform.position = originalPos;

        return tex;
    }
    
    private Camera GetWorkbenchCamera()
    {
        if (GameManager.Instance.workbenchCamera != null)
        {
            var brain = Camera.main?.GetComponent<CinemachineBrain>();
            if (brain != null && brain.ActiveVirtualCamera != null)
            {
                return Camera.main;
            }

            var vcam = GameManager.Instance.workbenchCamera;
            if (vcam.Follow != null)
            {
                var camObj = vcam.Follow.gameObject;
                var cam = camObj.GetComponent<Camera>();
                if (cam != null) return cam;
            }
        }

        if (Camera.main != null) return Camera.main;

        var anyCam = FindObjectOfType<Camera>();
        if (anyCam != null) return anyCam;

        Debug.LogError("No camera found for crafting!");
        return null;
    }

    private Vector3 GetCenter()
    {
        Vector3 c = Vector3.zero;
        foreach (var g in workbenchItems) c += g.transform.position;
        return c / workbenchItems.Count;
    }
}
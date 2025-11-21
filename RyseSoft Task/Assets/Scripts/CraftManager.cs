// CraftManager.cs - HDRP + Build Compatible
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;
using System.IO;
using System.Collections.Generic;
using Cinemachine;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class CraftManager : MonoBehaviour
{
public static CraftManager Instance;

    [Header("UI References")]
    public Button craftButton;           // Shown only when items present
    public GameObject namingPanel;       // Shown only after successful trace
    public TMPro.TMP_InputField nameInput;
    public Button confirmButton;

    [Header("Workbench")]
    public Transform workbenchContent;   // Parent of placed items

    public Transform workbenchPlacementCollider;
    public string workbenchTag = "WorkbenchArea";

    [Header("Tracing")]
    public LineRenderer traceLinePrefab;
    public float traceTolerance = 0.4f;

    private List<GameObject> workbenchItems = new List<GameObject>();
    private LineRenderer currentTrace;
    private List<Vector3> targetPath;
    private bool isTracing = false;

    private void Awake()
    {
        Instance = this;

        // Hide everything at start
        craftButton.gameObject.SetActive(false);
        namingPanel.SetActive(false);
        
        workbenchContent.gameObject.GetComponent<BoxCollider>().enabled = (false); // Hide workbench by default

        craftButton.onClick.AddListener(StartCrafting);
        confirmButton.onClick.AddListener(ConfirmCraft);
    }

    private bool hasStartedTracing = false; // ← ADD THIS at the top of the class

    private void Update()
    {
        UpdateCraftButtonVisibility();

        if (!isTracing || currentTrace == null) return;

        if (Input.GetMouseButtonDown(0))
        {
            hasStartedTracing = true; // Player officially started the trace
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

        // ONLY fail if player started tracing AND released the button before finishing
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

        // Show Craft button only if 2+ items
        bool canCraft = itemCount >= 2;
        if (craftButton.gameObject.activeSelf != canCraft)
        {
            craftButton.gameObject.SetActive(canCraft);
        }
    }

    public void StartCrafting()
    {
        
        workbenchContent.gameObject.GetComponent<BoxCollider>().enabled = true; // Ensure workbench is visible
        workbenchPlacementCollider.gameObject.GetComponent<BoxCollider>().enabled = (false); // Disable placement collider
        
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

        // Hide craft button during tracing
        craftButton.gameObject.SetActive(false);
    }

    private void GenerateTracePath()
    {
        targetPath = new List<Vector3>();
        Vector3 center = GetCenter();

        // Spawn HIGHER above small objects
        float traceHeight = center.y + 0.25f; // ← HIGHER: 0.18 above center

        int segments = workbenchItems.Count + 2;
        for (int i = 0; i < segments; i++)
        {
            float angle = i * Mathf.PI * 2f / segments;
            float radius = 0.08f + (i % 3) * 0.03f;
            Vector3 offset = new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)) * radius;
            Vector3 pos = center + offset;
            pos.y = traceHeight; // ← FIXED HEIGHT
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
    
        // ← FIXED: TINY WIDTH FOR SCALE 0.2
        currentTrace.startWidth = 0.025f;
        currentTrace.endWidth = 0.025f;
        //currentTrace.widthMultiplier = 1f;
    }

    private void CraftSuccess()
    {
        isTracing = false;
        currentTrace.material.SetColor("_EmissiveColor", Color.green * 15f);

        // NOW show naming panel
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
        hasStartedTracing = false; // ← ADD THIS
        UpdateCraftButtonVisibility();
    }

    public void ConfirmCraft()
    {
        string name = string.IsNullOrEmpty(nameInput.text) ? "Unnamed Creation" : nameInput.text.Trim();
        CreateNewItemHDRP(name);
        namingPanel.SetActive(false);
        nameInput.text = "";
        
        workbenchContent.gameObject.GetComponent<BoxCollider>().enabled = (false); // Hide workbench after crafting
        workbenchPlacementCollider.gameObject.GetComponent<BoxCollider>().enabled = (true); // Enable placement collider again
        
    }

    private void CreateNewItemHDRP(string itemName)
{
    // === STEP 1: Calculate exact center of all workbench items ===
    Bounds totalBounds = new Bounds(workbenchItems[0].transform.position, Vector3.zero);
    foreach (var item in workbenchItems)
    {
        var rend = item.GetComponentInChildren<Renderer>();
        if (rend) totalBounds.Encapsulate(rend.bounds);
    }
    Vector3 geometricCenter = totalBounds.center;

    // === STEP 2: Create root object at world center (will move later) ===
    GameObject finalGO = new GameObject(itemName);
    finalGO.transform.position = geometricCenter;
    finalGO.transform.rotation = Quaternion.identity;
    finalGO.transform.localScale = Vector3.one;

    // === STEP 3: Combine all meshes relative to this center ===
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

    // === STEP 4: Create final mesh ===
    Mesh finalMesh = new Mesh();
    finalMesh.name = itemName + "_Mesh";
    finalMesh.CombineMeshes(combine, true, true);

    // === STEP 5: RECENTER PIVOT TO GEOMETRIC CENTER (THE MAGIC) ===
    finalMesh.RecalculateBounds();
    Vector3 pivotOffset = finalMesh.bounds.center; // This is how far vertices are from (0,0,0)
    Vector3[] vertices = finalMesh.vertices;
    for (int i = 0; i < vertices.Length; i++)
        vertices[i] -= pivotOffset;
    finalMesh.vertices = vertices;
    finalMesh.RecalculateBounds(); // Final bounds now centered at origin

    // === STEP 6: Assign mesh & renderer ===
    var filter = finalGO.AddComponent<MeshFilter>();
    filter.sharedMesh = finalMesh;

    var renderer = finalGO.AddComponent<MeshRenderer>();
    renderer.sharedMaterials = sharedMaterials ?? new Material[] { new Material(Shader.Find("HDRP/Lit")) };

    // === STEP 7: Move object so visual center stays in world, but pivot is at 0,0,0 ===
    finalGO.transform.position += finalGO.transform.TransformVector(pivotOffset);

#if UNITY_EDITOR
    // === STEP 8: SAVE EVERYTHING TO DISK ===
    string folder = "Assets/PlayerCreatedItems/";
    Directory.CreateDirectory(folder + "Meshes");
    Directory.CreateDirectory(folder + "Icons");
    Directory.CreateDirectory(folder + "Prefabs");
    Directory.CreateDirectory(folder + "Data");

    // Save Mesh
    string meshPath = folder + "Meshes/" + itemName + ".mesh";
    AssetDatabase.CreateAsset(finalMesh, meshPath);

    // Capture & Save Icon as Sprite
    Texture2D iconTex = CaptureIconHDRP(finalGO);
    string iconPath = folder + "Icons/" + itemName + ".png";
    File.WriteAllBytes(iconPath, iconTex.EncodeToPNG());

    AssetDatabase.Refresh();
    var importer = (TextureImporter)AssetImporter.GetAtPath(iconPath);
    importer.textureType = TextureImporterType.Sprite;
    importer.spriteImportMode = SpriteImportMode.Single;
    importer.spritePixelsPerUnit = 100;
    importer.alphaIsTransparency = true;
    importer.mipmapEnabled = false;
    importer.filterMode = FilterMode.Bilinear;
    importer.SaveAndReimport();

    Sprite iconSprite = AssetDatabase.LoadAssetAtPath<Sprite>(iconPath);

    // Save Prefab
    string prefabPath = folder + "Prefabs/" + itemName + ".prefab";
    GameObject savedPrefab = PrefabUtility.SaveAsPrefabAsset(finalGO, prefabPath);

    // Create & Save ItemData
    string dataPath = folder + "Data/" + itemName + ".asset";
    ItemData newItem = ScriptableObject.CreateInstance<ItemData>();
    newItem.itemName = itemName;
    newItem.prefab = savedPrefab;
    newItem.mesh = finalMesh;
    newItem.icon = iconSprite;
    newItem.description = $"Player-crafted fusion of {workbenchItems.Count} items";

    AssetDatabase.CreateAsset(newItem, dataPath);
    AssetDatabase.SaveAssets();
    AssetDatabase.Refresh();

    // === STEP 9: Add to storage & update UI ===
    GameManager.Instance.storageBoxData.slots.Add(new InventorySlot(newItem, 1));
    MainUIManager.Instance.RefreshWorkbenchUI();

    Debug.Log($"[CRAFT SUCCESS] '{itemName}' created with PERFECT center pivot!");
#endif

    // === STEP 10: Cleanup ===
    foreach (var item in workbenchItems)
        if (item) Destroy(item.gameObject);
    Destroy(finalGO);
}

    private Texture2D CaptureIconHDRP(GameObject target)
    {
        // Temporarily center the object for perfect framing
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
        cam.backgroundColor = new Color(0, 0, 0, 0); // Transparent background
        cam.cullingMask = LayerMask.GetMask("Default");

        // Ensure target is visible
        int originalLayer = target.layer;
        foreach (Transform t in target.GetComponentsInChildren<Transform>(true))
            t.gameObject.layer = LayerMask.NameToLayer("Default");

        cam.Render();

        RenderTexture.active = rt;
        Texture2D tex = new Texture2D(256, 256, TextureFormat.RGBA32, false);
        tex.ReadPixels(new Rect(0, 0, 256, 256), 0, 0);
        tex.Apply();

        // Restore camera
        cam.targetTexture = oldRT;
        cam.clearFlags = oldClear;
        cam.backgroundColor = oldBG;
        cam.cullingMask = oldMask;
        RenderTexture.active = null;
        rt.Release();
        Destroy(rt);
        
        // Restore position
        target.transform.position = originalPos;

        return tex;
    }
    
    private Camera GetWorkbenchCamera()
    {
        // METHOD 1: Try Cinemachine (most common)
        if (GameManager.Instance.workbenchCamera != null)
        {
            var brain = Camera.main?.GetComponent<CinemachineBrain>();
            if (brain != null && brain.ActiveVirtualCamera != null)
            {
                return Camera.main; // This IS the active workbench camera when workbench is open
            }

            // Fallback: direct from virtual cam
            var vcam = GameManager.Instance.workbenchCamera;
            if (vcam.Follow != null)
            {
                var camObj = vcam.Follow.gameObject;
                var cam = camObj.GetComponent<Camera>();
                if (cam != null) return cam;
            }
        }

        // METHOD 2: Fallback to main camera
        if (Camera.main != null) return Camera.main;

        // METHOD 3: Find any active camera
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
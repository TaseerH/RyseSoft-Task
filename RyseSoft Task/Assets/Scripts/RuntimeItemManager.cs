// RuntimeItemManager.cs
using UnityEngine;
using System.IO;
using System.Collections.Generic;
using System.Runtime.Serialization.Formatters.Binary;

public class RuntimeItemManager : MonoBehaviour
{
    public static RuntimeItemManager Instance;
    
    private string saveFolderPath;
    private const string ITEMS_SAVE_FILE = "player_items.dat";
    
    [System.Serializable]
    public class SerializableBounds
    {
        public float centerX, centerY, centerZ;
        public float sizeX, sizeY, sizeZ;
        
        public SerializableBounds() { }
        
        public SerializableBounds(Bounds bounds)
        {
            centerX = bounds.center.x;
            centerY = bounds.center.y;
            centerZ = bounds.center.z;
            sizeX = bounds.size.x;
            sizeY = bounds.size.y;
            sizeZ = bounds.size.z;
        }
        
        public Bounds ToBounds()
        {
            return new Bounds(
                new Vector3(centerX, centerY, centerZ),
                new Vector3(sizeX, sizeY, sizeZ)
            );
        }
    }
    
    [System.Serializable]
    public class SerializableVector2
    {
        public float x, y;
        
        public SerializableVector2() { }
        
        public SerializableVector2(Vector2 vector)
        {
            x = vector.x;
            y = vector.y;
        }
        
        public Vector2 ToVector2()
        {
            return new Vector2(x, y);
        }
    }
    
    [System.Serializable]
    public class SerializableVector3
    {
        public float x, y, z;
        
        public SerializableVector3() { }
        
        public SerializableVector3(Vector3 vector)
        {
            x = vector.x;
            y = vector.y;
            z = vector.z;
        }
        
        public Vector3 ToVector3()
        {
            return new Vector3(x, y, z);
        }
    }
    
    [System.Serializable]
    public class RuntimeItemData
    {
        public string itemName;
        public byte[] iconData;
        public string description;
        public SerializableVector3[] meshVertices;
        public int[] meshTriangles;
        public SerializableVector3[] meshNormals;
        public SerializableVector2[] meshUVs;
        public SerializableBounds meshBounds;
        public string itemId;
        public float scaleX = 1f, scaleY = 1f, scaleZ = 1f;
    }
    
    [System.Serializable]
    public class SerializableInventorySlot
    {
        public string itemName;
        public string itemDescription;
        public byte[] iconData;
        public int quantity;
        
        public SerializableInventorySlot() { }
        
        public SerializableInventorySlot(InventorySlot slot)
        {
            if (slot.item != null)
            {
                itemName = slot.item.itemName;
                itemDescription = slot.item.description;
                
                if (slot.item.icon != null && slot.item.icon.texture != null)
                {
                    iconData = GetTextureData(slot.item.icon.texture);
                }
            }
            quantity = slot.quantity;
        }
        
        private byte[] GetTextureData(Texture2D texture)
        {
            if (texture == null) return null;
            
            try
            {
                if (texture.isReadable)
                {
                    return texture.EncodeToPNG();
                }
                
                RenderTexture rt = RenderTexture.GetTemporary(texture.width, texture.height, 0, RenderTextureFormat.Default, RenderTextureReadWrite.Linear);
                Graphics.Blit(texture, rt);
                
                RenderTexture previous = RenderTexture.active;
                RenderTexture.active = rt;
                
                Texture2D tempTexture = new Texture2D(texture.width, texture.height, TextureFormat.RGBA32, false);
                tempTexture.ReadPixels(new Rect(0, 0, texture.width, texture.height), 0, 0);
                tempTexture.Apply();
                
                RenderTexture.active = previous;
                RenderTexture.ReleaseTemporary(rt);
                
                byte[] data = tempTexture.EncodeToPNG();
                DestroyImmediate(tempTexture);
                return data;
            }
            catch (System.Exception)
            {
                return null;
            }
        }
    }
    
    [System.Serializable]
    public class SaveData
    {
        public List<RuntimeItemData> playerCreatedItems = new List<RuntimeItemData>();
        public List<SerializableInventorySlot> playerInventory = new List<SerializableInventorySlot>();
        public List<SerializableInventorySlot> storageBox = new List<SerializableInventorySlot>();
        public int saveVersion = 1;
    }
    
    private SaveData currentSaveData = new SaveData();

    [Header("Default Items")]
    public List<ItemData> defaultStorageItems = new List<ItemData>();
    public List<ItemData> defaultInventoryItems = new List<ItemData>();

    private bool isInitialized = false;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        
        Instance = this;
        DontDestroyOnLoad(gameObject);
        
        saveFolderPath = Application.persistentDataPath + "/PlayerSaves/";
        Directory.CreateDirectory(saveFolderPath);
        
        LoadAllGameData();
        
        // Delay initialization to ensure GameManager is ready
        Invoke("Initialize", 0.1f);
    }
    
    private void Initialize()
    {
        // Check if this is first run and add default items
        CheckAndAddDefaultItems();
        isInitialized = true;
    }
    
    private void CheckAndAddDefaultItems()
    {
        // If no player created items exist, this is first run
        if (currentSaveData.playerCreatedItems.Count == 0 && 
            currentSaveData.playerInventory.Count == 0 && 
            currentSaveData.storageBox.Count == 0)
        {
            Debug.Log("First run detected! Adding default items...");
            AddDefaultItems();
        }
        else
        {
            Debug.Log("Existing save file found, loading items.");
            // Load the existing items into the game
            LoadAllPlayerItemsIntoGame();
        }
    }
    
    private void AddDefaultItems()
    {
        // Safety check for GameManager
        if (GameManager.Instance == null)
        {
            Debug.LogError("GameManager.Instance is null! Cannot add default items.");
            return;
        }

        // Safety check for storage and inventory data
        if (GameManager.Instance.storageBoxData == null)
        {
            Debug.LogError("storageBoxData is null!");
            return;
        }

        if (GameManager.Instance.playerInventoryData == null)
        {
            Debug.LogError("playerInventoryData is null!");
            return;
        }

        // Add default items to storage
        foreach (ItemData item in defaultStorageItems)
        {
            if (item != null)
            {
                // Add to runtime items with proper scale preservation
                RuntimeItemData runtimeData = ConvertItemDataToRuntime(item);
                if (runtimeData != null)
                {
                    currentSaveData.playerCreatedItems.Add(runtimeData);
                }
                
                // Add to storage box
                currentSaveData.storageBox.Add(new SerializableInventorySlot(new InventorySlot(item, 1)));
                
                // Also add to the actual game data for immediate access
                GameManager.Instance.storageBoxData.slots.Add(new InventorySlot(item, 1));
                Debug.Log($"Added {item.itemName} to storage box");
            }
        }
        
        // Add default items to inventory
        foreach (ItemData item in defaultInventoryItems)
        {
            if (item != null)
            {
                // Add to runtime items if not already there
                RuntimeItemData runtimeData = ConvertItemDataToRuntime(item);
                if (runtimeData != null && !currentSaveData.playerCreatedItems.Exists(x => x.itemName == item.itemName))
                {
                    currentSaveData.playerCreatedItems.Add(runtimeData);
                }
                
                // Add to inventory
                currentSaveData.playerInventory.Add(new SerializableInventorySlot(new InventorySlot(item, 1)));
                
                // Also add to the actual game data for immediate access
                GameManager.Instance.playerInventoryData.slots.Add(new InventorySlot(item, 1));
                Debug.Log($"Added {item.itemName} to player inventory");
            }
        }
        
        // Save the initial state with default items
        SaveAllGameData();
        
        Debug.Log($"Added {defaultStorageItems.Count} default items to storage and {defaultInventoryItems.Count} to inventory");
        
        // Refresh UI if it's active and available
        if (MainUIManager.Instance != null)
        {
            MainUIManager.Instance.RefreshWorkbenchUI(false);
        }
    }
    
    public void SavePlayerCreatedItem(string itemName, Mesh mesh, Texture2D icon, string description)
    {
        string itemId = System.Guid.NewGuid().ToString();
        
        RuntimeItemData itemData = new RuntimeItemData
        {
            itemName = itemName,
            itemId = itemId,
            iconData = icon.EncodeToPNG(),
            description = description,
            meshVertices = ConvertVector3Array(mesh.vertices),
            meshTriangles = mesh.triangles,
            meshNormals = ConvertVector3Array(mesh.normals),
            meshUVs = ConvertVector2Array(mesh.uv),
            meshBounds = new SerializableBounds(mesh.bounds)
        };
        
        currentSaveData.playerCreatedItems.Add(itemData);
        SaveAllGameData();
        
        Debug.Log($"Saved player item: {itemName} with ID: {itemId}");
    }
    
    private SerializableVector3[] ConvertVector3Array(Vector3[] vectors)
    {
        if (vectors == null) return new SerializableVector3[0];
        
        SerializableVector3[] result = new SerializableVector3[vectors.Length];
        for (int i = 0; i < vectors.Length; i++)
        {
            result[i] = new SerializableVector3(vectors[i]);
        }
        return result;
    }
    
    private SerializableVector2[] ConvertVector2Array(Vector2[] vectors)
    {
        if (vectors == null) return new SerializableVector2[0];
        
        SerializableVector2[] result = new SerializableVector2[vectors.Length];
        for (int i = 0; i < vectors.Length; i++)
        {
            result[i] = new SerializableVector2(vectors[i]);
        }
        return result;
    }
    
    private Vector3[] ConvertSerializableVector3Array(SerializableVector3[] vectors)
    {
        if (vectors == null) return new Vector3[0];
        
        Vector3[] result = new Vector3[vectors.Length];
        for (int i = 0; i < vectors.Length; i++)
        {
            result[i] = vectors[i].ToVector3();
        }
        return result;
    }
    
    private Vector2[] ConvertSerializableVector2Array(SerializableVector2[] vectors)
    {
        if (vectors == null) return new Vector2[0];
        
        Vector2[] result = new Vector2[vectors.Length];
        for (int i = 0; i < vectors.Length; i++)
        {
            result[i] = vectors[i].ToVector2();
        }
        return result;
    }
    
    private void LoadAllGameData()
    {
        string filePath = saveFolderPath + ITEMS_SAVE_FILE;
        
        if (!File.Exists(filePath))
        {
            Debug.Log("No saved game data found. Starting fresh.");
            currentSaveData = new SaveData();
            return;
        }
        
        try
        {
            BinaryFormatter formatter = new BinaryFormatter();
            using (FileStream stream = new FileStream(filePath, FileMode.Open))
            {
                currentSaveData = (SaveData)formatter.Deserialize(stream);
            }
            
            Debug.Log($"Loaded {currentSaveData.playerCreatedItems.Count} player items, " +
                     $"{currentSaveData.playerInventory.Count} inventory items, " +
                     $"{currentSaveData.storageBox.Count} storage items");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Failed to load game data: {e.Message}");
            currentSaveData = new SaveData();
            
            try
            {
                File.Delete(filePath);
                Debug.Log("Deleted corrupted save file");
            }
            catch (System.Exception deleteEx)
            {
                Debug.LogError($"Failed to delete corrupted save file: {deleteEx.Message}");
            }
        }
    }
    
    private void SaveAllGameData()
    {
        string filePath = saveFolderPath + ITEMS_SAVE_FILE;
        
        try
        {
            BinaryFormatter formatter = new BinaryFormatter();
            using (FileStream stream = new FileStream(filePath, FileMode.Create))
            {
                formatter.Serialize(stream, currentSaveData);
            }
            
            Debug.Log($"Game data saved: {currentSaveData.playerCreatedItems.Count} items, " +
                     $"{currentSaveData.playerInventory.Count} inventory, " +
                     $"{currentSaveData.storageBox.Count} storage");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Failed to save game data: {e.Message}");
        }
    }
    
    public ItemData CreateItemDataFromRuntime(RuntimeItemData runtimeData)
    {
        if (runtimeData == null) return null;
        
        Mesh mesh = new Mesh();
        mesh.vertices = ConvertSerializableVector3Array(runtimeData.meshVertices);
        mesh.triangles = runtimeData.meshTriangles;
        mesh.normals = ConvertSerializableVector3Array(runtimeData.meshNormals);
        mesh.uv = ConvertSerializableVector2Array(runtimeData.meshUVs);
        mesh.bounds = runtimeData.meshBounds.ToBounds();
        mesh.name = runtimeData.itemName + "_Mesh";
        
        Sprite iconSprite = CreateSpriteFromIconData(runtimeData.iconData, runtimeData.itemName);
        
        GameObject runtimePrefab = CreateRuntimePrefab(runtimeData.itemName, mesh, new Vector3(runtimeData.scaleX, runtimeData.scaleY, runtimeData.scaleZ));
        
        ItemData itemData = ScriptableObject.CreateInstance<ItemData>();
        itemData.itemName = runtimeData.itemName;
        itemData.prefab = runtimePrefab;
        itemData.mesh = mesh;
        itemData.icon = iconSprite;
        itemData.description = runtimeData.description;
        
        return itemData;
    }
    
    private Sprite CreateSpriteFromIconData(byte[] iconData, string itemName)
    {
        if (iconData == null || iconData.Length == 0)
        {
            Debug.LogWarning($"No icon data for item: {itemName}");
            return CreateDefaultSprite();
        }
        
        try
        {
            Texture2D iconTexture = new Texture2D(2, 2);
            iconTexture.LoadImage(iconData);
            iconTexture.name = itemName + "_Icon";
            
            Sprite sprite = Sprite.Create(iconTexture, 
                new Rect(0, 0, iconTexture.width, iconTexture.height), 
                new Vector2(0.5f, 0.5f), 100f);
            sprite.name = itemName + "_Sprite";
            
            return sprite;
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Failed to create sprite for {itemName}: {e.Message}");
            return CreateDefaultSprite();
        }
    }
    
    private Sprite CreateDefaultSprite()
    {
        Texture2D defaultTexture = new Texture2D(64, 64);
        Color[] pixels = new Color[64 * 64];
        for (int i = 0; i < pixels.Length; i++)
        {
            pixels[i] = Color.gray;
        }
        defaultTexture.SetPixels(pixels);
        defaultTexture.Apply();
        
        return Sprite.Create(defaultTexture, new Rect(0, 0, 64, 64), new Vector2(0.5f, 0.5f));
    }
    
    private GameObject CreateRuntimePrefab(string name, Mesh mesh, Vector3 scale)
    {
        GameObject prefab = new GameObject(name);
        MeshFilter filter = prefab.AddComponent<MeshFilter>();
        MeshRenderer renderer = prefab.AddComponent<MeshRenderer>();
        
        filter.mesh = mesh;
        
        Material material = new Material(Shader.Find("HDRP/Lit"));
        renderer.material = material;
        
        MeshCollider collider = prefab.AddComponent<MeshCollider>();
        collider.sharedMesh = mesh;
        
        prefab.transform.localScale = scale;
        
        return prefab;
    }
    
    public void LoadAllPlayerItemsIntoGame()
    {
        if (GameManager.Instance == null || GameManager.Instance.storageBoxData == null || GameManager.Instance.playerInventoryData == null)
        {
            Debug.LogError("GameManager or its data is not ready!");
            return;
        }

        GameManager.Instance.playerInventoryData.slots.Clear();
        GameManager.Instance.storageBoxData.slots.Clear();
        
        Dictionary<string, ItemData> itemLookup = new Dictionary<string, ItemData>();
        foreach (RuntimeItemData runtimeData in currentSaveData.playerCreatedItems)
        {
            try
            {
                ItemData itemData = CreateItemDataFromRuntime(runtimeData);
                if (itemData != null)
                {
                    itemLookup[runtimeData.itemName] = itemData;
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Failed to load item {runtimeData.itemName}: {e.Message}");
            }
        }
        
        foreach (ItemData defaultItem in defaultStorageItems)
        {
            if (defaultItem != null && !itemLookup.ContainsKey(defaultItem.itemName))
            {
                itemLookup[defaultItem.itemName] = defaultItem;
            }
        }
        
        foreach (ItemData defaultItem in defaultInventoryItems)
        {
            if (defaultItem != null && !itemLookup.ContainsKey(defaultItem.itemName))
            {
                itemLookup[defaultItem.itemName] = defaultItem;
            }
        }
        
        foreach (var savedSlot in currentSaveData.playerInventory)
        {
            if (!string.IsNullOrEmpty(savedSlot.itemName) && itemLookup.ContainsKey(savedSlot.itemName))
            {
                ItemData itemData = itemLookup[savedSlot.itemName];
                GameManager.Instance.playerInventoryData.slots.Add(new InventorySlot(itemData, savedSlot.quantity));
            }
        }
        
        foreach (var savedSlot in currentSaveData.storageBox)
        {
            if (!string.IsNullOrEmpty(savedSlot.itemName) && itemLookup.ContainsKey(savedSlot.itemName))
            {
                ItemData itemData = itemLookup[savedSlot.itemName];
                GameManager.Instance.storageBoxData.slots.Add(new InventorySlot(itemData, savedSlot.quantity));
            }
        }
        
        Debug.Log($"Loaded {GameManager.Instance.playerInventoryData.slots.Count} inventory items and {GameManager.Instance.storageBoxData.slots.Count} storage items");
    }
    
    public void SaveCurrentGameState()
    {
        if (!isInitialized) return;
        
        if (GameManager.Instance == null || GameManager.Instance.playerInventoryData == null || GameManager.Instance.storageBoxData == null)
        {
            Debug.LogError("GameManager or its data is not ready!");
            return;
        }

        currentSaveData.playerInventory.Clear();
        currentSaveData.storageBox.Clear();
        
        foreach (var slot in GameManager.Instance.playerInventoryData.slots)
        {
            if (slot.item != null)
            {
                currentSaveData.playerInventory.Add(new SerializableInventorySlot(slot));
            }
        }
        
        foreach (var slot in GameManager.Instance.storageBoxData.slots)
        {
            if (slot.item != null)
            {
                currentSaveData.storageBox.Add(new SerializableInventorySlot(slot));
            }
        }
        
        SaveAllGameData();
    }
    
    private RuntimeItemData ConvertItemDataToRuntime(ItemData itemData)
    {
        if (itemData == null)
        {
            Debug.LogWarning("Cannot convert null ItemData to runtime data");
            return null;
        }
        
        try
        {
            RuntimeItemData runtimeData = new RuntimeItemData
            {
                itemName = itemData.itemName,
                itemId = System.Guid.NewGuid().ToString(),
                description = itemData.description
            };
            
            if (itemData.prefab != null)
            {
                runtimeData.scaleX = itemData.prefab.transform.localScale.x;
                runtimeData.scaleY = itemData.prefab.transform.localScale.y;
                runtimeData.scaleZ = itemData.prefab.transform.localScale.z;
                Debug.Log($"Preserved scale for {itemData.itemName}: {runtimeData.scaleX}, {runtimeData.scaleY}, {runtimeData.scaleZ}");
            }
            else if (itemData.mesh != null)
            {
                runtimeData.scaleX = 1f;
                runtimeData.scaleY = 1f;
                runtimeData.scaleZ = 1f;
            }
            
            if (itemData.mesh != null)
            {
                runtimeData.meshVertices = ConvertVector3Array(itemData.mesh.vertices);
                runtimeData.meshTriangles = itemData.mesh.triangles;
                runtimeData.meshNormals = ConvertVector3Array(itemData.mesh.normals);
                runtimeData.meshUVs = ConvertVector2Array(itemData.mesh.uv);
                runtimeData.meshBounds = new SerializableBounds(itemData.mesh.bounds);
            }
            
            if (itemData.icon != null && itemData.icon.texture != null)
            {
                runtimeData.iconData = GetTextureData(itemData.icon.texture);
            }
            
            return runtimeData;
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Failed to convert ItemData {itemData.itemName} to runtime: {e.Message}");
            return null;
        }
    }
    
    private byte[] GetTextureData(Texture2D texture)
    {
        if (texture == null) return null;
        
        try
        {
            if (texture.isReadable)
            {
                return texture.EncodeToPNG();
            }
            
            RenderTexture rt = RenderTexture.GetTemporary(texture.width, texture.height, 0, RenderTextureFormat.Default, RenderTextureReadWrite.Linear);
            Graphics.Blit(texture, rt);
            
            RenderTexture previous = RenderTexture.active;
            RenderTexture.active = rt;
            
            Texture2D tempTexture = new Texture2D(texture.width, texture.height, TextureFormat.RGBA32, false);
            tempTexture.ReadPixels(new Rect(0, 0, texture.width, texture.height), 0, 0);
            tempTexture.Apply();
            
            RenderTexture.active = previous;
            RenderTexture.ReleaseTemporary(rt);
            
            byte[] data = tempTexture.EncodeToPNG();
            DestroyImmediate(tempTexture);
            return data;
        }
        catch (System.Exception)
        {
            return null;
        }
    }
    
    public void OnItemCrafted(ItemData craftedItem)
    {
        SaveCurrentGameState();
        
        if (MainUIManager.Instance != null)
            MainUIManager.Instance.RefreshWorkbenchUI();
    }
    
    public void OnInventoryChanged()
    {
        SaveCurrentGameState();
    }
}
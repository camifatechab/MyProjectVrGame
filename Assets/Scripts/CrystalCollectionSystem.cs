using UnityEngine;
using UnityEngine.Events;

public class CrystalCollectionSystem : MonoBehaviour
{
    [Header("Collection Settings")]
    [SerializeField] private int totalCrystals = 5;
    [SerializeField] private bool enableDebugLogs = true;
    
    [Header("Events")]
    public UnityEvent<int, int> OnCrystalCollected; // (current, total)
    public UnityEvent OnAllCrystalsCollected;
    
    private int crystalsCollected = 0;
    private static CrystalCollectionSystem instance;
    
    public static CrystalCollectionSystem Instance => instance;
    
    void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }
        instance = this;
    }
    
    void Start()
    {
        // Count crystals in scene automatically
        Crystal[] crystalsInScene = FindObjectsOfType<Crystal>();
        totalCrystals = crystalsInScene.Length;
        
        if (enableDebugLogs)
        {
            Debug.Log($"Crystal Collection System initialized. Total crystals: {totalCrystals}");
        }
        
        // Initialize event
        OnCrystalCollected?.Invoke(crystalsCollected, totalCrystals);
    }
    
    public void CollectCrystal()
    {
        crystalsCollected++;
        
        if (enableDebugLogs)
        {
            Debug.Log($"Crystal collected! {crystalsCollected}/{totalCrystals}");
        }
        
        // Invoke events
        OnCrystalCollected?.Invoke(crystalsCollected, totalCrystals);
        
        // Check win condition
        if (crystalsCollected >= totalCrystals)
        {
            if (enableDebugLogs)
            {
                Debug.Log("All crystals collected! You win!");
            }
            OnAllCrystalsCollected?.Invoke();
        }
    }
    
    public int GetCrystalsCollected()
    {
        return crystalsCollected;
    }
    
    public int GetTotalCrystals()
    {
        return totalCrystals;
    }
    
    public float GetCollectionProgress()
    {
        return totalCrystals > 0 ? (float)crystalsCollected / totalCrystals : 0f;
    }
}

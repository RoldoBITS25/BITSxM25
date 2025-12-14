using UnityEngine;

public class CuttableObject : MonoBehaviour, IInteractable
{
    [Header("Cut Prefabs")]
    [Tooltip("Prefab for the left half of the cut object")]
    [SerializeField] private GameObject leftHalfPrefab;
    
    [Tooltip("Prefab for the right half of the cut object")]
    [SerializeField] private GameObject rightHalfPrefab;
    
    [Header("Cut Effect")]
    [SerializeField] private GameObject cutEffectPrefab;
    
    [Header("Physics Settings")]
    [Tooltip("Force applied to separate the halves")]
    [SerializeField] private float separationForce = 5f;
    
    [Tooltip("Torque applied to make halves rotate")]
    [SerializeField] private float rotationTorque = 3f;
    
    private bool hasBeenCut = false;

    public void Interact()
    {
        if (!hasBeenCut)
        {
            Debug.Log("Interacting with Cuttable Object - Cutting!");
            OnCut(transform.position);
        }
    }
    
    public void OnCut(Vector2 cutPosition)
    {
        if (hasBeenCut)
            return;
            
        hasBeenCut = true;
        Debug.Log($"Object {gameObject.name} cut at position {cutPosition}");

        // Spawn cut effect if available
        if (cutEffectPrefab != null)
        {
            Instantiate(cutEffectPrefab, cutPosition, Quaternion.identity);
        }

        // Create the two halves
        CreateCutHalves(cutPosition);

        // Destroy the original object
        Destroy(gameObject);
    }
    
    private void CreateCutHalves(Vector2 cutPosition)
    {
        // If no prefabs are assigned, create simple duplicates with scaled mesh
        if (leftHalfPrefab == null || rightHalfPrefab == null)
        {
            CreateDefaultHalves(cutPosition);
            return;
        }
        
        // Instantiate left half
        GameObject leftHalf = Instantiate(leftHalfPrefab, transform.position, transform.rotation);
        leftHalf.transform.localScale = transform.localScale;
        
        // Instantiate right half
        GameObject rightHalf = Instantiate(rightHalfPrefab, transform.position, transform.rotation);
        rightHalf.transform.localScale = transform.localScale;
        
        // Apply physics to separate the halves
        ApplyPhysicsToHalf(leftHalf, Vector2.left);
        ApplyPhysicsToHalf(rightHalf, Vector2.right);
    }
    
    private void CreateDefaultHalves(Vector2 cutPosition)
    {
        // Create left half
        GameObject leftHalf = CreateHalfObject("LeftHalf", Vector2.left);
        
        // Create right half
        GameObject rightHalf = CreateHalfObject("RightHalf", Vector2.right);
        
        // Apply physics
        ApplyPhysicsToHalf(leftHalf, Vector2.left);
        ApplyPhysicsToHalf(rightHalf, Vector2.right);
    }
    
    private GameObject CreateHalfObject(string name, Vector2 direction)
    {
        GameObject half = new GameObject($"{gameObject.name}_{name}");
        half.transform.position = transform.position;
        half.transform.rotation = transform.rotation;
        half.transform.localScale = transform.localScale;
        
        // Copy sprite renderer if exists
        SpriteRenderer originalRenderer = GetComponent<SpriteRenderer>();
        if (originalRenderer != null)
        {
            SpriteRenderer newRenderer = half.AddComponent<SpriteRenderer>();
            newRenderer.sprite = originalRenderer.sprite;
            newRenderer.color = originalRenderer.color;
            newRenderer.sortingLayerName = originalRenderer.sortingLayerName;
            newRenderer.sortingOrder = originalRenderer.sortingOrder;
            
            // Scale the sprite to appear as half
            Vector3 scale = half.transform.localScale;
            scale.x *= 0.5f;
            half.transform.localScale = scale;
            
            // Offset position slightly
            half.transform.position += (Vector3)(direction * 0.25f);
        }
        
        // Add rigidbody for physics
        Rigidbody rb = half.AddComponent<Rigidbody>();
        rb.mass = 0.5f;
        
        // Add collider
        BoxCollider collider = half.AddComponent<BoxCollider>();
        
        return half;
    }
    
    private void ApplyPhysicsToHalf(GameObject half, Vector2 direction)
    {
        Rigidbody rb = half.GetComponent<Rigidbody>();
        if (rb == null)
        {
            rb = half.AddComponent<Rigidbody>();
            rb.mass = 0.5f;
        }
        
        // Apply separation force
        rb.AddForce(direction * separationForce, ForceMode.Impulse);
        
        // Apply random torque for realistic tumbling
        Vector3 randomTorque = new Vector3(
            Random.Range(-rotationTorque, rotationTorque),
            Random.Range(-rotationTorque, rotationTorque),
            Random.Range(-rotationTorque, rotationTorque)
        );
        rb.AddTorque(randomTorque, ForceMode.Impulse);
    }
}

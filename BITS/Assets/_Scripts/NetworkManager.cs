using UnityEngine;
using System.Runtime.InteropServices;

[System.Serializable]
public class PlayerData
{
    public string id;
    public string type;
    public float x;
    public float y;
    public float z;
}

public class NetworkManager : MonoBehaviour
{
    // Import the JSLib function (see step 3)
    [DllImport("__Internal")]
    private static extern void SendToBrowser(string message);

    public string myId;

    void Start()
    {
        // Optional: Notify ready state
    }

    // Called by JS when connected
    public void OnRegistered(string id)
    {
        Debug.Log("Registered with ID: " + id);
        myId = id;
    }

    // Called by JS when another player joins
    public void OnPlayerJoined(string id)
    {
        Debug.Log("Player joined: " + id);
        // logic to spawn player avatar
    }

    // Called by JS when a player moves
    public void OnPlayerMoved(string json)
    {
        PlayerData data = JsonUtility.FromJson<PlayerData>(json);
        if (data.id != myId)
        {
            // logic to update other player's position
            Debug.Log($"Player {data.id} moved to {data.x}, {data.y}, {data.z}");
        }
    }

    // Called by JS when a player leaves
    public void OnPlayerLeft(string id)
    {
        Debug.Log("Player left: " + id);
        // logic to destroy player avatar
    }

    // Call this method to send your position to the server
    public void SendMove(Vector3 position)
    {
        string json = JsonUtility.ToJson(new PlayerData
        {
            type = "move", // We need to wrap data or handle logic in JS to differentiate message types better.
            // Simplified for this example, assuming JS expects { type: "move", data: { ... } } structure
        });

        // Actually better to construct the full object expected by main.js:
        // window.receiveFromUnity expects a JSON string with { type: 'move', data: { ... } }

        string payload = $"{{\"type\":\"move\",\"data\":{{\"x\":{position.x},\"y\":{position.y},\"z\":{position.z}}}}}";

#if UNITY_WEBGL && !UNITY_EDITOR
            SendToBrowser(payload);
#else
        Debug.Log("Sending to browser: " + payload);
#endif
    }
}

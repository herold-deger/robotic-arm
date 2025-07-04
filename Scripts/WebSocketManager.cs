using UnityEngine;
using NativeWebSocket;
using System.Text;

public class WebSocketManager : MonoBehaviour
{
    WebSocket websocket;
    public Transform Position_bras;
    public Transform Target; 
    public float[] receivedAngles;

    private float sendInterval = 1f;
    private float lastSendTime = 0f;

    async void Start()
    {
        Debug.Log("WebSocketManager script loaded");

        websocket = new WebSocket("ws://192.168.1.97:8765");
     
        
        websocket.OnOpen += () => Debug.Log("Connection open!");
        websocket.OnMessage += (bytes) =>
        {
            var message = Encoding.UTF8.GetString(bytes);
            Debug.Log("Received OnMessage! (" + message + ")");
            try
            {
                TargetPosition data = JsonUtility.FromJson<TargetPosition>(message);
                receivedAngles = data.Target_position;
            }
            catch
            {
                Debug.LogWarning("Message JSON non valide : " + message);
            }
        };

        await websocket.Connect();
    }

    void Update()
    {
        if (websocket != null && websocket.State == WebSocketState.Open && Target != null)
        {
            if (Time.time - lastSendTime > sendInterval)
            {
                Vector3 targetPos = Position_bras.InverseTransformPoint(Target.position);

                TargetPosition data = new TargetPosition
                {
                    Target_position = new float[] { targetPos.x, targetPos.y, targetPos.z }
                };
                var message = JsonUtility.ToJson(data);
                websocket.SendText(message);

                lastSendTime = Time.time;
            }
        }

        #if !UNITY_WEBGL || UNITY_EDITOR
        websocket?.DispatchMessageQueue();
        #endif
    }

    private async void OnApplicationQuit()
    {
        if (websocket != null)
            await websocket.Close();
    }
}

[System.Serializable]
public class TargetPosition
{
    public float[] Target_position;
}
using System;
using System.Net.Sockets;
using System.Text;
using UnityEngine;

public class QuestTcpClient : MonoBehaviour
{
    public string host = "127.0.0.1";
    public int port = 5005;
    public float reconnectInterval = 1.0f;

    private TcpClient client;
    private NetworkStream stream;
    private float lastReconnectAttempt;

    public bool IsConnected => client != null && stream != null && client.Connected;

    private void Start()
    {
        TryConnect();
    }

    private void Update()
    {
        if (IsConnected)
        {
            return;
        }

        if (Time.time - lastReconnectAttempt >= reconnectInterval)
        {
            TryConnect();
        }
    }

    public bool SendLine(string message)
    {
        if (!IsConnected)
        {
            return false;
        }

        try
        {
            if (!message.EndsWith("\n"))
            {
                message += "\n";
            }

            byte[] data = Encoding.UTF8.GetBytes(message);
            stream.Write(data, 0, data.Length);
            stream.Flush();
            return true;
        }
        catch (Exception e)
        {
            Debug.LogWarning("TCP send failed: " + e.Message);
            Close();
            return false;
        }
    }

    private void TryConnect()
    {
        lastReconnectAttempt = Time.time;

        try
        {
            Close();
            client = new TcpClient(host, port);
            stream = client.GetStream();
            Debug.Log("TCP connected to " + host + ":" + port);
        }
        catch (Exception e)
        {
            Debug.LogWarning("TCP connection failed. Will retry. Error: " + e.Message);
            Close();
        }
    }

    private void OnApplicationQuit()
    {
        Close();
    }

    private void OnDisable()
    {
        Close();
    }

    private void Close()
    {
        if (stream != null)
        {
            stream.Close();
            stream = null;
        }

        if (client != null)
        {
            client.Close();
            client = null;
        }
    }
}

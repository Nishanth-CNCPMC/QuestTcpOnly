using System;
using System.Net.Sockets;
using System.Text;
using UnityEngine;

public class QuestTcpClient : MonoBehaviour
{
    public string host = "10.42.0.1";
    public int port = 5005;
    public float reconnectInterval = 1.0f;

    private TcpClient client;
    private NetworkStream stream;
    private float lastReconnectAttempt;
    private readonly byte[] receiveBuffer = new byte[4096];
    private readonly StringBuilder incomingText = new StringBuilder();

    public bool IsConnected => client != null && stream != null && client.Connected;
    public event Action<string> LineReceived;

    private void Start()
    {
        TryConnect();
    }

    private void Update()
    {
        if (IsConnected)
        {
            ReadIncomingLines();
            return;
        }

        if (Time.time - lastReconnectAttempt >= reconnectInterval)
        {
            TryConnect();
        }
    }

    private void ReadIncomingLines()
    {
        try
        {
            while (stream != null && stream.DataAvailable)
            {
                int count = stream.Read(receiveBuffer, 0, receiveBuffer.Length);
                if (count <= 0)
                {
                    Close();
                    return;
                }

                incomingText.Append(Encoding.UTF8.GetString(receiveBuffer, 0, count));
            }

            DispatchIncomingLines();
        }
        catch (Exception e)
        {
            Debug.LogWarning("TCP receive failed: " + e.Message);
            Close();
        }
    }

    private void DispatchIncomingLines()
    {
        string text = incomingText.ToString();
        int newlineIndex = text.IndexOf('\n');
        while (newlineIndex >= 0)
        {
            string line = text.Substring(0, newlineIndex).Trim();
            text = text.Substring(newlineIndex + 1);
            if (!string.IsNullOrEmpty(line))
            {
                LineReceived?.Invoke(line);
            }

            newlineIndex = text.IndexOf('\n');
        }

        incomingText.Length = 0;
        incomingText.Append(text);
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
        incomingText.Length = 0;

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

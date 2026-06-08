using System;
using System.Collections;
using System.Text;
using Unity.WebRTC;
using UnityEngine;
using UnityEngine.Networking;

public enum StreamReceiverState
{
    Idle,
    Connecting,
    Connected,
    ConnectedNoVideoTrack,
    Failed,
    Disconnected
}

public class WebRTCStreamReceiver : MonoBehaviour
{
    public string whepUrl = "http://10.42.0.1:8889/quest/whep";
    public float iceGatherTimeout = 3.0f;
    public int httpTimeoutSeconds = 6;
    public bool connectOnStart = true;
    public bool retryOnFailure = true;
    public float retryInterval = 5.0f;

    private RTCPeerConnection peerConnection;
    private VideoStreamTrack videoTrack;
    private Coroutine connectionRoutine;
    private Coroutine webrtcUpdateRoutine;
    private float nextRetryTime;
    private bool iceGatheringComplete;
    private bool hasVideoTrack;
    private Texture receivedTexture;
    private string resourceUrl;

    public StreamReceiverState State { get; private set; } = StreamReceiverState.Idle;
    public Texture ReceivedTexture => receivedTexture;
    public string LastError { get; private set; } = "";
    public string StreamUrl => whepUrl;
    public bool HasVideoTrack => hasVideoTrack;

    private void Start()
    {
        webrtcUpdateRoutine = StartCoroutine(WebRTC.Update());

        if (connectOnStart)
        {
            Connect();
        }
    }

    private void Update()
    {
        if (videoTrack != null && receivedTexture == null && videoTrack.Texture != null)
        {
            SetReceivedTexture(videoTrack.Texture);
        }

        if (!retryOnFailure || connectionRoutine != null || Time.time < nextRetryTime)
        {
            return;
        }

        if (State == StreamReceiverState.Failed || State == StreamReceiverState.Disconnected)
        {
            Connect();
        }
    }

    public void Connect()
    {
        if (connectionRoutine != null)
        {
            return;
        }

        connectionRoutine = StartCoroutine(ConnectRoutine());
    }

    public void Retry()
    {
        CloseConnection(StreamReceiverState.Idle);
        Connect();
    }

    private IEnumerator ConnectRoutine()
    {
        CloseConnection(StreamReceiverState.Idle, false);
        State = StreamReceiverState.Connecting;
        LastError = "";
        receivedTexture = null;
        hasVideoTrack = false;
        iceGatheringComplete = false;

        CreatePeerConnection();

        RTCSessionDescriptionAsyncOperation offerOperation = peerConnection.CreateOffer();
        yield return offerOperation;

        if (offerOperation.IsError)
        {
            Fail("CreateOffer failed: " + offerOperation.Error.message);
            FinishConnectionAttempt();
            yield break;
        }

        RTCSessionDescription offer = offerOperation.Desc;
        RTCSetSessionDescriptionAsyncOperation localDescriptionOperation;

        try
        {
            localDescriptionOperation = peerConnection.SetLocalDescription(ref offer);
        }
        catch (Exception e)
        {
            Fail("SetLocalDescription exception: " + e.Message);
            FinishConnectionAttempt();
            yield break;
        }

        yield return localDescriptionOperation;

        if (localDescriptionOperation.IsError)
        {
            Fail("SetLocalDescription failed: " + localDescriptionOperation.Error.message);
            FinishConnectionAttempt();
            yield break;
        }

        yield return WaitForLocalDescription();

        string offerSdp = peerConnection.LocalDescription.sdp;
        if (string.IsNullOrEmpty(offerSdp))
        {
            Fail("Local SDP offer is empty.");
            FinishConnectionAttempt();
            yield break;
        }

        UnityWebRequest request = CreateSdpPostRequest(whepUrl, offerSdp, httpTimeoutSeconds);
        yield return request.SendWebRequest();

        long responseCode = request.responseCode;
        string responseBody = request.downloadHandler != null ? request.downloadHandler.text : "";
        resourceUrl = ResolveResourceUrl(whepUrl, request.GetResponseHeader("Location"));

        if (request.result != UnityWebRequest.Result.Success || responseCode < 200 || responseCode >= 300)
        {
            Fail("WHEP POST failed. HTTP " + responseCode + ": " + responseBody);
            Debug.LogError("WHEP POST failed at " + whepUrl + "\nHTTP " + responseCode + "\n" + responseBody);
            request.Dispose();
            FinishConnectionAttempt();
            yield break;
        }

        request.Dispose();

        if (string.IsNullOrWhiteSpace(responseBody))
        {
            Fail("WHEP answer SDP was empty.");
            FinishConnectionAttempt();
            yield break;
        }

        RTCSessionDescription answer = new RTCSessionDescription
        {
            type = RTCSdpType.Answer,
            sdp = responseBody
        };

        RTCSetSessionDescriptionAsyncOperation remoteDescriptionOperation;
        try
        {
            remoteDescriptionOperation = peerConnection.SetRemoteDescription(ref answer);
        }
        catch (Exception e)
        {
            Fail("SetRemoteDescription exception: " + e.Message);
            Debug.LogError("Invalid WHEP answer SDP:\n" + responseBody);
            FinishConnectionAttempt();
            yield break;
        }

        yield return remoteDescriptionOperation;

        if (remoteDescriptionOperation.IsError)
        {
            Fail("SetRemoteDescription failed: " + remoteDescriptionOperation.Error.message);
            Debug.LogError("Invalid WHEP answer SDP:\n" + responseBody);
            FinishConnectionAttempt();
            yield break;
        }

        State = hasVideoTrack ? StreamReceiverState.Connected : StreamReceiverState.ConnectedNoVideoTrack;
        FinishConnectionAttempt();
    }

    private void CreatePeerConnection()
    {
        RTCConfiguration configuration = default;
        peerConnection = new RTCPeerConnection(ref configuration);
        peerConnection.OnTrack = OnTrack;
        peerConnection.OnIceGatheringStateChange = state =>
        {
            iceGatheringComplete = state == RTCIceGatheringState.Complete;
        };
        peerConnection.OnIceCandidate = candidate =>
        {
            if (candidate != null)
            {
                Debug.Log("Unity WebRTC ICE candidate gathered for WHEP offer.");
            }
        };
        peerConnection.OnConnectionStateChange = OnConnectionStateChange;
        peerConnection.OnIceConnectionChange = OnIceConnectionStateChange;

        RTCRtpTransceiverInit transceiverInit = new RTCRtpTransceiverInit
        {
            direction = RTCRtpTransceiverDirection.RecvOnly
        };

        peerConnection.AddTransceiver(TrackKind.Video, transceiverInit);
    }

    private IEnumerator WaitForLocalDescription()
    {
        float start = Time.time;

        while (!iceGatheringComplete && Time.time - start < iceGatherTimeout)
        {
            if (peerConnection == null || peerConnection.GatheringState == RTCIceGatheringState.Complete)
            {
                iceGatheringComplete = true;
                break;
            }

            yield return null;
        }
    }

    private static UnityWebRequest CreateSdpPostRequest(string url, string sdp, int timeoutSeconds)
    {
        UnityWebRequest request = new UnityWebRequest(url, "POST");
        byte[] body = Encoding.UTF8.GetBytes(sdp);
        request.uploadHandler = new UploadHandlerRaw(body);
        request.downloadHandler = new DownloadHandlerBuffer();
        request.SetRequestHeader("Content-Type", "application/sdp");
        request.SetRequestHeader("Accept", "application/sdp");
        request.timeout = timeoutSeconds;
        return request;
    }

    private void OnTrack(RTCTrackEvent trackEvent)
    {
        if (trackEvent.Track is VideoStreamTrack track)
        {
            videoTrack = track;
            hasVideoTrack = true;
            State = StreamReceiverState.ConnectedNoVideoTrack;
            videoTrack.OnVideoReceived += SetReceivedTexture;
            Debug.Log("Unity WebRTC video track received from WHEP stream.");
            return;
        }

        Debug.Log("Unity WebRTC ignored non-video track: " + trackEvent.Track.Kind);
    }

    private void SetReceivedTexture(Texture texture)
    {
        if (texture == null)
        {
            return;
        }

        receivedTexture = texture;
        State = StreamReceiverState.Connected;
        LastError = "";
    }

    private void OnConnectionStateChange(RTCPeerConnectionState state)
    {
        Debug.Log("Unity WebRTC connection state: " + state);

        if (state == RTCPeerConnectionState.Connected)
        {
            State = hasVideoTrack ? StreamReceiverState.Connected : StreamReceiverState.ConnectedNoVideoTrack;
        }
        else if (state == RTCPeerConnectionState.Failed)
        {
            Fail("WebRTC connection failed.");
        }
        else if (state == RTCPeerConnectionState.Disconnected || state == RTCPeerConnectionState.Closed)
        {
            State = StreamReceiverState.Disconnected;
            LastError = state == RTCPeerConnectionState.Closed ? "" : "WebRTC connection disconnected.";
            nextRetryTime = Time.time + retryInterval;
        }
    }

    private void OnIceConnectionStateChange(RTCIceConnectionState state)
    {
        Debug.Log("Unity WebRTC ICE connection state: " + state);

        if (state == RTCIceConnectionState.Failed)
        {
            Fail("ICE connection failed.");
        }
        else if (state == RTCIceConnectionState.Disconnected)
        {
            State = StreamReceiverState.Disconnected;
            LastError = "ICE connection disconnected.";
            nextRetryTime = Time.time + retryInterval;
        }
    }

    private void Fail(string message)
    {
        State = StreamReceiverState.Failed;
        LastError = message;
        nextRetryTime = Time.time + retryInterval;
    }

    private void FinishConnectionAttempt()
    {
        connectionRoutine = null;
    }

    private void CloseConnection(StreamReceiverState nextState, bool stopConnectionRoutine = true)
    {
        if (stopConnectionRoutine && connectionRoutine != null)
        {
            StopCoroutine(connectionRoutine);
            connectionRoutine = null;
        }

        if (videoTrack != null)
        {
            videoTrack.OnVideoReceived -= SetReceivedTexture;
            videoTrack = null;
        }

        if (peerConnection != null)
        {
            peerConnection.Close();
            peerConnection.Dispose();
            peerConnection = null;
        }

        receivedTexture = null;
        hasVideoTrack = false;
        State = nextState;
    }

    private static string ResolveResourceUrl(string baseUrl, string location)
    {
        if (string.IsNullOrWhiteSpace(location))
        {
            return "";
        }

        if (Uri.TryCreate(location, UriKind.Absolute, out Uri absoluteUri))
        {
            return absoluteUri.ToString();
        }

        if (Uri.TryCreate(baseUrl, UriKind.Absolute, out Uri baseUri)
            && Uri.TryCreate(baseUri, location, out Uri resolvedUri))
        {
            return resolvedUri.ToString();
        }

        return location;
    }

    private void OnDisable()
    {
        CloseConnection(StreamReceiverState.Disconnected);

        if (webrtcUpdateRoutine != null)
        {
            StopCoroutine(webrtcUpdateRoutine);
            webrtcUpdateRoutine = null;
        }
    }
}

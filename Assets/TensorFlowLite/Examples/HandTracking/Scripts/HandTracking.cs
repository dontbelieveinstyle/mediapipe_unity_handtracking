
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;

public class HandTracking : MonoBehaviour 
{
    [Tooltip("Configurable TFLite model.")]
    [SerializeField]
    private int InputW = 256;
    [SerializeField]
    private int InputH = 256;
    [SerializeField]
    private TextAsset PalmDetection;
    [SerializeField]
    private TextAsset HandLandmark3D;
    [SerializeField]
    private int PalmDetectionLerpFrameCount = 3;
    [SerializeField]
    private int HandLandmark3DLerpFrameCount = 4;
    [SerializeField]
    private bool UseGPU = true;
    [SerializeField]
    private bool debugHandLandmarks3D = true;

    private RenderTexture videoTexture;
    private Texture2D texture;

    private Inferencer inferencer = new Inferencer();
    private GameObject debugPlane;
    private DebugRenderer debugRenderer;


    private string deviceName;
    private WebCamTexture webCam;
    private Quaternion baseRotation;
    private Quaternion webCamRotation;
    private float baseAspect = 0.0f;

    void Awake() { QualitySettings.vSyncCount = 0; }

    void Start() 
    {
        OpenCamera();
        //InitTexture();
        inferencer.Init(PalmDetection, HandLandmark3D, UseGPU,
                        PalmDetectionLerpFrameCount, HandLandmark3DLerpFrameCount);
        debugPlane = GameObject.Find("TensorFlowLite");
        debugRenderer = debugPlane.GetComponent<DebugRenderer>();
        debugRenderer.Init(inferencer.InputWidth, inferencer.InputHeight, debugPlane);
    }
    private void InitTexture()
    { 
        var rectTransform = GetComponent<RectTransform>();
        var renderer = GetComponent<Renderer>();

        var videoPlayer = GetComponent<VideoPlayer>();
        int width = (int)rectTransform.rect.width;
        int height = (int)rectTransform.rect.height;
        videoTexture = new RenderTexture(width, height, 24);
        videoPlayer.targetTexture = videoTexture;
        renderer.material.mainTexture = videoTexture;
        videoPlayer.Play();

        texture = new Texture2D(videoTexture.width, videoTexture.height, TextureFormat.RGB24, false);
    }

    void OpenCamera()
    {
        WebCamDevice [] devices= WebCamTexture.devices;
        if (devices.Length>0)
        {

            var rectTransform = GetComponent<RectTransform>();
            var renderer = GetComponent<Renderer>();
            int width = (int)rectTransform.rect.width;
            int height = (int)rectTransform.rect.height;
            videoTexture = new RenderTexture(width, height, 24);
            deviceName = devices[0].name;
            webCam = new WebCamTexture(deviceName);
            renderer.material.mainTexture = webCam;
            baseRotation = transform.rotation;
            webCam.Play();
            texture = new Texture2D(videoTexture.width, videoTexture.height, TextureFormat.RGB24, false);
        }
    }

    void Update() 
    {
        if (baseAspect == 0.0f)
            if (webCam.width > 100)
            {
                var rectTransform = GetComponent<RectTransform>();
                int width = (int)rectTransform.rect.width;
                int height = (int)rectTransform.rect.height;
                float modelAspect = (float)width / (float)height;
                Debug.Log($"Model aspects: w={width}, h={height}, ar={modelAspect}");
                float inputAspect = (float)webCam.width / (float)webCam.height;
                Debug.Log($"WebCam aspects: w={webCam.width}, h={webCam.height}, ar={inputAspect}");
                baseAspect = inputAspect/modelAspect;
                Debug.Log($"Base Aspect is: {baseAspect}");
            }
        transform.rotation = baseRotation * Quaternion.AngleAxis(webCam.videoRotationAngle, Vector3.up);
        webCamRotation = Quaternion.AngleAxis(webCam.videoRotationAngle, Vector3.up);
        Graphics.Blit(webCam, videoTexture);
        Graphics.SetRenderTarget(videoTexture);
        texture.ReadPixels(new Rect(0, 0, videoTexture.width, videoTexture.height), 0, 0);
        texture.Apply();
        Graphics.SetRenderTarget(null);
        if (inferencer != null)
            inferencer.Update(texture);
        
        
    }

    public void OnRenderObject() 
    {
        if (!inferencer.Initialized){ return; }

        if (debugHandLandmarks3D)
        { 
            var handLandmarks = inferencer.HandLandmarks;
            debugRenderer.DrawHand3D(handLandmarks, webCamRotation, baseAspect);
            if (debugRenderer.OpenHandPose())
                Debug.Log("手势 -> Open hand");
        }
    }

    void OnDestroy(){ inferencer.Destroy(); }
}

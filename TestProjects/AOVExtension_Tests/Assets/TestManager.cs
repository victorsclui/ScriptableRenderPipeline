using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;
using UnityEngine.Rendering.HighDefinition.Attributes;
using UnityEngine.UI;

public enum RP_TYPE
{
    ALBEDO = 0,
    NORMAL,
    DIRECT_DIFFUSE,
    DIRECT_SPECULAR,
    INDIRECT_DIFFUSE,
    REFLECT,
    REFRACT,
    TRANSMITTANCE,
    EMISSIVE,
}

[Serializable]
public struct RenderPassToggle
{
    public RP_TYPE type;
    public Toggle toggle;
}

[Serializable]
public struct RenderResolution
{
    public int width;
    public int height;
}

public class TestManager : MonoBehaviour
{
    [SerializeField] Camera targetCamera;
    [SerializeField] Canvas mainCanvas;
    [SerializeField] Button screenShotButton;

    [SerializeField] RenderPassToggle[] renderPassTogglesAOV;
    [SerializeField] RenderResolution renderpassOutputResolution;

    // Initialized at Start and never changed
    UnityAction handleButtonPressed;

    // State variables
    private bool isCapturing = false;

    void Start()
    {
        handleButtonPressed = TakeScreenshot;

        screenShotButton.onClick.AddListener(handleButtonPressed);
    }

    private void OnDestroy()
    {
        screenShotButton.onClick.RemoveListener(handleButtonPressed);
    }

    void TakeScreenshot()
    {
        if (isCapturing == false)
        {
            StartCoroutine(CaptureProcess());
        }
    }

    private IEnumerator CaptureProcess()
    {
        isCapturing = true;

        mainCanvas.gameObject.SetActive(false);

        // make sure the frame where we hide UI is rendered before we skip rendering to the game screen
        yield return new WaitForEndOfFrame();

        string photoPath = Application.dataPath + "/Screenshots/";
        if (!Directory.Exists(photoPath))
        {
            Directory.CreateDirectory(photoPath);
        }

        string fileName = DateTime.Now.ToString("yyyyMMdd_HHmmss");

        yield return TakeRenderPassShot(renderPassTogglesAOV, renderpassOutputResolution.width, renderpassOutputResolution.height, photoPath, fileName);

        mainCanvas.gameObject.SetActive(true);

        isCapturing = false;
    }

    private IEnumerator TakeRenderPassShot(RenderPassToggle[] toggles, int width, int height, string photoPath, string fileName)
    {
        HDAdditionalCameraData camData = targetCamera.GetComponent<HDAdditionalCameraData>();

        // Need to change render target so camera renders at a high resolution
        RenderTexture rtOld = targetCamera.targetTexture;
        RenderTexture camTargetRT = RenderTexture.GetTemporary(width, height, 24, RenderTextureFormat.ARGB32);
        targetCamera.targetTexture = camTargetRT;

        // Change AA method to avoid waiting for TAA to settle - SMAA should be good enough
        HDAdditionalCameraData.AntialiasingMode oldAAMode = camData.antialiasing;
        camData.antialiasing = HDAdditionalCameraData.AntialiasingMode.SubpixelMorphologicalAntiAliasing;

        AOVRequestBuilder aovRequestBuilder = new AOVRequestBuilder();

        RTHandle aovStoreBuffer = RTHandles.Alloc(width, height);

        Texture2D screenShot = new Texture2D(width, height, TextureFormat.ARGB32, false);

        AOVRequestBufferAllocator allocateAovStoreBuffer = (bufferId) =>
        {
            return aovStoreBuffer;
        };

        AOVBuffers[] requestColorBuffer = new[] { AOVBuffers.Color };
        AOVBuffers[] requestOutputBuffer = new[] { AOVBuffers.Output };

        FramePassCallback doNothing = (cmd, buffers, properties) =>
        {
        };

        for (int II = 0; II < toggles.Length; II++)
        {
            if (toggles[II].toggle.isOn)
            {
                AOVBuffers[] bufferToRequest = null;
                AOVRequest aovRequest = new AOVRequest(AOVRequest.@default).SetLightFilter(DebugLightFilterMode.None);

                switch (toggles[II].type)
                {
                    case RP_TYPE.ALBEDO:
                        bufferToRequest = requestOutputBuffer;
                        aovRequest = aovRequest.SetFullscreenOutput(MaterialSharedProperty.Albedo);
                        break;
                    default:
                        break;
                }

                aovRequestBuilder.Add(
                    aovRequest,
                    allocateAovStoreBuffer,
                    null,
                    bufferToRequest,
                    doNothing);

                AOVRequestDataCollection totalAOVRequests = aovRequestBuilder.Build();

                camData.SetAOVRequests(totalAOVRequests);

                // Finished for this frame, wait for the GPU to finish rendering...
                yield return new WaitForEndOfFrame();

                // At this point rendering of last frame to rtHandle is finished, read this back to file system
                {
                    RenderTexture oldRT = RenderTexture.active;
                    RenderTexture.active = aovStoreBuffer;
                    screenShot.ReadPixels(new Rect(0, 0, width, height), 0, 0);
                    RenderTexture.active = oldRT;

                    byte[] bytes = screenShot.EncodeToPNG();
                    string aovFileName = photoPath + fileName + "_" + toggles[II].type.ToString() + ".png";
                    File.WriteAllBytes(aovFileName, bytes);
                }

                // Advance to build next AOV Request if unnessesary in the same frame 
            }
        }

        Destroy(screenShot);

        RTHandles.Release(aovStoreBuffer);

        aovRequestBuilder.Dispose();

        camData.SetAOVRequests(null);
        camData.antialiasing = oldAAMode;

        targetCamera.targetTexture = rtOld;
        RenderTexture.ReleaseTemporary(camTargetRT);
    }
}

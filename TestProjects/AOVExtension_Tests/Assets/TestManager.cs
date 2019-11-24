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

    [SerializeField] Material refractionAOVSimplifyMat;

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

        AOVRequestBufferAllocator allocateAovStoreBuffer = (bufferId) =>
        {
            return aovStoreBuffer;
        };

        AOVBuffers[] requestOutputBuffer = new[] { AOVBuffers.Output };

        FramePassCallback doNothing = (cmd, buffers, properties) =>
        {
        };

        Texture2D screenShot = new Texture2D(width, height, TextureFormat.ARGB32, false);

        bool hasRefractionPassRequest = false;
        bool hasTransmittancePassRequest = false;
        for (int II = 0; II < toggles.Length; II++)
        {
            if (toggles[II].toggle.isOn)
            {
                bool leaveWorkForLater = false;
                AOVRequest aovRequest = new AOVRequest(AOVRequest.@default).SetLightFilter(DebugLightFilterMode.None);

                switch (toggles[II].type)
                {
                    case RP_TYPE.ALBEDO:
                        aovRequest = aovRequest.SetFullscreenOutput(MaterialSharedProperty.Albedo);
                        break;
                    case RP_TYPE.NORMAL:
                        aovRequest = aovRequest.SetFullscreenOutput(MaterialSharedProperty.Normal);
                        break;
                    case RP_TYPE.DIRECT_DIFFUSE:
                        aovRequest = aovRequest.SetFullscreenOutput(LightingProperty.DirectDiffuseOnly);
                        break;
                    case RP_TYPE.DIRECT_SPECULAR:
                        aovRequest = aovRequest.SetFullscreenOutput(LightingProperty.DirectSpecularOnly);
                        break;
                    case RP_TYPE.INDIRECT_DIFFUSE:
                        aovRequest = aovRequest.SetFullscreenOutput(LightingProperty.IndirectDiffuseOnly);
                        break;
                    case RP_TYPE.REFLECT:
                        aovRequest = aovRequest.SetFullscreenOutput(LightingProperty.ReflectionOnly);
                        break;
                    case RP_TYPE.REFRACT:
                        leaveWorkForLater = true;
                        hasRefractionPassRequest = true; // Will deal with this case specially
                        break;
                    case RP_TYPE.TRANSMITTANCE:
                        leaveWorkForLater = true;
                        hasTransmittancePassRequest = true; // Will deal with this case specially
                        break;
                    case RP_TYPE.EMISSIVE:
                        aovRequest = aovRequest.SetFullscreenOutput(LightingProperty.EmissiveOnly);
                        break;
                    default:
                        break;
                }

                if (!leaveWorkForLater)
                {
                    aovRequestBuilder.Add(
                    aovRequest,
                    allocateAovStoreBuffer,
                    null,
                    requestOutputBuffer,
                    doNothing);

                    camData.SetAOVRequests(aovRequestBuilder.Build());

                    // Finished for this frame, wait for the GPU to finish rendering...
                    yield return new WaitForEndOfFrame();

                    // At this point rendering of last frame to rtHandle is finished, read this back to file system
                    {
                        RenderTexture oldRT = RenderTexture.active;
                        RenderTexture.active = aovStoreBuffer;
                        screenShot.ReadPixels(new Rect(0, 0, width, height), 0, 0);
                        RenderTexture.active = oldRT;

                        string aovFileName = photoPath + fileName + "_" + toggles[II].type.ToString() + ".png";
                        File.WriteAllBytes(aovFileName, screenShot.EncodeToPNG());
                    }

                    // Advance to build next AOV Request if unnessesary in the same frame
                }
            }
        }

        if (hasTransmittancePassRequest || hasRefractionPassRequest)
        {
            AOVRequest aovRequest = new AOVRequest(AOVRequest.@default).SetLightFilter(DebugLightFilterMode.None);
            aovRequest = aovRequest.SetFullscreenOutput(LightingProperty.Transmittance);

            aovRequestBuilder.Add(
                aovRequest,
                allocateAovStoreBuffer,
                null,
                requestOutputBuffer,
                doNothing);

            camData.SetAOVRequests(aovRequestBuilder.Build());

            yield return new WaitForEndOfFrame();

            if (hasTransmittancePassRequest)
            {
                RenderTexture oldRT = RenderTexture.active;
                RenderTexture.active = aovStoreBuffer;
                screenShot.ReadPixels(new Rect(0, 0, width, height), 0, 0);
                RenderTexture.active = oldRT;

                string aovFileName = photoPath + fileName + "_" + RP_TYPE.TRANSMITTANCE.ToString() + ".png";
                File.WriteAllBytes(aovFileName, screenShot.EncodeToPNG());
            }
        }

        if (hasRefractionPassRequest)
        {
            // At this point, aovStoreBuffer should contain the transmittance AOV

            RTHandle extraAovStoreBuffer = RTHandles.Alloc(width, height);

            AOVRequestBufferAllocator allocateExtraAovStoreBuffer = (bufferId) =>
            {
                return extraAovStoreBuffer;
            };

            AOVRequest aovRequest = new AOVRequest(AOVRequest.@default).SetLightFilter(DebugLightFilterMode.None);
            aovRequest = aovRequest.SetFullscreenOutput(LightingProperty.RefractionOnly);

            aovRequestBuilder.Add(
                aovRequest,
                allocateExtraAovStoreBuffer,
                null,
                requestOutputBuffer,
                doNothing);

            camData.SetAOVRequests(aovRequestBuilder.Build());

            yield return new WaitForEndOfFrame();

            // We need to combine the transmittance and refraction pass
            // Note now camTargetRT is no longer in use, let's use it as destination RT

            targetCamera.enabled = false;

            refractionAOVSimplifyMat.SetTexture("_TransmittanceMap", aovStoreBuffer);
            Graphics.Blit(extraAovStoreBuffer, camTargetRT, refractionAOVSimplifyMat);

            yield return new WaitForEndOfFrame();

            RTHandles.Release(extraAovStoreBuffer);

            RenderTexture oldRT = RenderTexture.active;
            RenderTexture.active = camTargetRT;
            screenShot.ReadPixels(new Rect(0, 0, width, height), 0, 0);
            RenderTexture.active = oldRT;

            targetCamera.enabled = true;

            string aovFileName = photoPath + fileName + "_" + RP_TYPE.REFRACT.ToString() + ".png";
            File.WriteAllBytes(aovFileName, screenShot.EncodeToPNG());
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

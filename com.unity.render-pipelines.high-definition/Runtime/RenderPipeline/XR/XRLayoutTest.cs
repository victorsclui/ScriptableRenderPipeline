// Helper class to test different XR layouts
// Run Unity with -xr-tests to enable XR test mode

namespace UnityEngine.Rendering.HighDefinition
{
    internal class XRLayoutTest
    {
        // Set by test infrastructure
        internal static bool automatedTestRunning = false;

        internal enum Mode
        {
            Default,            // Default camera layout
            TestComposite       // Composite image using Texture2DArray, multi-pass, single-pass and double-wide
        }

        int totalCompositeViews = 2;

        RTHandle texArrayTarget = null;
        RTHandle doubleWideTarget = null;
        MaterialPropertyBlock matBlock = null;

        internal XRLayoutTest()
        {
            TextureXR.maxViews = 2;
            Debug.LogFormat("XR test mode enabled with '-xr-tests' argument: allocating {0} slices for each render target.", TextureXR.slices);

            matBlock = new MaterialPropertyBlock();
            texArrayTarget = RTHandles.Alloc(Vector2.one, TextureXR.slices, dimension: TextureXR.dimension, useDynamicScale: true, autoGenerateMips: false, name: "XRLayoutTest n-array");
            doubleWideTarget = RTHandles.Alloc(new Vector2(2.0f, 1.0f), 1, dimension: TextureXR.dimension, useDynamicScale: true, autoGenerateMips: false, name: "XRLayoutTest n-wide");
        }

        internal void Cleanup()
        {
            XRSystem.SetCustomFrameSetup(null);
            RTHandles.Release(texArrayTarget);
            RTHandles.Release(doubleWideTarget);
        }

        internal void Update(Mode mode)
        {
            if (automatedTestRunning)
                mode = Mode.TestComposite;

            switch (mode)
            {
                case Mode.Default:
                    XRSystem.SetCustomFrameSetup(null);
                    break;

                case Mode.TestComposite:
                    XRSystem.SetCustomFrameSetup(LayoutComposite);
                    break;
            }
        }

        bool LayoutComposite(XRSystem.FrameLayout frameLayout)
        {
            Camera camera = frameLayout.camera;

            if (camera != null && camera.cameraType == CameraType.Game && camera.TryGetCullingParameters(false, out var cullingParams))
            {
                // XRTODO: scenes with multiple cameras
                if (camera != Camera.main)
                    return false;

                cullingParams.stereoProjectionMatrix = camera.projectionMatrix;
                cullingParams.stereoViewMatrix = camera.worldToCameraMatrix;

                // Common pass settings
                var passInfo = new XRPassCreateInfo
                {
                    cullingPassId = 0,
                    cullingParameters = cullingParams
                };

                // Common view setings
                var viewInfo = new XRViewCreateInfo
                {
                    viewMatrix = camera.worldToCameraMatrix,
                    projMatrix = camera.projectionMatrix,
                    viewport = camera.pixelRect
                };

                // Pass 0 : single-pass 2x rendering to intermediate texture array
                {
                    passInfo.multipassId = 0;
                    passInfo.renderTarget = texArrayTarget;
                    passInfo.customMirrorView = MirrorComposite;

                    XRPass pass = frameLayout.CreatePass(passInfo);

                    for (int viewIndex = 0; viewIndex < TextureXR.slices; viewIndex++)
                    {
                        viewInfo.textureArraySlice = viewIndex;
                        frameLayout.AddViewToPass(viewInfo, pass);
                    }
                }

                // Pass 1 : multi-pass 1x rendering to double-wide target 
                {
                    passInfo.multipassId = 1;
                    passInfo.renderTarget = doubleWideTarget;
                    passInfo.customMirrorView = MirrorCompositeDoubleWide;

                    XRPass pass = frameLayout.CreatePass(passInfo);

                    viewInfo.viewport.x = viewInfo.viewport.width;
                    viewInfo.textureArraySlice = -1;
                    frameLayout.AddViewToPass(viewInfo, pass);
                }

                return true;
            }

            return false;
        }

        void MirrorComposite(XRPass pass, CommandBuffer cmd, RenderTexture rt, Rect viewport)
        {
            float oneOverViewCount = 1.0f / totalCompositeViews;
            var rtScaleSource = texArrayTarget.rtHandleProperties.rtHandleScale;

            cmd.SetRenderTarget(rt);

            var blitMaterial = HDUtils.GetBlitMaterial(texArrayTarget.rt.dimension, singleSlice: true);
            matBlock.SetTexture(HDShaderIDs._BlitTexture, texArrayTarget);
            matBlock.SetFloat(HDShaderIDs._BlitMipLevel, 0);

            viewport.width *= oneOverViewCount;
            viewport.height /= pass.viewCount;

            for (int viewIndex = 0; viewIndex < pass.viewCount; ++viewIndex)
            {
                float xBias = pass.multipassId * oneOverViewCount;
                float yBias = viewIndex / (float)pass.viewCount;
                var dyn = 1.0f / DynamicResolutionHandler.instance.GetCurrentScale();

                if (rt == null)
                    viewport.y = viewport.height - viewIndex * viewport.height;
                else
                    viewport.y = viewIndex * viewport.height;

                matBlock.SetInt(HDShaderIDs._BlitTexArraySlice, pass.GetTextureArraySlice(viewIndex));
                matBlock.SetVector(HDShaderIDs._BlitScaleBias, new Vector4(dyn * rtScaleSource.x * oneOverViewCount, dyn * rtScaleSource.y / pass.viewCount, dyn * rtScaleSource.x * xBias, dyn * rtScaleSource.y * yBias));
                matBlock.SetVector(HDShaderIDs._BlitScaleBiasRt, new Vector4(1.0f, 1.0f, 0.0f, 0.0f));

                cmd.SetViewport(viewport);
                cmd.SetGlobalVector(HDShaderIDs._RTHandleScale, texArrayTarget.rtHandleProperties.rtHandleScale);
                cmd.DrawProcedural(Matrix4x4.identity, blitMaterial, 2, MeshTopology.Quads, 4, 1, matBlock);
            }
        }

        void MirrorCompositeDoubleWide(XRPass pass, CommandBuffer cmd, RenderTexture rt, Rect viewport)
        {
            float oneOverViewCount = 1.0f / totalCompositeViews;
            var rtScaleSource = doubleWideTarget.rtHandleProperties.rtHandleScale;

            cmd.SetRenderTarget(rt);

            var blitMaterial = HDUtils.GetBlitMaterial(doubleWideTarget.rt.dimension);
            matBlock.SetTexture(HDShaderIDs._BlitTexture, doubleWideTarget);
            matBlock.SetFloat(HDShaderIDs._BlitMipLevel, 0);

            // copy last view
            int viewIndex = pass.viewCount - 1;
            float biasX =  pass.multipassId * oneOverViewCount;

            float dyn = 1.0f / DynamicResolutionHandler.instance.GetCurrentScale();

            viewport.x = viewport.width * biasX;
            viewport.width *= oneOverViewCount;

            float scaleX = 0.5f * oneOverViewCount;

            matBlock.SetVector(HDShaderIDs._BlitScaleBias, new Vector4(scaleX * dyn * rtScaleSource.x, dyn * rtScaleSource.y, (dyn * rtScaleSource.x) * (scaleX + biasX), 0.0f));
            matBlock.SetVector(HDShaderIDs._BlitScaleBiasRt, new Vector4(1.0f, 1.0f, 0.0f, 0.0f));

            cmd.SetViewport(viewport);
            cmd.SetGlobalVector(HDShaderIDs._RTHandleScale, doubleWideTarget.rtHandleProperties.rtHandleScale);
            cmd.DrawProcedural(Matrix4x4.identity, blitMaterial, 2, MeshTopology.Quads, 4, 1, matBlock);
        }
    }
}

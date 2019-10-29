// Helper class to test different XR Layout
// Run Unity with -xr-tests to enable XR test mode

namespace UnityEngine.Rendering.HighDefinition
{
    internal class XRLayoutTest
    {
        internal static bool automatedTestRunning = false;

        internal enum Mode
        {
            Default,            // Default camera layout
            TestComposite,      // Composite image using Texture2DArray, multi-pass, single-pass and double-wide
            TestFixedFoveated   // Simulate FFR for one eye using single-pass and 2 viewports
        }

        RTHandle texArrayTarget = null;
        RTHandle doubleWideTarget = null;
        MaterialPropertyBlock matBlock = null;

        int totalCompositeViews = 1;

        // [0.0 ; 1.0]
        float fixedFoveatedRatio = 0.5f;

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

                case Mode.TestFixedFoveated:
                    XRSystem.SetCustomFrameSetup(LayoutFFR);
                    break;
            }
        }

        bool LayoutComposite(XRSystem.FrameLayout frameLayout)
        {
            Camera camera = frameLayout.camera;

            // skip RT ? NOOO : because test always use render target

            //if (camera?.targetTexture != null)
            //    return false;

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

                //// Pass 0 : multi-pass 1x directly to target texture
                //{
                //    passInfo.multipassId = 0;
                //    passInfo.renderTarget = camera.targetTexture;
                //    passInfo.customMirrorView = null;

                //    XRPass pass = frameLayout.CreatePass(passInfo);

                //    viewInfo.textureArraySlice = -1;
                //    frameLayout.AddViewToPass(viewInfo, pass);
                //}

                //// Pass 1 : multi-pass 1x to texture array with custom mirror view
                //{
                //    passInfo.multipassId = 1;
                //    passInfo.renderTarget = texArrayTarget;
                //    passInfo.customMirrorView = MirrorComposite;

                //    XRPass pass = frameLayout.CreatePass(passInfo);

                //    viewInfo.textureArraySlice = 1;
                //    frameLayout.AddViewToPass(viewInfo, pass);
                //}

                // Pass 2 : single-pass rendering to intermediate targets with custom mirror view to final target
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

                //// Pass 3 : single-pass rendering to double-wide target
                //{
                //    passInfo.multipassId = 3;
                //    passInfo.renderTarget = doubleWideTarget;
                //    passInfo.customMirrorView = MirrorCompositeDoubleWide;

                //    XRPass pass = frameLayout.CreatePass(passInfo);

                //    for (int viewIndex = 0; viewIndex < TextureXR.slices; viewIndex++)
                //    {
                //        viewInfo.viewport.x = viewIndex * viewInfo.viewport.width;
                //        viewInfo.textureArraySlice = -1;
                //        frameLayout.AddViewToPass(viewInfo, pass);
                //    }
                //}

                return true;
            }

            return false;
        }

        void MirrorComposite(XRPass pass, CommandBuffer cmd, RenderTargetIdentifier rt, Rect viewport)
        {
            float oneOverViewCount = 1.0f / totalCompositeViews;
            var rtScaleSource = texArrayTarget.rtHandleProperties.rtHandleScale;

            cmd.SetRenderTarget(rt);

            var blitMaterial = HDUtils.GetBlitMaterial(texArrayTarget.rt.dimension, singleSlice: true);
            matBlock.SetTexture(HDShaderIDs._BlitTexture, texArrayTarget);
            matBlock.SetFloat(HDShaderIDs._BlitMipLevel, 0);

            //for (int viewIndex = 0; viewIndex < pass.viewCount; ++viewIndex)
            int viewIndex = pass.viewCount - 1;

            // TEMP
            //viewIndex = 0;
            //var viewport = pass.GetViewport(viewIndex);
            //var xBias = (viewport.x + pass.multipassId * oneOverViewCount) / (viewport.x + viewport.width);

            var xBias = pass.multipassId * oneOverViewCount;
            xBias = 0.0f;

            var dyn = 1.0f / DynamicResolutionHandler.instance.GetCurrentScale();

            //viewport.x += viewport.width * xBias;
            //viewport.width *= oneOverViewCount;

            matBlock.SetInt(HDShaderIDs._BlitTexArraySlice, pass.GetTextureArraySlice(viewIndex));
            matBlock.SetVector(HDShaderIDs._BlitScaleBias, new Vector4(dyn * rtScaleSource.x * oneOverViewCount, dyn * rtScaleSource.y, dyn * rtScaleSource.x * xBias, 0.0f));
            matBlock.SetVector(HDShaderIDs._BlitScaleBiasRt, new Vector4(1.0f, 1.0f, 0.0f, 0.0f));

            cmd.SetViewport(viewport);
            cmd.SetGlobalVector(HDShaderIDs._RTHandleScale, texArrayTarget.rtHandleProperties.rtHandleScale);

            // Point sampling with quad
            cmd.DrawProcedural(Matrix4x4.identity, blitMaterial, 2, MeshTopology.Quads, 4, 1, matBlock);
        }

        void MirrorCompositeDoubleWide(XRPass pass, CommandBuffer cmd, RenderTargetIdentifier rt, Rect viewport)
        {
            float oneOverViewCount = 1.0f / totalCompositeViews;
            var rtScaleSource = doubleWideTarget.rtHandleProperties.rtHandleScale;

            cmd.SetRenderTarget(rt);

            // VALIDATE in RenderDoc

            var blitMaterial = HDUtils.GetBlitMaterial(doubleWideTarget.rt.dimension);
            matBlock.SetTexture(HDShaderIDs._BlitTexture, doubleWideTarget);
            matBlock.SetFloat(HDShaderIDs._BlitMipLevel, 0);

            int viewIndex = pass.viewCount - 1;
            //var viewport = pass.GetViewport(viewIndex);
            var xBias =  pass.multipassId * oneOverViewCount;

            var dyn = 1.0f / DynamicResolutionHandler.instance.GetCurrentScale();

            viewport.x = viewport.width * xBias;
            viewport.width *= oneOverViewCount;

            matBlock.SetVector(HDShaderIDs._BlitScaleBias, new Vector4(0.5f * dyn * rtScaleSource.x * oneOverViewCount, dyn * rtScaleSource.y, 0.5f * dyn * rtScaleSource.x * xBias, 0.0f));
            matBlock.SetVector(HDShaderIDs._BlitScaleBiasRt, new Vector4(1.0f, 1.0f, 0.0f, 0.0f));

            cmd.SetViewport(viewport);
            cmd.SetGlobalVector(HDShaderIDs._RTHandleScale, doubleWideTarget.rtHandleProperties.rtHandleScale);
            cmd.DrawProcedural(Matrix4x4.identity, blitMaterial, 3, MeshTopology.Quads, 4, 1, matBlock);
        }

        bool LayoutFFR(XRSystem.FrameLayout frameLayout)
        {
            Camera camera = frameLayout.camera;

            if (camera != null && camera.cameraType == CameraType.Game && camera.TryGetCullingParameters(false, out var cullingParams))
            {
                cullingParams.stereoProjectionMatrix = camera.projectionMatrix;
                cullingParams.stereoViewMatrix = camera.worldToCameraMatrix;

                // Common pass settings
                var passInfo = new XRPassCreateInfo
                {
                    cullingPassId = 0,
                    cullingParameters = cullingParams,
                    renderTarget = texArrayTarget,
                    customMirrorView = MirrorFFR
                };

                XRPass pass = frameLayout.CreatePass(passInfo);

                // Common view setings
                var viewInfo = new XRViewCreateInfo
                {
                    viewMatrix = camera.worldToCameraMatrix,
                    projMatrix = camera.projectionMatrix,
                    viewport = camera.pixelRect,
                    textureArraySlice = -1,
                };

                var frustumPlanes = camera.projectionMatrix.decomposeProjection;

                // View 0 : low resolution fullscreen
                {
                    viewInfo.textureArraySlice = 0;
                    frameLayout.AddViewToPass(viewInfo, pass);
                }

                // View 1 : high resolution fovea
                {
                    var ffrScale = (1.0f - fixedFoveatedRatio) * 0.5f;

                    var planes = frustumPlanes;
                    planes.left = Mathf.Lerp(frustumPlanes.left, frustumPlanes.right, ffrScale);
                    planes.right = Mathf.Lerp(frustumPlanes.left, frustumPlanes.right, 1.0f - ffrScale);
                    planes.bottom = Mathf.Lerp(frustumPlanes.bottom, frustumPlanes.top, ffrScale);
                    planes.top = Mathf.Lerp(frustumPlanes.bottom, frustumPlanes.top, 1.0f - ffrScale);
                    viewInfo.projMatrix = camera.orthographic ? Matrix4x4.Ortho(planes.left, planes.right, planes.bottom, planes.top, planes.zNear, planes.zFar) : Matrix4x4.Frustum(planes);

                    viewInfo.textureArraySlice = 1;
                    frameLayout.AddViewToPass(viewInfo, pass);
                }

                return true;
            }

            return false;
        }

        void MirrorFFR(XRPass pass, CommandBuffer cmd, RenderTargetIdentifier rt, Rect viewport)
        {
            cmd.SetRenderTarget(rt);

            var rtScaleSource = texArrayTarget.rtHandleProperties.rtHandleScale;

            // rename this everywhere
            var dyn = 1.0f / DynamicResolutionHandler.instance.GetCurrentScale();

            var blitMaterial = HDUtils.GetBlitMaterial(texArrayTarget.rt.dimension, singleSlice: true);
            matBlock.SetTexture(HDShaderIDs._BlitTexture, texArrayTarget);
            matBlock.SetFloat(HDShaderIDs._BlitMipLevel, 0);

            // Full screen background view
            {
                int viewIndex = 0;
                //var viewport = pass.GetViewport(viewIndex);

                matBlock.SetInt(HDShaderIDs._BlitTexArraySlice, pass.GetTextureArraySlice(viewIndex));
                matBlock.SetVector(HDShaderIDs._BlitScaleBias, new Vector4(dyn * rtScaleSource.x, dyn * rtScaleSource.y, 0.0f, 0.0f));
                matBlock.SetVector(HDShaderIDs._BlitScaleBiasRt, new Vector4(1.0f, 1.0f, 0.0f, 0.0f));

                cmd.SetViewport(viewport);
                cmd.SetGlobalVector(HDShaderIDs._RTHandleScale, texArrayTarget.rtHandleProperties.rtHandleScale);
                cmd.DrawProcedural(Matrix4x4.identity, blitMaterial, 3, MeshTopology.Quads, 4, 1, matBlock);
            }

            // high res
            {
                int viewIndex = 1;
                //var viewport = pass.GetViewport(viewIndex);

                viewport.x += 0.5f * (viewport.width -  (viewport.width *  fixedFoveatedRatio));
                viewport.y += 0.5f * (viewport.height - (viewport.height * fixedFoveatedRatio));

                viewport.width  *= fixedFoveatedRatio;
                viewport.height *= fixedFoveatedRatio;

                matBlock.SetInt(HDShaderIDs._BlitTexArraySlice, pass.GetTextureArraySlice(viewIndex));
                matBlock.SetVector(HDShaderIDs._BlitScaleBias, new Vector4(dyn * rtScaleSource.x, dyn * rtScaleSource.y, 0.0f, 0.0f));
                matBlock.SetVector(HDShaderIDs._BlitScaleBiasRt, new Vector4(1.0f, 1.0f, 0.0f, 0.0f));

                cmd.SetViewport(viewport);
                cmd.SetGlobalVector(HDShaderIDs._RTHandleScale, texArrayTarget.rtHandleProperties.rtHandleScale);
                cmd.DrawProcedural(Matrix4x4.identity, blitMaterial, 3, MeshTopology.Quads, 4, 1, matBlock);
            }
        }
    }
}

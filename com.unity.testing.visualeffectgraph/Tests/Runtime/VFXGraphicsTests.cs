using System;
using System.Collections.Generic;
using System.Linq;
using System.Collections;
using UnityEngine;
using UnityEngine.VFX;
using UnityEngine.TestTools;
using UnityEngine.TestTools.Graphics;
using UnityEngine.Rendering;
using System.IO;
#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.VFX;
using System.Reflection;
#endif
using NUnit.Framework;
using Object = UnityEngine.Object;
using UnityEngine.VFX.Utility;

namespace UnityEngine.VFX.Test
{
    public class VFXGraphicsTests
    {
        int m_previousCaptureFrameRate;
        float m_previousFixedTimeStep;
        float m_previousMaxDeltaTime;
        [OneTimeSetUp]
        public void Init()
        {
            m_previousCaptureFrameRate = Time.captureFramerate;
            m_previousFixedTimeStep = UnityEngine.VFX.VFXManager.fixedTimeStep;
            m_previousMaxDeltaTime = UnityEngine.VFX.VFXManager.maxDeltaTime;
        }

        static readonly string[] ExcludedTestsButKeepLoadScene =
        {
            "RenderStates", // Unstable. There is an instability with shadow rendering. TODO Fix that
            "ConformAndSDF", // Turbulence is not deterministic
            "13_Decals", //doesn't render TODO investigate why <= this one is in world space
            "05_MotionVectors", //possible GPU Hang on this, skip it temporally
        };

        static readonly string[] UnstableMetalTests =
        {
            // Currently known unstable results, could be Metal or more generic HLSLcc issue across multiple graphics targets
        };

        [UnityTest, Category("VisualEffect")]
        [PrebuildSetup("SetupGraphicsTestCases")]
        [UseGraphicsTestCases]
        public IEnumerator Run(GraphicsTestCase testCase)
        {
#if UNITY_EDITOR
            while (SceneView.sceneViews.Count > 0)
            {
                var sceneView = SceneView.sceneViews[0] as SceneView;
                sceneView.Close();
            }
#endif
            SceneManagement.SceneManager.LoadScene(testCase.ScenePath);

            // Always wait one frame for scene load
            yield return null;

            var testSettingsInScene = Object.FindObjectOfType<GraphicsTestSettings>();
            var vfxTestSettingsInScene = Object.FindObjectOfType<VFXGraphicsTestSettings>();

            //Setup frame rate capture
            float simulateTime = VFXGraphicsTestSettings.defaultSimulateTime;
            int captureFrameRate = VFXGraphicsTestSettings.defaultCaptureFrameRate;

            if (vfxTestSettingsInScene != null)
            {
                simulateTime = vfxTestSettingsInScene.simulateTime;
                captureFrameRate = vfxTestSettingsInScene.captureFrameRate;
            }
            float frequency = 1.0f / captureFrameRate;

            Time.captureFramerate = captureFrameRate;
            UnityEngine.VFX.VFXManager.fixedTimeStep = frequency;
            UnityEngine.VFX.VFXManager.maxDeltaTime = frequency;

            int captureSizeWidth = 512;
            int captureSizeHeight = 512;
            if (testSettingsInScene != null)
            {
                captureSizeWidth = testSettingsInScene.ImageComparisonSettings.TargetWidth;
                captureSizeHeight = testSettingsInScene.ImageComparisonSettings.TargetHeight;
            }

            var camera = GameObject.FindGameObjectWithTag("MainCamera").GetComponent<Camera>();
            if (camera)
            {
                var vfxComponents = Resources.FindObjectsOfTypeAll<VisualEffect>();

                var rt = RenderTexture.GetTemporary(captureSizeWidth, captureSizeHeight, 24);
                camera.targetTexture = rt;

                foreach (var component in vfxComponents)
                {
                    component.Reinit();
                }

#if UNITY_EDITOR
                //When we change the graph, if animator was already enable, we should reinitialize animator to force all BindValues
                var animators = Resources.FindObjectsOfTypeAll<Animator>();
                foreach (var animator in animators)
                {
                    animator.Rebind();
                }
                var audioSources = Resources.FindObjectsOfTypeAll<AudioSource>();
#endif
                var paramBinders = Resources.FindObjectsOfTypeAll<VFXPropertyBinder>();
                foreach (var paramBinder in paramBinders)
                {
                    var binders = paramBinder.GetParameterBinders<VFXBinderBase>();
                    foreach (var binder in binders)
                    {
                        binder.Reset();
                    }
                }

                int waitFrameCount = (int)(simulateTime / frequency);
                int startFrameIndex = Time.frameCount;
                int expectedFrameIndex = startFrameIndex + waitFrameCount;
                while (Time.frameCount != expectedFrameIndex)
                {
                    yield return null;
#if UNITY_EDITOR
                    foreach (var audioSource in audioSources)
                        if (audioSource.clip != null && audioSource.playOnAwake)
                            audioSource.PlayDelayed(Mathf.Repeat(simulateTime, audioSource.clip.length));
#endif
                }

                Texture2D actual = null;
                try
                {
                    camera.targetTexture = null;
                    actual = new Texture2D(captureSizeWidth, captureSizeHeight, TextureFormat.RGB24, false);
                    RenderTexture.active = rt;
                    actual.ReadPixels(new Rect(0, 0, captureSizeWidth, captureSizeHeight), 0, 0);
                    RenderTexture.active = null;
                    actual.Apply();

                    var imageComparisonSettings = new ImageComparisonSettings() { AverageCorrectnessThreshold = 5e-4f };
                    if (testSettingsInScene != null)
                    {
                        imageComparisonSettings.AverageCorrectnessThreshold = testSettingsInScene.ImageComparisonSettings.AverageCorrectnessThreshold;
                    }

                    if (!ExcludedTestsButKeepLoadScene.Any(o => testCase.ScenePath.Contains(o)) &&
                        !(SystemInfo.graphicsDeviceType == GraphicsDeviceType.Metal && UnstableMetalTests.Any(o => testCase.ScenePath.Contains(o))))
                    {
                        ImageAssert.AreEqual(testCase.ReferenceImage, actual, imageComparisonSettings);
                    }
                    else
                    {
                        Debug.LogFormat("GraphicTest '{0}' result has been ignored", testCase.ReferenceImage);
                    }
                }
                finally
                {
                    RenderTexture.ReleaseTemporary(rt);
                    if (actual != null)
                        UnityEngine.Object.Destroy(actual);
                }
            }
        }

        [TearDown]
        public void TearDown()
        {
#if UNITY_EDITOR
            UnityEditor.TestTools.Graphics.ResultsUtility.ExtractImagesFromTestProperties(TestContext.CurrentContext.Test);
#endif
        }

        [OneTimeTearDown]
        public void OneTimeTearDown()
        {
            Time.captureFramerate = m_previousCaptureFrameRate;
            UnityEngine.VFX.VFXManager.fixedTimeStep = m_previousFixedTimeStep;
            UnityEngine.VFX.VFXManager.maxDeltaTime = m_previousMaxDeltaTime;

        }
    }
}

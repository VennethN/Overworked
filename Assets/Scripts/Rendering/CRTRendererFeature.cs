using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.RenderGraphModule.Util;
using UnityEngine.Rendering.Universal;
using static UnityEngine.Rendering.RenderGraphModule.Util.RenderGraphUtils;

public class CRTRendererFeature : ScriptableRendererFeature
{
    [System.Serializable]
    public class CRTSettings
    {
        [Tooltip("Material using the Custom/CRTEffect shader")]
        public Material crtMaterial;

        [Tooltip("When the CRT effect runs in the pipeline")]
        public RenderPassEvent injectionPoint = RenderPassEvent.AfterRenderingPostProcessing;
    }

    public CRTSettings settings = new CRTSettings();
    private CRTRenderPass _renderPass;

    public override void Create()
    {
        _renderPass = new CRTRenderPass();
        _renderPass.renderPassEvent = settings.injectionPoint;
    }

    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        if (settings.crtMaterial == null)
            return;

        _renderPass.Setup(settings.crtMaterial);
        renderer.EnqueuePass(_renderPass);
    }

    protected override void Dispose(bool disposing)
    {
        _renderPass?.Dispose();
    }

    private class CRTRenderPass : ScriptableRenderPass
    {
        private Material _material;
        private RTHandle _tempTexture;

        public void Setup(Material material)
        {
            _material = material;
        }

        public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
        {
            if (_material == null)
                return;

            var resourceData = frameData.Get<UniversalResourceData>();

            if (resourceData.isActiveTargetBackBuffer)
                return;

            // Step 1: Copy active color to a temp texture
            var source = resourceData.activeColorTexture;

            var desc = renderGraph.GetTextureDesc(source);
            desc.name = "_CRTTemp";
            desc.clearBuffer = false;
            var tempCopy = renderGraph.CreateTexture(desc);

            renderGraph.AddBlitPass(source, tempCopy, Vector2.one, Vector2.zero, passName: "CRT Copy");

            // Step 2: Blit from temp back to active color with CRT material
            var destination = resourceData.activeColorTexture;
            var blitParams = new BlitMaterialParameters(tempCopy, destination, _material, 0);
            renderGraph.AddBlitPass(blitParams, passName: "CRT Effect");
        }

        // Legacy fallback (compatibility mode)
        [System.Obsolete]
        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            if (_material == null)
                return;

            var cmd = CommandBufferPool.Get("CRT Effect");
            var cameraColor = renderingData.cameraData.renderer.cameraColorTargetHandle;

            Blitter.BlitCameraTexture(cmd, cameraColor, _tempTexture);
            Blitter.BlitCameraTexture(cmd, _tempTexture, cameraColor, _material, 0);

            context.ExecuteCommandBuffer(cmd);
            cmd.Clear();
            CommandBufferPool.Release(cmd);
        }

        [System.Obsolete]
        public override void OnCameraSetup(CommandBuffer cmd, ref RenderingData renderingData)
        {
            var descriptor = renderingData.cameraData.cameraTargetDescriptor;
            descriptor.depthBufferBits = 0;
            RenderingUtils.ReAllocateHandleIfNeeded(ref _tempTexture, descriptor, name: "_CRTTempTex");
        }

        public void Dispose()
        {
            _tempTexture?.Release();
        }
    }
}

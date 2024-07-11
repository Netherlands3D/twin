using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using System.Collections.Generic;
using Netherlands3D.Twin;
using Netherlands3D.Twin.UI.LayerInspector;

public class PreRenderingVisibilityControlPass : ScriptableRenderPass
{
    public List<LayerNL3DBase> targets = new();

    public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
    {
        foreach (var target in targets)
        {
            target.ActiveSelf = false;
        }
    }
}

public class PostRenderingVisibilityControlPass : ScriptableRenderPass
{
    public List<LayerNL3DBase> targets = new();

    public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
    {
        foreach (var target in targets)
        {
            target.ActiveSelf = true;
        }
    }
}

public class VisibilityRenderFeature : ScriptableRendererFeature
{
    PreRenderingVisibilityControlPass preRenderingPass;
    PostRenderingVisibilityControlPass postRenderingPass;

    public Camera targetCamera;

    public override void Create()
    {
        preRenderingPass = new PreRenderingVisibilityControlPass();
        postRenderingPass = new PostRenderingVisibilityControlPass();

        preRenderingPass.targets = SideBySideComparisonCamera.layers;
        postRenderingPass.targets = SideBySideComparisonCamera.layers;

        // set render pass event to decide when the render pass would be invoked
        preRenderingPass.renderPassEvent = RenderPassEvent.BeforeRenderingOpaques;
        postRenderingPass.renderPassEvent = RenderPassEvent.AfterRenderingTransparents;
    }

    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        if (renderingData.cameraData.camera == SideBySideComparisonCamera.targetCamera)
        {
            renderer.EnqueuePass(preRenderingPass);
            renderer.EnqueuePass(postRenderingPass);
        }
    }
}
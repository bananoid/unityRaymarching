using System;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.PostProcessing;

[Serializable]
[PostProcess(typeof(GlitchPostProcessRenderer), PostProcessEvent.BeforeStack, "VJ/GlitchPostProcess")]
public sealed class GlitchPostProcess : PostProcessEffectSettings
{

}

public sealed class GlitchPostProcessRenderer : PostProcessEffectRenderer<GlitchPostProcess>
{
    public override void Init()
    {
        base.Init();
    }

    public override void Render(PostProcessRenderContext context)
    {
        var sheet = context.propertySheets.Get(Shader.Find("VJ/GlitchPostProcess"));

        context.command.BlitFullscreenTriangle(context.source, context.destination, sheet, 0);

    }
}
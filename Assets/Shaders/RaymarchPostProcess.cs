using System;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.PostProcessing;

[Serializable]
public sealed class ShaderParameter : ParameterOverride<Shader> { }

[Serializable]
[PostProcess(typeof(RaymarchPostProcessRenderer), PostProcessEvent.BeforeStack, "Custom/RaymarchPostProcess")]
public sealed class RaymarchPostProcess : PostProcessEffectSettings
{
    public IntParameter maxIterations = new IntParameter { value = 64 };
    public FloatParameter maxDistance = new FloatParameter { value = 100f };
    public FloatParameter minDistance = new FloatParameter { value = 0.01f };

    public DepthTextureMode GetCameraFlags()
    {
        return DepthTextureMode.Depth; // DepthTextureMode.DepthNormals;
    }
}

public sealed class RaymarchPostProcessRenderer : PostProcessEffectRenderer<RaymarchPostProcess>
{
    Transform directionalLight;

    public override void Init()
    {
        base.Init();

        GameObject light = GameObject.FindGameObjectWithTag("MainLight");

        if (light)
            directionalLight = light.transform;
    }

    public override void Render(PostProcessRenderContext context)
    {
        Camera _cam = context.camera;

        var sheet = context.propertySheets.Get(Shader.Find("Raymarch/RaymarchHDRP"));
        sheet.properties.SetMatrix("_CamFrustum", FrustumCorners(_cam));
        sheet.properties.SetMatrix("_CamToWorld", _cam.cameraToWorldMatrix);
        sheet.properties.SetVector("_CamWorldSpace", _cam.transform.position);
        sheet.properties.SetInt("_MaxIterations", settings.maxIterations);
        sheet.properties.SetFloat("_MaxDistance", settings.maxDistance);
        sheet.properties.SetFloat("_MinDistance", settings.minDistance);

        if (directionalLight)
        {
            Vector3 position = directionalLight.forward;
            sheet.properties.SetVector("_LightDir", new Vector4(position.x, position.y, position.z, 1));
        }

        context.command.BlitFullscreenTriangle(context.source, context.destination, sheet, 0);
    }

    private Matrix4x4 FrustumCorners(Camera cam)
    {
        Transform camtr = cam.transform;

        Vector3[] frustumCorners = new Vector3[4];
        cam.CalculateFrustumCorners(new Rect(0, 0, 1, 1),
        cam.farClipPlane, cam.stereoActiveEye, frustumCorners);

        Vector3 bottomLeft = camtr.TransformVector(frustumCorners[1]);
        Vector3 topLeft = camtr.TransformVector(frustumCorners[0]);
        Vector3 bottomRight = camtr.TransformVector(frustumCorners[2]);

        Matrix4x4 frustumVectorsArray = Matrix4x4.identity;
        frustumVectorsArray.SetRow(0, bottomLeft);
        frustumVectorsArray.SetRow(1, bottomLeft + (bottomRight - bottomLeft) * 2);
        frustumVectorsArray.SetRow(2, bottomLeft + (topLeft - bottomLeft) * 2);

        return frustumVectorsArray;
    }
}
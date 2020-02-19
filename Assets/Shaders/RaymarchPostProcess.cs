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
    [Header("Setup")]
    public IntParameter maxIterations = new IntParameter { value = 200 };
    public FloatParameter maxDistance = new FloatParameter { value = 100f };
    public FloatParameter minDistance = new FloatParameter { value = 0.01f };

    [Header("Shadow")]
    public FloatParameter shadowIntensity = new FloatParameter { value = 1.0f };
    [ColorUsage(true, true)]
    public ColorParameter shadowColor = new ColorParameter { value = new Color(0,0,0) };
    public Vector2Parameter shadowDistance = new Vector2Parameter { value = new Vector2(0.1f, 100)};
    public FloatParameter shadowPenumbra = new FloatParameter { value = 1.5f };
    
    [Header("Ambient Occlusion")]
    public FloatParameter aoIntensity = new FloatParameter { value = 1.0f };
    public FloatParameter aoStepSize = new FloatParameter { value = 6.0f };
    public IntParameter aoIterations = new IntParameter { value = 5 };
    
    [Header("Signed Distance Field")]
    public Vector4Parameter sphere1 = new Vector4Parameter { value = new Vector4(0,0,0,3)};
    public Vector4Parameter sphere2 = new Vector4Parameter { value = new Vector4(0,1,0,3)};
    public FloatParameter sphereIntersectSmooth = new FloatParameter { value = 0.01f }; 

    public DepthTextureMode GetCameraFlags()
    {
        return DepthTextureMode.Depth; // DepthTextureMode.DepthNormals;
    }
}

public sealed class RaymarchPostProcessRenderer : PostProcessEffectRenderer<RaymarchPostProcess>
{
    GameObject directionalLight;
    Transform directionalLightTransform;

    public override void Init()
    {
        base.Init();

        directionalLight = GameObject.FindGameObjectWithTag("MainLight");

        // if (light){
        //     directionalLight = light.GetComponent<Light>();

        //     directionalLightTransform = light.transform;
        // }
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

        sheet.properties.SetColor("_ShadowColor", settings.shadowColor);
        sheet.properties.SetVector("_ShadowDistance", settings.shadowDistance);
        sheet.properties.SetFloat("_ShadowIntensity", settings.shadowIntensity);
        sheet.properties.SetFloat("_ShadowPenumbra", settings.shadowPenumbra);

        sheet.properties.SetFloat("_AoStepSize", settings.aoStepSize);
        sheet.properties.SetFloat("_AoIntensity", settings.aoIntensity);
        sheet.properties.SetInt("_AoIterations", settings.aoIterations);

        sheet.properties.SetVector("_sphere1", settings.sphere1);
        sheet.properties.SetVector("_sphere2", settings.sphere2);
        sheet.properties.SetFloat("_sphereIntersectSmooth", settings.sphereIntersectSmooth);

        if (directionalLight)
        {
            Vector3 position = directionalLight.transform.forward;
            Light light = directionalLight.GetComponent<Light>();
            sheet.properties.SetVector("_LightDir", new Vector4(position.x, position.y, position.z, 1));
            sheet.properties.SetColor("_LightCol", light.color);
            sheet.properties.SetFloat("_LightIntensity", light.intensity);
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
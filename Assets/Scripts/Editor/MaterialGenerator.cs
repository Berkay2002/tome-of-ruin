#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.IO;

public static class MaterialGenerator
{
    [MenuItem("Tools/Generate Materials")]
    public static void GenerateAll()
    {
        EnsureDirectory("Assets/Shaders");
        EnsureDirectory("Assets/Materials");

        CreateWhiteFlashShader();
        CreateWhiteFlashMaterial();

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("Materials generated!");
    }

    private static void CreateWhiteFlashShader()
    {
        string shaderPath = "Assets/Shaders/WhiteFlash.shader";
        if (File.Exists(shaderPath)) return;

        string shaderCode = @"Shader ""Custom/WhiteFlash""
{
    Properties
    {
        _MainTex (""Texture"", 2D) = ""white"" {}
    }
    SubShader
    {
        Tags { ""Queue""=""Transparent"" ""RenderType""=""Transparent"" ""RenderPipeline""=""UniversalPipeline"" }
        Blend SrcAlpha OneMinusSrcAlpha
        Cull Off
        ZWrite Off

        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include ""Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl""

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
                float4 color : COLOR;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                float4 color : COLOR;
            };

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                OUT.positionCS = TransformObjectToHClip(IN.positionOS.xyz);
                OUT.uv = IN.uv;
                OUT.color = IN.color;
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                half4 texColor = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, IN.uv);
                return half4(1, 1, 1, texColor.a * IN.color.a);
            }
            ENDHLSL
        }
    }
}";
        File.WriteAllText(shaderPath, shaderCode);
        AssetDatabase.ImportAsset(shaderPath);
    }

    private static void CreateWhiteFlashMaterial()
    {
        string matPath = "Assets/Materials/WhiteFlash.mat";
        if (File.Exists(matPath)) return;

        var shader = Shader.Find("Custom/WhiteFlash");
        if (shader == null)
        {
            Debug.LogError("WhiteFlash shader not found. Run Tools > Generate Materials again after compilation.");
            return;
        }

        var mat = new Material(shader);
        AssetDatabase.CreateAsset(mat, matPath);
    }

    private static void EnsureDirectory(string path)
    {
        if (!AssetDatabase.IsValidFolder(path))
        {
            string parent = System.IO.Path.GetDirectoryName(path).Replace("\\", "/");
            string folder = System.IO.Path.GetFileName(path);
            AssetDatabase.CreateFolder(parent, folder);
        }
    }
}
#endif

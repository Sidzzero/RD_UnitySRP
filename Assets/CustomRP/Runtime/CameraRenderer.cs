using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

public class CameraRenderer 
{
    const string bufferName = "Render Camera";
    static Material errorMaterial;
    static ShaderTagId unlitShaderTagId = new ShaderTagId("SRPDefaultUnlit");
    static ShaderTagId[] legacyShaderTagIds =
    {
        new ShaderTagId("Always"),
        new ShaderTagId("ForwardBase"),
        new ShaderTagId("PressPassBase"),
        new ShaderTagId("Vertex"),
        new ShaderTagId("VertexLMRGBM"),
        new ShaderTagId("VertexLM"),
    };
    private ScriptableRenderContext context;
    private Camera camera;
    private CommandBuffer buffer = new CommandBuffer { name = bufferName };

    private CullingResults cullingResults;
    public void Render(ScriptableRenderContext a_context, Camera a_camera)
    {
        context = a_context;
        camera = a_camera;

        if (Cull()==false)
        {
            return;
        }
        Setup();

        DrawVisibleGeometry();
        DrawUnsupportedGeomrety();

        Submit();
    }
   
    private void Setup()
    {
        context.SetupCameraProperties(camera);
        buffer.ClearRenderTarget(true, true, Color.clear);// If we clear and setup then it cleans in better way
      
        buffer.BeginSample(bufferName);
        ExcuteBuffer();
      
    }

    private void Submit()
    {
        buffer.EndSample(bufferName);
        ExcuteBuffer();
        context.Submit();
    }
    private void ExcuteBuffer()
    {
        context.ExecuteCommandBuffer(buffer);
        buffer.Clear();
    }

    private bool Cull()
    {
        if (camera.TryGetCullingParameters(out ScriptableCullingParameters p))
        {
            cullingResults = context.Cull(ref p) ;
            return true;
        }
        return false;
    }

    private void DrawVisibleGeometry()
    {
        //Draw opaque first
        var sortingSettings = new SortingSettings(camera)
        {
            criteria = SortingCriteria.CommonOpaque
        };//Ortho or distrance base sorting
        var drawSettings = new DrawingSettings(unlitShaderTagId, sortingSettings) { };
        var filterSettings = new FilteringSettings(RenderQueueRange.all);
        filterSettings.renderQueueRange = RenderQueueRange.opaque;
        context.DrawRenderers(cullingResults,ref drawSettings,ref filterSettings);

        context.DrawSkybox(camera);

        //Draw Transperant
        filterSettings.renderQueueRange = RenderQueueRange.transparent;
        sortingSettings.criteria = SortingCriteria.CommonTransparent;
        context.DrawRenderers(cullingResults, ref drawSettings, ref filterSettings);

    }

    private void DrawUnsupportedGeomrety()
    {
        if (errorMaterial == null)
        {
            errorMaterial = new Material(Shader.Find("Hidden/InternalErrorShader"));
        }
        var drawSettings = new DrawingSettings(legacyShaderTagIds[0], new SortingSettings(camera)) 
        { overrideMaterial = errorMaterial
        };

        for (int i=1;i<legacyShaderTagIds.Length;i++)
        {
            drawSettings.SetShaderPassName(i, legacyShaderTagIds[i]);
        }
        var filterSettings = FilteringSettings.defaultValue;
        context.DrawRenderers(cullingResults, ref drawSettings, ref filterSettings);
    }
}

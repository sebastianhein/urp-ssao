using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class SSAO : ScriptableRendererFeature
{
    [System.Serializable]
    public class SSAOSettings
    {
        public RenderPassEvent renderPassEvent = RenderPassEvent.AfterRenderingOpaques;
        public Material ssaoMaterial = null;

        [Range(0,1)] public float totalStrength = 0.5f;
        [Range(0,1)] public float brightnessCorrection = -0.37f;
        [Range(0.01f, 2)] public float area = 0.4f;
        public float falloff = 0.0001f;
        [Range(0.01f, 0.5f)] public float radius = 0.003f;
    }

    public SSAOSettings settings = new SSAOSettings();

    class CustomRenderPass : ScriptableRenderPass
    {
        public Material ssaoMaterial;
        public float totalStrength;
        public float brightnessCorrection;
        public float area;
        public float falloff;
        public float radius;

        string profilerTag;
        int tmpId1;
        RenderTargetIdentifier tmpRT1;
        
        private RenderTargetIdentifier source { get; set; }

        public void Setup(RenderTargetIdentifier source) {
            this.source = source;
        }

        public CustomRenderPass(string profilerTag)
        {
            this.profilerTag = profilerTag;
        }

        public override void Configure(CommandBuffer cmd, RenderTextureDescriptor cameraTextureDescriptor)
        {
            var width = cameraTextureDescriptor.width;
            var height = cameraTextureDescriptor.height;

            tmpId1 = Shader.PropertyToID("ssao_RT");
            cmd.GetTemporaryRT(tmpId1, width, height, 0, FilterMode.Bilinear, RenderTextureFormat.ARGB32);
            tmpRT1 = new RenderTargetIdentifier(tmpId1);            
            ConfigureTarget(tmpRT1);
        }

        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            CommandBuffer cmd = CommandBufferPool.Get(profilerTag);

            RenderTextureDescriptor opaqueDesc = renderingData.cameraData.cameraTargetDescriptor;
            opaqueDesc.depthBufferBits = 0;

            ssaoMaterial.SetFloat("_TotalStrength", totalStrength);
            ssaoMaterial.SetFloat("_Base", brightnessCorrection);
            ssaoMaterial.SetFloat("_Area", area);
            ssaoMaterial.SetFloat("_Falloff", falloff);
            ssaoMaterial.SetFloat("_Radius", radius);
            
            Blit(cmd, source, tmpRT1, ssaoMaterial, 0);
            Blit(cmd, tmpRT1, source);
   
            context.ExecuteCommandBuffer(cmd);
            CommandBufferPool.Release(cmd);
        }

        public override void FrameCleanup(CommandBuffer cmd)
        {
        }
    }

    CustomRenderPass scriptablePass;

    public override void Create()
    {
        scriptablePass = new CustomRenderPass("SSAO");
        scriptablePass.ssaoMaterial = settings.ssaoMaterial;
        scriptablePass.totalStrength = settings.totalStrength;
        scriptablePass.brightnessCorrection = settings.brightnessCorrection;
        scriptablePass.area = settings.area;
        scriptablePass.falloff = settings.falloff;
        scriptablePass.radius = settings.radius;

        scriptablePass.renderPassEvent = settings.renderPassEvent;
    }

    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        var src = renderer.cameraColorTarget;
        scriptablePass.Setup(src);
        renderer.EnqueuePass(scriptablePass);
    }
}



using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

[ReloadGroup]
public class SSAO : ScriptableRendererFeature
{
    [System.Serializable]
    public class SSAOSettings
    {
        public RenderPassEvent renderPassEvent = RenderPassEvent.AfterRenderingOpaques;

        [Range(0,2)] public float totalStrength = 1.1f;
        [Range(0,1)] public float brightnessCorrection = 0.0f;
        [Range(0.01f, 5)] public float area = 0.55f;
        public float falloff = 0.0001f;
        [Range(0.01f, 2.5f)] public float radius = 0.04f;
        public bool debug = false;
    }

    public SSAOSettings settings = new SSAOSettings();

    [ReloadGroup]
    class CustomRenderPass : ScriptableRenderPass
    {
        [Reload("Materials/ssao.mat")]
        public Material ssaoMaterial = null;
        // public Material upsampleMaterial;
        public float totalStrength;
        public float brightnessCorrection;
        public float area;
        public float falloff;
        public float radius;
        public bool debug;

        string profilerTag;
        RenderTargetIdentifier tmpRT1;
        
        private RenderTargetIdentifier source { get; set; }

        public void Setup(RenderTargetIdentifier source) {
#if UNITY_EDITOR
            if (!Application.isPlaying)
            {
                ResourceReloader.TryReloadAllNullIn(this, "Packages/com.sbstn_hn.rendererfeatures.urp-ssao");
            }
#endif 
            
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

            int tmpId = Shader.PropertyToID("ssao_RT");
            cmd.GetTemporaryRT(tmpId, width, height, 0, FilterMode.Bilinear, RenderTextureFormat.ARGB32);
            tmpRT1 = new RenderTargetIdentifier(tmpId);            
            ConfigureTarget(tmpRT1);
        }

        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            if (ssaoMaterial == null) {
                return;
            }

            // if (upsampleMaterial == null) {
            //     return;
            // }

            CommandBuffer cmd = CommandBufferPool.Get(profilerTag);

            RenderTextureDescriptor opaqueDesc = renderingData.cameraData.cameraTargetDescriptor;
            opaqueDesc.depthBufferBits = 0;

            ssaoMaterial.SetFloat("_TotalStrength", totalStrength);
            ssaoMaterial.SetFloat("_Base", brightnessCorrection);
            ssaoMaterial.SetFloat("_Area", area);
            ssaoMaterial.SetFloat("_Falloff", falloff);
            ssaoMaterial.SetFloat("_Radius", radius);
            ssaoMaterial.SetFloat("_Debug", debug?0.0f:1.0f);

            // calculate SSAO and store in a temporary texture
            Blit(cmd, source, tmpRT1, ssaoMaterial, 0);

            // blit thee result to the framebuffer
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

        scriptablePass.totalStrength = settings.totalStrength;
        scriptablePass.brightnessCorrection = settings.brightnessCorrection;
        scriptablePass.area = settings.area;
        scriptablePass.falloff = settings.falloff;
        scriptablePass.radius = settings.radius;
        scriptablePass.debug = settings.debug;

        scriptablePass.renderPassEvent = settings.renderPassEvent;
    }

    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        var src = renderer.cameraColorTarget;
        scriptablePass.Setup(src);
        renderer.EnqueuePass(scriptablePass);
    }
}



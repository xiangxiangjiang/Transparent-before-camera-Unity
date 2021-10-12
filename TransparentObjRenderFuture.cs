using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
namespace MyPostProcess
{
    //需要添加到URP的ForwardRenderer设置中，点击Add Render Future加入
    public class TransparentObjRenderFuture : ScriptableRendererFeature
    {
        class TransparentObjRenderPass : ScriptableRenderPass
        {
            static readonly string k_RenderTag = "Render Transparent Effects";
            TransparentObjPostProcess transparentObj;
            Material transparentObjMaterial;
            RenderTargetIdentifier destTarget;//目的
            RenderTargetIdentifier sourceTarget;//来源
            Texture baseTexture;
            public TransparentObjRenderPass(RenderPassEvent evt)
            {
                renderPassEvent = evt;
                Shader shader = Shader.Find("Shader Graphs/TransparentObjShader");
                if(shader==null)
                {
                    Debug.LogError("Shader Graphs/TransparentObjShader not Find");
                    return;
                }
                transparentObjMaterial = CoreUtils.CreateEngineMaterial(shader);
            }


            // This method is called before executing the render pass.
            // It can be used to configure render targets and their clear state. Also to create temporary render target textures.
            // When empty this render pass will render to the active camera render target.
            // You should never call CommandBuffer.SetRenderTarget. Instead call <c>ConfigureTarget</c> and <c>ConfigureClear</c>.
            // The render pipeline will ensure target setup and clearing happens in an performance manner.
            public override void Configure(CommandBuffer cmd, RenderTextureDescriptor cameraTextureDescriptor)
            {
                
                
            }


            //具体操作
            // Here you can implement the rendering logic.
            // Use <c>ScriptableRenderContext</c> to issue drawing commands or execute command buffers
            // https://docs.unity3d.com/ScriptReference/Rendering.ScriptableRenderContext.html
            // You don't have to call ScriptableRenderContext.submit, the render pipeline will call it at specific points in the pipeline.
            public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
            {
                if(transparentObjMaterial==null)
                {
                    Debug.LogError("transparentObjMaterial not created");
                    return;
                }
                //后期是否生效
                if (!renderingData.cameraData.postProcessEnabled) return;
                //使用 VolumeManager.instance.stack 的 GetComponent 方法来获得我们的自定义 Volume 类的实例；并获取里面的属性变量来做具体的后处理。
                var stack = VolumeManager.instance.stack;
                //获取后处理的配置参数
                transparentObj = stack.GetComponent<TransparentObjPostProcess>();
                if (transparentObj == null) return;
                if (!transparentObj.IsActive()) return;//IsActive是继承中实现的不是，面板上的toggle
                //然后从命令缓存池中获取一个 gl 命令缓存，CommandBuffer 主要用于收集一系列 gl 指令，然后之后执行。
                baseTexture = transparentObj.BaseTexture;
                
                var cmd = CommandBufferPool.Get(k_RenderTag);
                Render(cmd,ref renderingData);
                context.ExecuteCommandBuffer(cmd);//执行
                CommandBufferPool.Release(cmd);//释放


                //渲染设置
                void Render(CommandBuffer cmdBuffer,ref RenderingData renderingdata)
                {
                    ref var cameraData = ref renderingdata.cameraData;

                    //原始的图相机的图的ID
                    var source = sourceTarget;//原始相机的图
                    //获取图像长宽
                    var w = cameraData.camera.scaledPixelWidth;
                    var h = cameraData.camera.scaledPixelHeight;

                    //设置shader参数                    
                    transparentObjMaterial.SetFloat("_MinDistance", transparentObj.MinDistance);//对应的是Shader Graph中的Reference参数
                    transparentObjMaterial.SetFloat("_LerpRange", transparentObj.LerpRange);                    
                    transparentObjMaterial.SetFloat("_Distance", transparentObj.Distance);
                    transparentObjMaterial.SetFloat("_Power", transparentObj.Power);
                    transparentObjMaterial.SetTexture("_BaseTex",baseTexture);
                    
                    //创建一个临时变量保存当前画面
                    int TempTargetId = Shader.PropertyToID("_TempTargetZoomBlur");
                    cmd.GetTemporaryRT(TempTargetId, w, h, 0, FilterMode.Point, RenderTextureFormat.Default);                    
                    int shaderPass = 0;
                    cmdBuffer.Blit(source, TempTargetId);//将当前拷贝到临时目标
                    //Blit source会默认付给_MainTex
                    cmdBuffer.Blit(TempTargetId, destTarget,transparentObjMaterial, shaderPass);//将结果渲染到source
                    //释放
                    cmdBuffer.ReleaseTemporaryRT(TempTargetId);
                }
            }

            /// Cleanup any allocated resources that were created during the execution of this render pass.
            public override void FrameCleanup(CommandBuffer cmd)
            {
            }
            //传递数据
            public void SetUp(in RenderTargetIdentifier sourceTarget, in RenderTargetIdentifier destTarget)
            {
                this.sourceTarget = sourceTarget;
                this.destTarget = destTarget;
            }

        }

        TransparentObjRenderPass m_ScriptablePass;
        RenderTargetHandle afterPostProcessTexture;// 用于after PostProcess的render target
        public override void Create()
        {
            //会导致Bloom失效，是因为Bloom的强度要大于1，这个后处理之后强度在0-1之间了，导致无法bloom
            m_ScriptablePass = new TransparentObjRenderPass(RenderPassEvent.AfterRendering);
            // 初始化用于after PostProcess的render target,源不是renderer.cameraColorTarget
            afterPostProcessTexture.Init("_AfterPostProcessTexture");
        }

        // Here you can inject one or multiple render passes in the renderer.
        // This method is called when setting up the renderer once per-camera.
        public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
        {
            //renderer.cameraDepth;//相机深度,可用于鹰眼等效果
            //传入相机渲染结果图
            //afterPostProcessTexture.Init("_AfterPostProcessTexture");
            // 为每个render pass设置render target
            //var source = renderer.cameraColorTarget;
            //https://www.jianshu.com/p/b9cd6bb4c4aa?ivk_sa=1024320u 渲染目标分为后处理前后处理后            
            //子应用在主相机
            if(renderingData.cameraData.camera!=Camera.main)
            {
                return;
            }
            var source = afterPostProcessTexture;            
            m_ScriptablePass.SetUp(source.Identifier(),source.Identifier());
            renderer.EnqueuePass(m_ScriptablePass);
        }
    }
}





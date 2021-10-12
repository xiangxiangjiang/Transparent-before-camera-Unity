using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
namespace MyPostProcess
{    
    //性能很拉，因为是渲染2遍图，所以帧数会减半
    //需要给相机设置一个子相机，并且相机的参数设置和主相机相同，并且不渲染透明层   


    //还有一种实现方案，使用深度贴图，将需要透明和不需要透明的物体用相机分开渲染一次，最后结合深度贴图混合，关键点需要设置渲染图层以及恢复。不需要透明的物体深度值大于等于透明物体的深度用不透明的颜色，否则使用混合色。这里太麻烦不想做了，后面有时间实现。

    [DefaultExecutionOrder(int.MaxValue)]
    public class SetTransparentObjParam : MonoBehaviour
    {
        [SerializeField]
        private string transparentLayerName;//will not be rendered
        [SerializeField]
        private LayerMask ObstacleObjLayerMask;// what need to be fade
        [SerializeField]
        private List<string> ignoreTag = new List<string>();//wont be fade
        public float checkBoxSize = 0.2f;
        [Range(0, 100)]        
        public float fromCameraDistance = 3f;
        public float minDistance = 1f;
        public float power = 2f;


        Camera baseCamera;
        RenderTexture tex;
        
        // Start is called before the first frame updates
        TransparentObjPostProcess transparentObj;
        HashSet<Transform> settedobs = new HashSet<Transform>();
        Dictionary<Renderer, int> storeLayer = new Dictionary<Renderer, int>();
        // Update is called once per frame
        void LateUpdate()
        {
            if (baseCamera == null)
            {
                var obj = new GameObject();
                baseCamera = obj.AddComponent<Camera>();
                //拷贝数据
                baseCamera.transform.parent = Camera.main.transform;
                baseCamera.transform.localPosition = Vector3.zero;
                baseCamera.transform.localRotation = Quaternion.identity;
                baseCamera.transform.localScale = Vector3.one;
                baseCamera.enabled = false;
                baseCamera.nearClipPlane = Camera.main.nearClipPlane;
                baseCamera.farClipPlane = Camera.main.farClipPlane;
                baseCamera.fieldOfView = Camera.main.fieldOfView;
                baseCamera.cullingMask = Camera.main.cullingMask & (~LayerMask.GetMask(transparentLayerName));               
                var data = baseCamera.GetUniversalAdditionalCameraData();
                var mainData = Camera.main.GetUniversalAdditionalCameraData();
                data.renderPostProcessing = mainData.renderPostProcessing;
                data.renderShadows = mainData.renderShadows;
                data.renderType = mainData.renderType;
                data.requiresColorOption = mainData.requiresColorOption;
                data.requiresColorTexture = data.requiresDepthTexture;
                data.runInEditMode = data.runInEditMode;                

                baseCamera.gameObject.hideFlags = HideFlags.HideAndDontSave;
            }
            if (tex == null)
            {
                //必须使用UnityEngine.Experimental.Rendering.GraphicsFormat.R16G16B16A16_SFloat渲染才会有bloom
                tex = new RenderTexture(baseCamera.scaledPixelWidth, baseCamera.scaledPixelHeight,4,UnityEngine.Experimental.Rendering.GraphicsFormat.R16G16B16A16_SFloat);
                baseCamera.targetTexture = tex;
            }
            //准备接收的贴图
            if (tex == null)
            {
                tex = new RenderTexture(new RenderTextureDescriptor(baseCamera.scaledPixelWidth, baseCamera.scaledPixelHeight));
                baseCamera.targetTexture = tex;
            }
            if (transparentObj == null)
            {
                var stack = VolumeManager.instance.stack;
                transparentObj = stack.GetComponent<TransparentObjPostProcess>();
            }           
            //检测遮挡物
            var obs = Physics.OverlapBox(baseCamera.transform.position+baseCamera.transform.forward*fromCameraDistance/2,new Vector3(checkBoxSize, checkBoxSize, fromCameraDistance/2),baseCamera.transform.rotation,ObstacleObjLayerMask);
            //如果没有遮挡物，则可以不用这个后处理，也没必要进行后面的
            if(obs.Length==0)
            {
                //没有障碍物                
                transparentObj.Distance = float.MaxValue;
                return;
            }
            settedobs.Clear();
            storeLayer.Clear();
            //设置遮挡物的layer
            foreach (var c in obs)
            {
                if(!settedobs.Contains(c.transform)&&!ignoreTag.Contains(c.transform.tag))
                {
                    settedobs.Add(c.transform);
                    //获取该物体下所有的渲染层，并将layer设置为透明
                    var renders = c.transform.GetComponentsInChildren<Renderer>();
                    foreach(var r in renders)
                    {
                        storeLayer.Add(r, r.gameObject.layer);
                        r.gameObject.layer = LayerMask.NameToLayer(transparentLayerName);
                    }
                }
            }                        
            //渲染
            baseCamera.Render();
                        
            //复原遮挡物layer
            foreach (var k in storeLayer.Keys)
            {
                k.gameObject.layer = storeLayer[k];
            }
            
            //设置后处理的配置参数
            if (Physics.BoxCast(baseCamera.transform.position,new Vector3(checkBoxSize, checkBoxSize, 0.1f),baseCamera.transform.forward, out RaycastHit hit, baseCamera.transform.rotation,fromCameraDistance,ObstacleObjLayerMask))
            {
                transparentObj.Distance = hit.distance;
                transparentObj.LerpRange = fromCameraDistance - minDistance;
                transparentObj.Power = power;
                transparentObj.MinDistance = minDistance;
            }
            else
            {
                transparentObj.Distance = float.MaxValue;
            }
            if (transparentObj == null) return;
            if (!transparentObj.IsActive()) return;//IsActive是继承中实现的不是，面板上的toggle
                                                   //然后从命令缓存池中获取一个 gl 命令缓存，CommandBuffer 主要用于收集一系列 gl 指令，然后之后执行。
            transparentObj.BaseTexture = tex;
            
        }
    }
}


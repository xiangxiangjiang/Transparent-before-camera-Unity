using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace MyPostProcess
{
    public class TransparentObjPostProcess : VolumeComponent, IPostProcessComponent
    {
        /// <summary>
        /// 必须透明的距离
        /// </summary>
        private float minDistance = (1f);
        public float MinDistance
        {
            get => minDistance;
            set
            {
                if(value<0)
                {
                    value = 0;
                }
                minDistance = value;
            }
        }
        /// <summary>
        /// 半透明范围
        /// </summary>
        private float lerpRange = (4f);
        public float LerpRange
        {
            get => lerpRange;
            set
            {
                if (value <= 0)
                {
                    value = 0.0001f;
                }
                lerpRange = value;
            }
        }
        /// <summary>
        /// 到摄像头距离
        /// </summary>
        private float distance = float.MaxValue;
        public float Distance
        {
            get {
                if(distance<minDistance)
                {
                    distance = minDistance;
                }
                return distance;
            }
            set
            {
                if (value < 0)
                {
                    value = 0;
                }
                distance = value;
            }
        }
        /// <summary>
        /// 透明指数
        /// </summary>
        private float power = (2f);
        public float Power
        {
            get => power;
            set
            {
                if (value < 0)
                {
                    value = 0;
                }
                power = value;
            }
        }

        private Texture baseTexture=null;
        public Texture BaseTexture
        {
            get
            {
                if(baseTexture==null)
                {
                    baseTexture = Texture2D.whiteTexture;
                }
                return baseTexture;
            }
            set
            {
                if(value==null)
                {
                    value = Texture2D.whiteTexture;
                }
                baseTexture = value;
            }
        }

        public bool IsActive()
        {
            return distance<(minDistance+lerpRange);
        }

        public bool IsTileCompatible()
        {
            return false;
        }
    }
}


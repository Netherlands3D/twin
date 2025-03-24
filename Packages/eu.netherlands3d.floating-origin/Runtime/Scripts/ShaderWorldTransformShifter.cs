using Netherlands3D.Coordinates;
using UnityEngine;

namespace Netherlands3D.Twin.FloatingOrigin
{
    public class ShaderWorldTransformShifter : WorldTransformShifter
    {
        [SerializeField] [Tooltip("Global shader variable used in shaders/shadergraphs")] private string shaderKeyWord = "_WorldOriginOffset";
        [SerializeField]  private Vector2 shaderOffset = Vector2.zero;

        private Coordinate ShaderOrigin;

        private void Start()
        {
            ShaderOrigin = new Coordinate(new Vector3(-shaderOffset.x,0,-shaderOffset.y));
        }
        public override void PrepareToShift(WorldTransform worldTransform, Coordinate fromOrigin, Coordinate toOrigin)
        {
        }

        public override void ShiftTo(WorldTransform worldTransform, Coordinate fromOrigin, Coordinate toOrigin)
        {
            Vector3 ShaderOriginInUnity = ShaderOrigin.ToUnity();
            shaderOffset.x = -ShaderOriginInUnity.x;
            shaderOffset.y = -ShaderOriginInUnity.z;


            UpdateShaders();
        }

        private void OnValidate() {
            UpdateShaders();
        }

        private void UpdateShaders()
        {
            Shader.SetGlobalVector(shaderKeyWord, shaderOffset);
        }
    }
}

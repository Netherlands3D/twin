/*
*  Copyright (C) X Gemeente
*                X Amsterdam
*                X Economic Services Departments
*
*  Licensed under the EUPL, Version 1.2 or later (the "License");
*  You may not use this work except in compliance with the License.
*  You may obtain a copy of the License at:
*
*    https://github.com/Amsterdam/Netherlands3D/blob/main/LICENSE.txt
*
*  Unless required by applicable law or agreed to in writing, software
*  distributed under the License is distributed on an "AS IS" basis,
*  WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or
*  implied. See the License for the specific language governing
*  permissions and limitations under the License.
*/

using System;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace Netherlands3D.Functionalities.Wms
{
    public class TextureDecalProjector : TextureProjectorBase
    {
        [SerializeField] private DecalProjector projector;

        /// <summary>
        /// Convenience method to project a texture and set the projection parameters.
        /// </summary>
        /// <param name="tex">The Texture to project</param>
        /// <param name="size">The diameter of the projection in the width and depth axis'</param>
        /// <param name="height">The height of the projection, this should be high enough to intersect with a mesh</param>
        /// <param name="renderIndex">How to influence rendering order to ensure layering is done correctly, -1 for default</param>
        /// <param name="minDepth">The minimum rendering depth to prevent z-fighting, usually 110% of height</param>
        /// <param name="isEnabled">Toggle to immediately enable this projector, defaults to true</param>
        public override void Project(Texture2D tex, int size, float height, int renderIndex, float minDepth, bool isEnabled = true)
        {
            base.Project(tex, size, height, renderIndex, minDepth, isEnabled);

            //force the depth to be at least larger than its height to prevent z-fighting
            if (height >= projector.size.z)
            {
                SetSize(projector.size.x, projector.size.y, minDepth);
            }

            //set the render index, to make sure the render order is maintained
            SetPriority(renderIndex);
        }

        public override void SetSize(Vector3 size)
        {
            projector.size = size;
        }

        public override void SetTexture(Texture2D texture)
        {
            base.SetTexture(texture);
            if (!materialInstance)
            {
                materialInstance = new Material(projector.material);
                projector.material = materialInstance;
            }

            materialInstance.mainTexture = texture;
        }

        public void SetPriority(int priority)
        {
            projector.material.SetInt("_DrawOrder", priority);
        }
    }
}

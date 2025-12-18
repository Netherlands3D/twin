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
        [SerializeField] private bool forcePointingDown;

        private void Update()
        {
            // [S3DA-1904] This is a quick-fix/workaround because the origin shifting (correctly) aligns (thus rotates) a shifted object
            // to align with the surface vector of the CRS (some CRS are spheres and not planes). The better fix would be to
            // move this projector into a tile body, but this costs a bigger refactor in ImageProjectionLayer and WMSTileDataLayer
            // because there are multiple assumptions that the tile gameobject has this component 
            if (forcePointingDown)
                transform.rotation = Quaternion.Euler(90, 0, 0);
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

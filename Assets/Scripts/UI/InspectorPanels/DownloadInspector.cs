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
using System.Collections;
using System.Collections.Generic;
using Netherlands3D.GeoJSON;
using Netherlands3D.SubObjects;
using Netherlands3D.Twin;
using TMPro;
using UnityEngine;

using UnityEngine.InputSystem;



namespace Netherlands3D.Twin.Interface.BAG
{
	public class DownloadInspector : MonoBehaviour
	{
		[SerializeField] private AreaSelection areaSelection;
		[SerializeField] private RenderedThumbnail renderedThumbnail;

		private void OnEnable() {
			areaSelection.OnSelectionAreaBoundsChanged.AddListener(OnSelectionBoundsChanged);
		}	

		private void OnDisable() {
			areaSelection.OnSelectionAreaBoundsChanged.RemoveListener(OnSelectionBoundsChanged);
		}

		private void OnSelectionBoundsChanged(Bounds selectedArea)
		{
			renderedThumbnail.RenderThumbnail(selectedArea);
		}
    }
}
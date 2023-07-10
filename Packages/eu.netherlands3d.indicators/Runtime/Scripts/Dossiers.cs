using System;
using UnityEngine;

namespace Netherlands3D.Twin.Indicators
{
    public class Dossiers : MonoBehaviour
    {
        [Tooltip("Contains the URI Template where to find the dossier's JSON file. The dossier id can be inserted using {id} (without spaces).")]
        public string dossierUriTemplate = "";

        public void Open(string dossierId)
        {
            string dossierUri = AssembleUri(dossierId);
            Debug.Log($"<color=green>Loading dossier with id {dossierId} from {dossierUri}</color>");
        }

        private string AssembleUri(string dossierId)
        {
            return dossierUriTemplate.Replace("{id}", dossierId);
        }
    }
}
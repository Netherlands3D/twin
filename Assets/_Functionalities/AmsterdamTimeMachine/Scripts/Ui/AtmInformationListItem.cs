using KindMen.Uxios.Api;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Netherlands3D.Twin
{
    public class AtmInformationListItem : MonoBehaviour
    {
        private AtmInformationList.AtmInfo atmInfo;
        
        [SerializeField] private Texture2D thumbnailPlaceholder;
        [SerializeField] private TMP_Text titleField;
        [SerializeField] private TMP_Text subTextField;
        [SerializeField] private RawImage photoField;

        public void SetAtmInfo(AtmInformationList.AtmInfo atmInfo)
        {
            this.atmInfo = atmInfo;
            gameObject.name = this.atmInfo.title;

            SetTitle();
            SetSubText();
            LoadThumbnail();
        }

        private void SetTitle()
        {
            if (!titleField) return;

            titleField.text = atmInfo.title;
        }

        private void SetSubText()
        {
            if (!subTextField) return;

            subTextField.text = atmInfo.textDate;
        }

        private void SetThumbnail(Texture2D texture)
        {
            if (!photoField) return;

            photoField.texture = texture;
        }

        private void LoadThumbnail()
        {
            // Re-set thumbnail to the placeholder image
            SetThumbnail(thumbnailPlaceholder);
            
            // No photo field set - no thumbnail can be shown
            if (!photoField) return;
            
            // No thumbnail present in the atm information - no thumbnail can be shown
            if (string.IsNullOrEmpty(atmInfo.thumbnail)) return;
            
            // Get it!
            var resource = Resource<Texture2D>.At(atmInfo.thumbnail);
            resource.Value.Then(SetThumbnail);
        }

        public void Open()
        {
            Application.OpenURL(atmInfo.url);
        }
    }
}

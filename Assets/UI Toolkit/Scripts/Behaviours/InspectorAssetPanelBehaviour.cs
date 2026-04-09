using Netherlands3D.Credentials;
using Netherlands3D.Events;
using Netherlands3D.UI.Panels;
using UnityEngine;
using UnityEngine.UIElements;

namespace Netherlands3D.UI.Behaviours
{
    [RequireComponent(typeof(UIDocument), typeof(ICredentialHandler))]
    public class InspectorAssetPanelBehaviour : MonoBehaviour
    {
        private UIDocument appDocument;
        [SerializeField] private TriggerEvent uploadFileEvent;

        private VisualElement root;
        private VisualElement Root => root ??= appDocument?.rootVisualElement;
        
        private ImportAssetPanel panel;
        private ImportAssetPanel Panel => panel ??= Root?.Q<ImportAssetPanel>();

        private ICredentialHandler credentialHandler;

        private void Awake()
        {
            appDocument = GetComponent<UIDocument>();
            credentialHandler = GetComponent<ICredentialHandler>();
        }

        private void Start()
        {
            Panel.FileUploadStarted += OnUploadStarted;
            
            Panel.importSucceeded.AddListener(OnImportSucceeded);
            Panel.importFailed.AddListener(OnImportFailed);
            
            Panel.SetCredentialHandler(credentialHandler);
        }

        private void OnDestroy()
        {
            Panel.FileUploadStarted -= OnUploadStarted;
            
            Panel.importSucceeded.RemoveListener(OnImportSucceeded);
            Panel.importFailed.RemoveListener(OnImportFailed);
        }

        private void OnUploadStarted(ClickEvent evt)
        {
            uploadFileEvent.InvokeStarted(); //todo: remove scriptable event
        }
        
        private void OnImportSucceeded()
        {
            Panel.Hide();
        }

        private void OnImportFailed()
        {
           Debug.LogError("Failed to create a layer");
        }
    }
}

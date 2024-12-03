using System;
using System.Collections.Generic;
using KindMen.Uxios;
using KindMen.Uxios.Api;
using KindMen.Uxios.Errors.Connection;
using RSG;
using UnityEngine;
using QueryParameters = KindMen.Uxios.Http.QueryParameters;

namespace Netherlands3D.Twin
{
    public class AtmInformationList : MonoBehaviour
    {
        public class AtmInfo
        {
            public string url;
            public string title;
            public string startDate;
            public string endDate;
            public string textDate;
            public string thumbnail;

            public override string ToString()
            {
                return $"{{url: {url}, title: {title}, startDate: {startDate}, endDate: {endDate}, textDate: {textDate}, thumbnail: {thumbnail}}}";
            }
        }

        [SerializeField] private GameObject contentArea;
        [SerializeField] private AtmInformationListItem photoListItem;
        [SerializeField] private AtmInformationListItem regularListItem;
        private const string bagUriTemplate = "https://api.lod.uba.uva.nl/queries/ATM/images-building-nl3d/2/run?bag=http://bag.basisregistraties.overheid.nl/bag/id/pand/{id}";
        private const string adamLinkTemplate = "https://api.lod.uba.uva.nl/queries/ATM/images-historical-address-nl3d/2/run?address={id}";
        private Promise<List<AtmInfo>> activePromise;

        public void LoadFromBagId(string bagId)
        {
            QueryParameters parameters = new() { { "id", bagId } };

            PopulateList(new TemplatedUri(bagUriTemplate, parameters));
        }

        public void LoadFromFeature(FeatureMapping featureMapping)
        {
            var adamLinkId = featureMapping.Feature.Properties["id"]?.ToString();

            // check for an id field, otherwise it is not an Amsterdam Time Machine feature
            if (string.IsNullOrEmpty(adamLinkId)) return;

            // check if it is a valid Amsterdam Time Machine feature, otherwise we ignore it 
            if (adamLinkId.StartsWith("https://adamlink.nl/geo/address/") == false) return;

            QueryParameters parameters = new() { { "id", adamLinkId } };

            PopulateList(new TemplatedUri(adamLinkTemplate, parameters));
        }

        private void PopulateList(Uri url)
        {
            if (activePromise != null)
                Uxios.DefaultInstance.Abort(activePromise);
            
            contentArea.transform.ClearAllChildren();

            activePromise = Resource<List<AtmInfo>>.At(url).Value;
            activePromise.Then(CreateListItems);
            activePromise.Catch(OnErrorDuringLoading);
        }

        private void OnErrorDuringLoading(Exception exception)
        {
            var error = exception as Error;
            
            if(error is RequestAbortedError)
                return;

            Debug.LogError(
                $"An issue occurred while loading data from ATM ({error?.Response.Request.Url}):\n{exception.Message}"
            );
        }

        private void CreateListItems(List<AtmInfo> atmInformationObjects)
        {
            foreach (var atmInfo in atmInformationObjects)
            {
                CreateListItem(atmInfo);
            }
        }

        private void CreateListItem(AtmInfo atmInfo)
        {
            var listItemPrefab = string.IsNullOrEmpty(atmInfo.thumbnail) ? regularListItem : photoListItem;
            var listItem = Instantiate(listItemPrefab, contentArea.transform);
            listItem.SetAtmInfo(atmInfo);
        }
    }
}
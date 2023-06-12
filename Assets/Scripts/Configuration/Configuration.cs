using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Text;
using Netherlands3D.Core;
using Netherlands3D.Twin.Features;
using UnityEngine;
using UnityEngine.Events;

namespace Netherlands3D.Twin.Configuration
{
    [CreateAssetMenu(menuName = "Netherlands3D/Twin/Configuration", fileName = "Configuration", order = 0)]
    public class Configuration : ScriptableObject
    {
        [SerializeField]
        public string title = "Amersfoort";
        [SerializeField]
        private Vector3RD origin = new(161088, 503050, 300);
        public List<Feature> Features = new();

        public string Title
        {
            get => title;
            set
            {
                title = value;
                OnTitleChanged.Invoke(value);
            }
        }
        
        public Vector3RD Origin
        {
            get => origin;
            set
            {
                origin = value;
                OnOriginChanged.Invoke(value);
            }
        }

        public UnityEvent<Vector3RD> OnOriginChanged = new();
        public UnityEvent<string> OnTitleChanged = new();

        public bool LoadFromUrl(string url)
        {
            if (url == "") return false;

            var queryParameters = new NameValueCollection();
            ParseQueryString(new Uri(url).Query, queryParameters);
            if (UrlContainsConfiguration(queryParameters) == false)
            {
                return false;
            }

            LoadOriginFromString(queryParameters.Get("origin"));
            LoadFeaturesFromString(queryParameters.Get("features"));

            return true;
        }

        private bool UrlContainsConfiguration(NameValueCollection queryParameters) 
        {
            string origin = queryParameters.Get("origin");
            string features = queryParameters.Get("features");
            
            return origin != null && features != null;
        }

        private void LoadOriginFromString(string origin)
        {
            var originParts = origin.Split(',');
            int.TryParse(originParts[0].Trim(), out int x);
            int.TryParse(originParts[1].Trim(), out int y);
            int.TryParse(originParts[2].Trim(), out int z);

            Origin = new Vector3RD(x, y, z);
        }

        private void LoadFeaturesFromString(string features)
        {
            var featureIdentifiers = features.ToLower().Split(',');
            foreach (var feature in Features)
            {
                feature.IsEnabled = featureIdentifiers.Contains(feature.Id);
            }
        }

        public string GenerateQueryString()
        {
            string url = "?";
            url += $"origin={Origin.x},{origin.y},{origin.z}";
            url += "&features=";
            url += String.Join(',', Features.Where(feature => feature.IsEnabled).Select(feature => feature.Id).ToArray());

            return url;
        }

        /// <see href="https://gist.github.com/ranqn/d966423305ce70cbc320f319d9485fa2" />
        private void ParseQueryString(string query, NameValueCollection result, Encoding encoding = null)
        {
            encoding ??= Encoding.UTF8;

            if (query.Length == 0) return;

            var decodedLength = query.Length;
            var namePos = 0;
            var first = true;

            while (namePos <= decodedLength)
            {
                int valuePos = -1, valueEnd = -1;
                for (var q = namePos; q < decodedLength; q++)
                {
                    if ((valuePos == -1) && (query[q] == '='))
                    {
                        valuePos = q + 1;
                        continue;
                    }

                    if (query[q] != '&') continue;
                    
                    valueEnd = q;
                    break;
                }

                if (first)
                {
                    first = false;
                    if (query[namePos] == '?')
                        namePos++;
                }

                string name;
                if (valuePos == -1)
                {
                    name = null;
                    valuePos = namePos;
                }
                else
                {
                    name = WWW.UnEscapeURL(query.Substring(namePos, valuePos - namePos - 1), encoding);
                }

                if (valueEnd < 0)
                {
                    namePos = -1;
                    valueEnd = query.Length;
                }
                else
                {
                    namePos = valueEnd + 1;
                }

                var value = WWW.UnEscapeURL(query.Substring(valuePos, valueEnd - valuePos), encoding);

                result.Add(name, value);
                if (namePos == -1)
                    break;
            }
        }
    }
}
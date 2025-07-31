using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using GeoJSON.Net;
using KindMen.Uxios.Api;
using Netherlands3D.OgcApi.ExtensionMethods;
using Newtonsoft.Json;

namespace Netherlands3D.OgcApi.Features
{
    /// <summary>
    /// Defines the FeatureCollection type.
    /// </summary>
    /// <remarks>
    /// This class is a Clone of the GeoJSON.net's FeatureCollection class. For OGC API, we need additional
    /// properties, so we cannot use the GeoJSON.net's Feature class as it is not extendible.
    ///
    /// Original is copyright of Joerg Battermann 2014, Matt Hunt 2017 
    /// </remarks>
    public class FeatureCollection : GeoJSONObject, IEqualityComparer<FeatureCollection>, IEquatable<FeatureCollection>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="FeatureCollection" /> class.
        /// </summary>
        public FeatureCollection() : this(new List<Feature>())
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="FeatureCollection" /> class.
        /// </summary>
        /// <param name="features">The features.</param>
        public FeatureCollection(List<Feature> features)
        {
            if (features == null)
            {
                throw new ArgumentNullException(nameof(features));
            }

            Features = features;
        }

        public override GeoJSONObjectType Type => GeoJSONObjectType.FeatureCollection;

        /// <summary>
        /// Gets the features.
        /// </summary>
        /// <value>The features.</value>
        [JsonProperty(PropertyName = "features", Required = Required.Always)]
        public List<Feature> Features { get; private set; }

        #region OGC API Features Extension
        public DateTime? TimeStamp { get; set; } = null;

        public long NumberMatched { get; set; } = 0;

        public long NumberReturned { get; set; } = 0;

        public Link[] Links { get; set; }

        public bool First()
        {
            return Links.FirstBy(RelationTypes.prev) == null;
        }
        
        public bool Last()
        {
            return Links.FirstBy(RelationTypes.next) == null;
        }
        
        public async Task<FeatureCollection> Previous()
        {
            var link = Links.FirstBy(RelationTypes.prev)?.Href;
            if (link == null) return null;

            return await new Resource<FeatureCollection>(new Uri(link)).Value;
        }

        public async Task<FeatureCollection> Next()
        {
            var link = Links.FirstBy(RelationTypes.next)?.Href;
            if (link == null) return null;

            return await new Resource<FeatureCollection>(new Uri(link)).Value;
        }
        #endregion
        
        #region IEqualityComparer, IEquatable

        /// <summary>
        /// Determines whether the specified object is equal to the current object
        /// </summary>
        public override bool Equals(object obj)
        {
            return Equals(this, obj as FeatureCollection);
        }

        /// <summary>
        /// Determines whether the specified object is equal to the current object
        /// </summary>
        public bool Equals(FeatureCollection other)
        {
            return Equals(this, other);
        }

        /// <summary>
        /// Determines whether the specified object instances are considered equal
        /// </summary>
        public bool Equals(FeatureCollection left, FeatureCollection right)
        {
            if (base.Equals(left, right))
            {
                return left.Features.SequenceEqual(right.Features);
            }
            return false;
        }

        /// <summary>
        /// Determines whether the specified object instances are considered equal
        /// </summary>
        public static bool operator ==(FeatureCollection left, FeatureCollection right)
        {
            if (ReferenceEquals(left, right))
            {
                return true;
            }
            if (ReferenceEquals(null, right))
            {
                return false;
            }
            return left != null && left.Equals(right);
        }

        /// <summary>
        /// Determines whether the specified object instances are not considered equal
        /// </summary>
        public static bool operator !=(FeatureCollection left, FeatureCollection right)
        {
            return !(left == right);
        }

        /// <summary>
        /// Returns the hash code for this instance
        /// </summary>
        public override int GetHashCode()
        {
            int hash = base.GetHashCode();
            foreach (var feature in Features)
            {
                hash = (hash * 397) ^ feature.GetHashCode();
            }
            return hash;
        }

        /// <summary>
        /// Returns the hash code for the specified object
        /// </summary>
        public int GetHashCode(FeatureCollection other)
        {
            return other.GetHashCode();
        }

        #endregion
    }
}
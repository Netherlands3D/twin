using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;

namespace Netherlands3D.OgcApi.ExtensionMethods
{
    public static class LinkExtensionMethods
    {
        /// <summary>
        /// Filters a sequence of Links by the given relation types and media types.
        /// </summary>
        public static IEnumerable<Link> FilterBy(this IEnumerable<Link> links, string[] relation)
        {
            return links.Where(link => link.IsTypeOfRelation(relation));
        }

        public static IEnumerable<Link> FilterBy(
            this IEnumerable<Link> links,
            string[] relation,
            string[] mediaTypes)
        {
            return FilterBy(links, relation).Where(link => link.IsOfMediaType(mediaTypes));
        }

        /// <summary>
        /// Finds the first matching link (or null).
        /// </summary>
        [CanBeNull]
        public static Link FirstBy(
            this IEnumerable<Link> links,
            string[] relation,
            string[] mediaTypes)
        {
            return links.FilterBy(relation, mediaTypes).FirstOrDefault();
        }

        [CanBeNull]
        public static Link FirstBy(this IEnumerable<Link> links, string[] relation)
        {
            return links.FilterBy(relation).FirstOrDefault();
        }
    }
}
using System.Collections.Generic;
using Netherlands3D.LayerStyles;

namespace Netherlands3D.Twin.Layers
{
    /// <summary>
    /// Interface to indicate that this object will apply styling to its visualisations.
    ///
    /// Visualisations can, and should, support styling through the LayerStyles system, which is inspired by the OGC
    /// Cartography Symbology specification to define and apply specific types of styling in a CSS like manner:
    /// https://docs.ogc.org/DRAFTS/18-067r4.html
    ///
    /// See https://netherlands3d.eu/docs/developers/decisions/20241009-styling-of-layers/ for the ADR discussing
    /// styles.
    ///
    /// TODO: Write documentation and add that to this interface
    /// </summary>
    public interface ISupportsStyling
    {
        /// <summary>
        /// Convenience property to acquire all associated styles, this is usually a proxy to LayerData's Styles
        /// property.
        /// </summary>
        protected Dictionary<string, LayerStyle> Styles { get; }

        /// <summary>
        /// Visualisations produced by this may apply styling from all Styling rules associated with this object.
        ///
        /// This method may be called during the initialization phase, or whenever styling is changed. At which point
        /// the function should redraw any affected parts.
        /// </summary>
        public void ApplyStyling();
    }
}
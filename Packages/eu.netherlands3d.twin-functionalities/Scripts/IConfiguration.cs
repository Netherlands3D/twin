using System.Collections.Generic;

namespace Netherlands3D.Twin.Functionalities
{
    public interface IConfiguration : ISimpleJsonMapper, IQueryStringMapper
    {
        /// <summary>
        /// Functionalities cannot be enabled (or will actively be disabled) if this method returns
        /// one or more error messages. The returned messaged could be presented to the user, so they
        /// should be in Dutch and written in a user-friendly manner.
        /// </summary>
        /// <returns></returns>
        public List<string> Validate();
    }
}
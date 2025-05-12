using System.Collections;
using Netherlands3D.Tilekit.Changes;

namespace Netherlands3D.Tilekit
{
    public interface IChangeScheduler
    {
        void Schedule(Change change);
        IEnumerator Apply();
    }
}
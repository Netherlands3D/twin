using System.Collections;
using Netherlands3D.Tilekit.Changes;

namespace Netherlands3D.Tilekit
{
    public interface IChangeScheduler
    {
        void Schedule(ITileSetProvider tileSetProvider, Change change);
        IEnumerator Apply();
    }
}
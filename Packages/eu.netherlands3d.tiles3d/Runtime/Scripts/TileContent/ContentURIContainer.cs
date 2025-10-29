using UnityEngine;
using UnityEngine.UIElements;
using System.IO;
namespace Netherlands3D.Tiles3D
{
    [System.Serializable]
    public abstract class ContentURIContainer
    {
        public abstract void setup(int tilesetID);
        public abstract string getContentURIString(uint tileID);
        public abstract uint setContentURI(string contentURIString);

        public abstract void clear();
    }

    public class OnetilePerFile : ContentURIContainer
    {
        string saveFolder;
        uint tilecounter = 0;
        public override void setup(int tilesetID)
        {
            saveFolder = Path.Combine(Application.persistentDataPath, tilesetID.ToString());
            if (Directory.Exists(saveFolder))
            {
                Directory.Delete(saveFolder, true);
            }
            Directory.CreateDirectory(saveFolder);
        }
        public override void clear()
        {
            if (string.IsNullOrEmpty(saveFolder)) return;
            if (Directory.Exists(saveFolder))
            {
                Directory.Delete(saveFolder, true);
            }
        }
        public override uint setContentURI(string contentURIString)
        {
            tilecounter++;
            File.WriteAllText(Path.Combine(saveFolder, tilecounter.ToString()), contentURIString);
            return tilecounter;
        }
        public override string getContentURIString(uint tileID)
        {
            if (File.Exists(Path.Combine(saveFolder, tileID.ToString())))
            {
                return File.ReadAllText(Path.Combine(saveFolder, tileID.ToString()));
            }
            return "";
        }




    }
}
using System.Collections.Generic;
using Netherlands3D.Tilekit.TileSets;
using UnityEngine;
using UnityEngine.Serialization;

namespace Netherlands3D.Tilekit
{
    public class TilingManager : MonoBehaviour
    {
        // Constant used to signal the UpdateInterval that it needs to trigger each frame.
        public const int UpdateEveryFrame = 0;

        [Tooltip("Whether to immediately start the tile system, or wait until 'Resume' is called")]
        [SerializeField] private bool autoStart = true;
        [Tooltip("Update interval in ms; or 'UpdateEveryFrame' (0) for every frame")]
        [SerializeField] private int updateInterval = 200;

        public bool AutoStart
        {
            get => autoStart;
            set => autoStart = value;
        }

        public int UpdateInterval
        {
            get => updateInterval;
            set => updateInterval = value;
        }

        public bool IsPaused { get; private set; } = false;
        
        private TileSet tileSet;
        [SerializeField] private TileSetFactory tileSetFactory;
        [SerializeField] private List<TileSetRenderer> tileSetRenderers = new ();

        private void Start()
        {
            tileSet = tileSetFactory.CreateTileSet();

            if (AutoStart) Resume();
        }

        public void Pause()
        {
            IsPaused = true;
            CancelInvoke(nameof(Compute));
        }

        public void Resume()
        {
            IsPaused = false;
            if (UpdateInterval != UpdateEveryFrame)
            {
                InvokeRepeating(nameof(Compute), 0.0f, UpdateInterval * 0.001f);
            }
        }

        private void Update()
        {
            if (UpdateInterval == UpdateEveryFrame && IsPaused == false)
            {
                Compute();
            }
        }


        public void Compute()
        {
            if (tileSet == null) return;

            foreach (var tileSetRenderer in tileSetRenderers)
            {
                tileSetRenderer.Stage(tileSet);
                tileSetRenderer.Render(tileSet);
            }
        }
    }
}
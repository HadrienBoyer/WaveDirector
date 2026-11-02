using UnityEngine;

namespace TappyTale
{
    // Utility component for quickly previewing a specific wave from UI buttons, dev menus, etc.
    public sealed class WavePreviewRunner : MonoBehaviour
    {
        [SerializeField] private WaveDirector director;
        [SerializeField, Min(0)] private int waveIndex;

        public void Preview()
        {
            if (director == null) director = FindObjectOfType<WaveDirector>(true);
            if (director == null) return;
            director.StartPreviewWave(waveIndex);
        }
    }
}

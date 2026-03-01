using System.Collections.Generic;
using UnityEngine;

namespace TappyTale.WaveDirector.Runtime
{
    public sealed class WaveObjectiveTracker : MonoBehaviour
    {
        private readonly HashSet<string> completed = new HashSet<string>();

        public bool IsCompleted(string objectiveId)
        {
            if (string.IsNullOrWhiteSpace(objectiveId)) return false;
            return completed.Contains(objectiveId);
        }

        public void MarkCompleted(string objectiveId)
        {
            if (string.IsNullOrWhiteSpace(objectiveId)) return;
            completed.Add(objectiveId);
        }

        public void ClearAll() => completed.Clear();
    }
}

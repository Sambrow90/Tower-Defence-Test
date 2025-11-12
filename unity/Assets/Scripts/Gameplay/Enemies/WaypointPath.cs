using System.Collections.Generic;
using UnityEngine;

namespace TD.Gameplay.Enemies
{
    /// <summary>
    /// Stores an ordered list of waypoints that enemies will follow.
    /// Children of this transform are automatically treated as waypoints when the list is empty.
    /// </summary>
    public class WaypointPath : MonoBehaviour
    {
        [SerializeField] private List<Transform> waypoints = new();
        [SerializeField] private bool loop;

        public IReadOnlyList<Transform> Waypoints => waypoints;
        public bool Loop => loop;

        private void Awake()
        {
            if (waypoints.Count == 0)
            {
                waypoints.AddRange(GetChildWaypoints());
            }
        }

        public Vector3 GetWaypointPosition(int index)
        {
            if (waypoints.Count == 0)
            {
                return transform.position;
            }

            index = Mathf.Clamp(index, 0, waypoints.Count - 1);
            return waypoints[index].position;
        }

        public int GetNextIndex(int currentIndex)
        {
            if (waypoints.Count == 0)
            {
                return 0;
            }

            var nextIndex = currentIndex + 1;
            if (nextIndex >= waypoints.Count)
            {
                nextIndex = loop ? 0 : waypoints.Count - 1;
            }

            return nextIndex;
        }

        private IEnumerable<Transform> GetChildWaypoints()
        {
            foreach (Transform child in transform)
            {
                yield return child;
            }
        }

#if UNITY_EDITOR
        private void OnDrawGizmos()
        {
            if (waypoints.Count == 0)
            {
                return;
            }

            Gizmos.color = Color.yellow;
            for (var i = 0; i < waypoints.Count - 1; i++)
            {
                Gizmos.DrawLine(waypoints[i].position, waypoints[i + 1].position);
            }

            if (loop && waypoints.Count > 1)
            {
                Gizmos.DrawLine(waypoints[waypoints.Count - 1].position, waypoints[0].position);
            }
        }
#endif
    }
}

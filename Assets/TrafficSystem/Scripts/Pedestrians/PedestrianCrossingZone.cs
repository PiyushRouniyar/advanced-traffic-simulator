using System.Collections.Generic;
using UnityEngine;

namespace MyTrafficSystem.Pedestrians
{
    // Compatibility bridge for existing car AI dependency.
    [DisallowMultipleComponent]
    public class PedestrianCrossingZone : MonoBehaviour
    {
        private static readonly Dictionary<CitizenCrossingNode, HashSet<int>> CrossingCitizens = new Dictionary<CitizenCrossingNode, HashSet<int>>();

        public static void ReportCitizenCrossing(CitizenCrossingNode node, Vector3 nearPosition, bool entering)
        {
            // Backward-compatible bridge: route anonymous calls to a shared synthetic id.
            ReportCitizenCrossing(node, 0, entering);
        }

        public static void ReportCitizenCrossing(CitizenCrossingNode node, int citizenId, bool entering)
        {
            if (node == null) return;

            if (!CrossingCitizens.TryGetValue(node, out HashSet<int> set))
            {
                set = new HashSet<int>();
                CrossingCitizens[node] = set;
            }

            if (entering)
            {
                set.Add(citizenId);
            }
            else
            {
                set.Remove(citizenId);
            }
        }

        public static bool IsCrosswalkBlockingCars(Vector3 carPosition, Vector3 carForward, float lookAheadDistance)
        {
            float maxDistance = Mathf.Max(0.5f, lookAheadDistance);

            foreach (KeyValuePair<CitizenCrossingNode, HashSet<int>> kv in CrossingCitizens)
            {
                CitizenCrossingNode node = kv.Key;
                if (node == null || kv.Value == null || kv.Value.Count <= 0) continue;

                Vector3 toNode = node.transform.position - carPosition;
                toNode.y = 0f;
                if (toNode.sqrMagnitude > maxDistance * maxDistance) continue;

                Vector3 forwardFlat = carForward;
                forwardFlat.y = 0f;
                if (forwardFlat.sqrMagnitude < 0.001f) continue;

                if (Vector3.Dot(forwardFlat.normalized, toNode.normalized) < 0.2f) continue;
                return true;
            }

            return false;
        }
    }
}

using MyTrafficSystem.Lanes;
using UnityEngine;

namespace MyTrafficSystem.Managers
{
    public class SpawnZone : MonoBehaviour
    {
        [SerializeField] private Lane spawnLane;
        [SerializeField] private Vector3 spawnOffset = Vector3.zero;

        public Lane SpawnLane => spawnLane;

        public Vector3 GetSpawnPosition()
        {
            if (spawnLane != null && spawnLane.StartWaypoint != null)
            {
                return spawnLane.StartWaypoint.transform.position + spawnOffset;
            }

            return transform.position + spawnOffset;
        }
    }
}

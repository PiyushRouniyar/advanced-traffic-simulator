using UnityEngine;

namespace MyTrafficSystem.Pedestrians
{
    [DisallowMultipleComponent]
    public class CitizenLaneConnection : CitizenPathConnection
    {
        public bool TryAssign(CitizenLane from, CitizenLane to)
        {
            return base.TryAssign(from, to);
        }
    }
}

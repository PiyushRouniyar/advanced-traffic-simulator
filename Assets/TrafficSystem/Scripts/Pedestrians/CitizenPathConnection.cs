using UnityEngine;

namespace MyTrafficSystem.Pedestrians
{
    [DisallowMultipleComponent]
    public class CitizenPathConnection : PedestrianLaneConnection
    {
        public bool TryAssign(CitizenPath from, CitizenPath to)
        {
            return base.TryAssign(from, to);
        }
    }
}

using UnityEngine;

namespace MyTrafficSystem.Lanes
{
    public class IntersectionConnector : MonoBehaviour
    {
        [SerializeField] private Transform connectionContainer;

        public LaneConnection CreateConnection(Lane from, Lane to, LaneConnection.ConnectionType type)
        {
            if (!ConnectionValidator.IsValid(from, to, out string reason))
            {
                Debug.LogWarning($"Connection rejected: {reason}");
                return null;
            }

            GameObject go = new GameObject($"Conn_{from.LaneName}_to_{to.LaneName}");
            if (connectionContainer != null)
            {
                go.transform.SetParent(connectionContainer);
            }

            LaneConnection connection = go.AddComponent<LaneConnection>();
            if (!connection.TryAssign(from, to, type))
            {
                Destroy(go);
                return null;
            }

            return connection;
        }
    }
}

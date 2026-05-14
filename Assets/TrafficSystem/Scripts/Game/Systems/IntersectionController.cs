using MyTrafficSystem.TrafficLights;
using UnityEngine;

namespace MyTrafficSystem.Gameplay.Systems
{
    [DisallowMultipleComponent]
    public class IntersectionController : MonoBehaviour
    {
        [SerializeField] private TrafficIntersectionManager intersectionManager;
        [SerializeField] private int currentGroupIndex;

        private void Awake()
        {
            if (intersectionManager == null) intersectionManager = GetComponent<TrafficIntersectionManager>();
        }

        public void SetPhase(int groupIndex)
        {
            if (intersectionManager == null) return;
            currentGroupIndex = Mathf.Max(0, groupIndex);
            intersectionManager.SetGroupGreen(currentGroupIndex);
        }

        public void NextPhase()
        {
            if (intersectionManager == null) return;
            currentGroupIndex++;
            intersectionManager.SetGroupGreen(currentGroupIndex);
        }
    }
}

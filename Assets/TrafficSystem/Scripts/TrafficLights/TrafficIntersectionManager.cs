using System.Collections.Generic;
using UnityEngine;

namespace MyTrafficSystem.TrafficLights
{
    [DisallowMultipleComponent]
    public class TrafficIntersectionManager : MonoBehaviour
    {
        [SerializeField] private List<TrafficLightGroup> groups = new List<TrafficLightGroup>();
        [SerializeField] private int startGreenGroupIndex = 0;

        private void Awake()
        {
            if (groups.Count == 0)
            {
                groups.AddRange(GetComponentsInChildren<TrafficLightGroup>(true));
            }

            CleanupNullGroups();
            if (groups.Count == 0) { return; }
            SetGroupGreen(Mathf.Clamp(startGreenGroupIndex, 0, groups.Count - 1));
        }

        private void Update()
        {
            for (int i = 0; i < groups.Count; i++)
            {
                TrafficLightGroup group = groups[i];
                if (group != null && group.MatchesKeyDown())
                {
                    SetGroupGreen(i);
                    return;
                }
            }
        }

        public void RegisterGroup(TrafficLightGroup group)
        {
            if (group == null || groups.Contains(group)) { return; }
            groups.Add(group);
        }

        public void SetGroupGreen(int greenIndex)
        {
            CleanupNullGroups();
            for (int i = 0; i < groups.Count; i++)
            {
                bool isGreen = i == greenIndex;
                groups[i].SetGreen(isGreen);
                groups[i].AssignStateToLanes();
            }
        }

        private void CleanupNullGroups()
        {
            for (int i = groups.Count - 1; i >= 0; i--)
            {
                if (groups[i] == null)
                {
                    groups.RemoveAt(i);
                }
            }
        }
    }
}

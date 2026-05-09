using System;
using System.Collections.Generic;
using UnityEngine;

namespace MyTrafficSystem.Waypoints
{
    public enum TurnDirection
    {
        Left = 0,
        Straight = 1,
        Right = 2
    }

    [Serializable]
    public class LaneBranch
    {
        [SerializeField] private TurnDirection turnDirection = TurnDirection.Straight;
        [SerializeField] private WaypointPath targetPath;
        [SerializeField] private Waypoint targetWaypoint;
        [SerializeField] [Min(0f)] private float probability = 1f;

        public TurnDirection TurnDirection => turnDirection;
        public WaypointPath TargetPath => targetPath;
        public Waypoint TargetWaypoint => targetWaypoint;
        public float Probability => Mathf.Max(0f, probability);
    }

    /// <summary>
    /// Stores lane branch options at an intersection waypoint and returns weighted random routes.
    /// </summary>
    public class IntersectionNode : MonoBehaviour
    {
        [Header("Branch Connections")]
        [SerializeField] private List<LaneBranch> branches = new List<LaneBranch>();

        [Header("Gizmos")]
        [SerializeField] private bool drawBranchGizmos = true;

        public bool TryGetRandomBranch(out LaneBranch selectedBranch)
        {
            selectedBranch = null;
            if (branches.Count == 0)
            {
                return false;
            }

            float totalWeight = 0f;
            for (int i = 0; i < branches.Count; i++)
            {
                if (IsBranchValid(branches[i]))
                {
                    totalWeight += branches[i].Probability;
                }
            }

            if (totalWeight <= 0f)
            {
                return false;
            }

            float roll = UnityEngine.Random.Range(0f, totalWeight);
            float runningWeight = 0f;

            for (int i = 0; i < branches.Count; i++)
            {
                LaneBranch branch = branches[i];
                if (!IsBranchValid(branch))
                {
                    continue;
                }

                runningWeight += branch.Probability;
                if (roll <= runningWeight)
                {
                    selectedBranch = branch;
                    return true;
                }
            }

            return false;
        }

        private static bool IsBranchValid(LaneBranch branch)
        {
            return branch != null &&
                   branch.TargetPath != null &&
                   branch.TargetWaypoint != null &&
                   branch.Probability > 0f;
        }

        private void OnDrawGizmos()
        {
            if (!drawBranchGizmos || branches == null)
            {
                return;
            }

            for (int i = 0; i < branches.Count; i++)
            {
                LaneBranch branch = branches[i];
                if (branch == null || branch.TargetWaypoint == null)
                {
                    continue;
                }

                Gizmos.color = GetDirectionColor(branch.TurnDirection);
                Gizmos.DrawLine(transform.position, branch.TargetWaypoint.Position);
            }
        }

        private static Color GetDirectionColor(TurnDirection direction)
        {
            switch (direction)
            {
                case TurnDirection.Left:
                    return new Color(0.2f, 0.7f, 1f, 1f);
                case TurnDirection.Right:
                    return new Color(1f, 0.5f, 0.2f, 1f);
                default:
                    return Color.green;
            }
        }
    }
}

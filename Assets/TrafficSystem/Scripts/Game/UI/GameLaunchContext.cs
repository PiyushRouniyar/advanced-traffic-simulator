namespace MyTrafficSystem.Gameplay.UI
{
    public enum LaunchMode
    {
        Monitor,
        FreeRoam
    }

    public static class GameLaunchContext
    {
        public static LaunchMode NextLaunchMode = LaunchMode.Monitor;
        public static bool HasPendingLaunchMode { get; private set; }

        public static void SetLaunchMode(LaunchMode mode)
        {
            NextLaunchMode = mode;
            HasPendingLaunchMode = true;
        }

        public static LaunchMode ConsumeLaunchMode()
        {
            HasPendingLaunchMode = false;
            return NextLaunchMode;
        }
    }
}

using System;

namespace RealityDirector.Core
{
    public static class EventBus
    {
        public static event Action<WorldEvent> Published;

        public static void Publish(WorldEvent worldEvent)
        {
            Published?.Invoke(worldEvent);
        }
    }
}

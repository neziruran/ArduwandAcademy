using System;

public static class EventManager
{
    public static event Action ONLevelStart;
    public static event Action ONLevelCompleted;
    
    public static event Action ONGestureStarted;
    public static event Action ONGestureFail;
    public static event Action ONGestureCompleted;

    public static void OnGestureStarted()
    {
        ONGestureStarted?.Invoke();
    }

    public static void OnGestureFail()
    {
        ONGestureFail?.Invoke();
    }

    public static void OnGestureCompleted()
    {
        ONGestureCompleted?.Invoke();
    }

    public static void OnLevelStart()
    {
        ONLevelStart?.Invoke();
    }
    
    public static void OnLevelCompleted()
    {
        ONLevelCompleted?.Invoke();
    }
}
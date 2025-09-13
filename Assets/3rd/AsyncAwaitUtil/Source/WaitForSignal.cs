using System;
using System.Runtime.CompilerServices;

public class WaitForSignal : INotifyCompletion
{
    public bool IsCompleted { get; private set; }

    private Action callback;

    public WaitForSignal(ref Action signal)
    {
        signal += () =>
        {
            IsCompleted = true;
            Action c = callback;
            callback = null;
            c?.Invoke();
        };
    }

    public WaitForSignal GetAwaiter()
    {
        return this;
    }

    public void OnCompleted(Action continuation)
    {
        callback = continuation;
    }

    public void GetResult() { }
}

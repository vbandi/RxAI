namespace RxAI.Realtime;

public static class Helpers
{
    /// <summary>
    /// Fire and forget a task.
    /// </summary>
    /// <param name="task">The task to fire and forget.</param>
    public static void FireAndForget(this Task task) => task.ContinueWith(_ => { });

    /// <summary>
    /// Adds a disposable to a list of disposables.
    /// </summary>
    /// <param name="disposable">The disposable to add.</param>
    /// <param name="disposables">The list of disposables to add to.</param>
    public static void AddTo(this IDisposable disposable, IList<IDisposable> disposables)
    {
        lock (disposables)
        {
            disposables.Add(disposable);
        }
    }

    /// <summary>
    /// Disposes all disposables in a list.
    /// </summary>
    /// <param name="disposables">The list of disposables to dispose.</param>
    public static void DisposeAll(this IList<IDisposable> disposables)
    {
        lock (disposables)
        {
            foreach (var disposable in disposables)
            {
                disposable.Dispose();
            }
        }
    }
}

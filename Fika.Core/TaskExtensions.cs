using System;
using System.Threading.Tasks;

namespace Fika.Core;

public static class TaskExtensions
{
    /// <summary>
    /// Extension method to safely ignore an async Task while still logging exceptions. <br/>
    /// This is similar to “fire-and-forget” but avoids silently swallowing errors.
    /// </summary>
    /// <param name="task">The Task to run and ignore.</param>
    public static async void Forget(this Task task)
    {
        try
        {
            await task;
        }
        catch (Exception e)
        {
            Debug.LogException(e);
        }
    }
}

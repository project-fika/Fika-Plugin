using System;
using System.Threading;
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
        catch (OperationCanceledException)
        {

        }
        catch (Exception e)
        {
            Main.Utils.FikaGlobals.LogError(e.ToString());
        }
    }

    /// <summary>
    ///     Get a managed token that is cancelled together with an il2cpp token
    /// </summary>
    /// <param name="token">Il2cpp token, such as a component's destroyCancellationToken</param>
    /// <returns>Managed token that follows the il2cpp token</returns>
    public static CancellationToken ToManaged(this Il2CppSystem.Threading.CancellationToken token)
    {
        CancellationTokenSource source = new();
        token.Register(new Action(source.Cancel));
        return source.Token;
    }
}

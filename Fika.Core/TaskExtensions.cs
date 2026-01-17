using System;
using System.Threading.Tasks;
using Fika.Core.Main.Utils;

namespace Fika.Core;

public static class TaskExtensions
{
    public static async void Forget(this Task task)
    {
        try
        {
            await task;
        }
        catch (Exception e)
        {
            FikaGlobals.LogError(e);
        }
    }
}

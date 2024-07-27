using System;
using System.Collections.Generic;
using UnityEngine;

public class MainThreadDispatcher : MonoBehaviour
{
    private static MainThreadDispatcher _instance;
    private readonly Queue<Action> _actions = new();

    private void Awake()
    {
        if (_instance == null)
        {
            _instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Update()
    {
        lock (_actions)
        {
            while (_actions.Count > 0)
            {
                _actions.Dequeue()();
            }
        }
    }

    public static void RunOnMainThread(Action action)
    {
        if (_instance == null)
        {
            Debug.LogError("MainThreadDispatcher is not initialized!");
            return;
        }

        lock (_instance._actions)
        {
            _instance._actions.Enqueue(action);
        }
    }
}
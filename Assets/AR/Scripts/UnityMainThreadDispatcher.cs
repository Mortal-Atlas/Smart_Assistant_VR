using UnityEngine;
using System.Collections.Generic;
using System;

public class UnityMainThreadDispatcher : MonoBehaviour
{
    private static readonly Queue<Action> _executionQueue = new Queue<Action>();
    
    public static UnityMainThreadDispatcher Instance() 
    { 
        return _instance; 
    }
    
    private static UnityMainThreadDispatcher _instance;

    void Awake() 
    { 
        // Ensure there is only one of these in the scene
        if (_instance == null)
        {
            _instance = this; 
        }
        else
        {
            Destroy(this.gameObject);
        }
    }

    public void Enqueue(Action action) 
    { 
        lock(_executionQueue) 
        { 
            _executionQueue.Enqueue(action); 
        } 
    }

    void Update()
    {
        lock(_executionQueue) 
        { 
            while (_executionQueue.Count > 0) 
            {
                _executionQueue.Dequeue().Invoke(); 
            }
        }
    }
}
using System;
using UnityEngine;

public class FunctionTimer
{
    private Action action;
    private float timer;
    private bool isDetroyed;

    public FunctionTimer(Action action, float timer) {
        this.action = action;
        this.timer = timer;
        isDetroyed = false;
    }

    public void Update()
    {
        if (!isDetroyed)
        {
            timer -= Time.deltaTime;
            if(timer < 0)
            {
                action();
                DestroySelf();
            }
        }
    }

    private void DestroySelf()
    {
        isDetroyed = true;
    }
}

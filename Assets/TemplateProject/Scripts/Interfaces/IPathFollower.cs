using System;
using UnityEngine;

namespace BusJamClone.Scripts.Runtime.Interfaces
{
    public interface IPathFollower
    {
        void SetPath(Vector3[] newPath, Action onComplete = null);
        void Run(Vector3[] newPath, Action onComplete = null);
        void Stop();
        void ChangePath(Vector3[] newPath, Action onComplete = null);
    }
}
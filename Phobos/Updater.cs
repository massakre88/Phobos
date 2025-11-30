using Phobos.ECS;
using Phobos.Navigation;
using UnityEngine;

namespace Phobos;

public class Updater : MonoBehaviour
{
    public SystemOrchestrator SystemOrchestrator;
    public NavJobExecutor NavJobExecutor;
    
    public void Update()
    {
        SystemOrchestrator.Update();
        NavJobExecutor.Update();
    }
}
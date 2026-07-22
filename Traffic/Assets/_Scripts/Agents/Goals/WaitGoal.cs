using UnityEngine;
using System.Collections;

public class WaitGoal : Goal
{
    private float _waitTime = 0;

    public WaitGoal(float waitTime)
    {
        _waitTime = waitTime;
    }

    public override void Initialise(AgentController agent)
    {
        agent.StartCoroutine(Wait(agent));
    }

    public override void OnArrived(AgentController agent)
    {
        Debug.Log($"We have waited for {_waitTime} seconds");
    }

    private IEnumerator Wait(AgentController agent)
    {
        yield return new WaitForSeconds(_waitTime);
        agent.OnMovementFinished();
    }
}

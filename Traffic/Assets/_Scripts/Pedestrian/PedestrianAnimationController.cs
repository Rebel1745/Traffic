using UnityEngine;

[RequireComponent(typeof(Animator))]
public class PedestrianAnimationController : MonoBehaviour
{
    private Animator _animator;
    private string _idleAnimationString = "idle";
    private string _walkAnimationString = "walk";
    private string _runAnimationString = "run";
    private string _waveAnimationString = "wave";

    private PedestrianAnimationType _currentAnimationType;

    private void Awake()
    {
        _animator = GetComponent<Animator>();
    }

    public void SetAnimation(PedestrianAnimationType type)
    {
        if (type == _currentAnimationType) return;

        _animator.SetTrigger(GetTriggerStringFromType(type));
        _currentAnimationType = type;
    }

    private string GetTriggerStringFromType(PedestrianAnimationType type)
    {
        return type switch
        {
            PedestrianAnimationType.Idle => _idleAnimationString,
            PedestrianAnimationType.Walk => _walkAnimationString,
            PedestrianAnimationType.Run => _runAnimationString,
            PedestrianAnimationType.Wave => _waveAnimationString,
            _ => _idleAnimationString
        };
    }
}

public enum PedestrianAnimationType
{
    Idle,
    Walk,
    Run,
    Wave
}

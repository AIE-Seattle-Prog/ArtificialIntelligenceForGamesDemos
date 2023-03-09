public enum BehaviorResult
{
    Success,
    Failure
}

public interface IBehavior
{
    BehaviorResult DoBehavior();
}
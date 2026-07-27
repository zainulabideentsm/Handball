using UnityEngine;

[DisallowMultipleComponent]
public sealed class PlayerAnimationEventRelay : MonoBehaviour
{
    [SerializeField] private PlayerBallPickup ballPickup;
    [SerializeField] private PlayerThrowController throwController;

    // Called from PickUp and RunningPickup clips.
    public void AE_AttachBall()
    {
        Debug.Log("AE_AttachBall fired");
        ballPickup?.AttachPendingBall();
    }

    // Called from the Throw clip.
    public void AE_ReleaseBall()
    {
        throwController?.ReleasePendingBall();
    }
}
using UnityEngine;

public class IdleVariation : StateMachineBehaviour
{
    //OnStateEnter is called when a transition starts and the state machine starts to evaluate this state
    override public void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        float blendValue = Random.Range(0.0f, 1.0f);
        animator.SetFloat("IdleBlend", blendValue);
    }
}

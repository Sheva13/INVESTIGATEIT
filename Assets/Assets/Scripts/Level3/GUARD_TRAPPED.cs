using UnityEngine;

public class GUARD_TRAPPED : MonoBehaviour
{
    private GuardAI guardAI;
    private UnityEngine.AI.NavMeshAgent agent;
    private Animator animator;
    private bool trapped = false;

    void Awake()
    {
        guardAI = GetComponent<GuardAI>();
        agent = GetComponent<UnityEngine.AI.NavMeshAgent>();
        animator = GetComponent<Animator>();
    }

    public void SetTrapped(bool value)
    {
        trapped = value;

        if (guardAI != null)
        {
            if (trapped)
            {
                guardAI.currentState = GuardState.Trapped;

                if (agent != null)
                    agent.isStopped = true;

                var rb = GetComponent<Rigidbody2D>();
                if (rb != null)
                    rb.linearVelocity = Vector2.zero;

                if (animator != null)
                    animator.SetFloat("Speed", 0f);
            }
        }
    }

    public bool IsTrapped => trapped;
}

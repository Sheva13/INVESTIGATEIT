using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class TrapTrigger : Interactable
{
    [Header("Trap Objects")]
    public Transform[] trapObjects;
    public float fallDuration = 1f;
    public float fallDelay = 0.05f;

    [Header("Guard Area")]
    public string areaName = "Area";
    public float trapRadius = 5f;

    private bool hasTriggered = false;

    protected override void ShowPrompt()
    {
        if (hasTriggered) return;
        GameManager gm = FindAnyObjectByType<GameManager>();
        if (gm != null)
            gm.ShowNotification("Tekan [E] untuk menjebak", Color.white, 999f);
    }

    protected override void HidePrompt()
    {
        GameManager gm = FindAnyObjectByType<GameManager>();
        if (gm != null)
            gm.ShowNotification("", Color.white, 0f);
    }

    public override void OnInteract()
    {
        if (hasTriggered) return;
        hasTriggered = true;

        HidePrompt();
        TrapGuards();
        StartCoroutine(CollapseAnimation());
        NotifyGameManager();
    }

    void TrapGuards()
    {
        var guards = FindObjectsByType<GuardAI>(FindObjectsSortMode.None);
        int trapped = 0;
        foreach (var guard in guards)
        {
            if (guard == null) continue;
            float dist = Vector2.Distance(transform.position, guard.transform.position);
            if (dist <= trapRadius)
            {
                var gt = guard.GetComponent<GUARD_TRAPPED>();
                if (gt == null) gt = guard.gameObject.AddComponent<GUARD_TRAPPED>();
                gt.SetTrapped(true);
                trapped++;
            }
        }
        Debug.Log($"TrapTrigger: {trapped} guard(s) trapped in {areaName}");
    }

    IEnumerator CollapseAnimation()
    {
        if (trapObjects == null || trapObjects.Length == 0) yield break;

        for (int i = 0; i < trapObjects.Length; i++)
        {
            if (trapObjects[i] == null) continue;

            Vector2 startPos = trapObjects[i].position;
            Vector2 fallDir = new Vector2(Random.Range(-1f, 1f), Random.Range(-1f, -0.5f)).normalized;
            float fallDist = Random.Range(1f, 2.5f);
            Vector2 endPos = startPos + fallDir * fallDist;

            float duration = fallDuration + i * fallDelay;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;
                trapObjects[i].position = Vector2.Lerp(startPos, endPos, t);
                trapObjects[i].Rotate(0f, 0f, t * Random.Range(120f, 270f));
                yield return null;
            }

            trapObjects[i].position = endPos;
        }

        Debug.Log($"TrapTrigger: Collapse selesai untuk {areaName}");
    }

    void NotifyGameManager()
    {
        GameManager gm = FindAnyObjectByType<GameManager>();
        if (gm != null)
            gm.RegisterTrapTriggered(areaName);
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, trapRadius);
    }
}

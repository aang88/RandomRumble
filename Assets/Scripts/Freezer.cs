using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Freezer : MonoBehaviour
{
    public float duration = 0.5f;
    bool isFrozen = false;
    public float pendingFreezeDuration = 0f;
    // Start is called before the first frame update
    void Start()
    {
        pendingFreezeDuration = duration;
    }

    // Update is called once per frame
    void Update()
    {
        if(pendingFreezeDuration < 0 && !isFrozen)
        {
            StartCoroutine(Freeze());
        }
    }

    public IEnumerator Freeze()
    {
        UnityEngine.Debug.Log("HIT: Freezing");
        isFrozen = true;
        var original  = Time.timeScale;
        Time.timeScale = 0;
        yield return new WaitForSecondsRealtime(duration);

        Time.timeScale = original;
        pendingFreezeDuration = 0;
        isFrozen = false;
    }


}

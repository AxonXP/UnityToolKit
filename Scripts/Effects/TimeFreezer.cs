using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TimeFreezeInfo
{
    public float duration;
    public float power;
    public TimeFreezeInfo(float duration, float power)
    {
        this.duration = duration;
        this.power = power;
    }
    public TimeFreezeInfo(float duration)
    {
        this.duration = duration;
    }
}

public class TimeFreezer : MonoBehaviour
{
    public void FreezeTime(TimeFreezeInfo info)
    {
        StopAllCoroutines();
        StartCoroutine(DoTimeFreeze(info.duration, info.power));
    }
    public void FreezeTime(float duration, float power)
    {
        StopAllCoroutines();
        StartCoroutine(DoTimeFreeze(duration, power));
    }
    private IEnumerator DoTimeFreeze(float duration, float power)
    {
        Time.timeScale = power;
        yield return new WaitForSecondsRealtime(duration);
        Time.timeScale = 1;
    }
    public void StopTimeFreeze()
    {
        StopAllCoroutines();
        Time.timeScale = 1;
    }
}

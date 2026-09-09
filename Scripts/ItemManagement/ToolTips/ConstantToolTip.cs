using SaintsField.Playa;
using UnityEngine;

public class ConstantToolTip : ToolTip
{
    override protected void Awake()
    {
        base.Awake();
        OpenTip();
    }
}
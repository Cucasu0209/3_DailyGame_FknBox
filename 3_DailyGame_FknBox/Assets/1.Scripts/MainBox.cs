using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class MainBox : BaseBox
{
    private void Start()
    {
        if (this == GameManager.Instance.MainBox) UserInput.Instance.OnUserMove += Move;
    }
    public override void SetLevel(int level)
    {
        base.SetLevel(level);
        if (this == GameManager.Instance.MainBox) GameManager.Instance.OnMainLevelChange?.Invoke();
    }
}

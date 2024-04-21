using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEditor;
using UnityEngine;

public class MainBox : BaseBox
{
    public override void Start()
    {
        base.Start();
        if (this == GameManager.Instance.MainBox) UserInput.Instance.OnUserMove += Move;
    }
    public override void SetLevel(int level)
    {
        base.SetLevel(level);
        if (this == GameManager.Instance.MainBox) GameManager.Instance.OnMainLevelChange?.Invoke();
    }
}

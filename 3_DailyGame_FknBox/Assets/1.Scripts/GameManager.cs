using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices.WindowsRuntime;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;

    public Action OnMainLevelChange;
    public Action<int, Direction, MoveType> OnMove;
    private void Awake()
    {
        Instance = this;
    }
    public MainBox MainBox;
    [SerializeField] private Dictionary<int, List<BaseBox>> Boxes = new Dictionary<int, List<BaseBox>>();
    public List<ExtendableBoxProperty> Props;

    public BaseBox GetBoxInLevel(int level, int id)
    {
        if (Boxes.ContainsKey(level) == false) return null;
        foreach (var box in Boxes[level])
        {
            if (box.Prop.Internal.Id == id) return box;
        }
        return null;
    }
    public ExtendableBoxProperty GetProp(int id)
    {
        foreach (var prop in Props)
        {
            if (prop.Internal.Id == id) return prop;
        }
        return null;
    }
    public void AddToLevel(int level, BaseBox box)
    {
        if (Boxes.ContainsKey(level) == false) Boxes.Add(level, new List<BaseBox>());
        if (Boxes[level].Contains(box) == false) Boxes[level].Add(box);
    }
    public void RemoveFromLevel(int level, BaseBox box)
    {
        if (Boxes.ContainsKey(level) == false) return;
        else Boxes[level].Remove(box);
    }

    public List<BaseBox> GetBoxes(int level)
    {
        if (!Boxes.ContainsKey(level)) return null;
        else return Boxes[level];
    }

}


using UnityEngine;
using DG.Tweening;
using System.Collections.Generic;
using static UnityEngine.RuleTile.TilingRuleOutput;
using UnityEngine.UIElements;
using System.ComponentModel;
using Unity.VisualScripting;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Collections;


public enum MoveType
{
    GoOutSide,
    GoInSide,
    Push,
    NoObstacle,
    CantMove,
}
public class BaseBox : MonoBehaviour
{
    [SerializeField] private int Level;
    public ExtendableBoxProperty Prop;
    public SpriteRenderer Border;
    public SpriteRenderer Inside;

    public virtual void Start()
    {
        GameManager.Instance.OnMove += OnPropChange;
    }

    #region Init
    public virtual void Setup(int level, ExtendableBoxProperty myProp, BaseBox parent = null)
    {
        SetLevel(level);
        Prop = myProp;
        SetupColor();
        if (parent != null)
        {
            transform.parent = parent.Inside.transform;
            transform.localPosition = parent.GetLocalWorldPosition(myProp.External.Position);
            transform.localScale = parent.GetChildScale();
        }
    }
    public Vector2 GetLocalWorldPosition(Vector2Int position)
    {
        return new Vector2(-1f / 2 + (1f / Prop.Internal.Size.x) * (position.x + 0.5f),
        -1f / 2 + (1f / Prop.Internal.Size.y) * (position.y + 0.5f));
    }
    public Vector2 GetChildScale()
    {
        return new Vector2(1f / Prop.Internal.Size.x, 1f / Prop.Internal.Size.y);
    }
    protected virtual void SetupColor()
    {
        Border.color = Prop.Internal.BorderColor;
        Inside.color = Prop.Internal.InsideColor;
    }
    public void SetScaleByChildren(Vector2 childrenScale)
    {
        transform.localScale = childrenScale;
    }
    public virtual void SetLevel(int level) { Level = level; }
    public int GetLevel() => Level;
    #endregion

    #region Action Move
    public void Move(Direction dir)
    {
        Dictionary<int, MoveType> moveResult = Prop.Move(dir);
        foreach (var res in moveResult)
        {
            GameManager.Instance.OnMove?.Invoke(res.Key, dir, res.Value);
        }
        SetLevel(GetLevel());
    }
    private void FreeMove(Direction dir)
    {
        BaseBox boxParent = GameManager.Instance.GetBoxInLevel(Level + 1, Prop.External.Parent);
        if (boxParent == null) return;
        transform.DOLocalMove(boxParent.GetLocalWorldPosition(Prop.External.Position), 0.2f).SetEase(Ease.Linear);
    }
    private IEnumerator MoveToOutside(Direction dir)
    {
        if (this is MainBox && this.GetLevel() > GameManager.Instance.MainBox.GetLevel()) { }
        else
        {
            GameManager.Instance.RemoveFromLevel(GetLevel(), this);
            SetLevel(GetLevel() + 1);
            GameManager.Instance.AddToLevel(GetLevel(), this);

            yield return new WaitForEndOfFrame();

            ExtendableBox lastParent = GameManager.Instance.GetBoxInLevel(GetLevel(), Prop.GetSibling(ExtendableBoxProperty.GetOppositeDir(dir)).Internal.Id)?.gameObject.GetComponent<ExtendableBox>();
            if (lastParent != null)
            {
                transform.DOLocalMove(lastParent.GetLocalPosOutsideDoor(), 0.1f).SetEase(Ease.Linear).OnComplete(() =>
                {
                    BaseBox boxParent = GameManager.Instance.GetBoxInLevel(GetLevel() + 1, Prop.External.Parent).gameObject.GetComponent<BaseBox>();
                    transform.parent = boxParent.Inside.transform;
                    transform.DOScale(boxParent.GetChildScale(), 0.1f).SetEase(Ease.Linear);
                    transform.DOLocalMove(boxParent.GetLocalWorldPosition(Prop.External.Position), 0.1f).SetEase(Ease.Linear);
                });
            }
        }
    }
    private IEnumerator MoveToInside(Direction dir)
    {
        if (this is MainBox && this.GetLevel() > GameManager.Instance.MainBox.GetLevel()) { }
        else
        {
            GameManager.Instance.RemoveFromLevel(GetLevel(), this);
            SetLevel(GetLevel() - 1);
            GameManager.Instance.AddToLevel(GetLevel(), this);

            yield return new WaitForEndOfFrame();

            ExtendableBox boxParent = GameManager.Instance.GetBoxInLevel(GetLevel() + 1, Prop.External.Parent)?.gameObject.GetComponent<ExtendableBox>();
            if (boxParent != null)
            {
                transform.parent = boxParent.Inside.transform;
                transform.DOScale(boxParent.GetChildScale(), 0.1f).SetEase(Ease.Linear);
                transform.DOLocalMove(boxParent.GetLocalPosOutsideDoor(), 0.1f).SetEase(Ease.Linear).OnComplete(() =>
                {
                    transform.DOLocalMove(boxParent.GetLocalPosInsideDoor(), 0.1f).SetEase(Ease.Linear);
                });
            }

        }
    }
    public void OnPropChange(int id, Direction dir, MoveType moveType)
    {
        if (id == Prop.Internal.Id)
        {
            Debug.Log("level: " + GetLevel() + ",id: " + Prop.Internal.Id + ",type: " + moveType.ToString());
            if (moveType == MoveType.NoObstacle || moveType == MoveType.Push) FreeMove(dir);
            else if (moveType == MoveType.GoOutSide) StartCoroutine(MoveToOutside(dir));
            else if (moveType == MoveType.GoInSide) StartCoroutine(MoveToInside(dir));
        }
    }
    #endregion
}

[System.Serializable]
public class ExtendableBoxProperty
{
    public ExternalFeature External;
    public InternalFeature Internal;

    public static bool CheckOppositeDir(Direction a, Direction b)
    {
        return a == Direction.Top && b == Direction.Bottom
        || a == Direction.Bottom && b == Direction.Top
        || a == Direction.Left && b == Direction.Right
        || a == Direction.Right && b == Direction.Left;
    }
    public MoveType CheckMove(Direction dir)
    {
        //new assumption position
        //   BaseBox boxParent = GameManager.Instance.GetBoxInLevel(Level + 1, Prop.External.Parent);
        ExtendableBoxProperty propParent = GameManager.Instance.GetProp(External.Parent);
        Vector2Int newPosion = External.GetNextMove(dir);
        //check door parent
        if (propParent.Internal.DoorPositionInside.Equals(External.Position)
              && propParent.Internal.DoorDir == dir)
        {
            foreach (ExtendableBoxProperty ParentSibling in GameManager.Instance.Props)
            {
                if (propParent.External.Parent == ParentSibling.External.Parent && propParent.External.GetNextMove(dir).Equals(ParentSibling.External.Position))
                {
                    switch (ParentSibling.CheckMove(dir))
                    {
                        case MoveType.GoOutSide:
                        case MoveType.GoInSide:
                        case MoveType.NoObstacle:
                            return MoveType.GoOutSide;
                        case MoveType.Push:
                        case MoveType.CantMove:
                            return MoveType.CantMove;
                    }
                }
            }
            return MoveType.GoOutSide;
        }

        //check border parent
        if (newPosion.x < 0 || newPosion.x >= propParent.Internal.Size.x
            || newPosion.y < 0 || newPosion.y >= propParent.Internal.Size.y)
        {
            return MoveType.CantMove;
        }

        //check next box
        foreach (ExtendableBoxProperty NextSibling in GameManager.Instance.Props)
        {
            if (NextSibling.External.Parent == External.Parent && newPosion.Equals(NextSibling.External.Position))
            {
                //check door next box
                if (CheckOppositeDir(dir, NextSibling.Internal.DoorDir))
                {
                    foreach (ExtendableBoxProperty ChildSibling in GameManager.Instance.Props)
                    {
                        if (NextSibling.Internal.Id == ChildSibling.External.Parent && ChildSibling.External.Position.Equals(NextSibling.Internal.DoorPositionInside))
                        {
                            switch (ChildSibling.CheckMove(dir))
                            {
                                case MoveType.GoOutSide:
                                case MoveType.GoInSide:
                                case MoveType.NoObstacle:
                                    return MoveType.GoInSide;
                                case MoveType.Push:
                                case MoveType.CantMove:
                                    return MoveType.CantMove;
                            }
                        }
                    }
                    return MoveType.GoInSide;
                }
                //check can push
                switch (NextSibling.CheckMove(dir))
                {
                    case MoveType.GoOutSide:
                    case MoveType.GoInSide:
                    case MoveType.NoObstacle:
                        return MoveType.Push;
                    case MoveType.Push:
                    case MoveType.CantMove:
                        return MoveType.CantMove;
                }
            }
        }


        //normal move
        return MoveType.NoObstacle;
    }
    public Dictionary<int, MoveType> Move(Direction dir)
    {
        ExtendableBoxProperty sibling = GetSibling(dir);
        ExtendableBoxProperty parent = GetParent();
        MoveType moveType = CheckMove(dir);
        Dictionary<int, MoveType> result = new Dictionary<int, MoveType>()
        {
            {Internal.Id, moveType},
        };
        Debug.Log(Internal.Id + " " + moveType);
        if (moveType == MoveType.NoObstacle)
        {
            External.Position = External.GetNextMove(dir);
        }
        else if (moveType == MoveType.GoOutSide)
        {
            sibling = parent.GetSibling(dir);
            if (sibling != null) result.Add(sibling.Internal.Id, sibling.Move(dir)[sibling.Internal.Id]);
            External.Parent = parent.External.Parent;
            External.Position = parent.External.GetNextMove(dir);
        }
        else if (moveType == MoveType.GoInSide)
        {
            sibling = sibling.GetChild(sibling.Internal.DoorPositionInside);
            if (sibling != null) result.Add(sibling.Internal.Id, sibling.Move(dir)[sibling.Internal.Id]);
            sibling = GetSibling(dir);
            External.Parent = sibling.Internal.Id;
            External.Position = sibling.Internal.DoorPositionInside;
        }
        else if (moveType == MoveType.Push)
        {

            External.Position = External.GetNextMove(dir);
            result.Add(sibling.Internal.Id, sibling.Move(dir)[sibling.Internal.Id]);
        }

        return result;
    }
    public ExtendableBoxProperty GetSibling(Direction dir)
    {
        foreach (ExtendableBoxProperty sibling in GameManager.Instance.Props)
        {
            if (sibling.External.Parent == External.Parent && External.GetNextMove(dir).Equals(sibling.External.Position))
            {
                return sibling;
            }
        }
        return null;
    }
    public ExtendableBoxProperty GetParent()
    {
        foreach (ExtendableBoxProperty parent in GameManager.Instance.Props)
        {
            if (parent.Internal.Id == External.Parent)
            {
                return parent;
            }
        }
        return null;
    }
    public ExtendableBoxProperty GetChild(Vector2Int position)
    {
        foreach (ExtendableBoxProperty child in GameManager.Instance.Props)
        {
            if (Internal.Id == child.External.Parent && position.Equals(child.External.Position))
            {
                return child;
            }
        }
        return null;
    }
    public static Direction GetOppositeDir(Direction dir)
    {
        if (dir == Direction.Left) return Direction.Right;
        if (dir == Direction.Right) return Direction.Left;
        if (dir == Direction.Top) return Direction.Bottom;
        if (dir == Direction.Bottom) return Direction.Top;
        return Direction.None;
    }
}

[System.Serializable]
public class ExternalFeature
{
    public int Parent;
    public Vector2Int Position;//(DOWN,LEFT)->(0,0)

    public Vector2Int GetNextMove(Direction dir)
    {
        Vector2Int newPosion = new Vector2Int(Position.x, Position.y);
        switch (dir)
        {
            case Direction.Left: newPosion = new Vector2Int(Position.x - 1, Position.y); break;
            case Direction.Right: newPosion = new Vector2Int(Position.x + 1, Position.y); break;
            case Direction.Top: newPosion = new Vector2Int(Position.x, Position.y + 1); break;
            case Direction.Bottom: newPosion = new Vector2Int(Position.x, Position.y - 1); break;
        }
        return newPosion;
    }
}

[System.Serializable]
public class InternalFeature
{
    public int Id;
    public Vector2Int Size = new Vector2Int(5, 5);
    public Direction DoorDir;
    public Vector2Int DoorPositionInside;
    public Color BorderColor;
    public Color InsideColor;

}

public enum Direction
{
    Left, Right, Top, Bottom, None
}
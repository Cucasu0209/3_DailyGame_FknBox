
using UnityEngine;
using DG.Tweening;
using TreeEditor;
using System.Runtime.InteropServices.WindowsRuntime;
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
    private int Level;
    public ExtendableBoxProperty Prop;
    public SpriteRenderer Border;
    public SpriteRenderer Inside;

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
    private bool CheckOppositeDir(Direction a, Direction b)
    {
        return a == Direction.Top && b == Direction.Bottom
        || a == Direction.Bottom && b == Direction.Top
        || a == Direction.Left && b == Direction.Right
        || a == Direction.Right && b == Direction.Left;

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

    #region Action Move
    public MoveType CheckMove(Direction dir)
    {
        //new assumption position
        BaseBox boxParent = GameManager.Instance.GetBoxInLevel(Level + 1, Prop.External.Parent);
        ExtendableBoxProperty propParent = boxParent.Prop;
        Vector2Int newPosion = GetNextMove(dir);
        //check door parent
        if (propParent.Internal.DoorPositionInside.Equals(Prop.External.Position)
              && propParent.Internal.DoorDir == dir)
        {
            foreach (BaseBox ParentSibling in GameManager.Instance.GetBoxes(Level + 1))
            {
                if (propParent.External.Parent == ParentSibling.Prop.External.Parent && boxParent.GetNextMove(dir).Equals(ParentSibling.Prop.External.Position))
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
        foreach (BaseBox NextSibling in GameManager.Instance.GetBoxes(Level))
        {
            if (NextSibling.Prop.External.Parent == Prop.External.Parent && newPosion.Equals(NextSibling.Prop.External.Position))
            {
                //check door next box
                if (CheckOppositeDir(dir, NextSibling.Prop.Internal.DoorDir))
                {
                    foreach (BaseBox ChildSibling in GameManager.Instance.GetBoxes(Level - 1))
                    {
                        if (NextSibling.Prop.Internal.Id == ChildSibling.Prop.External.Parent && ChildSibling.Prop.External.Position.Equals(NextSibling.Prop.Internal.DoorPositionInside))
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
    public Vector2Int GetNextMove(Direction dir)
    {
        Vector2Int newPosion = new Vector2Int(Prop.External.Position.x, Prop.External.Position.y);
        switch (dir)
        {
            case Direction.Left: newPosion = new Vector2Int(Prop.External.Position.x - 1, Prop.External.Position.y); break;
            case Direction.Right: newPosion = new Vector2Int(Prop.External.Position.x + 1, Prop.External.Position.y); break;
            case Direction.Top: newPosion = new Vector2Int(Prop.External.Position.x, Prop.External.Position.y + 1); break;
            case Direction.Bottom: newPosion = new Vector2Int(Prop.External.Position.x, Prop.External.Position.y - 1); break;
        }
        return newPosion;
    }
    public void Move(Direction dir)
    {
        MoveType moveType = CheckMove(dir);
        Debug.Log(moveType.ToString());
        if (moveType == MoveType.NoObstacle) FreeMove(dir);
        else if (moveType == MoveType.GoOutSide) MoveToOutside(dir);
        else if (moveType == MoveType.GoInSide) MoveToInside(dir);
        else if (moveType == MoveType.Push) MoveAndPush(dir);
    }
    private void FreeMove(Direction dir)
    {
        BaseBox boxParent = GameManager.Instance.GetBoxInLevel(Level + 1, Prop.External.Parent);
        Vector2Int newPosion = GetNextMove(dir);
        Prop.External.Position = newPosion;
        transform.DOLocalMove(boxParent.GetLocalWorldPosition(newPosion), 0.2f).SetEase(Ease.Linear);
    }
    private void MoveToOutside(Direction dir)
    {
        ExtendableBox boxParent = GameManager.Instance.GetBoxInLevel(Level + 1, Prop.External.Parent).gameObject.GetComponent<ExtendableBox>();
        foreach (BaseBox ParentSibling in GameManager.Instance.GetBoxes(Level + 1))
        {
            if (boxParent.Prop.External.Parent == ParentSibling.Prop.External.Parent && boxParent.GetNextMove(dir).Equals(ParentSibling.Prop.External.Position))
            {
                ParentSibling.Move(dir);
                break;
            }
        }
        boxParent.GoOutsideMe(this);
    }
    private void MoveToInside(Direction dir)
    {
        Vector2Int newPosion = GetNextMove(dir);
        foreach (BaseBox NextSibling in GameManager.Instance.GetBoxes(Level))
        {
            if (NextSibling.Prop.External.Parent == Prop.External.Parent && newPosion.Equals(NextSibling.Prop.External.Position))
            {
                //check door next box
                if (CheckOppositeDir(dir, NextSibling.Prop.Internal.DoorDir))
                {
                    foreach (BaseBox ChildSibling in GameManager.Instance.GetBoxes(Level - 1))
                    {
                        if (NextSibling.Prop.Internal.Id == ChildSibling.Prop.External.Parent && ChildSibling.Prop.External.Position.Equals(NextSibling.Prop.Internal.DoorPositionInside))
                        {
                            ChildSibling.Move(dir);
                            break;
                        }
                    }
                    ExtendableBox NewParent = NextSibling.gameObject.GetComponent<ExtendableBox>();
                    NewParent.GoInsideMe(this);
                    return;
                }
            }
        }
    }
    private void MoveAndPush(Direction dir)
    {
        Vector2Int newPosion = GetNextMove(dir);
        foreach (BaseBox NextSibling in GameManager.Instance.GetBoxes(Level))
        {
            if (NextSibling.Prop.External.Parent == Prop.External.Parent && newPosion.Equals(NextSibling.Prop.External.Position))
            {
                FreeMove(dir);
                NextSibling.Move(dir);
                return;
            }
        }
    }

    #endregion
}

[System.Serializable]
public class ExtendableBoxProperty
{
    public ExternalFeature External;
    public InternalFeature Internal;
}

[System.Serializable]
public class ExternalFeature
{
    public int Parent;
    public Vector2Int Position;//(DOWN,LEFT)->(0,0)
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
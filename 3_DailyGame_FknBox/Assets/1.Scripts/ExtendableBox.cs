using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

public class ExtendableBox : BaseBox
{
    #region Variables
    [Header("For Setup")]
    public SpriteRenderer Door;
    #endregion

    #region Unity
    private void Start()
    {

    }
    #endregion

    #region Actions
    public override void Setup(int level, ExtendableBoxProperty myProp, BaseBox parent = null)
    {
        base.Setup(level, myProp, parent);
        SetupDoor();
    }
    protected override void SetupColor()
    {
        base.SetupColor();
        Door.color = Prop.Internal.InsideColor;
    }
    private void SetupDoor()
    {
        Door.transform.parent = Border.transform;
        switch (Prop.Internal.DoorDir)
        {
            case Direction.Left:
            case Direction.Right:
                Prop.Internal.DoorPositionInside = new Vector2Int((Prop.Internal.DoorDir == Direction.Right) ? Prop.Internal.Size.x - 1 : 0, Prop.Internal.DoorPositionInside.y);
                Door.transform.localPosition = new Vector2(((Prop.Internal.DoorDir == Direction.Right) ? 1 : -1) * Inside.transform.localScale.x / 2 + (1 - Inside.transform.localScale.x) / 4,
                    -Inside.transform.localScale.y / 2 + (Inside.transform.localScale.y / Prop.Internal.Size.y) * (Prop.Internal.DoorPositionInside.y + 0.5f));
                Door.transform.localScale = new Vector3((1 - Inside.transform.localScale.x) / 2, Inside.transform.localScale.y / Prop.Internal.Size.y, 1);
                break;
            case Direction.Top:
            case Direction.Bottom:
                Prop.Internal.DoorPositionInside = new Vector2Int(Prop.Internal.DoorPositionInside.x, (Prop.Internal.DoorDir == Direction.Bottom) ? 0 : Prop.Internal.Size.y - 1);
                Door.transform.localPosition = new Vector2(-Inside.transform.localScale.x / 2 + (Inside.transform.localScale.x / Prop.Internal.Size.x) * (Prop.Internal.DoorPositionInside.x + 0.5f),
                    ((Prop.Internal.DoorDir == Direction.Top) ? 1 : -1) * Inside.transform.localScale.y / 2 + (1 - Inside.transform.localScale.y) / 4);
                Door.transform.localScale = new Vector3(Inside.transform.localScale.x / Prop.Internal.Size.x, (1 - Inside.transform.localScale.y) / 2, 1);
                break;
        }
        Door.transform.parent = Inside.transform;
    }
    private Vector3 GetLocalPosOutsideDoor()
    {
        return Door.transform.localPosition * 2 - GetLocalPosInsideDoor();
    }
    private Vector3 GetLocalPosInsideDoor()
    {
        return GetLocalWorldPosition(Prop.Internal.DoorPositionInside);
    }
    public void GoInsideMe(BaseBox box)
    {
        GameManager.Instance.RemoveFromLevel(box.GetLevel(), box);
        GameManager.Instance.AddToLevel(GetLevel() - 1, box);
        box.SetLevel(GetLevel() - 1);
        box.Prop.External.Parent = Prop.Internal.Id;
        box.Prop.External.Position = Prop.Internal.DoorPositionInside;
        box.transform.parent = Inside.transform;
        box.transform.DOScale(GetChildScale(), 0.1f).SetEase(Ease.Linear);
        box.transform.DOLocalMove(GetLocalPosOutsideDoor(), 0.1f).SetEase(Ease.Linear).OnComplete(() =>
        {
            box.transform.DOLocalMove(GetLocalPosInsideDoor(), 0.1f).SetEase(Ease.Linear);
        });
    }
    public void GoOutsideMe(BaseBox box)
    {
        box.transform.DOLocalMove(GetLocalPosOutsideDoor(), 0.1f).SetEase(Ease.Linear).OnComplete(() =>
        {
            GameManager.Instance.RemoveFromLevel(box.GetLevel(), box);
            GameManager.Instance.AddToLevel(GetLevel(), box);
            box.SetLevel(GetLevel());
            box.transform.parent = transform.parent;
            box.Prop.External.Parent = Prop.External.Parent;
            box.Prop.External.Position = GetNextMove(Prop.Internal.DoorDir);
            BaseBox boxParent = GameManager.Instance.GetBoxInLevel(GetLevel() + 1, Prop.External.Parent);
            box.transform.DOScale(boxParent.GetChildScale(), 0.1f).SetEase(Ease.Linear);
            box.transform.DOLocalMove(boxParent.GetLocalWorldPosition(box.Prop.External.Position), 0.1f).SetEase(Ease.Linear);
        });
    }
    #endregion
}






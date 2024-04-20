using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using DG.Tweening;
public class Map : MonoBehaviour
{
    public float CamsizePerUnit = 0.6f;
    public BaseBox MainBoxPrefab;
    public BaseBox BoxPrefab;
    public List<ExtendableBoxProperty> Props;
    void Start()
    {
        BaseBox Root = Instantiate(MainBoxPrefab, Vector3.zero, Quaternion.identity);
        GameManager.Instance.MainBox = Root.gameObject.GetComponent<MainBox>();
        Root.Setup(0, Props[0]);
        GameManager.Instance.AddToLevel(0, Root);
        CreateFullMap();
        GameManager.Instance.OnMainLevelChange += OnLevelChange;

        OnLevelChange();
    }

    private void CreateFullMap()
    {
        CreateChild(CreateParent(GameManager.Instance.MainBox));
        CreateChild(CreateParent(CreateParent(GameManager.Instance.MainBox)));

        foreach (var siblingRootLevel in GameManager.Instance.GetBoxes(0))
        {
            CreateChild(siblingRootLevel);
        }
    }
    private void CreateChild(BaseBox ParentBox)
    {
        foreach (var prop in Props)
        {
            if (prop.External.Parent == ParentBox.Prop.Internal.Id && GameManager.Instance.GetBoxInLevel(ParentBox.GetLevel() - 1, prop.Internal.Id) == null)
            {
                BaseBox Box = Instantiate((prop.Internal.Id == 0) ? MainBoxPrefab : BoxPrefab);
                Box.Setup(ParentBox.GetLevel() - 1, prop, ParentBox);
                GameManager.Instance.AddToLevel(ParentBox.GetLevel() - 1, Box);
            }
        }
    }
    private BaseBox CreateParent(BaseBox ChildBox)
    {
        if (GameManager.Instance.GetBoxInLevel(ChildBox.GetLevel() + 1, ChildBox.Prop.External.Parent) != null)
            return GameManager.Instance.GetBoxInLevel(ChildBox.GetLevel() + 1, ChildBox.Prop.External.Parent);
        foreach (var prop in Props)
        {
            if (prop.Internal.Id == ChildBox.Prop.External.Parent)
            {
                BaseBox NewBox = Instantiate((prop.Internal.Id == 0) ? MainBoxPrefab : BoxPrefab);
                NewBox.Setup(ChildBox.GetLevel() + 1, prop);
                NewBox.transform.localScale = new Vector3(ChildBox.transform.localScale.x * prop.Internal.Size.x / NewBox.Inside.transform.localScale.x,
                    ChildBox.transform.localScale.y * prop.Internal.Size.y / NewBox.Inside.transform.localScale.y);

                Vector3 childPos = ChildBox.transform.position;
                ChildBox.transform.parent = NewBox.Inside.transform;
                ChildBox.transform.localPosition = NewBox.GetLocalWorldPosition(ChildBox.Prop.External.Position);
                NewBox.transform.position += (childPos - ChildBox.transform.position);

                GameManager.Instance.AddToLevel(ChildBox.GetLevel() + 1, NewBox);
                return NewBox;
            }
        }
        return null;
    }

    public void OnLevelChange()
    {
        StartCoroutine(DelayOneFrame());
    }

    IEnumerator DelayOneFrame()
    {
        yield return new WaitForEndOfFrame();
        CreateFullMap();
        Camera.main.transform.DOMove(CreateParent(GameManager.Instance.MainBox).transform.position + Vector3.forward * -10, 0.3f).SetEase(Ease.Linear).SetDelay(0.1f);
        Camera.main.DOOrthoSize(CamsizePerUnit * (CreateParent(GameManager.Instance.MainBox).transform.lossyScale.x), 0.3f).SetEase(Ease.Linear).SetDelay(0.1f);

    }
}

using System;
using UnityEngine;

public class UserInput : MonoBehaviour
{

    public static UserInput Instance;
    private void Awake()
    {
        Instance = this;
    }
    float LasttimeInput = -1;
    public Action<Direction> OnUserMove;
    void Update()
    {
        if (Time.time > LasttimeInput + 0.2f)
        {
            if (Input.GetKeyDown(KeyCode.W) || Input.GetKeyDown(KeyCode.UpArrow))
            {
                LasttimeInput = Time.time;
                OnUserMove?.Invoke(Direction.Top);
            }
            if (Input.GetKeyDown(KeyCode.S) || Input.GetKeyDown(KeyCode.DownArrow))
            {
                LasttimeInput = Time.time;
                OnUserMove?.Invoke(Direction.Bottom);
            }
            if (Input.GetKeyDown(KeyCode.D) || Input.GetKeyDown(KeyCode.RightArrow))
            {
                LasttimeInput = Time.time;
                OnUserMove?.Invoke(Direction.Right);
            }
            if (Input.GetKeyDown(KeyCode.A) || Input.GetKeyDown(KeyCode.LeftArrow))
            {
                LasttimeInput = Time.time;
                OnUserMove?.Invoke(Direction.Left);
            }
        }

    }
}

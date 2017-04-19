using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

public class WinNum : MonoBehaviour
{
    public Image img;
    public Text val;
    public Sprite red, green, black;

    public void ShowWinNumber(string color_, int value)
    {
        switch (color_)
        {
            case "red":
                img.sprite = red;
                break;
            case "green":
                img.sprite = green;
                break;
            case "black":
                img.sprite = black;
                break;
        }

        val.text = value.ToString();
    }
}

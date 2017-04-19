using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using PathologicalGames;
using DG.Tweening;


public class WinStrip : MonoBehaviour {
    public static WinStrip instance;
    public List<string> colors = new List<string>();
    public List<WinNum> win_list = new List<WinNum>();

    void Awake()
    {
        instance = this;
    }

    //Add win number to strip;
    //====================================================;
    public void AddWinNumber(int[] data, bool flag)
    {
        for (int i = 0; i < 13; i++)
        {
            string _color = "";
            int num = data[i];
            switch (colors[num])
            {
                case "r":
                    _color = "red";
                    break;
                case "g":
                    _color = "green";
                    break;
                case "b":
                    _color = "black";
                    break;
            }

            int _index = 0;

            switch (colors[data[0]])
            {
                case "r":
                    _index = 1;
                    break;
                case "g":
                    _index = 0;
                    break;
                case "b":
                    _index = 2;
                    break;
            }

            if (flag)
            {
                SoundManager.instance.PlaySound(2);
                LightMap.instance.ChangeColorScarab(_index);
            }
            else
            {
                LightMap.instance.ChangeColorScarab(3);
            }
            win_list[i].ShowWinNumber(_color, num);
        }
    }
}

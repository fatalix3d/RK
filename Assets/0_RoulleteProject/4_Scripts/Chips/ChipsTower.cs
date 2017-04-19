﻿using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using PathologicalGames;

public class ChipsTower : MonoBehaviour
{
    public Image chip_img;
    public Text chip_text;
    public RectTransform chip_transform;

    public List<Sprite> chips_img_a = new List<Sprite>();
    public List<Sprite> chips_img_b = new List<Sprite>();
    public List<Sprite> chips_img_c = new List<Sprite>();
    public List<Sprite> chips_img_d = new List<Sprite>();

    public Sprite chip_win;
    public Sequence winSeq;

    public int chip_value;

    //Отрисовка суммы ставки на фишках;
    //--------------------------------;
    public void UpdateChipValue(int chip_val)
    {

        Vector2 t_pos = new Vector2();
        chip_value = chip_val;

        //------------------5-------------------;
        if (chip_value > 0 && chip_value < 10)
        {
            chip_img.sprite = chips_img_a[0];
            t_pos = new Vector2(0, 0.9f);
        }

        //------------------10-------------------;
        if (chip_value >= 10 && chip_value < 15)
        {
            chip_img.sprite = chips_img_b[0];
            t_pos = new Vector2(0, 0.9f);
        }

        if (chip_value >= 15 && chip_value < 25)
        {
            chip_img.sprite = chips_img_b[1];
            t_pos = new Vector2(0, 4.7f);
        }

        //------------------25-------------------;
        if (chip_value >= 25 && chip_value < 30)
        {
            chip_img.sprite = chips_img_c[0];
            t_pos = new Vector2(0f, 0.9f);
        }

        if (chip_value >= 30 && chip_value < 40)
        {
            chip_img.sprite = chips_img_c[1];
            t_pos = new Vector2(0f, 4.7f);
        }

        if (chip_value >= 40 && chip_value < 50)
        {
            chip_img.sprite = chips_img_c[2];
            t_pos = new Vector2(0f, 8.8f);
        }
        //------------------50-------------------;
        if (chip_value >= 50 && chip_value < 100)
        {
            chip_img.sprite = chips_img_d[0];
            t_pos = new Vector2(0f, 0.9f);
        }

        //------------------100-------------------;
        if (chip_value >= 100 && chip_value < 150)
        {
            chip_img.sprite = chips_img_d[1];
            t_pos = new Vector2(0f, 4.7f);
        }

        //------------------150-------------------;
        if (chip_value >= 150)
        {
            chip_img.sprite = chips_img_d[2];
            t_pos = new Vector2(0f, 8.8f);
        }

        //------------------>999-------------------;
        if (chip_value > 999)
        {
            float t_val = chip_value / 1000f;
            chip_text.fontSize = 25;
            chip_text.text = string.Format("{0:0.###K}", t_val);
        }
        else
        {
            chip_text.fontSize = 37;
            chip_text.text = chip_val.ToString();
        }

        chip_text.rectTransform.localPosition = t_pos;
    }

    public void DespawnChip()
    {
        if (chip_transform != null)
        {
            chip_transform.DOScale(0.01f, 0.15f).OnComplete(() =>
            {
                PoolManager.Pools["chips_pool"].Despawn(chip_transform);
            });
        }
    }
}
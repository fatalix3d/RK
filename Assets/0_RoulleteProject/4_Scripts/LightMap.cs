using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using PathologicalGames;
using DG.Tweening;

public class LightMap : MonoBehaviour {
    #region vars;
    public static LightMap instance;

    public List<Image> main_field_lights = new List<Image>();
    public List<Image> roll_field_lights = new List<Image>();
    private List<int> last_indexes = new List<int>();
    private List<int> last_indexes_roll = new List<int>();
    public List<Image> neigbors_chips = new List<Image>();
    public List<Transform> neigbors_chips_trans = new List<Transform>();
    public List<Text> neigbors_chips_text = new List<Text>();

    public Sequence WinSeq, LoseSeq,WinSeqB, returnTurret;
    public RectTransform turret_trans;
    public RectTransform zeroPoint, playerPoint;
    public RectTransform zeroPoint_small, playerPoint_small, WinText;

    public Color lastClr;

    public List<Image> last_numbers = new List<Image>();
    private Sequence showSeq;
    private int curWin;

    public Transform lastNumberTrans;
    public List<Vector3> lastNumbersPos = new List<Vector3>();

    public List<Vector3> idle_rot = new List<Vector3>();
    public List<Color> scarabColors = new List<Color>();
    public Image scarabImg, jukeImg;
    public Transform palka_a, palka_b;

    public Sequence skarabSeq;
    public Transform juke_trans;
    public Text winText;
    public Sequence winScarabSeq;

    #endregion

    public void AnimateScarab(bool open)
    {
        skarabSeq = DOTween.Sequence();
        if (!open)
        {
            skarabSeq.Insert(0, palka_a.DORotate(idle_rot[0], 0.35f, RotateMode.Fast));
            skarabSeq.Insert(0, palka_b.DORotate(idle_rot[1], 0.35f, RotateMode.Fast));
        }
        else
        {
            skarabSeq = DOTween.Sequence();
            skarabSeq.Insert(0, palka_a.DORotate(new Vector3(0f, 0f, 14.107f), 0.35f, RotateMode.Fast));
            skarabSeq.Insert(0, palka_b.DORotate(new Vector3(0f, 0f, -17.23f), 0.35f, RotateMode.Fast));
        }
    }

    public void ChangeColorScarab(int index_color)
    {
            scarabImg.DOColor(scarabColors[index_color], 0.35f);
    }

    public void AnimateWinNum(bool flag, int num)
    {

        if (winScarabSeq != null)
        {
            winScarabSeq.Kill(false);
        }

        if (!flag)
        {
            winText.DOFade(0f, 0f);
            winText.text = num.ToString();
            winScarabSeq = DOTween.Sequence();
            winScarabSeq.Insert(0f, juke_trans.DORotate(new Vector3(0f, 180f, 0f), 0.35f, RotateMode.Fast));
            winScarabSeq.Insert(0f, jukeImg.DOFade(0f,0.25f));
            winScarabSeq.Insert(0f, winText.DOFade(1f,0.35f));
        }
        else
        {
            winScarabSeq = DOTween.Sequence();
            winScarabSeq.Insert(0f, juke_trans.DORotate(new Vector3(0f, 360f, 0f), 0.35f, RotateMode.Fast));
            winScarabSeq.Insert(0f, jukeImg.DOFade(1f, 0.25f));
            winScarabSeq.Insert(0f, winText.DOFade(0f, 0.35f));
        }
    }
    //INITIALIZE;
    //======================================;
    void Awake()
    {
        instance = this;

        for (int i = 0; i < neigbors_chips.Count; i++)
        {
            neigbors_chips_trans.Add(neigbors_chips[i].GetComponent<Transform>());
        }

        for (int i = 0; i < last_numbers.Count; i++)
        {
            lastNumbersPos.Add(last_numbers[i].GetComponent<Transform>().localPosition);
        }
    }

    //Показать последние номера <выгравшие>;
    //======================================;
    public void ShowLastNumbers(int[] indexes)
    {
        for(int i =0; i < last_numbers.Count; i++)
        {
            last_numbers[i].enabled = false;
            if(i == 1)
            {
                last_numbers[indexes[i]].DOColor(Color.white, 0.75f);
                last_numbers[indexes[i]].GetComponent<Transform>().DOScale(1.0f, 0.75f);
            }
        }


        for (int i = 0; i < indexes.Length; i++)
        {
            if (i < 14)
            {
                last_numbers[indexes[i]].enabled = true;

                if (i == 0)
                {
                    last_numbers[indexes[i]].DOColor(lastClr, 0.75f);
                    last_numbers[indexes[i]].enabled = false;
                    if (lastNumbersPos[indexes[i]] != null)
                    {
                        lastNumberTrans.localPosition = lastNumbersPos[indexes[i]];
                    }
                }
            }
        }
    }

    //2 - Remove lose chips;
    //======================================;
    public void RemoveLose(int index, List<int> win, List<int> lose, List<int> win_n, List<int> lose_n)
    {
        int count_t = lose.Count + lose_n.Count;
        float tick = (1.2f / count_t);
        float timer = 0f;
        float step = 0;

        //create anim seq;
        //-----------------------------------;
        LoseSeq = DOTween.Sequence();
        for (int i = 0; i < lose.Count; i++)
        {
            int l_index = lose[i];
            Cell t_cell = GameManager.instance.t_data.cells_list[l_index];

            //Base;
            //-----------------------------------;
            if (t_cell.cell_chip_instance != null)
            {
                LoseSeq.Insert(timer, t_cell.cell_chip_instance.DOLocalMove(zeroPoint.localPosition, 0.7f, false).OnComplete(() =>
                {
                    PoolManager.Pools["chips_pool"].Despawn(GameManager.instance.t_data.cells_list[l_index].cell_chip_instance);
                    GameManager.instance.t_data.cells_list[l_index].cell_chip_instance = null;
                }));

                timer += tick;
                step += tick;
            }

            //Set;
            //-----------------------------------;
            if (t_cell.cell_extra_chip_instance != null)
            {
                LoseSeq.Insert(timer, t_cell.cell_extra_chip_instance.DOLocalMove(zeroPoint_small.localPosition, 0.7f, false).OnComplete(() =>
                {
                    PoolManager.Pools["chips_pool"].Despawn(GameManager.instance.t_data.cells_list[l_index].cell_extra_chip_instance);
                    GameManager.instance.t_data.cells_list[l_index].cell_extra_chip_instance = null;
                }));
                timer += tick;
                step += tick;
            }
        }

        //Neighbors;
        //-----------------------------------;
        for (int i = 0; i < lose_n.Count; i++)
        {
            int l_index = lose_n[i];
            Cell t_cell = GameManager.instance.t_data.cells_list[l_index];

            if (t_cell.cell_neighbors_knot_instance != null)
            {
                LoseSeq.Insert(timer, t_cell.cell_neighbors_knot_instance.DOLocalMove(zeroPoint_small.localPosition, 0.7f, false).OnComplete(() =>
                {
                    PoolManager.Pools["chips_pool"].Despawn(GameManager.instance.t_data.cells_list[l_index].cell_neighbors_knot_instance);
                    GameManager.instance.t_data.cells_list[l_index].cell_neighbors_knot_instance = null;
                }));

                timer += tick;
                step += tick;
            }
        }


        //Add callback to end of anim;
        LoseSeq.AppendInterval(0.5f);
        LoseSeq.AppendCallback(() => { RemoveWinChips(index, win, lose, win_n, lose_n); });
    }

    //3 - Remove winners chips;
    //======================================;
    public void RemoveWinChips(int index, List<int> win, List<int> lose, List<int> win_n, List<int> lose_n)
    {
        List<int> win_chips = new List<int>();

        float tick = (1.2f / win.Count);
        float timer = 0f;
        float step = 0;

        //create anim seq;
        WinSeqB = DOTween.Sequence();

        //Find win chips;
        int counter = 0;
        float total_win_ = 0;
        float win_step_ = 0;

        //Search basic chips;
        //-----------------------------------;
        for (int i = 0; i < win.Count; i++)
        {
            int l_index = win[i];
            Cell t_cell = GameManager.instance.t_data.cells_list[l_index];

            //check for basic chip;
            if (t_cell.cell_chip_instance != null)
            {
                counter++;
                //Debug.Log("ADD WIN CHIP : " + t_cell.cell_label + " (BASIC)");
            }

            //check for set chip;
            if (t_cell.cell_extra_chip_instance != null)
            {
                counter++;
                //Debug.Log("ADD WIN CHIP : " + t_cell.cell_label + " (SET)");
            }
        }

        //Search n knot chips;
        //-----------------------------------;
        for (int i = 0; i < win_n.Count; i++)
        {
            int l_index = win_n[i];
            Cell t_cell = GameManager.instance.t_data.cells_list[l_index];

            //check for n-knot chip;
            if (t_cell.cell_neighbors_knot_instance != null)
            {
                counter++;
                Debug.Log("ADD WIN CHIP : " + t_cell.cell_label + " (nKNOT)");
            }
        }

        total_win_ = GameManager.instance.p_last_win;
        win_step_ = total_win_ / counter;
        GameManager.instance.p_last_win = 0;
        //Debug.Log("Total win : " + total_win_ + " / Total  chips : " + counter.ToString() + "/ Step : " + win_step_.ToString());

        //Create SEQUENCE;
        //-----------------------------------;
        for (int i = 0; i < win.Count; i++)
        {
            int l_index = win[i];
            Cell t_cell = GameManager.instance.t_data.cells_list[l_index];
            
            //Basic;
            if (t_cell.cell_chip_instance != null)
            {
                WinSeqB.Insert(timer, t_cell.cell_chip_instance.DOLocalMove(playerPoint.localPosition, 0.5f, false).OnComplete(() =>
                {
                    PoolManager.Pools["chips_pool"].Despawn(GameManager.instance.t_data.cells_list[l_index].cell_chip_instance);
                    GameManager.instance.t_data.cells_list[l_index].cell_chip_instance = null;

                    WinText.DOScale(1.25f, 0.5f).OnComplete(()=>
                    {
                        WinText.DOScale(1.0f, 0.15f);
                    });

                    GameManager.instance.p_last_win += win_step_;
                    GameManager.instance.UpdateLabels();

                }));

                timer += tick;
                step += tick;
            }

            //Set;
            if (t_cell.cell_extra_chip_instance != null)
            {
                WinSeqB.Insert(timer, t_cell.cell_extra_chip_instance.DOLocalMove(playerPoint_small.localPosition, 0.5f, false).OnComplete(() =>
                {
                    PoolManager.Pools["chips_pool"].Despawn(GameManager.instance.t_data.cells_list[l_index].cell_extra_chip_instance);
                    GameManager.instance.t_data.cells_list[l_index].cell_extra_chip_instance = null;
                    GameManager.instance.p_last_win += win_step_;
                    GameManager.instance.UpdateLabels();

                    WinText.DOScale(1.25f, 0.5f).OnComplete(() =>
                    {
                        WinText.DOScale(1.0f, 0.15f);
                    });

                }));

                timer += tick;
                step += tick;
            }
        }

        //Neighbors;
        for (int i = 0; i < win_n.Count; i++)
        {
            int l_index = win_n[i];
            Cell t_cell = GameManager.instance.t_data.cells_list[l_index];

            if (t_cell.cell_neighbors_knot_instance != null)
            {
                WinSeqB.Insert(timer, t_cell.cell_neighbors_knot_instance.DOLocalMove(playerPoint_small.localPosition, 0.5f, false).OnComplete(() =>
                {
                    PoolManager.Pools["chips_pool"].Despawn(GameManager.instance.t_data.cells_list[l_index].cell_neighbors_knot_instance);
                    GameManager.instance.t_data.cells_list[l_index].cell_neighbors_knot_instance = null;
                    GameManager.instance.p_last_win += win_step_;
                    GameManager.instance.UpdateLabels();

                    WinText.DOScale(1.25f, 0.5f).OnComplete(() =>
                    {
                        WinText.DOScale(1.0f, 0.15f);
                    });

                }));

                timer += tick;
                step += tick;
            }
        }

        //Add callback to end of anim;
        WinSeqB.AppendInterval(step);
        WinSeqB.AppendCallback(ReturnTurret);
    }

    // 4 - Return turret;
    //======================================;
    public void ReturnTurret()
    {
        //create anim seq;
        returnTurret = DOTween.Sequence();
        returnTurret.Append(turret_trans.DOScale(1.3f, 0.15f).SetEase(Ease.InOutSine));
        returnTurret.Append(turret_trans.DOScale(0.01f, 0.5f).SetEase(Ease.InOutSine)).OnComplete(() =>
        {
            turret_trans.transform.localScale = new Vector3(1.0f, 1.0f, 1.0f);
            turret_trans.localPosition = new Vector2(0.7999833f, 678.1582f);
        });

        //Add callback to end of anim;
        returnTurret.AppendCallback(() =>
        {
            GameManager.instance.SaveLastBet();     //Save last bet values;
            GameManager.instance.ClearGameData();   //Clear all bet values;
            GameManager.instance.UpdateCredit();    //Update credits;
        });
    }

    //Показать выгравший номер;
    //======================================;
    public void MoveTowerToCell(int index, List<int> win, List<int> lose, List<int> win_n, List<int> lose_n)
    {
        //Debug.Log("Move turret event => " + index.ToString());
        WinSeq = DOTween.Sequence();

        //Move turret to chip;
        //--------------------------------------;
        WinSeq.AppendInterval(0.2f);
        WinSeq.Append(turret_trans.DOMove(main_field_lights[index].transform.position, 1.0f).SetEase(Ease.InOutSine));
        WinSeq.AppendInterval(1.0f);

        //Remove lose chips;
        //--------------------------------------;
        WinSeq.AppendCallback(() => {
            RemoveLose(index,win, lose, win_n, lose_n);
        });


        string win_ = "";
        for(int i =0; i < win.Count; i++)
        {
            win_ += ":" + win[i].ToString() + ",";
        }

        string los_ = "";
        for (int i = 0; i < lose.Count; i++)
        {
            los_ += ":" + lose[i].ToString() + ",";
        }
    }

    //Выделение номеров (основное поле);
    //======================================;
    public void HighLightCells(List<int> indexes)
    {
        for (int i = 0; i < indexes.Count; i++)
        {
            main_field_lights[indexes[i]].enabled = true;
            last_indexes.Add(indexes[i]);
        }
    }

    //Выкл. подсветки номеров (основное поле);
    //======================================;
    public void DeLightCells()
    {
        if (last_indexes.Count > 0)
        {
            for (int i = 0; i < last_indexes.Count; i++)
            {
                main_field_lights[last_indexes[i]].enabled = false;
            }

            last_indexes.Clear();
        }
    }

    //Выделение номеров (поле соседей);
    //======================================;
    public void HighLightRollCells(List<int> indexes, int set_index, bool _set)
    {
        for (int i = 0; i < indexes.Count; i++)
        {
            roll_field_lights[indexes[i]].enabled = true;
            last_indexes_roll.Add(indexes[i]);
        }

        if (set_index > 0)
        {
            main_field_lights[set_index].enabled = true;
            last_indexes.Add(set_index);
        }
    }

    //Выкл. подсветки номеров (поле соседей);
    //======================================;
    public void DeLightRollCells()
    {
        if (last_indexes_roll.Count > 0)
        {
            for (int i = 0; i < last_indexes_roll.Count; i++)
            {
                roll_field_lights[last_indexes_roll[i]].enabled = false;
            }

            last_indexes_roll.Clear();
        }
    }

    //Показать соседей;
    //======================================;
    public void NeighborsShow(List<int> indexes)
    {
        for (int i = 0; i < indexes.Count; i++)
        {
            if (neigbors_chips[indexes[i]].enabled == false)
            {
                neigbors_chips_trans[indexes[i]].localScale = new Vector3(0.01f, 0.01f, 0.01f);
                neigbors_chips[indexes[i]].enabled = true;
                neigbors_chips_trans[indexes[i]].DOScale(1.0f, 0.3f);
            }
        }
    }

    //Показать соседа;
    //======================================;
    public void NeighborShow(int index)
    {
        if (neigbors_chips[index].enabled == false)
        {
            neigbors_chips_trans[index].localScale = new Vector3(0.01f, 0.01f, 0.01f);
            neigbors_chips[index].enabled = true;
            neigbors_chips_trans[index].DOScale(1.0f, 0.3f);
        }
    }

    //Загасить соседей >:();
    //======================================;
    public void NeighborsHide(int index)
    {
        Image t_img = neigbors_chips[index];
        //neigbors_chips_trans[index].DOScale(0.01f, 0.3f).OnComplete(() => HideChip(t_img));
        HideChip(t_img);
    }

    //Обновление соседей <!>;
    //======================================;
    public void UpdateNeighbors(int pos, int value)
    {
        if (value > 0)
        {

        }
        else
        {
            NeighborsHide(pos);
        }
    }

    void HideChip(Image img)
    {
        img.enabled = false;
    }

    //Удалить соседей <!>;
    //======================================;
    public void RemoveNeighborsChips()
    {
        for(int i =0; i < neigbors_chips.Count; i++)
        {
            Image t_img = neigbors_chips[i];
            neigbors_chips_trans[i].DOScale(0.01f, 0.3f).OnComplete(() => HideChip(t_img));
        }
    }

}

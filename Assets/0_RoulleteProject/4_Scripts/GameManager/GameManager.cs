using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using PathologicalGames;
using DG.Tweening;
using Newtonsoft.Json;

public class GameManager : MonoBehaviour {

    //Переменные;
    //====================================================;
    #region vars;
    public static GameManager instance;
    public List<BetChips> betChips = new List<BetChips>();

    //Nieghbors;
    public int n_count = 2;
    public int cur_n_id = 0;
    public List<NeighborsCell> neigbors = new List<NeighborsCell>();

    public List<int> cur_neighbors = new List<int>();
    public List<int> neighbors_pos = new List<int>();

    //Core data;
    public CellList data;
    public CellList t_data;
    private Cell cur_cell, last_cell;
    public bool cur_cell_isRoll = false;
    public bool last_cell_isRoll = false;
    public int win_number = 0;

    //Bets;
    public int cur_bet_value = 1;
    public int cur_chip_selection = 0;

    //Chips;
    public Transform main_field_root, roll_field_root;
    public Transform chips_instance;

    //CREDITS;
    public float p_balance = 0;
    public float p_credit = 0;
    public float p_denomination = 10.0f;
    public float p_last_win = 0;

    public Text g_balance, g_credit, g_win, last_win;
    public Popup popupWindow, popupState;

    //History;
    public UndoList history_list;
    public UndoCell last_bet_data;
    public bool game_lock = false;
    
    //Tweens;
    public Sequence creditSeq, _winSeq, timerSeq;
    public Sequence timer_tween;
    public bool labels_update = false;
    public int cur_balance, cur_win;

    //Selector;
    public bool selector_flag = false;
    public Text popupText;
    public List<Color> stateColors = new List<Color>();
    public Image progressBar, glowImg, glowCloud;
    public RectTransform progressBar_trans, glowImg_trans, glowCloudTrans;
    public ScrollRect strip_rect;

    //Global lock;
    public bool global_lock = false;
    public CanvasGroup global_lock_group;
    public GameObject global_lock_obj;
    public Text global_lock_text;
    public Image transaction_logo;

    //Keyboard data;
    public Hashtable key_code = new Hashtable();

    //Timer;
    public Image timerProgress;
    public List<Color> txt_color = new List<Color>();
    public Text InfoMSG, cur_bet_lbl;

    //Neighbors;
    public bool nSet = false;
    public int nSetId = 99;

    //Limit to bets;
    public float max_field_bet = 25; //SET 5
    public float max_cell_bet = 10;  //SET 6
    public float min_cell_bet = 10;  //SET 7
    public float min_out_bet = 10;  //SET 8

    public void SetDataValues(float set5, float set6, float set7, float set8)
    {
        max_field_bet = set5;
        max_cell_bet = set6;
        min_cell_bet = set7;
        min_out_bet = set8;
    }
    #endregion

    //Start;
    //======================================;
    void Start()
    {
        t_data = ScriptableObject.CreateInstance<CellList>();

        //Заполняем поля;
        for (int i = 0; i < data.cells_list.Count; i++)
        {
            t_data.cells_list.Add(new Cell()
            {
                cell_id = data.cells_list[i].cell_id,
                cell_key = data.cells_list[i].cell_key,
                cell_label = data.cells_list[i].cell_label,
                cell_selected = data.cells_list[i].cell_selected,
                cell_set_light_index = data.cells_list[i].cell_set_light_index,
                cell_close_numbers = new List<int>(data.cells_list[i].cell_close_numbers),
                cell_close_id = new List<int>(data.cells_list[i].cell_close_id),
                cell_chip_instance = data.cells_list[i].cell_chip_instance,
                cell_chips_pos = new List<Vector2>(data.cells_list[i].cell_chips_pos),
                cell_roll_chips_pos = data.cells_list[i].cell_roll_chips_pos,
                cell_bet_value = data.cells_list[i].cell_bet_value,
                cell_roll_chip = new List<bool>(data.cells_list[i].cell_roll_chip),
                cell_extra_chip_instance = data.cells_list[i].cell_extra_chip_instance,
                cell_ext_chip = data.cells_list[i].cell_ext_chip,
            });

            //Add key data;
            key_code.Add(data.cells_list[i].cell_key, data.cells_list[i].cell_id);
        }

        for (int i = 0; i < LightMap.instance.roll_field_lights.Count; i++)
        {
            for (int j = 0; j < neigbors.Count; j++)
            {
                if (neigbors[j].n_field_pos == i)
                {
                    neigbors[j].n_pos = LightMap.instance.roll_field_lights[i].rectTransform.localPosition;
                    break;
                }
            }
        }

        UpdateLabels();
        Screen.sleepTimeout = SleepTimeout.NeverSleep;
    }

    //Read Keyboard Button;
    //======================================;
    public void SimKeyCode(string dataIndex)
    {
        if (!string.IsNullOrEmpty(dataIndex))
        {
            if (!game_lock)
            {
                int kIndex = (int)key_code[dataIndex];
                SelectCell(kIndex, false, 0);
            }
        }
    }

    //Global Lock Event;
    //======================================;
    public void GlobalLockEvent(string msg, bool flag)
    {
        global_lock = true;
        global_lock_text.text = msg;
        global_lock_obj.SetActive(true);

        if (flag)
        {
            transaction_logo.enabled = true;
        }
        else
        {
            transaction_logo.enabled = false;
        }
    }

    //Global Unlock Event;
    //======================================;
    public void GlobalUnlockEvent()
    {
        global_lock = false;
        global_lock_obj.SetActive(false);
    }

    //Invalid Spin;
    //======================================;
    public void InvalidSpin()
    {
        Debug.Log("Invalid spin");
        CancelAllBetsL();
        GameLockEvent(null);
        popupState.ShowPopup("<size=50>INVALID SPIN!</size>");
        InfoMSG.color = txt_color[1];
        InfoMSG.text = "INVALID SPIN!";
    }

    //Перетекание счетчиков <анимация>;
    //======================================;
    public void UpdateCredit()
    {
        if (cur_win > 0)
        {
            labels_update = true;

            //Credit animation;
            creditSeq = DOTween.Sequence();
            creditSeq.Append(g_balance.rectTransform.DOScale(new Vector3(1.5f, 1.5f, 1.1f), 0.15f));
            creditSeq.Append(g_balance.rectTransform.DOScale(new Vector3(1.2f, 1.2f, 1.2f), 0.15f));
            creditSeq.SetLoops(50);

            //Win animation;
            _winSeq = DOTween.Sequence();
            _winSeq.Append(last_win.rectTransform.DOScale(new Vector3(1.5f, 1.5f, 1.1f), 0.15f));
            _winSeq.Append(last_win.rectTransform.DOScale(new Vector3(1.2f, 1.2f, 1.2f), 0.15f));
            _winSeq.SetLoops(50);

            DOTween.To(() => p_balance, x => p_balance = x, cur_balance, 3.0f);
            DOTween.To(() => p_last_win, x => p_last_win = x, 0, 3.0f).OnComplete(() =>
            {
                labels_update = false;
                creditSeq.Complete();
                _winSeq.Complete();
                p_balance = cur_balance;
                UpdateLabels();
            });
        }
    }

    //Update;
    //======================================;
    void Update()
    {
        if (labels_update)
        {
            UpdateLabels();
        }
    }

    //Обновить счетчики;
    //======================================;
    public void UpdateLabels()
    {
        p_credit = p_balance / p_denomination;

        //Обновить счетчики;
        g_balance.text = string.Format("{0:## ### ##0}", p_balance);
        g_credit.text = string.Format("{0:## ### ##0}", p_credit);

        if (p_last_win > 0)
        {
            last_win.text = string.Format("{0:## ### ##0}", p_last_win);
        }
        else
        {
            last_win.text = "0";
        }

        //Посчитать общую ставку;
        int total_val = 0;
        for (int i = 0; i < t_data.cells_list.Count; i++)
        {
            if (t_data.cells_list[i].cell_bet_value > 0)
            {
                total_val += t_data.cells_list[i].cell_bet_value;
            }
        }

        if (total_val > 0)
        {
            g_win.text = string.Format("{0:##,###,###}", total_val);
        }
        else
        {
            g_win.text = "0";
        }
    }

    void Awake()
    {
        instance = this;
    }

    //Очистить текущий игровой сеанс;
    //======================================;
    public void ClearGameData()
    {
        cur_n_id = 0;
        cur_neighbors.Clear();
        neighbors_pos.Clear();
        history_list.undo_states.Clear();

        //Обнулить значения соседей;
        for (int i = 0; i < neigbors.Count; i++)
        {
            neigbors[i].n_value = 0;
            neigbors[i].n_root_number = 0;
            neigbors[i].n_bet_value = 0;
            neigbors[i].n_close_id.Clear();
            neigbors[i].n_close_numbers.Clear();
            neigbors[i].n_pos_id.Clear();
        }

        //Выключить фишки поля;
        for (int i = 0; i < t_data.cells_list.Count; i++)
        {
            t_data.cells_list[i].cell_chip_instance = null;
            t_data.cells_list[i].cell_bet_value = 0;
            t_data.cells_list[i].cell_selected = false;

            if (t_data.cells_list[i].cell_extra_chip_instance != null)
            {
                PoolManager.Pools["chips_pool"].Despawn(t_data.cells_list[i].cell_extra_chip_instance);
                t_data.cells_list[i].cell_extra_chip_instance = null;
            }
            if (t_data.cells_list[i].cell_neighbors_knot_instance != null)
            {
                PoolManager.Pools["chips_pool"].Despawn(t_data.cells_list[i].cell_neighbors_knot_instance);
                t_data.cells_list[i].cell_neighbors_knot_instance = null;
            }
        }

        UpdateLabels();

        //Выключить фишки соседей;
        LightMap.instance.RemoveNeighborsChips();
    }

    //Блокировка / Разблокировка поля;
    //======================================;
    public void GameLockEvent(string msg = null)
    {
        if (game_lock == false)
        {
            game_lock = true;
            ButtonManager.instance.LockEvent();
            LightMap.instance.AnimateScarab(false);
            //popupText.DOColor(stateColors[2], 0f);

            if (timer_tween != null && timer_tween.IsActive())
            {
                timer_tween.Kill(true);
            }

            if (timerSeq != null)
            {
                timerSeq.Kill(true);
            }

            //progressBar_trans.DOSizeDelta(new Vector2(595f, 76f), 0.0f);
            //progressBar.DOFade(1.0f, 0.75f);
            //popupState.ShowPopup("<size=50>-STOP-</size>");
            InfoMSG.color = txt_color[1];
            InfoMSG.text = (msg != null) ? msg : "NO MORE BETS";
        }

        game_lock = true;
    }

    public void GameUnlockEvent()
    {
        game_lock = false;
        ButtonManager.instance.UnlockEvent();
        LightMap.instance.AnimateScarab(true);
        LightMap.instance.ChangeColorScarab(3);
        popupText.DOColor(stateColors[2], 0f);

        //ShowCloud(true);
        //popupState.ShowPopup("<size=50>Ставки открыты</size>");
        InfoMSG.color = txt_color[0];
        InfoMSG.text = "LAY YOUR BETS";
        //ButtonManager.instance.betChips[cur_chip_selection].MyClick();
        //ButtonManager.instance.betChips[cur_chip_selection].isSelected = true;
    }

    //Выбор номера на поле;
    //======================================;
    public void SelectCell(int id, bool isRoll, int n_id)
    {
        if (!game_lock)
        {
            cur_n_id = n_id;
            cur_cell = t_data.cells_list[id];

            //IF LAST CELL != CURENT CELL;
            //-------------------------------------------;
            if (last_cell != null)
            {
                if (last_cell.cell_id != cur_cell.cell_id)
                {
                    last_cell.cell_selected = false;
                    LightMap.instance.DeLightCells();
                }
            }

            last_cell = cur_cell;
            last_cell_isRoll = isRoll;

            cur_cell.cell_selected = true;
            ThrowBet();
        }
    }

    //Сделать ставку <Event>;
    //======================================;
    public void ThrowBet()
    {
        SoundManager.instance.PlaySound(0);

        if (cur_cell != null && cur_cell.cell_selected)
        {
            if (!cur_cell_isRoll)
            {
                //THROW STANDART;
                if(cur_bet_value > 0)
                SetBetToCell(cur_cell, cur_bet_value);
            }
            else
            {
                //THROW TO NEIGHBORS;
                SetToNeighbors();
            }
        }
    }

    //Тип ставки;
    //======================================;
    public void SetBetToCell(Cell _cell, int value)
    {
        var t_val = _cell.cell_bet_value + value;

        if (t_val > 0)
        {
            if (!_cell.cell_ext_chip)
            {
                SetBet(_cell, value, _cell.cell_close_id);
            }
            else
            {
                SetBet(_cell, value, _cell.cell_close_id);
            }
        }
    }

    //Set Bet to Last Chip;
    public void SetToLastBet(int summ)
    {
        if (!game_lock)
        {
            if (last_cell != null)
            {
                SetBet(last_cell, summ, last_cell.cell_close_id);
            }
        }
    }

    //Поставить ставку;
    //======================================;
    public void SetBet(Cell _cell, int _value, List<int> indexes)
    {
        int t_all_sum = 0;
        int t_max_cell = 0;
        int ttb = 0;

        //Min max values;
        float min_val = 0;
        float max_val = 0;

        //Set min/max values based on cell type;
        //-------------------------------------------;
        if (_cell.cell_id >= 145 && _cell.cell_id <= 156)
        {
            min_val = min_out_bet;
            max_val = max_cell_bet;
        }
        else
        {
            min_val = min_cell_bet;
            max_val = max_cell_bet;
        }

        //Set to basic bet;
        //-------------------------------------------;
        if (!_cell.cell_ext_chip)
        {
            if (_value <= p_credit)
            {
                t_max_cell = _cell.cell_bet_value + _value;
                ttb = CalcCurBet();

                if (t_max_cell >= min_val && t_max_cell <= max_val)
                {
                    if ((ttb + _value) <= max_field_bet)
                    {
                        //Debug.Log("TOTAL : " + ttb + " + " + _value + " = " + (ttb + _value) + ", OK!");
                        _cell.cell_bet_value += _value;
                        SetBasicChip(_cell, _value);
                        p_credit -= _value;
                        p_balance -= _value * p_denomination;
                        UpdateLabels();
                        SaveHistory(_value);
                    }
                    else
                    {
                        popupWindow.ShowPopup("<size=50>Макс. ставка на поле</size>"
                        + "\n<size=35>Вы ставите (<color=yellow>"
                        + (ttb + _value).ToString()
                        + "</color>), ставка на поле (<color=yellow>"
                        + max_field_bet.ToString()
                        + "</color>);</size>"
                        );
                    }
                }
                else
                {
                    popupWindow.ShowPopup("<size=50>Недопустимая ставка.</size>");
                }
            }
            else
            {
                popupWindow.ShowPopup("<size=50>Не хватает кредитов</size>"
                + "\n<size=35>Вы ставите (<color=yellow>"
                + _value.ToString()
                + "</color>), доступный кредит (<color=yellow>"
                + p_credit.ToString()
                + "</color>);</size>"
                );
            }
        }
        else
        {
            //Set to set bet;
            //-------------------------------------------;
            t_all_sum = 0;

            //Calculate operation summ;
            for (int i = 0; i < indexes.Count; i++)
            {
                Cell tCell = t_data.cells_list[indexes[i]];
                t_max_cell = tCell.cell_bet_value + _value;

                if (t_max_cell <= max_cell_bet)
                {
                    t_all_sum += _value;
                }
            }

            ttb = CalcCurBet();

            if (t_all_sum > 0 && t_all_sum <= p_credit)
            {
                if ((ttb + t_all_sum) <= max_field_bet)
                {
                    for (int i = 0; i < indexes.Count; i++)
                    {
                        Cell tCell = t_data.cells_list[indexes[i]];
                        t_max_cell = tCell.cell_bet_value + _value;

                        if (t_max_cell <= max_cell_bet)
                        {
                            tCell.cell_bet_value += _value;
                            SetBasicChip(tCell, _value);
                            p_credit -= _value;
                            p_balance -= _value * p_denomination;
                        }
                    }

                    //SetSetChip(_cell, t_all_sum);
                    UpdateLabels();
                    SaveHistory(t_all_sum);
                }
                else
                {
                    popupWindow.ShowPopup("<size=50>Макс. ставка на поле</size>"
                    + "\n<size=35>Вы ставите (<color=yellow>"
                    + (ttb + t_all_sum).ToString()
                    + "</color>), ставка на поле (<color=yellow>"
                    + max_field_bet.ToString()
                    + "</color>);</size>"
                    );
                }
            }
            else
            {
                //popupWindow.ShowPopup("<size=50>Не хватает кредитов</size>"
                //+ "\n<size=35>Вы ставите (<color=yellow>"
                //+ t_all_sum.ToString()
                //+ "</color>), доступный кредит (<color=yellow>"
                //+ p_credit.ToString()
                //+ "</color>);</size>"
                //);
            }
        }
    }

    //Удвоить все ставки;
    //======================================;
    public void DoubleAllBets()
    {
        int ttb = 0;
        ttb = CalcCurBet();

        if (!game_lock)
        {
            //Calculate sum;
            int t_all_sum = 0;
            for (int i = 0; i < t_data.cells_list.Count; i++)
            {
                Cell tCell = t_data.cells_list[i];
                if (tCell.cell_bet_value > 0)
                {
                    t_all_sum += tCell.cell_bet_value;
                }
            }

            //Set new values;
            if (t_all_sum <= p_credit)
            {
                if ((t_all_sum + ttb) <= max_field_bet)
                {
                    for (int i = 0; i < t_data.cells_list.Count; i++)
                    {
                        Cell tCell = t_data.cells_list[i];

                        if (tCell.cell_bet_value > 0)
                        {
                            tCell.cell_bet_value += tCell.cell_bet_value;

                            if (tCell.cell_chip_instance != null)
                            {
                                ChipsTower trw = tCell.cell_chip_instance.GetComponent<ChipsTower>();
                                int cur_chip_val = trw.chip_value * 2;
                                trw.UpdateChipValue(cur_chip_val);
                            }

                            if (tCell.cell_extra_chip_instance != null)
                            {
                                ChipsTower trw = tCell.cell_extra_chip_instance.GetComponent<ChipsTower>();
                                int cur_chip_val = trw.chip_value * 2;
                                trw.UpdateChipValue(cur_chip_val);
                            }

                            if (tCell.cell_neighbors_knot_instance != null)
                            {
                                ChipsTower trw = tCell.cell_neighbors_knot_instance.GetComponent<ChipsTower>();
                                int cur_chip_val = trw.chip_value * 2;
                                trw.UpdateChipValue(cur_chip_val);
                            }
                        }

                        //Check for set chips;
                        if (tCell.cell_extra_chip_instance != null)
                        {
                            ChipsTower trw = tCell.cell_extra_chip_instance.GetComponent<ChipsTower>();
                            int cur_chip_val = trw.chip_value * 2;
                            trw.UpdateChipValue(cur_chip_val);
                        }
                    }

                    //Update credit;
                    p_credit -= t_all_sum;
                    p_balance -= t_all_sum * p_denomination;
                    UpdateLabels();
                    SaveHistory(t_all_sum);
                }
                else
                {
                    popupWindow.ShowPopup("<size=50>Макс. ставка на поле</size>"
                    + "\n<size=35>Вы ставите (<color=yellow>"
                    + (ttb + t_all_sum).ToString()
                    + "</color>), ставка на поле (<color=yellow>"
                    + max_field_bet.ToString()
                    + "</color>);</size>"
                    );
                }
            }
            else
            {
                popupWindow.ShowPopup("<size=50>Не хватает кредитов</size>"
                + "\n<size=35>Вы ставите (<color=yellow>"
                + t_all_sum.ToString()
                + "</color>), доступный кредит (<color=yellow>"
                + p_credit.ToString()
                + "</color>);</size>"
                );
            }
        }
    }

    //Поставить на соседей;
    //======================================;
    public void SetToNeighbors()
    {
        int t_all_sum = 0;
        int n_root = cur_neighbors[0];
        int n_id;

        int ttb = 0;
        int t_max_cell = 0;


        //Calculate operation summ;
        //-------------------------------------------;
        for (int i = 0; i < cur_neighbors.Count; i++)
        {
            var n_map_id = cur_neighbors[i];
            n_id = neigbors[n_map_id].n_index;
            t_max_cell = t_data.cells_list[n_id].cell_bet_value + cur_bet_value;

            if (t_max_cell <= max_cell_bet)
            {
                t_all_sum += cur_bet_value;
            }
        }
        ttb = CalcCurBet();

        //Делаем ставку на закрываемые номера;
        //-------------------------------------------;
        if (t_all_sum > 0 && t_all_sum <= p_credit)
        {
            if ((t_all_sum + ttb) <= max_field_bet)
            {
                //Manage current neighbors state;
                neigbors[n_root].n_root_number = cur_n_id;                      //Set a root neighbors bet;
                neigbors[n_root].n_close_id.Clear();                            //Clear root => close id, map;
                neigbors[n_root].n_close_numbers.Clear();                       //Clear root => close num map;
                neigbors[n_root].n_pos_id.Clear();

                //Check each neighbors for bet value;
                for (int i = 0; i < cur_neighbors.Count; i++)
                {
                    var n_map_id = cur_neighbors[i];
                    n_id = neigbors[n_map_id].n_index;
                    var n_map_pos = neighbors_pos[i];

                    neigbors[n_root].n_close_id.Add(n_id);                      //Root => add close id;
                    neigbors[n_root].n_close_numbers.Add(n_map_pos);            //Root => add close num;
                    neigbors[n_root].n_pos_id.Add(n_map_id);

                    //Get current cell bet value;
                    t_max_cell = t_data.cells_list[n_id].cell_bet_value + cur_bet_value;

                    if (t_max_cell <= max_cell_bet)
                    {
                        //Root => create bet to this cell;
                        t_data.cells_list[n_id].cell_bet_value += cur_bet_value;

                        //Root => save n_value;
                        neigbors[n_map_id].n_value += cur_bet_value;
                    }
                }

                neigbors[n_root].n_bet_value = neigbors[n_root].n_value;

                //Create a neighbors knot chip;
                //-------------------------------------------;
                n_id = neigbors[n_root].n_index;
                SetNeighborsKnot(t_data.cells_list[n_id], t_all_sum, neigbors[n_root].n_pos);
                p_credit -= t_all_sum;
                p_balance -= t_all_sum * p_denomination;
                UpdateLabels();
                SaveHistory(t_all_sum);
                LightMap.instance.NeighborsShow(neighbors_pos);
            }
            else
            {
                popupWindow.ShowPopup("<size=50>Макс. ставка на поле</size>"
                + "\n<size=35>Вы ставите (<color=yellow>"
                + (ttb + t_all_sum).ToString()
                + "</color>), ставка на поле (<color=yellow>"
                + max_field_bet.ToString()
                + "</color>);</size>"
                );
            }
        }
        else
        {
            //Show error message;
            popupWindow.ShowPopup("<size=50>Не хватает кредитов</size>"
            + "\n<size=35>Вы ставите (<color=yellow>"
            + t_all_sum.ToString()
            + "</color>), доступный кредит (<color=yellow>"
            + p_credit.ToString()
            + "</color>);</size>"
            );
        }
    }

    //Записать последнее действие;
    //======================================;
    public void SaveHistory(int add_sum)
    {
        int index;

        //Create zero point in history;
        //-----------------------------------;
        if (history_list.undo_states.Count == 0)
        {
            history_list.undo_states.Add(new UndoCell());
            index = history_list.undo_states.Count - 1;
            for (int i = 0; i < t_data.cells_list.Count; i++)
            {
                Cell tCell = t_data.cells_list[i];
                history_list.undo_states[index].u_cell_id.Add(tCell.cell_id);
                history_list.undo_states[index].u_cell_bet_value.Add(0);
                history_list.undo_states[index].u_cell_chip_value.Add(0);
                history_list.undo_states[index].u_cell_ext_value.Add(0);
            }

            for (int i = 0; i < neigbors.Count; i++)
            {
                history_list.undo_states[index].u_neighbors_value.Add(0);
                history_list.undo_states[index].u_neighbors_knot_value.Add(0);
                history_list.undo_states[index].u_neighbors_knot_pos.Add(0);
            }

            //Save n - data;
            history_list.undo_states[index].u_neigbors_data = new List<NeighborsCell>();
            history_list.undo_states[index].u_last_cell_data = null;
        }

        history_list.undo_states.Add(new UndoCell());
        index = history_list.undo_states.Count - 1;

        //Save basic values;
        //-----------------------------------;
        for (int i = 0; i < t_data.cells_list.Count; i++)
        {
            Cell tCell = t_data.cells_list[i];
            history_list.undo_states[index].u_cell_id.Add(tCell.cell_id);
            history_list.undo_states[index].u_cell_bet_value.Add(tCell.cell_bet_value);

            //Basic;
            //-----------------------------------;
            if (tCell.cell_chip_instance != null)
            {
                history_list.undo_states[index].u_cell_chip_value.Add(tCell.cell_chip_tower.chip_value);
            }
            else
            {
                history_list.undo_states[index].u_cell_chip_value.Add(0);
            }

            //Extra bet;
            //-----------------------------------;
            if (tCell.cell_extra_chip_instance != null)
            {
                history_list.undo_states[index].u_cell_ext_value.Add(tCell.ext_cell_chip_tower.chip_value);
            }
            else
            {
                history_list.undo_states[index].u_cell_ext_value.Add(0);
            }
        }

        history_list.undo_states[index].total_bet_val = add_sum;

        //Save neighbors values;
        //-----------------------------------;
        for (int i = 0; i < neigbors.Count; i++)
        {
            history_list.undo_states[index].u_neighbors_value.Add(neigbors[i].n_value);
            history_list.undo_states[index].u_neighbors_knot_pos.Add(neigbors[i].n_root_number);
            history_list.undo_states[index].u_neighbors_close.Add(neigbors[i].n_close_numbers.Count);

            //Save n - data;
            history_list.undo_states[index].u_neigbors_data.Add(new NeighborsCell());

            if (neigbors[i].n_close_id.Count > 0)
            {
                for (int k = 0; k < neigbors[i].n_close_id.Count; k++)
                {
                    history_list.undo_states[index].u_neigbors_data[i].n_close_id.Add(neigbors[i].n_close_id[k]);
                }
            }

            int index_ = neigbors[i].n_index;
            Cell tCell = t_data.cells_list[index_];

            if (tCell.cell_neighbors_knot_instance != null)
            {
                ChipsTower trw = tCell.cell_neighbors_knot_instance.GetComponent<ChipsTower>();
                history_list.undo_states[index].u_neighbors_knot_value.Add(trw.chip_value);
            }
            else
            {
                history_list.undo_states[index].u_neighbors_knot_value.Add(0);
            }
        }

        if (last_cell != null)
        {
            history_list.undo_states[index].u_last_cell_data = last_cell;
        }
    }

    public void CancelLastBetL()
    {
        if (!game_lock)
        {
            CancelLastBet();
        }
    }

    //Отменить последнее действие;
    //======================================;
    public void CancelLastBet()
    {
        if (history_list.undo_states.Count > 0)
        {
            int index = history_list.undo_states.Count - 1;

            //Calculate credit;
            //-----------------------------------;
            p_credit += history_list.undo_states[index].total_bet_val;
            p_balance += history_list.undo_states[index].total_bet_val * p_denomination;
            UpdateLabels();

            history_list.undo_states.RemoveAt(index);
            index = history_list.undo_states.Count - 1;

            for (int i = 0; i < t_data.cells_list.Count; i++)
            {
                int id = history_list.undo_states[index].u_cell_id[i];
                Cell tCell = t_data.cells_list[id];
                tCell.cell_bet_value = history_list.undo_states[index].u_cell_bet_value[i];

                //Refresh basic chip;
                //-----------------------------------;
                if (tCell.cell_chip_instance != null)
                {
                    ChipsTower trw = tCell.cell_chip_instance.GetComponent<ChipsTower>();
                    int cur_chip_val = history_list.undo_states[index].u_cell_chip_value[i];
                    trw.UpdateChipValue(cur_chip_val);

                    //Despawn chip;
                    if (cur_chip_val == 0)
                    {
                        tCell.cell_chip_instance = null;
                        tCell.cell_chip_tower.DespawnChip();
                    }
                }

                //Refresh set chip;
                //-----------------------------------;
                if (tCell.cell_extra_chip_instance != null)
                {
                    ChipsTower trw = tCell.cell_extra_chip_instance.GetComponent<ChipsTower>();
                    int cur_chip_val = history_list.undo_states[index].u_cell_ext_value[i];
                    trw.UpdateChipValue(cur_chip_val);

                    //Despawn chip;
                    if (cur_chip_val == 0)
                    {
                        tCell.cell_extra_chip_instance = null;
                        tCell.ext_cell_chip_tower.DespawnChip();
                    }
                }
            }

            //Neighbors reverse;
            //-----------------------------------;
            for (int i = 0; i < neigbors.Count; i++)
            {
                neigbors[i].n_value = history_list.undo_states[index].u_neighbors_value[i];
                LightMap.instance.UpdateNeighbors(neigbors[i].n_field_pos, neigbors[i].n_value);

                int index_ = neigbors[i].n_index;
                Cell tCell = t_data.cells_list[index_];

                //If cell have knot;
                if (tCell.cell_neighbors_knot_instance != null)
                {
                    ChipsTower trw = tCell.cell_neighbors_knot_instance.GetComponent<ChipsTower>();
                    var cur_knot_val = history_list.undo_states[index].u_neighbors_knot_value[i];
                    trw.UpdateChipValue(cur_knot_val);

                    //Despawn knot;
                    //-----------------------------------;
                    if (cur_knot_val == 0)
                    {
                        //Despawn chip;
                        tCell.cell_neighbors_knot_instance = null;
                        trw.DespawnChip();

                        //Clear neighbors data;
                        for (int k = 0; k < neigbors.Count; k++)
                        {
                            if (tCell.cell_id == neigbors[k].n_index)
                            {
                                neigbors[k].n_close_id.Clear();
                                neigbors[k].n_close_numbers.Clear();
                                neigbors[k].n_bet_value = 0;
                                break;
                            }
                        }
                    }
                }
            }

            UpdateLabels();

            last_cell = history_list.undo_states[index].u_last_cell_data;
        }


        //If last in history;
        //-----------------------------------;
        if (history_list.undo_states.Count == 1)
        {
            history_list.undo_states.RemoveAt(0);
        }
    }

    //Отменить все действия;
    //======================================;
    public void CancelAllBetsL()
    {
        int _count = history_list.undo_states.Count;
        for (int i = 0; i < _count; i++)
        {
            CancelLastBet();
        }
    }

    public void CancelAllBets()
    {
        if (!game_lock)
        {
            int _count = history_list.undo_states.Count;
            for (int i = 0; i < _count; i++)
            {
                CancelLastBet();
            }
        }
    }

    //Создание чипа на поле <Basic>;
    //====================================================;
    public void SetBasicChip(Cell t_cell, int value)
    {
        //Spawn chip;
        SpawnChip(chips_instance, main_field_root, t_cell.cell_chips_pos[0], value, t_cell, false);
    }

    //Создание чипа на поле <SET>;
    //====================================================;
    public void SetSetChip(Cell t_cell, int value)
    {
        SpawnChip(chips_instance, main_field_root, t_cell.cell_chips_pos[0], value, t_cell, true);
    }

    //Создание чипа соседей <Neighbors Knot>;
    //====================================================;
    public void SetNeighborsKnot(Cell t_cell, int value, Vector3 pos)
    {
        ChipsTower trw_;
        int temp_val = 0;

        //Update chip;
        //-----------------------------------;
        if (t_cell.cell_neighbors_knot_instance != null)
        {
            trw_ = t_cell.cell_neighbors_knot_instance.GetComponent<ChipsTower>();
            temp_val = trw_.chip_value;
            trw_.UpdateChipValue(temp_val + value);
        }
        else
        {
            //Create chip;
            //-----------------------------------;
            t_cell.cell_neighbors_knot_instance = PoolManager.Pools["chips_pool"].Spawn(chips_instance.transform);
            t_cell.cell_neighbors_knot_instance.SetParent(roll_field_root, true);
            t_cell.cell_neighbors_knot_instance.localScale = new Vector3(0.01f, 0.01f, 0.01f);
            t_cell.cell_neighbors_knot_instance.localPosition = pos;
            t_cell.cell_neighbors_knot_instance.GetComponent<ChipsTower>().UpdateChipValue(value);

            //Grow anim;
            //-----------------------------------;
            t_cell.cell_neighbors_knot_instance.DOScale(0.7f, 0.15f);
        }
    }

    //Выбор соседей <neighbors select>;
    //======================================;
    public void SelectNeighbors()
    {
        if (!game_lock)
        {
            int id;
            int t_all_sum = 0;

            int t_max_cell = 0;
            int ttb = 0;

            if (last_cell != null)
            {
                if (last_cell.cell_close_numbers.Count == 1)
                {
                    id = last_cell.cell_close_id[0];

                    if (nSetId != id)
                    {
                        nSetId = id;
                        nSet = false;
                        t_all_sum = cur_bet_value * 2;
                    }
                    else
                    {
                        nSet = true;
                        t_all_sum = cur_bet_value * 3;
                    }

                    int a = 0;
                    int b = 0;
                    int c = 0;

                    List<int> sosedi = new List<int>();

                    for (int i = 0; i < neigbors.Count; i++)
                    {
                        if (neigbors[i].n_index == id)
                        {
                            //core;
                            if (nSet == false)
                            {

                            }
                            else
                            {
                                b = neigbors[i].n_index;
                                sosedi.Add(b);
                            }

                            //back;
                            if ((i - 1) >= 0)
                            {
                                a = neigbors[i - 1].n_index;
                                sosedi.Add(a);
                            }
                            else
                            {
                                a = neigbors[36].n_index;
                                sosedi.Add(a);
                            }

                            //forward;
                            if ((i + 1) <= 36)
                            {
                                c = neigbors[i + 1].n_index;
                                sosedi.Add(c);
                            }
                            else
                            {
                                c = neigbors[0].n_index;
                                sosedi.Add(c);
                            }
                            break;
                        }
                    }

                    ttb = CalcCurBet();

                    if (t_all_sum > 0 && t_all_sum <= p_credit)
                    {
                        if ((ttb + t_all_sum) <= max_field_bet)
                        {
                            for (int i = 0; i < sosedi.Count; i++)
                            {
                                int n = sosedi[i];
                                Cell n_cell = t_data.cells_list[n];

                                t_max_cell = n_cell.cell_bet_value + cur_bet_value;

                                if (t_max_cell <= max_cell_bet)
                                {
                                    n_cell.cell_bet_value += cur_bet_value;
                                    SetBasicChip(n_cell, cur_bet_value);
                                    p_credit -= cur_bet_value;
                                    p_balance -= cur_bet_value * p_denomination;
                                }
                            }

                            UpdateLabels();
                            SaveHistory(t_all_sum);
                        }
                        else
                        {
                            popupWindow.ShowPopup("<size=50>Макс. ставка на поле</size>"
                            + "\n<size=35>Вы ставите (<color=yellow>"
                            + (ttb + t_all_sum).ToString()
                            + "</color>), ставка на поле (<color=yellow>"
                            + max_field_bet.ToString()
                            + "</color>);</size>"
                            );
                        }
                    }
                    else
                    {
                        //Show not enought credit;
                        //-----------------------------------;
                        popupWindow.ShowPopup("<size=30>Не хватает кредитов</size>"
                        + "\n<size=20>Вы ставите (<color=yellow>"
                        + t_all_sum.ToString()
                        + "</color>), доступный кредит (<color=yellow>"
                        + p_credit.ToString()
                        + "</color>);</size>"
                        );
                    }
                }
                else
                {
                    Debug.Log("Wrong selection");
                }
            }
        }
    }

    //Показать выгравшие ставки;
    //======================================;
    public void ShowWinNumber(int win, int _credit, int _win)
    {
        Cell curCell;
        int cur_win_id = 0;

        List<int> win_id = new List<int>();
        List<int> lose_id = new List<int>();

        List<int> win_n_id = new List<int>();
        List<int> lose_n_id = new List<int>();

        cur_balance = _credit;
        cur_win = _win;

        //Return postion of win strip;
        strip_rect.DOVerticalNormalizedPos(1f, 0.25f);

        //Find main number;
        //-----------------------------------;
        for (int i = 0; i < t_data.cells_list.Count; i++)
        {
            curCell = t_data.cells_list[i];

            if (curCell.cell_close_numbers.Count == 1)
            {
                if (win == curCell.cell_close_numbers[0])
                {
                    cur_win_id = curCell.cell_id;
                }
            }
        }

        //Find all sub win numbers;
        //-----------------------------------;
        for (int i = 0; i < t_data.cells_list.Count; i++)
        {
            curCell = t_data.cells_list[i];

            //Win number found in current cell;
            bool found = false;

            //Find basic bets;
            if (i <= 156)
            {
                for (int j = 0; j < curCell.cell_close_id.Count; j++)
                {
                    if (cur_win_id == curCell.cell_close_id[j])
                    {
                        win_id.Add(curCell.cell_id);
                        found = true;
                        break;
                    }
                }
            }
            else
            {
                //Find set bets;
                for (int j = 0; j < curCell.cell_close_id.Count; j++)
                {
                    var tCell = t_data.cells_list[curCell.cell_close_id[j]];

                    for (int k = 0; k < tCell.cell_close_id.Count; k++)
                    {
                        if (cur_win_id == tCell.cell_close_id[k])
                        {
                            win_id.Add(curCell.cell_id);
                            found = true;
                            break;
                        }
                    }
                }
            }

            //Not found;
            if (!found)
            {
                if (curCell.cell_chip_instance != null || curCell.cell_extra_chip_instance != null)
                {
                    lose_id.Add(curCell.cell_id);
                }
            }
        }

        //Find all neigbors;
        //-----------------------------------;
        for (int i = 0; i < neigbors.Count; i++)
        {
            //Win N-knot found in current cell;
            bool n_found = false;

            for (int j = 0; j < neigbors[i].n_close_id.Count; j++)
            {
                if (cur_win_id == neigbors[i].n_close_id[j])
                {
                    win_n_id.Add(neigbors[i].n_index);
                    n_found = true;
                    break;
                }
            }

            //Not found;
            //-----------------------------------;
            if (!n_found)
            {
                int tc = neigbors[i].n_index;
                if (t_data.cells_list[tc].cell_neighbors_knot_instance != null)
                {
                    //Debug.Log("Win not found, but have chip => add to LOSE");
                    lose_n_id.Add(tc);
                }
            }
        }

        DelightField();
        LightMap.instance.HighLightCells(t_data.cells_list[cur_win_id].cell_close_numbers);
        LightMap.instance.MoveTowerToCell(t_data.cells_list[cur_win_id].cell_close_numbers[0], win_id, lose_id, win_n_id, lose_n_id);
        
        //Show popup <win number>;
        progressBar.DOFade(0.0f, 0.75f);
        glowImg.DOFade(0.0f, 0.75f).OnComplete(() => {
            ShowCloud(true);
        });
        popupState.ShowPopup("<size=50>Выиграл номер : " + win.ToString() + "</size>");
    }

    //Убрать подсветку;
    //======================================;
    public void DelightField()
    {
        LightMap.instance.DeLightCells();
        LightMap.instance.DeLightRollCells();
    }

    //Сохранить последнию ставку;
    //======================================;
    public void SaveLastBet()
    {
        if (history_list.undo_states.Count > 1)
        {
            int index = history_list.undo_states.Count - 1;
            last_bet_data = history_list.undo_states[index];
        }
    }

    //Восстановить последнию ставку <Last bet>;
    //======================================;
    public void RestoreLastBet()
    {
        if (!game_lock) {

            if (last_bet_data.u_cell_bet_value.Count > 0)
            {
                int total_val = 0;

                //Total value of operation;
                for (int i = 0; i < last_bet_data.u_cell_bet_value.Count; i++)
                {
                    total_val += last_bet_data.u_cell_bet_value[i];
                }

                //Total value < player balance;
                //-----------------------------------;
                if (total_val <= p_credit)
                {

                    //Remove all previous bets;
                    //-----------------------------------;
                    CancelAllBets();
                    for (int i = 0; i < t_data.cells_list.Count; i++)
                    {
                        int id = last_bet_data.u_cell_id[i];
                        Cell tCell = t_data.cells_list[id];
                        tCell.cell_bet_value = last_bet_data.u_cell_bet_value[i];

                        //Return basic chip;
                        //-----------------------------------;
                        if (last_bet_data.u_cell_chip_value[i] > 0)
                        {
                            SetBasicChip(tCell, last_bet_data.u_cell_chip_value[i]);
                        }

                        //Return set chip;
                        //-----------------------------------;
                        if (last_bet_data.u_cell_ext_value[i] > 0)
                        {
                            SetSetChip(tCell, last_bet_data.u_cell_ext_value[i]);
                        }
                    }

                    //Neighbors return;
                    //-----------------------------------;
                    for (int i = 0; i < last_bet_data.u_neighbors_value.Count; i++)
                    {
                        neigbors[i].n_value = last_bet_data.u_neighbors_value[i];

                        if(last_bet_data.u_neigbors_data[i].n_close_id.Count > 0)
                        {
                            for(int k = 0; k < last_bet_data.u_neigbors_data[i].n_close_id.Count; k++)
                            {
                                neigbors[i].n_close_id.Add(last_bet_data.u_neigbors_data[i].n_close_id[k]);
                            }
                        }

                        LightMap.instance.UpdateNeighbors(neigbors[i].n_field_pos, neigbors[i].n_value);

                        //Return n chips;
                        if (last_bet_data.u_neighbors_value[i] > 0)
                        {
                            LightMap.instance.NeighborShow(neigbors[i].n_field_pos);
                        }

                        //Return knots;
                        if (last_bet_data.u_neighbors_knot_value[i] > 0)
                        {
                            int index_ = neigbors[i].n_index;
                            Cell tCell = t_data.cells_list[index_];
                            int knotIndex = last_bet_data.u_neighbors_knot_pos[i];
                            SetNeighborsKnot(tCell, last_bet_data.u_neighbors_knot_value[i], neigbors[knotIndex].n_pos);
                            neigbors[i].n_root_number = last_bet_data.u_neighbors_knot_pos[i];
                        }
                    }

                    //Update credit;
                    //-----------------------------------;
                    p_credit -= total_val;
                    p_balance -= total_val * p_denomination;
                    UpdateLabels();
                    SaveHistory(total_val);
                }
                else
                {
                    //Show not enought credit;
                    //-----------------------------------;
                    popupWindow.ShowPopup("<size=30>Не хватает кредитов</size>"
                    + "\n<size=20>Вы ставите (<color=yellow>"
                    + total_val.ToString()
                    + "</color>), доступный кредит (<color=yellow>"
                    + p_credit.ToString()
                    + "</color>);</size>"
                    );
                }
            }
        }
    }

    //Создать фишку;
    //======================================;
    public void SpawnChip(Transform _trans, Transform _parent, Vector3 pos, int val, Cell curCell, bool extend)
    {
        if (!extend)
        {
            //Update Chip;
            if (curCell.cell_chip_instance != null)
            {
                int temp_val = 0;
                temp_val = curCell.cell_chip_tower.chip_value;
                curCell.cell_chip_tower.UpdateChipValue(temp_val + val);
            }
            else
            {
                //Create instance;
                curCell.cell_chip_instance = PoolManager.Pools["chips_pool"].Spawn(_trans);
                Transform curTrans = curCell.cell_chip_instance;

                //Set data;
                curTrans.SetParent(_parent, false);
                curTrans.localScale = new Vector3(0.01f, 0.01f, 0.01f);
                curTrans.localPosition = pos;
                curCell.cell_chip_tower = curTrans.GetComponent<ChipsTower>();
                curCell.cell_chip_tower.UpdateChipValue(val);

                //Grow anim;
                curTrans.DOScale(0.5f, 0.15f);
            }
        }
        else
        {
            if (curCell.cell_extra_chip_instance != null)
            {
                int temp_val = 0;
                temp_val = curCell.ext_cell_chip_tower.chip_value;
                curCell.ext_cell_chip_tower.UpdateChipValue(temp_val + val);
            }
            else
            {
                //Create instance;
                curCell.cell_extra_chip_instance = PoolManager.Pools["chips_pool"].Spawn(_trans);
                Transform curTrans = curCell.cell_extra_chip_instance;

                //Set data;
                curTrans.SetParent(roll_field_root, false);
                curTrans.localScale = new Vector3(0.01f, 0.01f, 0.01f);
                curTrans.localPosition = curCell.cell_chips_pos[curCell.cell_chips_pos.Count - 1];
                curCell.ext_cell_chip_tower = curTrans.GetComponent<ChipsTower>();
                curCell.ext_cell_chip_tower.UpdateChipValue(val);

                //Grow anim;
                curTrans.DOScale(0.5f, 0.15f);
            }
        }
    }

    //Показать облако;
    //======================================;
    public void ShowCloud(bool show)
    {
        if (show)
        {
            glowCloudTrans.DOScale(0.01f, 0.0f);
            glowCloud.DOFade(0.0f, 0.0f);
            progressBar.DOFade(0f, 0f);

            Sequence cloudSeq = DOTween.Sequence();
            cloudSeq.Insert(0.0f, glowCloudTrans.DOScale(1.0f, 0.75f));
            cloudSeq.Insert(0.0f, glowCloud.DOFade(1.0f, 0.75f));
        }
        else
        {
            glowCloudTrans.DOScale(0.01f, 1.75f);
            glowCloud.DOFade(0.0f, 1.75f);
        }
    }

    public void TimerEvent(float timer)
    {
        timer_tween = DOTween.Sequence();
        timer_tween.Append(timerProgress.DOFade(1.0f,0.25f));
        timer_tween.Append(timerProgress.DOFillAmount(1f, timer).SetEase(Ease.Linear));
        timer_tween.Append(timerProgress.DOFade(0.0f, 0.25f));
        timer_tween.OnComplete(()=> {
            timerProgress.fillAmount = 0;
        });
    }

    public void MoveStripUp(bool up)
    {
        if (up)
        {
            strip_rect.DOVerticalNormalizedPos(1f, 0.35f);
        }
        else
        {
            strip_rect.DOVerticalNormalizedPos(0f, 0.35f);
        }
    }

    public void SwitchBetValue()
    {

        if (cur_bet_value < 50)
        {
            cur_bet_value += 5;
        }
        else
        if (cur_bet_value >= 50 && cur_bet_value < 100)
        {
            cur_bet_value += 10;
        }
        else
        if (cur_bet_value >= 100 && cur_bet_value < 500)
        {
            cur_bet_value += 50;
        }
        else
        if (cur_bet_value >= 500)
        {
            cur_bet_value = 5;
        }

        cur_bet_lbl.text = cur_bet_value.ToString();
    }

    void OnGUI()
    {
        //if (last_cell != null)
        //{
        //    GUILayout.Label(last_cell.cell_label);
        //}
    }

    public int CalcCurBet()
    {
        //Посчитать общую ставку;
        int total_val = 0;
        for (int i = 0; i < t_data.cells_list.Count; i++)
        {
            if (t_data.cells_list[i].cell_bet_value > 0)
            {
                total_val += t_data.cells_list[i].cell_bet_value;
            }
        }

        return total_val;
    }
}

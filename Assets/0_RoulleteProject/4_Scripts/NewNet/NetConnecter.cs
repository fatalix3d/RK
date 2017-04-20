using System.Collections;
using System.Collections.Generic;
using Newtonsoft.Json;
using UnityEngine;
using UnityEngine.UI;
using System.Net;
using System.Net.Sockets;
using System.Net.NetworkInformation;
using System.Linq;
using System;
using System.Text;
using DG.Tweening;
using System.IO;

//Add some comments;
public class NetConnecter : MonoBehaviour {
    #region vars
    public string adress_, port_;
    public int id_;
    public int win_number = 0;

    private Socket s;
    string str;
    private List<byte> buffer = new List<byte>();

    public bool playerSetup;

    public Text dInfo;
    public UInt64 packetID;
    FileInfo f;
    public bool connectStatus;
    public static event NetworkAvailabilityChangedEventHandler NetworkAvailabilityChanged;
    private string IpAddress;

    public CanvasGroup lockScreen;
    public GameObject lostConnection_obj;
    public Text LostConnectionLabel;
    public Sequence lostConnectionTimer;
    private bool connection_false = false;

    public Text seat_id;
    private bool cardPlaced = false;
    private int monitorId = 0;

    //Invalid spin;
    public bool invalidSpin = false;
    public string com_target;
    public static NetConnecter instance;
    public bool debugInfo = false;
    #endregion
    void Awake()
    {
        instance = this;
    }

    //0) - Start connect coroutine;
    //======================================;
    public void StartConnect()
    {
        Connect(adress_, port_);
    }

    //1) - Connect to server;
    //======================================;
    public void Connect(string adress, string port)
    {
        try
        {
            s = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp) { };
            s.Connect(adress, 33600);
            Debug.Log("Try to connect : " + adress + " / " + port);
        }
        catch (Exception e){
            //Debug.Log(e.ToString());
            ConnectionFailed("Подключение к серверу потеряно");
        }

        StartCoroutine(Connection());
    }

    //2) -CONNECTION COROUTINE;
    //======================================;
    IEnumerator Connection()
    {
        if (s.Connected)
        {
            Debug.Log("Connected : " + adress_ + ", " + port_);
            string json;

            json = JsonConvert.SerializeObject(new { NUM = id_.ToString(), CMD = 1.ToString(), ID = 1.ToString() });
            SendResponce(json);

            json = JsonConvert.SerializeObject(new { NUM = id_.ToString(), CMD = 3.ToString(), ID = 2.ToString() });
            SendResponce(json);

            json = JsonConvert.SerializeObject(new { NUM = id_.ToString(), CMD = 56.ToString(), ID = 3.ToString() });
            SendResponce(json);

            StartCoroutine(SendPing());
        }
        else
        {
            ConnectionFailed("Подключение к серверу потеряно");
        }

        while (true)
        {
            if (connection_false)
            {
                yield break;
            }
            else
            {
                String Str = String.Empty;
                if (s.Connected)
                {
                    int avail = s.Available;

                    if (avail > 0)
                    {
                        var _buffer = new byte[avail];
                        s.Receive(_buffer);
                        buffer.AddRange(_buffer);
                    }

                    Str = GetJsonText(ref buffer);
                    if (Str != null)
                    {
                        if (!playerSetup)
                        {
                            OnMessageInStart(Str);
                        }
                        else
                        {
                            OnMessageIn(Str);
                        }
                    }
                }
                else
                {
                }
                yield return null;
            }
        }
    }

    //Auto send ping;
    //======================================;
    public IEnumerator SendPing()
    {
        while (true)
        {
            connectStatus = false;
            string json = JsonConvert.SerializeObject(new { NUM = id_.ToString(), CMD = "63", ID = packetID });
            SendResponce(json);

            if (connection_false)
            {
                yield break;
            }
            else
            {
                yield return new WaitForSeconds(3.5f);
            }

        }
    }

    //DISABLE CONNECTION;
    //======================================;
    public void DisableConnection()
    {
        if (s.Connected)
        {
            s.Disconnect(false);
        }
    }

    //CONNECTION FAIL;
    //======================================;
    public void ConnectionFailed(string info)
    {
        StopCoroutine(Connection());

        connection_false = true;
        ShowLockScreen(true);

        LostConnectionLabel.text = "Соединение прервано.. \n" + info;
        GameManager.instance.GameLockEvent(null);
        playerSetup = false;

        if (lostConnectionTimer != null)
        {
            lostConnectionTimer.Kill(false);
        }

        lostConnectionTimer = DOTween.Sequence();
        int tick = 0;

        lostConnectionTimer.AppendInterval(1.0f);
        lostConnectionTimer.AppendCallback(()=> {
            tick += 1;
            if(tick >= 9)
            {
                tick = 0;
                connection_false = false;
                lostConnectionTimer.Complete(false);
                Connect(adress_, port_);
                Debug.Log("Trying to connect....");
            }
        }).SetLoops(10);
    }

    //ReciveData;
    //======================================;
    private String GetJsonText(ref List<byte> b)
    {
        String s = string.Empty;
        int length = b.IndexOf(0x00);

        if (length > 0)
        {
            //Trim command;
            //------------------------;
            s = ASCIIEncoding.Default.GetString(b.Take(length).ToArray());
            b.RemoveRange(0, length + 1);
            return s;
        }
        return null;
    }

    //Send Responce;
    //======================================;
    public void SendResponce(string msg)
    {
        try
        {
            packetID++;
            byte[] message = ASCIIEncoding.Default.GetBytes(msg);
            s.Send(message);
            s.Send(new byte[] { 0 });
        }
        catch (SocketException e)
        {
            ConnectionFailed("Подключение к серверу потеряно");
        }
    }

    //MESSAGE IN;
    //======================================;
    public void OnMessageInStart(string str)
    {
        try
        {
            var newMessage = JsonConvert.DeserializeObject<NetData>(str);
            string json;
            //Debug.Log(str);
            //packetID = newMessage.ID;
            ShowLockScreen(false);

            if (newMessage.NUM != id_)
            {
                return;
            }

            switch (newMessage.CMD)
            {
                case 1:
                    switch (newMessage.STATUS)
                    {
                        case 7:
                            Debug.Log("Game error;");
                            GameManager.instance.popupWindow.ShowPopup("<color=red>GAME ERROR</color>");
                            GameManager.instance.GameLockEvent("FATAL ERROR");
                            json = JsonConvert.SerializeObject(new { NUM = id_.ToString(), CMD = newMessage.CMD.ToString() });
                            SendResponce(json);
                            playerSetup = true;
                            ShowLockScreen(false);
                            GameManager.instance.global_lock = false;
                            break;

                        default:
                            GameManager.instance.UpdateLabels();

                            //Update last win numbers;
                            if (newMessage.NUMBER.Length > 19)
                            {
                                WinStrip.instance.AddWinNumber(newMessage.NUMBER, true);
                                for (int i = 0; i < 19; i++)
                                {
                                    WinStrip.instance.AddWinNumber(newMessage.NUMBER, false);
                                }
                            }
                            else
                            {
                                WinStrip.instance.AddWinNumber(newMessage.NUMBER, true);
                                for (int i = 0; i < newMessage.NUMBER.Length; i++)
                                {
                                    WinStrip.instance.AddWinNumber(newMessage.NUMBER, false);
                                }
                            }

                            LightMap.instance.ShowLastNumbers(newMessage.NUMBER);

                            json = JsonConvert.SerializeObject(new { NUM = id_.ToString(), CMD = newMessage.CMD.ToString(), ID = packetID, STATUS = newMessage.STATUS.ToString() });
                            SendResponce(json);
                            ShowLockScreen(false);
                            playerSetup = true;
                            break;

                    }
                    break;


                case 129:
                    switch (newMessage.STATUS)
                    {
                        case 0:
                            ////Debug.Log("Server : Нет игры, CMD 129, STATUS 1;");
                            GameManager.instance.GameLockEvent(null);
                            break;
                        case 1:
                            ////Debug.Log("Server : Начало игры, (Cтавки разрешены), CMD 129, STATUS 1;");
                            GameManager.instance.GameUnlockEvent();
                            break;
                        case 2:
                            ////Debug.Log("Server : Последние ставки , CMD 129, STATUS 2;");
                            GameManager.instance.TimerEvent(10f);
                            GameManager.instance.GameLockEvent(null);
                            break;

                        case 3:
                            ////Debug.Log("Server : Блокировка поля, CMD 129, STATUS 3;");
                            if (!GameManager.instance.global_lock)
                            {
                                GameManager.instance.GameLockEvent(null);
                                GameManager.instance.DelightField();
                            }
                            break;
                        case 4:
                            if (!invalidSpin)
                            {
                                ////Debug.Log("Server : Выпал номер, CMD 129, STATUS 4;");
                                GameManager.instance.GameLockEvent(null);
                                json = JsonConvert.SerializeObject(new { NUM = id_.ToString(), CMD = newMessage.CMD.ToString(), STATUS = newMessage.STATUS, ID = newMessage.ID.ToString() });
                                SendResponce(json);
                            }
                            break;
                        case 5:
                            ////Debug.Log("Server : Конец игры (Очистка поля), CMD 129, STATUS 5;");
                            GameManager.instance.GameLockEvent(null);
                            break;
                        case 6:
                            ////Debug.Log("Server : Ошибка спина, CMD 129, STATUS 6;");
                            GameManager.instance.GameLockEvent(null);
                            break;
                        case 7:
                            Debug.Log("Game error;");
                            GameManager.instance.popupWindow.ShowPopup("<color=red>GAME ERROR</color>");
                            GameManager.instance.GameLockEvent("FATAL ERROR");
                            json = JsonConvert.SerializeObject(new { NUM = id_.ToString(), CMD = newMessage.CMD.ToString() });
                            SendResponce(json);
                            break;
                    }

                    //Update last win numbers;
                    WinStrip.instance.AddWinNumber(newMessage.NUMBER, false);
                    LightMap.instance.ShowLastNumbers(newMessage.NUMBER);


                    json = JsonConvert.SerializeObject(new { NUM = id_.ToString(), CMD = 2.ToString(), ID = newMessage.ID.ToString() });
                    SendResponce(json);
                    break;

                //CARD RFID;
                //----------------------------------;
                case 3:
                    int bet_data = GameManager.instance.CalcCurBet();

                    if ((newMessage.STATUS & 0x1) != 0x00)
                    {
                        //Debug.Log("Terminal : Карта приложена, CMD 3");

                        if (bet_data == 0)
                        {
                            switch (newMessage.TRANS)
                            {
                                case 0:
                                    //Debug.Log("Transaction complete, CMD 3, TRANS 0");
                                    GameManager.instance.p_balance = newMessage.CREDIT;
                                    GameManager.instance.UpdateLabels();
                                    GameManager.instance.GlobalUnlockEvent();
                                    json = JsonConvert.SerializeObject(new { NUM = id_.ToString(), CMD = newMessage.CMD.ToString(), ID = newMessage.ID});
                                    SendResponce(json);
                                    break;

                                case 1:
                                    //Debug.Log("Transaction in progress, CMD 3, TRANS 1");
                                    GameManager.instance.GlobalLockEvent("Cashless transfer - please wait...", true);
                                    json = JsonConvert.SerializeObject(new { NUM = id_.ToString(), CMD = newMessage.CMD.ToString(), ID = newMessage.ID });
                                    SendResponce(json);
                                    break;

                                case 255:
                                    //Debug.Log("Transaction ERROR, CMD 3, TRANS 255");
                                    GameManager.instance.popupWindow.ShowPopup("TRANSACTION ERROR");

                                    json = JsonConvert.SerializeObject(new { NUM = id_.ToString(), CMD = newMessage.CMD.ToString(), ID = newMessage.ID });
                                    SendResponce(json);
                                    break;
                            }

                            if (!cardPlaced)
                            {
                                CardInfo.instance.ShowCardInfo(0, Int64.Parse(newMessage.RFID), true);
                                cardPlaced = true;
                            }
                        }
                        else
                        {
                            //Debug.Log("Command broken, reason - detected active bets");
                        }
                    }
                    else
                    {
                        switch (newMessage.TRANS)
                        {
                            case 0:
                                //Debug.Log("Card removed : Transaction complete, CMD 3, TRANS 0");
                                GameManager.instance.p_balance = newMessage.CREDIT;
                                GameManager.instance.UpdateLabels();
                                GameManager.instance.GlobalUnlockEvent();

                                cardPlaced = false;
                                CardInfo.instance.ShowCardInfo(0, Int64.Parse(newMessage.RFID), false);
                                json = JsonConvert.SerializeObject(new { NUM = id_.ToString(), CMD = newMessage.CMD.ToString(), ID = newMessage.ID });
                                SendResponce(json);
                                break;

                            case 1:
                                //Debug.Log("Transaction in progress, CMD 3, TRANS 1");
                                GameManager.instance.GlobalLockEvent("Cashless transfer - please wait...", true);
                                json = JsonConvert.SerializeObject(new { NUM = id_.ToString(), CMD = newMessage.CMD.ToString(), ID = newMessage.ID });
                                SendResponce(json);
                                break;

                            case 255:
                                //Debug.Log("Transaction ERROR, CMD 3, TRANS 255");
                                GameManager.instance.GlobalUnlockEvent();
                                GameManager.instance.popupWindow.ShowPopup("TRANSACTION ERROR");

                                cardPlaced = false;
                                CardInfo.instance.ShowCardInfo(0, Int64.Parse(newMessage.RFID), false);
                                json = JsonConvert.SerializeObject(new { NUM = id_.ToString(), CMD = newMessage.CMD.ToString(), ID = newMessage.ID });
                                SendResponce(json);
                                break;
                        }
                    }
                    break;

                //PING;
                //----------------------------------;
                case 223:
                    //Debug.Log("Ответ : Ping, CMD 223");
                    json = JsonConvert.SerializeObject(new { NUM = id_.ToString(), CMD = newMessage.CMD.ToString(), ID = newMessage.ID });
                    SendResponce(json);
                    connectStatus = true;
                    break;
            }
        }catch (Exception e){
            //Debug.Log(e);
        }
    }

    //MESSAGE IN;
    //======================================;
    public void OnMessageIn(string str)
    {
        string json;
        try
        {
            var newMessage = JsonConvert.DeserializeObject<NetData>(str);
            if (newMessage.NUM != id_)
                return;

            ShowLockScreen(false);

            switch (newMessage.CMD)
            {

                case 129:
                    switch (newMessage.STATUS)
                    {
                        //GAME STATUS;
                        //----------------------------------;
                        case 0:
                            if (!GameManager.instance.global_lock)
                            {
                                json = JsonConvert.SerializeObject(new { NUM = id_.ToString(), CMD = newMessage.CMD.ToString(), ID = newMessage.ID.ToString() });
                                SendResponce(json);
                                //GameManager.instance.GameUnlockEvent();
                            }
                            break;

                        //GAME STARTED;
                        //----------------------------------;
                        case 1:
                            if (!GameManager.instance.global_lock)
                            {
                                json = JsonConvert.SerializeObject(new { NUM = id_.ToString(), CMD = newMessage.CMD.ToString(), ID = newMessage.ID.ToString() });
                                SendResponce(json);
                                LightMap.instance.AnimateWinNum(true, 0);
                                GameManager.instance.GameUnlockEvent();
                                invalidSpin = false;
                            }
                            break;

                        //TIMER START;
                        //----------------------------------;
                        case 2:
                            if (!GameManager.instance.global_lock)
                            {
                                json = JsonConvert.SerializeObject(new { NUM = id_.ToString(), CMD = newMessage.CMD.ToString(), ID = newMessage.ID.ToString() });
                                SendResponce(json);

                                if (!invalidSpin)
                                {
                                    GameManager.instance.TimerEvent(10f);
                                }
                            }
                            break;

                        //CLOSE BETS;
                        //----------------------------------;
                        case 3:
                            if (!GameManager.instance.global_lock)
                            {
                                json = JsonConvert.SerializeObject(new { NUM = id_.ToString(), CMD = newMessage.CMD.ToString(), ID = newMessage.ID.ToString() });
                                SendResponce(json);
                                GameManager.instance.GameLockEvent(null);
                                GameManager.instance.DelightField();
                            }
                            break;

                        //SHOW WIN NUMBER;
                        //----------------------------------;
                        case 4:
                            if (!GameManager.instance.global_lock)
                            {
                                if (!invalidSpin)
                                {
                                    GameManager.instance.ShowCloud(true);
                                    GameManager.instance.popupState.ShowPopup("<size=50>Выйграл номер : " + newMessage.NUMBER[0].ToString() + "</size>");
                                    win_number = newMessage.NUMBER[0];
                                    LightMap.instance.ShowLastNumbers(newMessage.NUMBER);
                                    LightMap.instance.AnimateWinNum(false, newMessage.NUMBER[0]); //Show win num in juke;
                                    WinStrip.instance.AddWinNumber(newMessage.NUMBER, true);
                                    json = JsonConvert.SerializeObject(new { NUM = id_.ToString(), CMD = newMessage.CMD.ToString(), ID = newMessage.ID.ToString() });
                                    SendResponce(json);
                                }
                            }
                            break;
                        //GAME END;
                        //----------------------------------;
                        case 5:
                            if (!GameManager.instance.global_lock)
                            {
                                json = JsonConvert.SerializeObject(new { NUM = id_.ToString(), CMD = newMessage.CMD.ToString(), STATUS = newMessage.STATUS.ToString(), ID = newMessage.ID.ToString() });
                                SendResponce(json);
                            }
                            break;

                        //INVALID SPIN;
                        //----------------------------------;
                        case 6:
                            if (!GameManager.instance.global_lock)
                            {
                                invalidSpin = true;
                                GameManager.instance.InvalidSpin();
                                json = JsonConvert.SerializeObject(new { NUM = id_.ToString(), CMD = newMessage.CMD.ToString(), STATUS = newMessage.STATUS, ID = newMessage.ID.ToString() });
                                SendResponce(json);
                            }
                            break;

                        //Game Error;
                        //----------------------------------;
                        case 7:
                            Debug.Log("Game error;");
                            GameManager.instance.popupWindow.ShowPopup("<color=red>GAME ERROR</color>");
                            GameManager.instance.GameLockEvent("ROULETTE FATAL ERROR");
                            json = JsonConvert.SerializeObject(new { NUM = id_.ToString(), CMD = newMessage.CMD.ToString() });
                            SendResponce(json);
                            break;
                    }
                    break;

                //PING;
                //----------------------------------;
                case 223:
                        json = JsonConvert.SerializeObject(new { NUM = id_.ToString(), CMD = newMessage.CMD.ToString(), ID = newMessage.ID });
                        SendResponce(json);
                        connectStatus = true;
                    break;

                //Ping
                case 63:
                    if (!GameManager.instance.global_lock)
                    {
                        json = JsonConvert.SerializeObject(new { NUM = id_.ToString(), CMD = newMessage.CMD.ToString(), ID = packetID });
                        connectStatus = true;
                    }
                    break;

                //GET CLIENT BETS;
                //----------------------------------;
                case 130:
                    if (!GameManager.instance.global_lock)
                    {
                        float[] bets = new float[157];
                        int[] n_knot = new int[37];
                        int[] n_close = new int[37];

                        for (int i = 0; i < 157; i++)
                        {
                            bets[i] = GameManager.instance.t_data.cells_list[i].cell_bet_value * GameManager.instance.p_denomination;
                        }

                        int u_index = GameManager.instance.history_list.undo_states.Count - 1;

                        if (GameManager.instance.history_list.undo_states.Count > 0)
                        {
                            for (int i = 0; i < 37; i++)
                            {
                                n_knot[i] = GameManager.instance.history_list.undo_states[u_index].u_neighbors_knot_value[i];
                                n_close[i] = GameManager.instance.history_list.undo_states[u_index].u_neighbors_close[i];
                            }
                        }
                        else
                        {
                            for (int i = 0; i < 37; i++)
                            {
                                n_knot[i] = 0;
                                n_close[i] = 0;
                            }
                        }

                        //SEND CLIENT DATA;
                        json = JsonConvert.SerializeObject(new
                        {
                            NUM = id_.ToString(),
                            CMD = newMessage.CMD.ToString(),
                            ID = newMessage.ID.ToString(),
                            BETS = bets,
                            NKNOT = n_knot,
                            NCLOSE = n_close,
                            CREDIT = GameManager.instance.p_balance.ToString()
                        });

                        SendResponce(json);
                    }
                    break;

                //WIN BETS;
                //----------------------------------;
                case 131:
                    if (!GameManager.instance.global_lock)
                    {
                        if (!invalidSpin)
                        {
                            GameManager.instance.ShowWinNumber(win_number, newMessage.CREDIT, newMessage.WIN);
                            GameManager.instance.cur_balance = newMessage.CREDIT;
                            GameManager.instance.p_last_win = (newMessage.WIN / GameManager.instance.p_denomination);
                            json = JsonConvert.SerializeObject(new { NUM = id_.ToString(), CMD = newMessage.CMD.ToString(), ID = newMessage.ID.ToString() });
                            SendResponce(json);
                        }
                        else
                        {
                            json = JsonConvert.SerializeObject(new { NUM = id_.ToString(), CMD = newMessage.CMD.ToString(), ID = newMessage.ID.ToString() });
                            SendResponce(json);
                        }
                    }
                    else
                    {
                        json = JsonConvert.SerializeObject(new { NUM = id_.ToString(), CMD = newMessage.CMD.ToString(), ID = newMessage.ID.ToString() });
                        SendResponce(json);
                    }
                    break;

                    //CARD RFID;
                    //----------------------------------;
                    case 132:
                        int bet_data = GameManager.instance.CalcCurBet();

                        Debug.Log("Card placed");

                        if ((newMessage.STATUS & 0x1) != 0x00)
                        {
                            if (bet_data == 0)
                            {
                                switch (newMessage.TRANS)
                                {
                                    case 0:
                                        Debug.Log("Transaction complete");
                                        GameManager.instance.p_balance = newMessage.CREDIT;
                                        GameManager.instance.UpdateLabels();
                                        GameManager.instance.GlobalUnlockEvent();
                                        json = JsonConvert.SerializeObject(new { NUM = id_.ToString(), CMD = newMessage.CMD.ToString(), ID = newMessage.ID.ToString() });
                                        SendResponce(json);
                                        Debug.Log(newMessage.CREDIT);
                                    break;

                                    case 1:
                                        Debug.Log("Transaction in progress");
                                        GameManager.instance.GlobalLockEvent("Cashless transfer - please wait...", true);
                                        json = JsonConvert.SerializeObject(new { NUM = id_.ToString(), CMD = newMessage.CMD.ToString(), ID = newMessage.ID.ToString() });
                                        SendResponce(json);
                                        break;

                                    case 255:
                                        Debug.Log("Transaction ERROR");
                                        GameManager.instance.GlobalUnlockEvent();
                                        GameManager.instance.popupWindow.ShowPopup("<color=red>TRANSACTION ERROR</color>");
                                        json = JsonConvert.SerializeObject(new { NUM = id_.ToString(), CMD = newMessage.CMD.ToString(), ID = newMessage.ID.ToString() });
                                        SendResponce(json);
                                        break;
                                }

                                if (!cardPlaced)
                                {
                                    CardInfo.instance.ShowCardInfo(0, Int64.Parse(newMessage.RFID), true);
                                    cardPlaced = true;
                                }
                            }
                            else
                            {
                                Debug.Log("Command broken, reason - detected active bets");
                            }
                        }
                        else
                        {
                            switch (newMessage.TRANS)
                            {
                                case 0:
                                    GameManager.instance.p_balance = newMessage.CREDIT;
                                    GameManager.instance.UpdateLabels();
                                    GameManager.instance.GlobalUnlockEvent();
                                    Debug.Log(newMessage.CREDIT);

                                    cardPlaced = false;
                                    CardInfo.instance.ShowCardInfo(0, Int64.Parse(newMessage.RFID), false);
                                    json = JsonConvert.SerializeObject(new { NUM = id_.ToString(), CMD = newMessage.CMD.ToString(), ID = newMessage.ID.ToString() });
                                    SendResponce(json);
                                    break;

                                case 1:
                                    GameManager.instance.GlobalLockEvent("Cashless transfer - please wait...", true);
                                    json = JsonConvert.SerializeObject(new { NUM = id_.ToString(), CMD = newMessage.CMD.ToString(), ID = newMessage.ID.ToString() });
                                    SendResponce(json);
                                    break;

                                case 255:
                                    GameManager.instance.GlobalUnlockEvent();
                                    GameManager.instance.popupWindow.ShowPopup("<color=red>TRANSACTION ERROR</color>");

                                    cardPlaced = false;
                                    CardInfo.instance.ShowCardInfo(0, Int64.Parse(newMessage.RFID), false);
                                    json = JsonConvert.SerializeObject(new { NUM = id_.ToString(), CMD = newMessage.CMD.ToString(), ID = newMessage.ID.ToString() });
                                    SendResponce(json);
                                    break;
                            }
                        }
                        break;

                //LOCK SEAT;
                //----------------------------------;
                case 136:
                    GameManager.instance.GlobalLockEvent("Терминал заблокирован", false);
                    json = JsonConvert.SerializeObject(new { NUM = id_.ToString(), CMD = newMessage.CMD.ToString(), ID = newMessage.ID.ToString() });
                    SendResponce(json);
                    break;

                case 137:
                    GameManager.instance.GlobalUnlockEvent();
                    json = JsonConvert.SerializeObject(new { NUM = id_.ToString(), CMD = newMessage.CMD.ToString(), ID = newMessage.ID.ToString() });
                    SendResponce(json);
                    break;

                case 56:
                    GameManager.instance.SetDataValues(newMessage.SET5, newMessage.SET6, newMessage.SET7, newMessage.SET8);
                    json = JsonConvert.SerializeObject(new { NUM = id_.ToString(), CMD = newMessage.CMD.ToString(), ID = newMessage.ID.ToString() });
                    //SendResponce(json);
                    break;
            }
        }
        catch (Exception e)
        {
            //Debug.Log(e);
        }
    }

    public void CardPlacedEvent(string RFID, string TYPE_)
    {
        string json;
        json = JsonConvert.SerializeObject(new { NUM = id_.ToString(), CMD = 4, ID = packetID, STATUS = TYPE_, RFID = RFID, ERROR = 0, AUTO = 1 });
        SendResponce(json);
    }

    public void CardRemoveEvent(string RFID, string TYPE_)
    {
        string json;
        json = JsonConvert.SerializeObject(new { NUM = id_.ToString(), CMD = 4, ID = packetID, STATUS = TYPE_, RFID = RFID, ERROR = 0, AUTO = 1 });
        SendResponce(json);
    }

    //Check for config file;
    //======================================;
    void Start()
    {
        lostConnection_obj.SetActive(true);

        f = new FileInfo(Application.dataPath + "\\" + "terminal.txt");
        if (!f.Exists)
        {
            File.WriteAllText(Application.dataPath + "\\" + "terminal.txt", "terminal_ip,terminal_id,port_33600,COM1");

            string mes = "Не обнаружен файл настроек, создан новый.@1)Закройте приложение.@2)В проводнике найдите файл (terminal.txt) по адресу : "
                + Application.dataPath + "\\" + "terminal.txt@" + "3)Пропишите настроки, сохрание и снова запустите терминал.";
            string[] message_txt = mes.Split(new[] { '@' }, StringSplitOptions.RemoveEmptyEntries);

            dInfo.text += message_txt[0] + "\n";
            dInfo.text += message_txt[1] + "\n";
            dInfo.text += message_txt[2] + "\n";
        }
        else
        {
            string config = File.ReadAllText(Application.dataPath + "\\" + "terminal.txt");
            string[] spConfig = config.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
            adress_ = spConfig[0];
            IpAddress = adress_;
            id_ = int.Parse(spConfig[1]);
            port_ = spConfig[2];
            com_target = spConfig[3];
            Connect(adress_, port_);
            seat_id.text = "SEAT " + id_.ToString();
        }
    }

    private void AddressChangedCallback(object sender, EventArgs e)
    {
        NetworkInterface[] adapters = NetworkInterface.GetAllNetworkInterfaces();
        foreach (NetworkInterface n in adapters)
        {
            Console.WriteLine("{0} is {1}", n.Name, n.OperationalStatus);
        }
    }

    //On extit disable current connection;
    //======================================;
    void OnDisable()
    {
        if (s.Connected)
        {
            s.Disconnect(false);
        }
    }

    //DISABLE LOCKSCREEN;
    //======================================;
    public void ShowLockScreen(bool locked)
    {
        if (locked)
        {
            lockScreen.blocksRaycasts = true;
            lockScreen.DOFade(1.0f, 0.5f);
        }
        else
        {
            lockScreen.DOFade(0.0f, 0.5f).OnComplete(() =>
            {
                lockScreen.blocksRaycasts = false;
            });
        }
    }

}



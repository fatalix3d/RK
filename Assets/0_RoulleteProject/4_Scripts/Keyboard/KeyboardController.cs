using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.IO.Ports;
using System.Text;
using System.Linq;
using System;
using System.IO;

public class KeyboardController : MonoBehaviour
{
    public static SerialPort s_serial;
    private static List<KeyboardController> s_instances = new List<KeyboardController>();
    private int nCoroutineRunning = 0;
    private static float s_lastDataIn = 0;
    private static float s_lastDataCheck = 0;
    public string current_key;

    public int dataRate = 0;

    private bool prev_byte;

    private byte BIT_GL_STATUS_FIND_CARD = 0x01;
    private byte BIT_GL_STATUS_NEW_CARD = 0x02;
    private byte BIT_GL_STATUS_CLIENT_CARD = 0x04;
    private byte BIT_GL_STATUS_OWNER_CARD = 0x08;
    private byte BIT_GL_STATUS_MASTER_CARD = 0x10;

    public static string targetPort;
    public bool time_out;

    void OnEnable()
    {
        s_instances.Add(this);
        OpenPort(9600);
    }

    void Start()
    {
        if (OpenPort())
        {
            StartIncome();
        }
    }

    //Open Port;
    //=======================================;
    public static bool OpenPort(int portSpeed = 9600)
    {
        if (s_serial == null)
        {
            //string portName = GetPortName();
            string portName = NetConnecter.instance.com_target;
            if (portName == "")
            {
                //Debug.Log("Error: Couldn't find serial port.");
                return false;
            }
            else
            {
                Debug.Log("Opening serial port: " + portName);
            }

            s_serial = new SerialPort(portName, portSpeed);
            s_serial.BaudRate = 19200;
            s_serial.Parity = Parity.None;
            s_serial.StopBits = StopBits.One;
            s_serial.DataBits = 8;
            s_serial.Handshake = Handshake.None;
            s_serial.DtrEnable = true;
            s_serial.ReadTimeout = 100;

            s_serial.Open();
            s_serial.DiscardInBuffer();
        }

        return s_serial.IsOpen;
    }

    //Close Port;
    //=======================================;
    void OnDisable()
    {
        //Debug.Log("Serial OnDisable");
        s_instances.Remove(this);
        s_serial.Close();
    }

    //Get port names;
    //=======================================;
    static string GetPortName()
    {
        string[] portNames;


        switch (Application.platform)
        {
            default:
                portNames = System.IO.Ports.SerialPort.GetPortNames();
                if (portNames.Length > 0)
                    return portNames[portNames.Length - 1];
                else
                    return "";
        }
    }

    //Serial Update;
    //=======================================;
    void Update()
    {
        if (s_serial != null && s_serial.IsOpen)
        {
            if (nCoroutineRunning == 0)
            {
                // Each instance has its own coroutine but only one will be active
                s_serial.ReadTimeout = 1;
                StartCoroutine(ReadSerialLoopWin());
            }
            else
            {
                if (nCoroutineRunning > 1)
                    Debug.Log(nCoroutineRunning + " coroutines in " + name);

                nCoroutineRunning = 0;
            }
        }
    }

    public IEnumerator ReadSerialLoop()
    {
        while (true)
        {
            if (!enabled)
            {
                Debug.Log("behaviour not enabled, stopping coroutine");
                yield break;
            }

            //print("ReadSerialLoop ");
            nCoroutineRunning++;

            try
            {
                s_lastDataCheck = Time.time;

                // BytesToRead crashes on Windows -> use ReadLine or ReadByte in a Thread or Coroutine
                while (s_serial.BytesToRead > 0)
                {
                    string serialIn = s_serial.ReadExisting();

                    // Dispatch new data to each instance
                    foreach (KeyboardController inst in s_instances)
                    {
                        inst.receivedData(serialIn);
                    }

                    s_lastDataIn = s_lastDataCheck;
                }

            }
            catch (System.Exception e)
            {
                Debug.Log("System.Exception in serial.ReadExisting: " + e.ToString());
            }

            yield return null;
        }
    }

    public IEnumerator ReadSerialLoopWin()
    {

        while (true)
        {

            if (!enabled)
            {
                yield break;
            }

            nCoroutineRunning++;

            string serialIn = "";
            try
            {
                if (dataRate < 50)
                {
                    serialIn = SerialPortReader.ReadPortData(s_serial, 12);
                }
                else
                {
                    serialIn = SerialPortReader.ReadPortData2(s_serial, 18);
                }
            }
            catch (System.TimeoutException)
            {
                //print ("System.TimeoutException in serial.ReadLine: " + te.ToString ());
            }
            catch (System.Exception e)
            {
                print("System.Exception in serial.ReadLine: " + e.ToString());
            }

            if (serialIn.Length > 0)
            {
                foreach (KeyboardController inst in s_instances)
                {
                    inst.receivedData(serialIn);
                }
            }
            yield return null;
        }
    }

    public IEnumerator StartTimer()
    {
        time_out = true;
        yield return new WaitForSeconds(0.2f);
        if (time_out)
        {
            //Debug.Log("RFID Not responce");
            time_out = false;
            if (dataRate < 50)
            {
                dataRate++;
            }
            else
            {
                dataRate = 0;
            }
        }
    }

    public void StartIncome()
    {
        StartCoroutine(PingKeyboard());
    }

    public IEnumerator PingKeyboard()
    {
        while (true)
        {
            yield return new WaitForSeconds(0.04f);
            Write();

        }
    }

    public void Write()
    {
        if (OpenPort())
        {

            if (dataRate < 50)
            {

                byte[] Command = new byte[9] { 0x55, 0x05, 0x00, 0x41, 0x0C, 0x04, 0x00, 0x00, 0xC3 };
                s_serial.Write(Command, 0, 9);
            }
            else
            {
                byte[] Command_reader = new byte[9] { 0x55, 0x05, 0x00, 0x0A, 0x06, 0x02, 0x00, 0x00, 0x90 };
                s_serial.Write(Command_reader, 0, 9);
                StartCoroutine(StartTimer());
            }
        }
    }

    protected void receivedData(string data)
    {

        byte[] newBytes = Convert.FromBase64String(data);

        //string log = string.Empty;
        //for (int i = 0; i < newBytes.Length; i++)
        //{
        //    log = log + "" + newBytes[i].ToString("X2");
        //}

        if (dataRate < 50) {
            time_out = false;
            dataRate++;
            string key_a = newBytes[7].ToString("X2");
            string key_b = newBytes[8].ToString("X2");
            current_key = key_a + key_b;

            if (key_a != "FF" && key_b != "FF")
            {
                //Debug.Log(current_key);
                switch (key_b)
                {
                    case "AD":
                        current_key = "0x52";
                        GameManager.instance.SimKeyCode(current_key);
                        break;

                    case "9E":
                        current_key = "ORFELINS";
                        GameManager.instance.SimKeyCode(current_key);
                        break;

                    case "9F":
                        current_key = "SERIES58";
                        GameManager.instance.SimKeyCode(current_key);
                        break;

                    case "A0":
                        current_key = "SERIES023";
                        GameManager.instance.SimKeyCode(current_key);
                        break;

                    case "A1":
                        current_key = "SPIEL";
                        GameManager.instance.SimKeyCode(current_key);
                        break;

                    case "A2":
                        current_key = "SOSEDI";
                        GameManager.instance.SelectNeighbors();
                        break;

                    case "A5":
                        current_key = "BET";
                        GameManager.instance.SwitchBetValue();
                        break;

                    case "A6":
                        current_key = "+1";
                        GameManager.instance.SetToLastBet(1);
                        break;

                    case "A7":
                        current_key = "+10";
                        GameManager.instance.SetToLastBet(10);
                        break;

                    case "A8":
                        current_key = "CancelBets";
                        GameManager.instance.CancelLastBetL();
                        break;

                    case "A9":
                        current_key = "CancelAllBets";
                        GameManager.instance.CancelAllBets();
                        break;

                    case "AA":
                        current_key = "REPEAT";
                        GameManager.instance.RestoreLastBet();
                        break;

                    default:
                        current_key = "0x" + key_b;
                        GameManager.instance.SimKeyCode(current_key);
                        break;
                }

            }
            else
            {
                if (key_a == "01" && key_b == "FF")
                {
                    current_key = "0x00";
                    GameManager.instance.SimKeyCode(current_key);
                }
                else
                {
                    current_key = String.Empty;
                }
            }
        }
        else
        {
            time_out = false;
            StopCoroutine(StartTimer());
            dataRate = 0;
            UInt64 num = 0x00;
            num = ReaderProtocolDataUnit.NumberFromBytes(newBytes, 9);
            string numStr = num.ToString();

            if ((newBytes[8] & BIT_GL_STATUS_FIND_CARD) != 0)
            {
                if (!prev_byte)
                {
                    Debug.Log("New card : " + num.ToString());
                    prev_byte = true;
                    NetConnecter.instance.CardPlacedEvent(num.ToString(), newBytes[8].ToString());
                    //Debug.Log("CARD TYPE :" + newBytes[8].ToString());
                }
            }
            else
            {
                if (prev_byte)
                {
                    Debug.Log("Card removed : " + num.ToString());
                    prev_byte = false;
                    NetConnecter.instance.CardRemoveEvent(num.ToString(), newBytes[8].ToString());
                    //Debug.Log("CARD TYPE :" + newBytes[8].ToString());
                }
            }

        }
    }

    void OnGUI()
    {
        //GUILayout.Label("COM : " + current_key);
    }
}

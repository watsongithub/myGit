#region Version Info
/*========================================================================
* 【本类功能概述】
* 
* 作者：wen      时间：#CreateTime#
* 文件名：SerialCommunication
* 版本：V1.0.1
*
* 修改者：          时间：              
* 修改说明：
* ========================================================================
*/
#endregion

using UnityEngine;
using System.Collections;
using System;
using System.IO;
using System.IO.Ports;
using System.Collections.Generic;
using System.Threading;
using System.Text;

public class SerialCommunication : MonoBehaviour
{
    #region 定义串口属性
    //定义基本信息
    private int portNum = 1;//串口名数字
    public string portName = "COM3";//串口名
    public int baudRate = 19200;//波特率
    public Parity parity = Parity.None;//效验位
    public int dataBits = 8;//数据位
    public StopBits stopBits = StopBits.One;//停止位
    SerialPort sp = null;
    Thread dataReceiveThread;
    //发送的消息
    string message = "";
    public List<byte> listReceive = new List<byte>();
    char[] strchar = new char[100];//接收的字符信息转换为字符数组信息
    string str;
    public int strLong = 25;
    public string loadMessage = "";
    public bool IsLoadMessage = false;
    private MainControl mainControl;
    public int num01 = 0;
    public int num02 = 0;
    private bool isInitOver = false;
    private int initNum = 0;
    private bool isInterrupt = false;
    private bool isInt = false;
    private float waitTime = 0;

    #endregion
    void Start()
    {
        mainControl = gameObject.GetComponent<MainControl>();
        OpenPort();
        dataReceiveThread = new Thread(new ThreadStart(DataReceiveFunction));
        dataReceiveThread.Start();
        isInitOver = false;
        isInt = false;
    }
    void Update()
    {
        waitTime += Time.deltaTime;
        if (isInterrupt && waitTime >= 1 && isInt)
        {
            Reconnection();
        }
    }

    public void Reconnection()
    {
        Debug.LogWarning("断线重连");
        //捕获异常时，每隔一秒重连一次
        waitTime = 0;
        ClosePort();
        OpenPort();
        dataReceiveThread = new Thread(new ThreadStart(DataReceiveFunction));
        dataReceiveThread.Start();
        isInitOver = false;
    }

    #region 创建串口，并打开串口
    public void OpenPort()
    {
        portNum = 1;
        while (true)
        {
            string name = portName + portNum;
            //创建串口
            sp = new SerialPort(name, baudRate, parity, dataBits, stopBits);
            sp.ReadTimeout = 400;
            try
            {
                sp.Open();
                isInt = false; 
            }
            catch (Exception ex)
            {
                isInt = true;
                Debug.Log(ex.Message);
            }
            if (sp.IsOpen && !isInterrupt)
            {
                WriteData("01 06 00 08 00 01 C9 C8");
                return;
            }
            if (!isInt && sp.IsOpen)
                return;
            if (portNum >= 10)
                return;
            portNum++;
        }
    }
    #endregion



    #region 程序退出时关闭串口
    void OnApplicationQuit()
    {
        ClosePort();
    }
    public void ClosePort()
    {
        try
        {
            sp.Close();
            dataReceiveThread.Abort();
        }
        catch (Exception ex)
        {
            Debug.Log(ex.Message);
        }
        Debug.Log("close");
    }
    #endregion


    /// <summary>
    /// 打印接收的信息
    /// </summary>
    void PrintData()
    {
        for (int i = 0; i < listReceive.Count; i++)
        {
            strchar[i] = (char)(listReceive[i]);
            str = new string(strchar);
        }
        Debug.Log(str);

    }
    public string strbytes = "";
    int count;
    int missCount = 0;
    #region 接收数据
    void DataReceiveFunction()
    {
        #region 按单个字节发送处理信息，不能接收中文
        //while (sp != null && sp.IsOpen)
        //{
        //    Thread.Sleep(1);
        //    try
        //    {
        //        print(sp.ReadExisting());
        //        byte addr = Convert.ToByte(sp.ReadByte());
        //        sp.DiscardInBuffer();
        //        listReceive.Add(addr);
        //        PrintData();
        //    }
        //    catch
        //    {
        //        IsLoadMessage = false;
        //        //listReceive.Clear();
        //    }
        //}
        #endregion


        #region 按字节数组发送处理信息，信息缺失
        byte[] buffer = new byte[9];
        int bytes = 0;
        strbytes = "";
        while (true)
        {
            bytes = 0;
            buffer = new byte[9];
            if (sp != null && sp.IsOpen)
            {
                try
                {
                    bytes = sp.Read(buffer, 0, buffer.Length);//接收字节
                    if (bytes <= 2)
                    {
                        continue;
                    }
                    else
                    {
                        strbytes = byteToHexStr(buffer);
                        //strbytes = Encoding.Default.GetString(buffer); 
                        IsLoadMessage = true;
                        if (strbytes == "000000000000000000")
                        {
                            missCount++;
                            //Debug.Log(missCount);
                            if (missCount>=100)
                            {
                                missCount = 0;
                                Reconnection();
                            }
                        }
                        else
                        {
                            //Debug.Log(strbytes);
                            missCount = 0;
                            HexStrToInt();
                        }
                        //Debug.Log(System.Convert.ToInt32(strbytes, 16));
                    }
                    isInt = false;
                }
                catch (Exception ex)
                {
                    isInt = true;
                    isInterrupt = true;
                    Debug.Log(ex.Message);
                }
            }
            Thread.Sleep(10);
            loadMessage = strbytes;
            IsLoadMessage = false;
        }
        #endregion
    }
    #endregion



    #region 发送数据
    public void WriteData(string dataStr)
    {
        if (sp.IsOpen)
        {
            byte[] b = strToToHexByte(dataStr);
            //sp.Write()
            sp.Write(b,0,b.Length);
            //listReceive.Clear();
        }
    }

    /// <summary>
    /// 十六进制转字节
    /// </summary>
    /// <param name="hexString"></param>
    /// <returns></returns>
    private static byte[] strToToHexByte(string hexString)
    {
        hexString = hexString.Replace(" ", "");
        if ((hexString.Length % 2) != 0)
            hexString += " ";
        byte[] returnBytes = new byte[hexString.Length / 2];
        for (int i = 0; i < returnBytes.Length; i++)
            returnBytes[i] = Convert.ToByte(hexString.Substring(i * 2, 2), 16);
        return returnBytes;
    }
    /// <summary>
    /// 字节转十六进制
    /// </summary>
    /// <param name="bytes"></param>
    /// <returns></returns>
    private static string byteToHexStr(byte[] bytes)
    {
        string returnStr = "";
        if (bytes != null)
        {
            for (int i = 0; i < bytes.Length; i++)
            {
                returnStr += bytes[i].ToString("X2");
            }
        }
        return returnStr;
    }
    private void HexStrToInt()
    {
        //strbytes
        string[] sArray = new string[0];

        try
        {
            for (int i = 2; i < strbytes.Length; i += 2 + 1)
                strbytes = strbytes.Insert(i, ",");
            sArray = strbytes.Split(',');
        }
        catch (Exception e)
        {
            Debug.LogError(e.Message);
            return;
        }

        if (Convert.ToInt32(sArray[0]) <= 1)
            return;
        //print(sArray[0] + sArray[1] + sArray[2] + sArray[3] + sArray[4] + sArray[5]);
        num01 = Convert.ToInt32(sArray[2] + sArray[3] + sArray[4] + sArray[5], 16);
        if (num02 != num01)
            print(num01);
        num02 = num01;
        //print(num01);
        //最大移动距离
        int maxLoght = mainControl.configurationFileData.MaxDistance / mainControl.configurationFileData.Perimeter * 1024 - mainControl.configurationFileData.FrameLong;
        //print(maxLoght);
        //现在位置

        mainControl.mNowPosition = (float)num01 / (float)maxLoght;

        if (mainControl.mNowPosition > 1 && mainControl.mNowPosition < 2)
        {
            mainControl.mNowPosition = 1;
        }
        else if (mainControl.mNowPosition >= 0 && mainControl.mNowPosition <= 1)
        {
            mainControl.mNowPosition = (float)num01 / (float)maxLoght;
        }
        else
        {
            mainControl.mNowPosition = 0;
        }
    }

    #endregion
}
//****************************************************************************
// Description: only use tcp socket to send and receive message
// Should achieve pack and unpack logic by yourself
// Author: hiramtan@live.com
//****************************************************************************

using HiSocket;
using System;
using System.Collections.Generic;

using UnityEngine;

public class Test : MonoBehaviour
{
    //private ITcp _tcp;
    private TcpConnection _tcp;

    //private IPackage _packer = new Packer();
    // Use this for initialization
    void Start()
    {
        //_tcp = new TcpConnection(_packer);
        _tcp = TcpConnection.getInstance();
        _tcp.StateChangeEvent += OnState;
        _tcp.ReceiveEvent += OnReceive;
        Connect();
        _tcp.TreadRun();
        Send();
    }

    void Connect()
    {
        _tcp.Connect("127.0.0.1", 7777);
    }

    // Update is called once per frame
    void Update()
    {
        //_tcp.Run();
    }

    void OnState(SocketState state)
    {
        //Debug.Log("current state is: " + state);
        if (state == SocketState.Connected)
        { 
            Debug.Log("socket Connected");
        }

        if (state == SocketState.Connecting){
            Debug.Log("socket Conneting");
        }

        if (state == SocketState.DisConnected){
            Debug.Log("socket DisConnected");
        }
    }

    void Send()
    {
        HINetworkData hinfo = new HINetworkData();
        HINetworkItem item = new HINetworkItem();
        item.ID = 123;
        item.Name = "myalex";
        hinfo.ndada.Add("objname1", item);

        //物件轉位元組陣列
        byte[] MemberABytes = HIUtils.ToByteArray(hinfo);

	//action=0x11,chann=0x22,id=0x33,data
        byte[] data = HIUtils.JoinHeaderBytes(0x11, 0x22, 0x33, MemberABytes);
        _tcp.Send( data );
    }

    private void OnApplicationQuit()
    {
        if (_tcp.IsConnected)
            _tcp.DisConnect();
    }

    void OnReceive(byte[] bytes)
    {
        //Debug.Log("receive bytes: " + bytes.Length);
        //string msg = Encoding.Unicode.GetString(bytes, 0,bytes.Length);
        Debug.Log("OnReceive");
        //action
        Debug.Log(bytes[0].ToString("X2")); 
        //chann
        Debug.Log(bytes[1].ToString("X2"));
        //id
        Debug.Log(bytes[2].ToString("X2"));

        byte[] data = HIUtils.SplitHeaderBytes(bytes);
        HINetworkData info = (HINetworkData)HIUtils.ToObject(data);
        Debug.Log( info.ndada["objname1"].Name );
    }

}

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
    }

    void Connect()
    {
        _tcp.Connect("127.0.0.1", 7777);
    }

    // Update is called once per frame
    void Update()
    {
        _tcp.Run();
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
        byte[] data = HIUtils.JoinHeaderBytes(0x04, 0x22, new byte[]{ 0x33, 0x44}, MemberABytes);
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

        //ID
        Debug.Log(bytes[2].ToString("X2"));
        Debug.Log(bytes[3].ToString("X2"));

        byte[] data = HIUtils.SplitHeaderBytes(bytes);
        HINetworkData info = (HINetworkData)HIUtils.ToObject(data);
        Debug.Log( info.ndada["objname1"].Name );
    }


    //test
    void OnGUI(){
        if (GUI.Button(new Rect(0, 0, 100, 25), "Connect"))
        {
            Debug.Log("onGui Connect");
            _tcp.Connect("127.0.0.1", 7777);
            _tcp.ID = new byte[]{ 0x01, 0x01 };
        }

        if (GUI.Button(new Rect(0, 25, 100, 25), "DisConnect"))
        {
            Debug.Log("onGui DisConnect");
            _tcp.DisConnect();
        }

        if (GUI.Button(new Rect(0, 50, 100, 25), "SubScribe"))
        {
            Debug.Log("onGui SubScribe");
            _tcp.SubScribe(ref _tcp, 0x99);
        }

        if (GUI.Button(new Rect(0, 75, 100, 25), "UnSubscribe"))
        {
            Debug.Log("onGui UnSubscribe");
            _tcp.UnSubscribe(ref _tcp,0x99);
        }

        if (GUI.Button(new Rect(0, 100, 100, 25), "Send"))
        {
            Debug.Log("onGui Send"); 

            HINetworkData hinfo = new HINetworkData();
            HINetworkItem item = new HINetworkItem();
            item.ID = 123;
            item.Name = "myalex";
            hinfo.ndada.Add("objname1", item);

            //物件轉位元組陣列
            byte[] MemberABytes = HIUtils.ToByteArray(hinfo);
            _tcp.HISend(ref _tcp,0x99, MemberABytes);
        }

        if (GUI.Button(new Rect(0, 125, 100, 25), "TEST"))
        {
            Debug.Log("onGui TEST");
            _tcp.HISend(ref _tcp, 0x99, new byte[]{0x11});
        }

    }
}


using System;
using System.Collections;
using System.IO;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using UnityEngine;
using WebSocketSharp;
using static DanmakuManager;
using static DanmakuModel;

/// <summary>
/// 与弹幕API通信，发送接收数据包
/// </summary>
public class DanmakuLoader : MonoBehaviour
{
    const string danmakuAPI = "ws://broadcastlv.chat.bilibili.com:2244/sub";
    const string bilibiliLiveAPI = "https://api.live.bilibili.com/room/v1/Room/room_init?id={0}";

    public long RoomID;
    WebSocket socket;

    private void Start()
    {
        RoomID = getRoomID(RoomID);
        InitConnection();
        StartCoroutine(sendHeartBeat_Cor());
    }
    //建立链接
    public void InitConnection()
    {
        socket = new WebSocket(danmakuAPI);
        socket.OnOpen += (sender, e) =>
        {
            Debug.Log("链接成功");
            sendJoinRoom();
            //StartCoroutine(sendHeartBeat_Cor());
        };
        socket.OnMessage += (sender, e) =>
        {
            //Debug.Log("接受包");
            byte[] buffer = e.RawData;
            var packet = Decode(buffer);
            var packets = DecodeDanmakuPacket(packet);

            switch (packets[0].operation)
            {
                case 8:
                    Debug.Log("Join Room Successful");
                    break;
                case 3:
                    Debug.Log("Receive Heart Beat Packet Response");
                    break;
                case 5:
                    foreach (var p in packets)
                    {
                        Danmaku d = ProcessDanmaku(p.data);
                        switch (d.type)
                        {
                            case "DANMU_MSG":
                                DanmakuManager.Instance.AddDanmaku(DanmakuType.DM, d);
                                break;
                            case "SEND_GIFT":
                                DanmakuManager.Instance.AddDanmaku(DanmakuType.GIFT, d);
                                break;
                        }
                    }
                    break;
            }
        };
        socket.OnError += (sender, e) =>
        {
            Debug.Log("ERROR: " + e.Exception.ToString());
        };

        socket.OnClose += (sender, e) =>
        {
            Debug.Log("链接关闭");
        };
        socket.ConnectAsync();
    }

    long getRoomID(long id)
    {
        string liveAPI = String.Format(bilibiliLiveAPI, id);
        WebClient wc = new WebClient();
        Stream stream = wc.OpenRead(liveAPI);
        string jsonStr = (new StreamReader(stream)).ReadToEnd();
        stream.Close();
        var str = Regex.Match(jsonStr, "\\\"room_id\\\":[0-9]+").ToString().Split(':')[1];
        return Int64.Parse(str);

    }
    public void sendJoinRoom()
    {
        string joinInfo = $"{{\"uid\": 0,\"roomid\": {RoomID},\"protover\": 1,\"platform\": \"web\",\"clientver\": \"1.4.0\"}}";
        var data = Encoding.UTF8.GetBytes(joinInfo);
        Packet joinPacket = Pack(OP_JOIN_ROOM, data);
        byte[] buffer = DanmakuModel.Encode(joinPacket);
        socket.Send(buffer);
        Debug.Log("发送入房包");
    }

    IEnumerator sendHeartBeat_Cor()
    {
        while (true)
        {
            Debug.Log("发送心跳包");
            yield return new WaitForSeconds(30);
            byte[] heart_beat = Encode(Pack(OP_HEART_BEAT, new byte[0]));
            socket.Send(heart_beat);
        }
    }
}

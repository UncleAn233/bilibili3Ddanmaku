using System.Collections.Generic;
using UnityEngine;
using BitConverter;
using Ionic.Zlib;
using Newtonsoft.Json.Linq;
using System;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

/// <summary>
/// socket通信时所用到的一些工具及方法
/// </summary>
public class DanmakuModel : MonoBehaviour
{

    //通信包结构
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct Packet
    {
        public Int32 pack_length;   //包长度 一般为 header_length+data.length
        public Int16 header_length; //头长度 一般为16
        public Int16 protocol_ver;  //协议版本 小于2时表示数据未压缩
        public Int32 operation; //操作类型 参见下方
        public Int32 seq_id;    //不知道干嘛用的 反正写1
        public byte[] data; //具体数据
    }

    //从Packet提取的有用的数据结构
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct PacketData
    {
        public Int32 operation; //
        public dynamic data;
    }

    //最终弹幕结构
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct Danmaku
    {
        public string type;   //类型：弹幕or礼物
        public string user; //用户
        public string content;  //内容：弹幕内容or礼物名
        public int num; //礼物数量
    }

    public const int OP_HEART_BEAT = 2;   // Client. Heartbeat Packet, every 30s.
    public const int OP_HEART_BEAT_RESP = 3;   // Server. Heartbeat Response
    public const int OP_NOTIFICATION = 5;   // Server. Event Notification
    public const int OP_JOIN_ROOM = 7;   // Client. Enter a room
    public const int OP_JOIN_ROOM_RESP = 8;   // Server. Response to entering a room.

    //将数据包编码为字节流
    public static byte[] Encode(Packet packet)
    {
        var buffer = new byte[packet.pack_length];

        var packLen = EndianBitConverter.BigEndian.GetBytes(buffer.Length);
        var headerLen = EndianBitConverter.BigEndian.GetBytes(packet.header_length);
        var ver = EndianBitConverter.BigEndian.GetBytes(packet.protocol_ver);
        var op = EndianBitConverter.BigEndian.GetBytes(packet.operation);
        var seq = EndianBitConverter.BigEndian.GetBytes(packet.seq_id);

        buffer = packLen.Concat<byte>(headerLen).Concat<byte>(ver).Concat<byte>(op).Concat<byte>(seq).Concat<byte>(packet.data).ToArray();

        return buffer;
    }
    //将字节流解码为数据包
    public static Packet Decode(byte[] buffer)
    {
        Packet result = new Packet();

        result.pack_length = (Int32)(buffer[0] << 24) | (buffer[1] << 16) | (buffer[2] << 8) | buffer[3];
        result.header_length = (Int16)((buffer[4] << 8) | buffer[5]);
        result.protocol_ver = (Int16)((buffer[6] << 8) | buffer[7]);
        result.operation = (Int32)(buffer[8] << 24) | (buffer[9] << 16) | (buffer[10] << 8) | buffer[11];
        result.seq_id = (Int32)(buffer[12] << 24) | (buffer[13] << 16) | (buffer[14] << 8) | buffer[15];

        result.data = new byte[result.pack_length - result.header_length];

        Array.Copy(buffer, 16, result.data, 0, result.pack_length - result.header_length);

        return result;
    }
    //打包数据
    public static Packet Pack(int operation, byte[] data)
    {
        return new Packet { pack_length = 16 + data.Length, header_length = 16, protocol_ver = 1, operation = operation, seq_id = 1, data = data };
    }

    //将接收回来的包解码并提取数据
    public static PacketData[] DecodeDanmakuPacket(Packet packet)
    {
        PacketData result = new PacketData();
        List<PacketData> results = new List<PacketData>();

        result.operation = packet.operation;

        switch (packet.operation)
        {
            case OP_HEART_BEAT_RESP:    //心跳包的回复 当前人气值
                if (packet.protocol_ver >= 2)
                {
                    byte[] unCompressedRaw = ZlibStream.UncompressBuffer(packet.data);
                    result.data = JObject.Parse(Encoding.UTF8.GetString(unCompressedRaw.Skip(16).ToArray()));
                }
                else
                {
                    result.data = (Int32)(packet.data[0] << 24) | (packet.data[1] << 16) | (packet.data[2] << 8) | packet.data[3];
                }
                results.Add(result);
                break;
            case OP_JOIN_ROOM_RESP:     //入房包的回复
                result.data = true;
                results.Add(result);
                break;
            case OP_NOTIFICATION:   //弹幕和礼物
                String jsonData;
                if (packet.protocol_ver >= 2)   //当协议版本大于等于2时数据为zlib压缩数据
                {
                    byte[] unCompressedRaw = ZlibStream.UncompressBuffer(packet.data);
                    int startByte = 0;

                    while (startByte < unCompressedRaw.Length)  //一个包可能包含多条弹幕数据
                    {
                        Packet nextPacket = Decode(unCompressedRaw.Skip(startByte).ToArray());
                        startByte += nextPacket.pack_length;
                        results.AddRange(DecodeDanmakuPacket(nextPacket));
                    }
                }
                else
                {
                    jsonData = Encoding.UTF8.GetString(packet.data);
                    result.data = JObject.Parse(jsonData);
                    results.Add(result);
                }
                break;
            default:
                result.data = null;
                results.Add(result);
                break;
        }
        return results.ToArray();
    }

    //提取弹幕/礼物数据
    public static Danmaku ProcessDanmaku(JObject danmakuJS)
    {
        string danmaku_cmd = ((JValue)danmakuJS["cmd"]).ToString();
        Danmaku danmaku;
        switch (danmaku_cmd)
        {
            case "DANMU_MSG":
                JArray danmaku_info = (JArray)danmakuJS["info"];
                danmaku = new Danmaku { type = danmaku_cmd, user = danmaku_info[2][1].ToString(), content = danmaku_info[1].ToString() };
                break;
            case "SEND_GIFT":
                JObject danmaku_data = (JObject)danmakuJS["data"];
                danmaku = new Danmaku { type = danmaku_cmd, user = danmaku_data["uname"].ToString(), content = danmaku_data["giftName"].ToString(), num = Int32.Parse(danmaku_data["num"].ToString()) };
                break;
            default:
                danmaku = new Danmaku();
                break;
        }
        return danmaku;
    }
}

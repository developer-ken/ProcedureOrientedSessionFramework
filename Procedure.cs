using Mirai_CSharp;
using Mirai_CSharp.Models;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace TaskOrientedSessionFramework
{
    public abstract class Procedure : IEquatable<Procedure>, IEquatable<Procedure.Message>
    {
        public struct Message
        {
            /// <summary>
            /// 消息来源类型
            /// </summary>
            public SourceType Source;
            /// <summary>
            /// 如果是好友消息，获取好友的信息
            /// </summary>
            public IFriendInfo Friend;
            /// <summary>
            /// 如果是来自群(私)聊的消息，获取群员信息
            /// </summary>
            public IGroupMemberInfo GroupMember;
            /// <summary>
            /// 消息内容
            /// <para>如果消息含有多种内容类型，该值为第一个</para>
            /// </summary>
            public IMessageBase Content;
            /// <summary>
            /// 消息完整内容
            /// </summary>
            public IMessageBase[] ContentChain;
        }

        public enum SourceType
        {
            Friend = 0, Group = 1, Temp = 2
        }

        /// <summary>
        /// Flag: 是否已经执行完成
        /// </summary>
        public bool IsFinished { get; protected set; }

        /// <summary>
        /// 消息队列
        /// </summary>
        private Queue<Message> queue;

        /// <summary>
        /// 会话
        /// <para>建议使用框架接口收发消息，而不是直接操作Session</para>
        /// </summary>
        protected MiraiHttpSession Session { get; private set; }

        /// <summary>
        /// 触发本次流程的消息
        /// </summary>
        protected readonly Message InitMessage;

        /// <summary>
        /// 对方QQ号
        /// </summary>
        public long qq => UserInfo.Id;

        /// <summary>
        /// 对方来源群号
        /// <para>如果不是群(私)聊会抛出错误</para>
        /// </summary>
        public long groupid => GroupInfo.Id;

        /// <summary>
        /// 来源类型
        /// </summary>
        public SourceType Stype => InitMessage.Source;

        /// <summary>
        /// 对方的信息
        /// </summary>
        public IBaseInfo UserInfo => InitMessage.Source switch
        {
            SourceType.Friend => InitMessage.Friend,
            SourceType.Temp or SourceType.Group => InitMessage.GroupMember,
            _ => throw new InvalidOperationException(),
        };

        /// <summary>
        /// 群信息
        /// <para>如果不是群(私)聊会抛出错误</para>
        /// </summary>
        public IGroupInfo GroupInfo => InitMessage.Source switch
        {
            SourceType.Friend => throw new InvalidOperationException(),
            SourceType.Temp or SourceType.Group => InitMessage.GroupMember.Group,
            _ => throw new InvalidOperationException(),
        };

        /// <summary>
        /// 单个流程
        /// </summary>
        /// <param name="initmsg">触发流程的消息</param>
        /// <param name="session">Mirai主会话</param>
        public Procedure(Message initmsg, MiraiHttpSession session)
        {
            queue = new Queue<Message>();
            this.Session = session;
            InitMessage = initmsg;
            queue.Enqueue(InitMessage);
            session.FriendMessageEvt += Session_FriendMessageEvt;
            session.GroupMessageEvt += Session_GroupMessageEvt;
            session.TempMessageEvt += Session_TempMessageEvt;
        }

        public async Task<bool> Session_TempMessageEvt(MiraiHttpSession sender, ITempMessageEventArgs e)
        {
            RECV_MSG(new Message { ContentChain = e.Chain, Content = e.Chain[1], GroupMember = e.Sender, Source = SourceType.Temp });
            return false;
        }

        public async Task<bool> Session_GroupMessageEvt(MiraiHttpSession sender, IGroupMessageEventArgs e)
        {
            RECV_MSG(new Message { ContentChain = e.Chain, Content = e.Chain[1], GroupMember = e.Sender, Source = SourceType.Group });
            return false;
        }

        public async Task<bool> Session_FriendMessageEvt(MiraiHttpSession sender, Mirai_CSharp.Models.IFriendMessageEventArgs e)
        {
            RECV_MSG(new Message { ContentChain = e.Chain, Content = e.Chain[1], Friend = e.Sender, Source = SourceType.Friend });
            return false;
        }

        /// <summary>
        /// 主函数，被系统自动执行
        /// </summary>
        public abstract void Main();

        /// <summary>
        /// 开始执行主函数
        /// <para>不要在内部调用！！</para>
        /// </summary>
        public void RunMain()
        {
            Main();
            Abort();
        }

        /// <summary>
        /// 停止当前流程
        /// <para>内部需要结束流程时应当主动结束Main函数[而不是]执行这个函数</para>
        /// </summary>
        public void Abort()
        {
            ThrowFinished();
            IsFinished = true;
        }

        /// <summary>
        /// 登记新接收的消息
        /// <para>传入的消息会经过二次检查</para>
        /// </summary>
        /// <param name="msg">传入的消息</param>
        public void RECV_MSG(Message msg)
        {
            if (!IsFinished && IsMyMsg(msg)) queue.Enqueue(msg);
        }

        /// <summary>
        /// 如果流程已经结束则抛出一个错误
        /// </summary>
        private void ThrowFinished()
        {
            if (IsFinished) throw new InvalidOperationException("Can't handle the procedure when it is aborted.");
        }

        /// <summary>
        /// 判断一条消息是否应当被当前流程处理
        /// </summary>
        /// <param name="msg">传入的消息</param>
        /// <returns></returns>
        protected bool IsMyMsg(Message msg)
        {
            if (Stype != msg.Source) return false;
            switch (Stype)
            {
                case SourceType.Friend:
                    return qq == msg.Friend.Id;
                case SourceType.Temp:
                case SourceType.Group:
                    return qq == msg.GroupMember.Id && groupid == msg.GroupMember.Group.Id;
                default:
                    throw new InvalidOperationException();
            }
        }

        /// <summary>
        /// 接收一条消息
        /// </summary>
        /// <returns>收到的消息</returns>
        protected Message ReadItem()
        {
            ThrowFinished();
            try
            {
                while (queue.Count < 1)
                    Thread.Sleep(0);
            }
            catch { }
            return queue.Dequeue();
        }

        protected string ReadLine()
        {
            return GetStringMessage(ReadItem().ContentChain);
        }

        public static string GetStringMessage(params IMessageBase[] chain)
        {
            StringBuilder sb = new StringBuilder();
            foreach (IMessageBase msg in chain)
            {
                if (msg.Type == "Plain")
                {
                    PlainMessage plmsg = (PlainMessage)msg;
                    sb.Append(plmsg.Message);
                }
            }
            return sb.ToString();
        }

        /// <summary>
        /// 发送一条文本消息
        /// </summary>
        /// <param name="str">文本内容</param>
        protected void WriteLine(string str)
        {
            WriteObject(Txt(str));
        }

        /// <summary>
        /// 从Bitmap生成图片消息
        /// </summary>
        /// <param name="pic">Bitmap图片</param>
        /// <returns>图片消息</returns>
        protected ImageMessage Pic(Bitmap pic)
        {
            MemoryStream ms = new MemoryStream();
            pic.Save(ms, System.Drawing.Imaging.ImageFormat.Png);
            ms.Position = 0;
            ImageMessage msg = Session.UploadPictureAsync((UploadTarget)Stype, ms).Result;
            ms.Close();
            return msg;
        }

        protected PlainMessage Txt(string txt)
        {
            return new PlainMessage(txt);
        }

        /// <summary>
        /// 发送一条图片消息
        /// </summary>
        /// <param name="picture">图片</param>
        protected void WritePicture(Bitmap picture)
        {
            WriteObject(Pic(picture));
        }

        /// <summary>
        /// 发送一条复合消息
        /// </summary>
        /// <param name="msgchain">消息内容表</param>
        protected void WriteObject(params IMessageBase[] msgchain)
        {
            ThrowFinished();
            switch (Stype)
            {
                case SourceType.Friend:
                    Session.SendFriendMessageAsync(qq, msgchain);
                    break;
                case SourceType.Temp:
                    Session.SendTempMessageAsync(qq, groupid, msgchain);
                    break;
                case SourceType.Group:
                    Session.SendGroupMessageAsync(groupid, msgchain);
                    break;
                default:
                    throw new InvalidOperationException();
            }
        }

        public bool Equals(Procedure other)
        {
            return IsMyMsg(other.InitMessage);
        }

        public bool Equals(Message other)
        {
            return IsMyMsg(other);
        }
    }
}

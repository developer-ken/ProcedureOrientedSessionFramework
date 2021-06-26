using Mirai_CSharp;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ProcedureOrientedSessionFramework
{
    public class SessionHandler
    {
        private readonly MiraiHttpSession session;
        private readonly List<Procedure> ProcedureList;
        private readonly Func<Procedure.Message, MiraiHttpSession, Procedure> GetProcedure;

        public SessionHandler(MiraiHttpSession mirai, Func<Procedure.Message, MiraiHttpSession, Procedure> getprocedureobj)
        {
            ProcedureList = new List<Procedure>();
            GetProcedure = getprocedureobj;
            session = mirai;
            session.FriendMessageEvt += Session_FriendMessageEvt;
            session.GroupMessageEvt += Session_GroupMessageEvt;
            session.TempMessageEvt += Session_TempMessageEvt;
        }

        public bool IsProcedureExists(Procedure.Message msg)
        {
            List<Procedure> toremove = new List<Procedure>();
            bool hit = false;
            foreach (Procedure p in ProcedureList)
            {
                if (p.IsFinished)
                {
                    toremove.Add(p);
                }
                else
                if (p.Equals(msg)) { hit = true; }
            }
            foreach (Procedure p in toremove) { ProcedureList.Remove(p); }
            toremove.Clear();
            return hit;
        }

        private async System.Threading.Tasks.Task<bool> Session_TempMessageEvt(MiraiHttpSession sender, Mirai_CSharp.Models.ITempMessageEventArgs e)
        {
            var msg = new Procedure.Message { ContentChain = e.Chain, Content = e.Chain[1], GroupMember = e.Sender, Source = Procedure.SourceType.Temp };
            if (!IsProcedureExists(msg))
            {
                var pro = GetProcedure(msg, session);
                if (pro == null) { return false; }
                ProcedureList.Add(pro);
                Task.Run(pro.RunMain);
            }
            return false;
        }

        private async System.Threading.Tasks.Task<bool> Session_GroupMessageEvt(MiraiHttpSession sender, Mirai_CSharp.Models.IGroupMessageEventArgs e)
        {
            var msg = new Procedure.Message { ContentChain = e.Chain, Content = e.Chain[1], GroupMember = e.Sender, Source = Procedure.SourceType.Group };
            if (!IsProcedureExists(msg))
            {
                var pro = GetProcedure(msg, session);
                if (pro == null) return false;
                ProcedureList.Add(pro);
                Task.Run(pro.RunMain);
            }
            return false;
        }

        private async System.Threading.Tasks.Task<bool> Session_FriendMessageEvt(MiraiHttpSession sender, Mirai_CSharp.Models.IFriendMessageEventArgs e)
        {
            var msg = new Procedure.Message { ContentChain = e.Chain, Content = e.Chain[1], Friend = e.Sender, Source = Procedure.SourceType.Friend };
            if (!IsProcedureExists(msg))
            {
                var pro = GetProcedure(msg, session);
                if (pro == null) { return false; }
                ProcedureList.Add(pro);
                Task.Run(pro.RunMain);
            }
            return false;
        }
    }
}
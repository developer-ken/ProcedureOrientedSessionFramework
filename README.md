# ProcedureOrientedSessionFramework

[![Codacy Badge](https://app.codacy.com/project/badge/Grade/98c45dd9417943b0892e7d2f5bed4023)](https://www.codacy.com/gh/developer-ken/ProcedureOrientedSessionFramework/dashboard?utm_source=github.com&amp;utm_medium=referral&amp;utm_content=developer-ken/ProcedureOrientedSessionFramework&amp;utm_campaign=Badge_Grade)

[![NuGet version (ProcedureOrientedSessionFramework)](https://img.shields.io/nuget/v/ProcedureOrientedSessionFramework.svg?style=flat)](https://www.nuget.org/packages/ProcedureOrientedSessionFramework/)

#### 介绍
面向对象基于会话的Mirai机器人开发框架

#### 为什么需要
新手开发机器人时，常因为各种风骚的异步写法无法简单上手。  
本框架倒行逆施，将异步写法封装成了同步的，主要业务流程可以直接顺序写完，方便新手上手，在编写一些基于固定顺序逻辑的项目时也很方便。

#### 安装教程

建议[使用Nuget安装](https://www.nuget.org/packages/ProcedureOrientedSessionFramework)
也可以克隆本仓库并创建项目引用

#### 示例代码

In program.cs write:
```csharp
static void Main(){
    long qq=123456;//Your qq logged in on Mirai
    var opt = new MiraiHttpSessionOptions();
    //Fill your own options here
    var session = new MiraiHttpSession();
    session.ConnectAsync(options, qq).Wait();
    
    SessionHandler sh = new SessionHandler(session, (msg,sess)=>new MyProcedure(msg,sess));
    // SessionHandler call its second parameter evey time to get a user defined procedure
    // when a message is received and no procedure exists to handle that session.
    // If no procedure should be started, return null is fine.
}
```

Create a new class called MyProcedure and let it inherit ProcedureOrientedSessionFramework.Procedure:

```csharp
    using ProcedureOrientedSessionFramework;

    class MyProcedure : Procedure
    {
        public MyProcedure (Procedure.Message msg, MiraiHttpSession ss) : base(msg, ss) { }
        // Always remember to pass the constructor parameters to base class.
        // It will handle many operation for you.

        public override void Main()
        {
            ReadLine();
            // The first read message operation (any kind of read) gets the message that led to the procedure creation.
            // To avoid confusion, we get rid of it first.

            string str = ReadLine();// Read all the text components in the next message.
            WriteLine("You wrote:" + str);// Send a simple text message to user.

            //Send mixed message (with texts and pictures, in any order and amount you provide) to user.
            WriteObject(Txt("This is a block of string"),
            Pic(new Bitmap())//This is a block of image. Accepts Bitmap format, and handle it as PNG files.
            );
        }
    }
```

namespace MediaPortal2.Alexa
{
    using MediaPortal.Common;
    using MediaPortal.Common.Async;
    using MediaPortal.Common.Logging;
    using MediaPortal.Plugins.SlimTv.Client.Models;
    using MediaPortal.Plugins.SlimTv.Interfaces;
    using MediaPortal.Plugins.SlimTv.Interfaces.Items;
    using MediaPortal.UI.Presentation.Workflow;
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Linq;
    using System.Runtime.CompilerServices;
    using System.Text.RegularExpressions;
    using System.Threading.Tasks;

    public class TvChannelHelper
    {
        private readonly ILogger log;
        private IList<IChannel> channels;

        public TvChannelHelper(ILogger log)
        {
            this.log = log;
        }

        [AsyncStateMachine(typeof(<CacheChannels>d__3)), DebuggerStepThrough]
        private Task CacheChannels()
        {
            <CacheChannels>d__3 stateMachine = new <CacheChannels>d__3 {
                <>t__builder = AsyncTaskMethodBuilder.Create(),
                <>4__this = this,
                <>1__state = -1
            };
            stateMachine.<>t__builder.Start<<CacheChannels>d__3>(ref stateMachine);
            return stateMachine.<>t__builder.Task;
        }

        public string ChangeChannel(string channelName)
        {
            string str;
            if (ReferenceEquals(this.channels, null))
            {
                Task.Run(new Func<Task>(this.CacheChannels)).Wait();
            }
            if (this.channels.Count == 0)
            {
                this.log.Info("Alexa did not get any TV Channels from MediaPortal", Array.Empty<object>());
                str = null;
            }
            else
            {
                string name = StripNonLettersAndDigits(channelName);
                object[] args = new object[] { name };
                this.log.Debug("Alexa looking for channel '{0}'", args);
                IChannel channel = this.channels.FirstOrDefault<IChannel>(c => IsChannelNameEqual(name, c.Name));
                if (channel == null)
                {
                    this.log.Info("Alexa found no matching channel", Array.Empty<object>());
                    str = null;
                }
                else
                {
                    object[] objArray2 = new object[] { channel.Name };
                    this.log.Info("Alexa found channel '{0}'", objArray2);
                    Task.Run((Func<Task>) (() => this.Tune(channel)));
                    str = channel.Name;
                }
            }
            return str;
        }

        private static bool IsChannelNameEqual(string searchName, string channelName)
        {
            string str = StripNonLettersAndDigits(channelName);
            return ((str == searchName) || (str.EndsWith("hd") ? (str.Substring(0, str.Length - 2).Trim() == searchName) : (str.EndsWith("television") ? (str.Substring(0, str.Length - 10).Trim() == searchName) : false)));
        }

        private static string StripNonLettersAndDigits(string channelName) => 
            Regex.Replace(channelName.ToLowerInvariant(), @"[^\p{L}0-9]", string.Empty, RegexOptions.Compiled);

        [AsyncStateMachine(typeof(<Tune>d__5)), DebuggerStepThrough]
        private Task Tune(IChannel channel)
        {
            <Tune>d__5 stateMachine = new <Tune>d__5 {
                <>t__builder = AsyncTaskMethodBuilder.Create(),
                <>4__this = this,
                channel = channel,
                <>1__state = -1
            };
            stateMachine.<>t__builder.Start<<Tune>d__5>(ref stateMachine);
            return stateMachine.<>t__builder.Task;
        }

        [Serializable, CompilerGenerated]
        private sealed class <>c
        {
            public static readonly TvChannelHelper.<>c <>9 = new TvChannelHelper.<>c();
            public static Func<IChannel, string> <>9__3_0;

            internal string <CacheChannels>b__3_0(IChannel c) => 
                c.Name;
        }

        [CompilerGenerated]
        private sealed class <CacheChannels>d__3 : IAsyncStateMachine
        {
            public int <>1__state;
            public AsyncTaskMethodBuilder <>t__builder;
            public TvChannelHelper <>4__this;
            private ITvHandler <tvHandler>5__1;
            private IEnumerator<IChannelGroup> <>s__2;
            private AsyncResult<IList<IChannelGroup>> <>s__3;
            private IChannelGroup <channelGroup>5__4;
            private IEnumerator<IChannel> <>s__5;
            private AsyncResult<IList<IChannel>> <>s__6;
            private IChannel <channel>5__7;
            private TaskAwaiter<AsyncResult<IList<IChannelGroup>>> <>u__1;
            private TaskAwaiter<AsyncResult<IList<IChannel>>> <>u__2;

            private void MoveNext()
            {
                int num = this.<>1__state;
                try
                {
                    TaskAwaiter<AsyncResult<IList<IChannelGroup>>> awaiter;
                    TvChannelHelper.<CacheChannels>d__3 d__;
                    if (num == 0)
                    {
                        awaiter = this.<>u__1;
                        this.<>u__1 = new TaskAwaiter<AsyncResult<IList<IChannelGroup>>>();
                        this.<>1__state = num = -1;
                        goto TR_001D;
                    }
                    else if (num == 1)
                    {
                        goto TR_001C;
                    }
                    else
                    {
                        this.<>4__this.channels = new List<IChannel>();
                        if (ServiceRegistration.IsRegistered<ITvHandler>())
                        {
                            this.<tvHandler>5__1 = ServiceRegistration.Get<ITvHandler>();
                            awaiter = this.<tvHandler>5__1.ChannelAndGroupInfo.GetChannelGroupsAsync().GetAwaiter();
                            if (awaiter.IsCompleted)
                            {
                                goto TR_001D;
                            }
                            else
                            {
                                this.<>1__state = num = 0;
                                this.<>u__1 = awaiter;
                                d__ = this;
                                this.<>t__builder.AwaitUnsafeOnCompleted<TaskAwaiter<AsyncResult<IList<IChannelGroup>>>, TvChannelHelper.<CacheChannels>d__3>(ref awaiter, ref d__);
                            }
                            return;
                        }
                        else
                        {
                            this.<>4__this.log.Info("Alexa: Changing Channels won't be possible: No tv handler registered", Array.Empty<object>());
                        }
                        goto TR_0002;
                    }
                    return;
                TR_000A:
                    this.<>s__2 = null;
                    this.<>4__this.log.Info($"Alexa found {this.<>4__this.channels.Count} TV channels to choose from", Array.Empty<object>());
                    Func<IChannel, string> selector = TvChannelHelper.<>c.<>9__3_0;
                    if (TvChannelHelper.<>c.<>9__3_0 == null)
                    {
                        Func<IChannel, string> local1 = TvChannelHelper.<>c.<>9__3_0;
                        selector = TvChannelHelper.<>c.<>9__3_0 = new Func<IChannel, string>(this.<CacheChannels>b__3_0);
                    }
                    this.<>4__this.log.Debug($"Alexa found {this.<>4__this.channels.Count} TV channels to choose from: {string.Join("|", this.<>4__this.channels.Select<IChannel, string>(selector).ToArray<string>())}", Array.Empty<object>());
                    this.<tvHandler>5__1 = null;
                    goto TR_0002;
                TR_001C:
                    try
                    {
                        TaskAwaiter<AsyncResult<IList<IChannel>>> awaiter2;
                        if (num == 1)
                        {
                            awaiter2 = this.<>u__2;
                            this.<>u__2 = new TaskAwaiter<AsyncResult<IList<IChannel>>>();
                            this.<>1__state = num = -1;
                        }
                        else
                        {
                            goto TR_0019;
                        }
                    TR_0015:
                        this.<>s__6 = awaiter2.GetResult();
                        this.<>s__5 = this.<>s__6.Result.GetEnumerator();
                        this.<>s__6 = null;
                        try
                        {
                            while (this.<>s__5.MoveNext())
                            {
                                this.<channel>5__7 = this.<>s__5.Current;
                                this.<>4__this.channels.Add(this.<channel>5__7);
                                this.<channel>5__7 = null;
                            }
                        }
                        finally
                        {
                            if ((num < 0) && (this.<>s__5 != null))
                            {
                                this.<>s__5.Dispose();
                            }
                        }
                        this.<>s__5 = null;
                        this.<channelGroup>5__4 = null;
                    TR_0019:
                        while (true)
                        {
                            if (this.<>s__2.MoveNext())
                            {
                                this.<channelGroup>5__4 = this.<>s__2.Current;
                                awaiter2 = this.<tvHandler>5__1.ChannelAndGroupInfo.GetChannelsAsync(this.<channelGroup>5__4).GetAwaiter();
                                if (awaiter2.IsCompleted)
                                {
                                    goto TR_0015;
                                }
                                else
                                {
                                    this.<>1__state = num = 1;
                                    this.<>u__2 = awaiter2;
                                    d__ = this;
                                    this.<>t__builder.AwaitUnsafeOnCompleted<TaskAwaiter<AsyncResult<IList<IChannel>>>, TvChannelHelper.<CacheChannels>d__3>(ref awaiter2, ref d__);
                                }
                            }
                            else
                            {
                                goto TR_000A;
                            }
                            break;
                        }
                        return;
                    }
                    finally
                    {
                        if ((num < 0) && (this.<>s__2 != null))
                        {
                            this.<>s__2.Dispose();
                        }
                    }
                TR_001D:
                    this.<>s__3 = awaiter.GetResult();
                    this.<>s__2 = this.<>s__3.Result.GetEnumerator();
                    this.<>s__3 = null;
                    goto TR_001C;
                }
                catch (Exception exception)
                {
                    this.<>1__state = -2;
                    this.<>t__builder.SetException(exception);
                    return;
                }
            TR_0002:
                this.<>1__state = -2;
                this.<>t__builder.SetResult();
            }

            [DebuggerHidden]
            private void SetStateMachine(IAsyncStateMachine stateMachine)
            {
            }
        }

        [CompilerGenerated]
        private sealed class <Tune>d__5 : IAsyncStateMachine
        {
            public int <>1__state;
            public AsyncTaskMethodBuilder <>t__builder;
            public IChannel channel;
            public TvChannelHelper <>4__this;
            private IWorkflowManager <workflowManager>5__1;
            private SlimTvClientModel <model>5__2;
            private TaskAwaiter<bool> <>u__1;

            private void MoveNext()
            {
                int num = this.<>1__state;
                try
                {
                    TaskAwaiter<bool> awaiter;
                    if (num == 0)
                    {
                        awaiter = this.<>u__1;
                        this.<>u__1 = new TaskAwaiter<bool>();
                        this.<>1__state = num = -1;
                    }
                    else
                    {
                        this.<workflowManager>5__1 = ServiceRegistration.Get<IWorkflowManager>();
                        this.<model>5__2 = this.<workflowManager>5__1.GetModel(SlimTvClientModel.MODEL_ID) as SlimTvClientModel;
                        if (this.<model>5__2 == null)
                        {
                            goto TR_0003;
                        }
                        else
                        {
                            awaiter = this.<model>5__2.Tune(this.channel).GetAwaiter();
                            if (awaiter.IsCompleted)
                            {
                                goto TR_0004;
                            }
                            else
                            {
                                this.<>1__state = num = 0;
                                this.<>u__1 = awaiter;
                                TvChannelHelper.<Tune>d__5 stateMachine = this;
                                this.<>t__builder.AwaitUnsafeOnCompleted<TaskAwaiter<bool>, TvChannelHelper.<Tune>d__5>(ref awaiter, ref stateMachine);
                            }
                        }
                        return;
                    }
                TR_0004:
                    awaiter.GetResult();
                }
                catch (Exception exception)
                {
                    this.<>1__state = -2;
                    this.<workflowManager>5__1 = null;
                    this.<model>5__2 = null;
                    this.<>t__builder.SetException(exception);
                    return;
                }
            TR_0003:
                this.<>1__state = -2;
                this.<workflowManager>5__1 = null;
                this.<model>5__2 = null;
                this.<>t__builder.SetResult();
            }

            [DebuggerHidden]
            private void SetStateMachine(IAsyncStateMachine stateMachine)
            {
            }
        }
    }
}


﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using UnityEngine;
using System.Linq.Expressions;
using System.Collections;
using System.Text.RegularExpressions;
using KSPAPIExtensions.PartMessage;
using DeftTech.DuckTyping;

namespace KSPAPIExtensions.PartMessage
{
    #region Duck Typing interfaces
    /// <summary>
    /// Interface to allow duck casting of message listeners.
    /// 
    /// <b>Do not change this interface or duck casting will fail.</b>
    /// </summary>
    internal interface IPartMessageListenerV1
    {
        Type DelegateType { get; }

        GameSceneFilter Scenes { get; }

        PartRelationship Relations { get; }
    }

    /// <summary>
    /// Interface to allow duck casting of messages.
    /// 
    /// <b>Do not change this interface or duck casting will fail.</b>
    /// </summary>
    internal interface IPartMessageDelegateV1
    {
        Type Parent { get; }

        bool IsAbstract { get; }
    }
    #endregion

    #region Current Event Info
    internal class CurrentEventInfoImpl : ICurrentEventInfo, IDisposable
    {
        #region Internal Bits
        [ThreadStatic]
        internal static CurrentEventInfoImpl current;

#if DEBUG
        private bool onStack = false;
#endif
        private CurrentEventInfoImpl previous;

        internal CurrentEventInfoImpl(IPartMessage message, object source, Part part, object[] args)
        {
            Source = source;
            SourcePart = part;
            Message = message;
            Arguments = args;
        }

        internal IDisposable Push()
        {
#if DEBUG
            if(onStack)
                throw new InvalidProgramException("Pushing message onto the stack when it's already on it");
            onStack = true;
#endif

            previous = current;
            current = this;
            return (IDisposable)this;
        }

        void IDisposable.Dispose()
        {
#if DEBUG
            if (!onStack)
                throw new InvalidProgramException("Disposed called when not on the stack.");
#endif

            current = previous;
            previous = null;
            onStack = false;
        }

#if DEBUG
        ~CurrentEventInfoImpl()
        {
            if (onStack)
            {
                Debug.LogError("CurrentEventInfoImpl somehow left on the call stack");
            }

        }
#endif

        #endregion

        #region Interface methods

        public IPartMessage Message { get; private set; }

        public object Source { get; private set; }

        public Part SourcePart { get; private set; }

        public object[] Arguments { get; private set; }

        public PartModule SourceModule { get { return Source as PartModule; } }

        public PartRelationship SourceRelationTo(Part destPart)
        {
            return PartUtils.RelationTo(SourcePart, destPart);
        }

        public override string ToString()
        {
            return string.Format("CurrentEventInfoImpl(Message:{0}, Source:{1}, SourcePart:{2}, Arguments.Length={3})", Message, Source, SourcePart, (Arguments == null) ? -1 : Arguments.Length);
        }
        #endregion
    }

    internal class CurrentEventInfoCompare : IEqualityComparer<ICurrentEventInfo>
    {
        public static CurrentEventInfoCompare Instance = new CurrentEventInfoCompare();

        public bool Equals(ICurrentEventInfo x, ICurrentEventInfo y)
        {
            if (x == y)
                return true;

            if (x.Source != y.Source)
                return false;
            if (x.Message.Name != y.Message.Name)
                return false;
            if (x.Arguments.Length != y.Arguments.Length)
                return false;
            for (int i = 0; i < x.Arguments.Length; i++)
                if (!x.Arguments[i].Equals(y.Arguments[i]))
                    return false;
            return true;
        }

        public int GetHashCode(ICurrentEventInfo obj)
        {
            if (obj == null)
                return -1;

            int hashCode =
                obj.Source.GetHashCode()
                ^ ((obj.SourcePart == null) ? 0 : obj.SourcePart.GetHashCode())
                ^ obj.Message.Name.GetHashCode()
                ^ obj.Arguments.Length;
            foreach (object arg in obj.Arguments)
                hashCode ^= (arg == null ? 0 : arg.GetHashCode());
            return hashCode;
        }
    }

    #endregion

    #region Part Message
    internal class MessageImpl : IPartMessage
    {
        private IPartMessageDelegateV1 ifMsg;
        internal MessageImpl parent;

        internal MessageImpl(ServiceImpl service, Type message)
        {
            if (!typeof(Delegate).IsAssignableFrom(message))
                throw new ArgumentException("Message type " + message + " is not a delegate type");

            Attribute attribute;
            foreach(Attribute attr in message.GetCustomAttributes(false))
                if (attr.GetType().FullName == typeof(PartMessageDelegate).FullName)
                {
                    attribute = attr;
                    goto foundAttribute;
                }
            throw new ArgumentException("Message does not have the PartMessageDelegate attribute");

            foundAttribute:
            DelegateType = message;

            ifMsg = DuckTyping.Cast<IPartMessageDelegateV1>(attribute);
            if(ifMsg.Parent != null)
                parent = (MessageImpl)service.AsIPartMessage(ifMsg.Parent);
        }

        public string Name
        {
            get { return DelegateType.FullName; }
        }

        public Type DelegateType
        {
            get;
            private set;
        }

        public IPartMessage Parent
        {
            get { return parent; }
        }

        public bool IsAbstract
        {
            get { return ifMsg.IsAbstract; }
        }

        public IEnumerator<IPartMessage> GetEnumerator()
        {
            return new Enumerator() {
                head = this
            };
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        private class Enumerator : IEnumerator<IPartMessage>
        {
            internal MessageImpl head;
            private MessageImpl current;
            private bool atEnd = false;

            public IPartMessage Current
            {
                get 
                {
                    if(head == null)
                        throw new InvalidOperationException("Iterator disposed");
                    if (current == null)
                        throw new InvalidOperationException("Iterator is at " + (atEnd?"end":"start"));
                    return current;
                }
            }

            public void Dispose()
            {
                current = head = null;
                atEnd = true;
            }

            object IEnumerator.Current
            {
                get { return Current; }
            }

            public bool MoveNext()
            {
                if (head == null)
                    throw new InvalidOperationException("Iterator disposed");
                if (atEnd)
                    throw new InvalidOperationException("Iterator is at end");
                if (current == null)
                    current = head;
                else
                    current = current.parent;
                return !(atEnd = (current == null));
            }

            public void Reset()
            {
                current = null;
                atEnd = false;
            }
        }

        public override string ToString()
        {
            return Name;
        }
    }
    #endregion

    internal class ServiceImpl : MonoBehaviour, IPartMessageService
    {
        public ICurrentEventInfo CurrentEventInfo
        {
            get {
                if (CurrentEventInfoImpl.current == null)
                    throw new InvalidOperationException("Cannot retrieve source info as not currently in invocation.");

                return CurrentEventInfoImpl.current;
            }
        }

        #region Registration
        /// <summary>
        /// Scan an object for message events and message listeners and hook them up.
        /// Note that all references are dumped on game scene change, so objects must be rescanned when reloaded.
        /// </summary>
        /// <param name="obj">the object to scan</param>
        public void Register<T>(T obj)
        {
            Type objType = typeof(T);

            foreach (MethodInfo meth in objType.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly))
            {
                foreach (object attr in meth.GetCustomAttributes(true))
                    if(attr.GetType().FullName == typeof(PartMessageListener).FullName)
                        AddListener(obj, meth, AsListener(attr));
            }

            foreach (EventInfo evt in objType.GetEvents(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly))
            {
                foreach (object attr in evt.GetCustomAttributes(true))
                    if (attr.GetType().FullName == typeof(PartMessageEvent).FullName)
                    {
                        goto foundEvent;
                    }
                continue;

            foundEvent:
                Type deleg = evt.EventHandlerType;

                // sanity check
                foreach (object attr in evt.GetCustomAttributes(true))
                    if (attr.GetType().FullName == typeof(PartMessageEvent).FullName)
                    {
                        goto checkedDelegate;
                    }

                Debug.LogWarning(string.Format("[PartMessageService] Event: {0} in class: {1} declares an event with a part message, but does not have the PartMessageEvent attribute. Will ignore", evt.Name, objType.FullName));
                continue;

            checkedDelegate:
                GenerateEventHandoff(obj, evt);
            }
        }

        private IPartMessageListenerV1 AsListener(object obj) 
        {
            // TODO: We may want to do something clever with this in the future...
            return DuckTyping.Cast<IPartMessageListenerV1>(obj);
        }
        #endregion

        #region Listeners and Event Delegates

        private Dictionary<string, LinkedList<ListenerInfo>> listeners = new Dictionary<string, LinkedList<ListenerInfo>>();

        private class ListenerInfo
        {
            public WeakReference targetRef;
            public MethodInfo method;
            public IPartMessageListenerV1 attr;

            public LinkedListNode<ListenerInfo> node;

            public object Target
            {
                get
                {
                    return targetRef.Target;
                }
            }

            public Part TargetPart
            {
                get
                {
                    object target = this.Target;
                    return AsPart(target);
                }
            }

            public PartModule TargetModule
            {
                get
                {
                    return Target as PartModule;
                }
            }

            public bool CheckPrereq(ICurrentEventInfo info)
            {
                if (!attr.Scenes.IsLoaded())
                    return false;
                if (!PartUtils.RelationTest(info.SourcePart, TargetPart, attr.Relations))
                    return false;
                return true;
            }
        }

        private void AddListener(object target, MethodInfo meth, IPartMessageListenerV1 attr)
        {
            if (!attr.Scenes.IsLoaded())
                return;

            if (Delegate.CreateDelegate(attr.DelegateType, target, meth, false) == null)
            {
                Debug.LogError(string.Format("PartMessageListener method {0}.{1} does not support the delegate type {2} as declared in the attribute", meth.DeclaringType, meth.Name, attr.DelegateType.FullName));
                return;
            }

            string message = attr.DelegateType.FullName;

            LinkedList<ListenerInfo> listenerList;
            if (!listeners.TryGetValue(message, out listenerList))
            {
                listenerList = new LinkedList<ListenerInfo>();
                listeners.Add(message, listenerList);
            }

            ListenerInfo info = new ListenerInfo()
            {
                targetRef = new WeakReference(target),
                method = meth,
                attr = attr
            };
            info.node = listenerList.AddLast(info);
        }

        private static readonly MethodInfo handoffSend = typeof(ServiceImpl).GetMethod("Send", BindingFlags.Instance | BindingFlags.Public, null, new Type[] { typeof(Type), typeof(object), typeof(Part), typeof(object[]) }, null);

        private void GenerateEventHandoff(object source, EventInfo evt)
        {
            MethodAttributes addAttrs = evt.GetAddMethod(true).Attributes;
            Part part = AsPart(source);

            // This generates a dynamic method that pulls the properties of the event
            // plus the arguments passed and hands it off to the EventHandler method below.
            Type message = evt.EventHandlerType;
            MethodInfo m = message.GetMethod("Invoke", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);


            ParameterInfo[] pLst = m.GetParameters();
            ParameterExpression[] peLst = new ParameterExpression[pLst.Length];
            Expression[] cvrt = new Expression[pLst.Length];
            for (int i = 0; i < pLst.Length; i++)
            {
                peLst[i] = Expression.Parameter(pLst[i].ParameterType, pLst[i].Name);
                cvrt[i] = Expression.Convert(peLst[i], typeof(object));
            }
            Expression createArr = Expression.NewArrayInit(typeof(object), cvrt);

            Expression invoke = Expression.Call(Expression.Constant(this), handoffSend,
                Expression.Constant(message), Expression.Constant(source), Expression.Constant(part), createArr);

            Delegate d = Expression.Lambda(message, invoke, peLst).Compile();

            // Shouldn't need to use a weak delegate here.
            evt.AddEventHandler(source, d);
        }

        #endregion

        #region Message delivery

        public void Send<T>(object source, params object[] args)
        {
            Send(typeof(T), source, AsPart(source), args);
        }

        public void Send(Type message, object source, params object[] args)
        {
            Send(message, source, AsPart(source), args);
        }

        public void Send<T>(object source, Part part, params object[] args)
        {
            Send(typeof(T), source, part, args);
        }

        public void Send(Type messageCls, object source, Part part, params object[] args)
        {
            IPartMessage message = AsIPartMessage(messageCls);
            CurrentEventInfoImpl info = new CurrentEventInfoImpl(message, source, part, args);
            Send(info);
        }

        internal void Send(CurrentEventInfoImpl message)
        {
            if (!gameObject)
                return;

            using (message.Push())
            {
                if (filters != null)
                    foreach (FilterInfo info in filters)
                        if (info.CheckPrereq(message) && info.Filter(message))
                            return;

                // Send the message
                foreach (IPartMessage currMessage in message.Message)
                {
                    string messageName = currMessage.Name;

                    LinkedList<ListenerInfo> listenerList;
                    if (!listeners.TryGetValue(messageName, out listenerList))
                        continue;

                    // Shorten parameter list if required
                    object[] newArgs = null;

                    for (var node = listenerList.First; node != null; )
                    {
                        // hold reference for duration of call
                        ListenerInfo info = node.Value;
                        object target = info.Target;
                        if (target == null)
                        {
                            // Remove dead links from the list
                            var tmp = node;
                            node = node.Next;
                            listenerList.Remove(tmp);
                            continue;
                        }

                        // Declarative event filtering
                        if (!info.CheckPrereq(message))
                        {
                            node = node.Next;
                            continue;
                        }


                        if (newArgs == null)
                            newArgs = ShortenArgs(message.Arguments, currMessage.DelegateType);

                        try
                        {
                            node.Value.method.Invoke(target, newArgs);
                        }
                        catch (TargetException ex)
                        {
                            // Swallow target exceptions, but not anything else.
                            Debug.LogError(string.Format("Invoking {0}.{1} to handle DelegateType {2} resulted in an exception.", target.GetType(), node.Value.method, CurrentEventInfo.Message));
                            Debug.LogException(ex.InnerException);
                        }

                        node = node.Next;
                    }

                }
            }
        }

        private static object[] ShortenArgs(object[] args, Type messageCls)
        {
            ParameterInfo[] methodParams = messageCls.GetMethod("Invoke").GetParameters();
            object[] newArgs = args;
            if (args.Length > methodParams.Length)
            {
                newArgs = new object[methodParams.Length];
                Array.Copy(args, newArgs, methodParams.Length);
            }
            return newArgs;
        }
        #endregion

        #region Message Filters

        // Store the list of current filters in a thread static
        [ThreadStatic]
        private static LinkedList<FilterInfo> filters;

        /// <summary>
        /// Register a message filter. This delegate will be called for every message sent from the source.
        /// If it returns true, the message is considered handled and no futher processing will occour.
        /// </summary>
        /// <param name="filter">The delegate for the filter</param>
        /// <param name="source">Message source, must match. If null will match all sources.</param>
        /// <param name="part">Part to filter. If null will match all parts.</param>
        /// <param name="messages">Optional list of messages to match. If empty, all messages are matched.</param>
        /// <returns>Disposable object. When done call dispose. Works well with using clauses.</returns>
        public IDisposable Filter(PartMessageFilter filter, object source = null, Part part = null, params Type[] messages)
        {
            FilterInfo info = new FilterInfo();
            info.Filter = filter;

            RegisterFilterInfo(source, part, messages, info);

            return info;
        }

        /// <summary>
        /// Consolidate messages. All messages sent by the source will be held until the returned object is destroyed.
        /// Any duplicates of the same message will be swallowed silently.
        /// </summary>
        /// <param name="source">source to consolidate from. Null will match all sources</param>
        /// <param name="part">Part to filter. If null will match all parts.</param>
        /// <param name="messages">messages to consolidate. If not specified, all messages are consolidated.</param>
        /// <returns>Disposable object. When done call dispose. Works well with using clauses.</returns>
        public IDisposable Consolidate(object source = null, Part part = null, params Type[] messages)
        {
            FilterInfo consolidator = new MessageConsolidator();

            RegisterFilterInfo(source, part, messages, consolidator);

            return consolidator;
        }

        /// <summary>
        /// Ignore messages sent by the source until the returned object is destroyed.
        /// </summary>
        /// <param name="source">Source to ignore. Null will ignore all sources.</param>
        /// <param name="part">Part to filter. If null will match all parts.</param>
        /// <param name="messages">Messages to ignore. If not specified, all messages are ignored.</param>
        /// <returns>Disposable object. When done call dispose. Works well with using clauses.</returns>
        public IDisposable Ignore(object source = null, Part part = null, params Type[] messages)
        {
            return Filter((message) => true, source, part, messages);
        }

        private void RegisterFilterInfo(object source, Part part, Type[] messages, FilterInfo info)
        {
            info.source = source;
            info.part = part;
            info.service = this;

            foreach (Type message in messages)
                info.messages.Add(message.FullName);

            if(ServiceImpl.filters == null)
                ServiceImpl.filters = new LinkedList<FilterInfo>();

            info.node = ServiceImpl.filters.AddFirst(info);
        }

        internal class FilterInfo : IDisposable
        {
            public object source;
            public Part part;
            public PartMessageFilter Filter;
            public HashSet<string> messages = new HashSet<string>();

            public ServiceImpl service;

            public LinkedListNode<FilterInfo> node;

            public bool CheckPrereq(ICurrentEventInfo info)
            {
                if (this.source != null && this.source != info.Source)
                    return false;
                if (this.part != null && this.part != info.SourcePart)
                    return false;
                if (this.messages.Count == 0)
                    return true;

                foreach(IPartMessage message in info.Message)
                    if (!this.messages.Contains(message.Name))
                        return true;
                return false;
            }

            public virtual void Dispose()
            {
                if(service == null)
                    throw new InvalidOperationException("Already disposed");

                ServiceImpl.filters.Remove(node);
                if (ServiceImpl.filters.Count == 0)
                    ServiceImpl.filters = null;
                service = null;
            }

            ~FilterInfo()
            {
                if (service == null)
                    return;
                Dispose();
                Debug.LogError("Warning: Filter has been created and not disposed prior to finalization. Please check the code.");
            }
        }

        internal class MessageConsolidator : FilterInfo
        {
            public MessageConsolidator()
            {
                this.Filter = ConsolidatingFilter;
            }

            private HashSet<ICurrentEventInfo> messageSet = new HashSet<ICurrentEventInfo>(CurrentEventInfoCompare.Instance);
            private List<CurrentEventInfoImpl> messageList = new List<CurrentEventInfoImpl>();

            private bool ConsolidatingFilter(ICurrentEventInfo message)
            {
                if (messageSet.Add(message))
                    messageList.Add((CurrentEventInfoImpl)message);
                return true;
            }

            public override void Dispose()
            {
                ServiceImpl service = this.service;

                base.Dispose();

                // Safe as we've already deregistered the filter, so no loops.
                foreach (ICurrentEventInfo message in messageList)
                    service.Send((CurrentEventInfoImpl)message);
                
                messageSet = null;
                messageList = null;
            }

        }
        #endregion

        #region Startup

        private EventData<GameScenes>.OnEvent sceneChangedListener;

        internal void Awake() 
        {
            // Clear the listeners list when reloaded.
            sceneChangedListener = scene => listeners.Clear();
            GameEvents.onGameSceneLoadRequested.Add(sceneChangedListener);
        }

        internal void OnDestroy()
        {
            GameEvents.onGameSceneLoadRequested.Remove(sceneChangedListener);
            sceneChangedListener = null;
            listeners = null;
        }

        #endregion

        #region Conversion to IPartMessage
        private Dictionary<Type, IPartMessage> cachedPartMessages = new Dictionary<Type, IPartMessage>();

        /// <summary>
        /// Convert delegate type into the IPartMessage interface.
        /// </summary>
        /// <param name="type">Delegate type to convert. This must be a delegate type marked with the <see cref="PartMessageDelegate"/> attribute.</param>
        public IPartMessage AsIPartMessage(Type type)
        {
            IPartMessage value;
            if (cachedPartMessages.TryGetValue(type, out value))
                return value;
            return cachedPartMessages[type] = new MessageImpl(this, type);
        }

        /// <summary>
        /// Convert delegate type into the IPartMessage interface.
        /// </summary>
        /// <typeparam name="T">Delegate type to convert. This must be a delegate type marked with the <see cref="PartMessageDelegate"/> attribute.</typeparam>
        public IPartMessage AsIPartMessage<T>()
        {
            return AsIPartMessage(typeof(T));
        }
        #endregion

        #region Utility Functions
        private static Part AsPart(object src) 
        {
            if (src is Part)
                return (Part)src;
            if (src is PartModule)
                return ((PartModule)src).part;
            if (src is PartMessagePartProxy)
                return ((PartMessagePartProxy)src).ProxyPart;
            return null;
        }
        #endregion
    }

    #region Initialization
    [KSPAddonFixed(KSPAddon.Startup.Instantly, false, typeof(PartMessageServiceInitializer))]
    internal class PartMessageServiceInitializer : MonoBehaviour
    {
        internal void Update()
        {
            // Use the Update method to be sure this runs *after* module manager. (MM used OnGUI)

            // If we're not ready to run, return and wait for the next Update
            if (!GameDatabase.Instance.IsReady() && !GameSceneFilter.AnyInitializing.IsLoaded())
                return;

            // If we are loaded from the first loaded assembly that has this class, then we are responsible to destroy
            var candidates = (from ass in AssemblyLoader.loadedAssemblies
                              where ass.assembly.GetType(typeof(PartMessageService).FullName, false) != null
                              orderby ass.assembly.GetName().Version descending, ass.path ascending
                              select ass).ToArray();
            var winner = candidates.First();

            if (Assembly.GetExecutingAssembly() != winner.assembly)
            {
                // We are not the winner, return.
                UnityEngine.Object.Destroy(gameObject);
                enabled = false;
                return;
            }

            if (candidates.Length > 1)
            {
                string losers = string.Join("\n", (from t in candidates
                                                   where t != winner
                                                   select string.Format("Version: {0} Location: {1}", t.assembly.GetName().Version, t.path)).ToArray());

                Debug.Log("[PartMessageService] version " + winner.assembly.GetName().Version + " at " + winner.path + " won the election against\n" + losers);
            }
            else
                Debug.Log("[PartMessageService] Elected unopposed version= " + winner.assembly.GetName().Version + " at " + winner.path);

            // So at this point we know we have won the election, and will be using the class versions as in this assembly.

            // Destroy the old service
            if (PartMessageService._instance != null)
            {
                Debug.Log("[PartMessageService] destroying service from previous load");
                UnityEngine.Object.Destroy(((ServiceImpl)PartMessageService._instance).gameObject);
            }

            // Create the part message service
            GameObject serviceGo = new GameObject(PartMessageService.partMessageServiceName);
            UnityEngine.Object.DontDestroyOnLoad(serviceGo);

            // Assign the service to the static variable
            PartMessageService._instance = serviceGo.AddComponent<ServiceImpl>();

            // At this point the losers will duck-type themselves to the latest version of the service if they're called.

            // Destroy ourself because there's no reason to still hang around
            UnityEngine.Object.Destroy(gameObject);
            enabled = false;
        }


    }
    #endregion

    #region Message Enumerator
    internal class MessageEnumerable : IEnumerable<Type>
    {
        internal MessageEnumerable(Type message)
        {
            this.message = message;
        }

        readonly internal Type message;

        IEnumerator<Type> IEnumerable<Type>.GetEnumerator()
        {
            return new MessageEnumerator(message);
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return new MessageEnumerator(message);
        }

        private class MessageEnumerator : IEnumerator<Type>
        {

            public MessageEnumerator(Type top)
            {
                this.current = this.top = top;
            }

            private int pos = -1;
            private Type current;
            private Type top;

            object IEnumerator.Current
            {
                get
                {
                    if (pos != 0)
                        throw new InvalidOperationException();
                    return current;
                }
            }

            Type IEnumerator<Type>.Current
            {
                get
                {
                    if (pos != 0)
                        throw new InvalidOperationException();
                    return current;
                }
            }

            bool IEnumerator.MoveNext()
            {
                switch (pos)
                {
                    case -1:
                        current = top;
                        pos = 0;
                        break;
                    case 1:
                        return false;
                    case 0:
                        PartMessageDelegate evt = (PartMessageDelegate)current.GetCustomAttributes(typeof(PartMessageDelegate), true)[0];
                        current = evt.Parent;
                        break;
                    case 2:
                        throw new InvalidOperationException("Enumerator disposed");
                }
                if (current == null)
                {
                    pos = 1;
                    return false;
                }
                return true;
            }

            void IEnumerator.Reset()
            {
                pos = -1;
                current = null;
            }

            void IDisposable.Dispose()
            {
                current = top = null;
                pos = 2;
            }
        }
    }
    #endregion

}
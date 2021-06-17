using Newtonsoft.Json;
using Simplic.Collections.Generic;
using Simplic.Flow.Configuration;
using Simplic.Flow.Event;
using Simplic.Flow.EventQueue;
using Simplic.Flow.Log;
using Simplic.Flow.Node;
using Simplic.Flow.Node.IO;
using Simplic.FlowInstance;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Unity;

namespace Simplic.Flow.Service
{
    public class FlowService : IFlowService
    {
        #region Events
        private event EventHandler OnStarted;
        private event EventHandler OnCompleted;
        private object eventLock = new Object();

        event EventHandler IFlowService.OnStarted
        {
            add
            {
                lock (eventLock)
                {
                    OnStarted += value;
                }
            }
            remove
            {
                lock (eventLock)
                {
                    OnStarted -= value;
                }
            }
        }

        event EventHandler IFlowService.OnCompleted
        {
            add
            {
                lock (eventLock)
                {
                    OnCompleted += value;
                }
            }
            remove
            {
                lock (eventLock)
                {
                    OnCompleted -= value;
                }
            }
        }
        #endregion

        #region Private Members                
        private IList<FlowConfiguration> flowConfigurations;
        private IList<ActiveFlow.ActiveFlow> finishedInstances = new List<ActiveFlow.ActiveFlow>();
        private ConcurrentList<ThreadStateInfo> executions = new ConcurrentList<ThreadStateInfo>();
        private IDictionary<string, IList<EventDelegate>> eventDelegates;
        private Dequeue<EventCall> eventQueue;

        private readonly IUnityContainer unityContainer;
        private readonly IFlowInstanceService flowInstanceService;
        private readonly IFlowConfigurationService flowConfigurationService;
        private readonly IFlowEventQueueService flowEventQueueService;
        private readonly IFlowEventService flowEventService;
        private readonly IFlowLogService flowLogService;

        private string machineName;
        private string serviceName;
        #endregion

        #region Constructor
        public FlowService(IUnityContainer unityContainer)
        {
            this.unityContainer = unityContainer;

            Flows = new List<Flow>();
            ActiveFlows = new List<Simplic.ActiveFlow.ActiveFlow>();
            eventQueue = new Dequeue<EventCall>();
            eventDelegates = new Dictionary<string, IList<EventDelegate>>();

            this.flowInstanceService = unityContainer.Resolve<IFlowInstanceService>();
            this.flowConfigurationService = unityContainer.Resolve<IFlowConfigurationService>();
            this.flowEventQueueService = unityContainer.Resolve<IFlowEventQueueService>();
            this.flowEventService = unityContainer.Resolve<IFlowEventService>();
            this.flowLogService = unityContainer.Resolve<IFlowLogService>();

            unityContainer.RegisterType<INodeResolver, GenericNodeResolver<ConsoleWriteLineNode>>("ConsoleWriteLineNode");
            unityContainer.RegisterType<INodeResolver, GenericNodeResolver<ForeachNode>>("ForeachNode");
            unityContainer.RegisterType<INodeResolver, GenericNodeResolver<SequenceNode>>("SequenceNode");
            unityContainer.RegisterType<INodeResolver, GenericNodeResolver<StartWithConditionNode>>("StartWithConditionNode");
            unityContainer.RegisterType<INodeResolver, GenericNodeResolver<EndWithConditionNode>>("EndWithConditionNode");
            unityContainer.RegisterType<INodeResolver, GenericNodeResolver<DeleteFileNode>>("DeleteFileNode");
            unityContainer.RegisterType<INodeResolver, GenericNodeResolver<GetDirectoryContentNode>>(nameof(GetDirectoryContentNode));
            unityContainer.RegisterType<INodeResolver, GenericNodeResolver<OnCheckDirectoryContentNode>>(nameof(OnCheckDirectoryContentNode));
            unityContainer.RegisterType<INodeResolver, GenericNodeResolver<GetFileExtensionNode>>("GetFileExtensionNode");
            unityContainer.RegisterType<INodeResolver, GenericNodeResolver<GetVariableNode>>(nameof(GetVariableNode));
            unityContainer.RegisterType<INodeResolver, GenericNodeResolver<SetVariableNode>>(nameof(SetVariableNode));
            unityContainer.RegisterType<INodeResolver, GenericNodeResolver<AddNode>>(nameof(AddNode));
            unityContainer.RegisterType<INodeResolver, GenericNodeResolver<DivideNode>>(nameof(DivideNode));
            unityContainer.RegisterType<INodeResolver, GenericNodeResolver<MultiplyNode>>(nameof(MultiplyNode));
            unityContainer.RegisterType<INodeResolver, GenericNodeResolver<SubtractNode>>(nameof(SubtractNode));
            unityContainer.RegisterType<INodeResolver, GenericNodeResolver<ReadAllTextNode>>(nameof(ReadAllTextNode));
            unityContainer.RegisterType<INodeResolver, GenericNodeResolver<ReadAllBytesNode>>(nameof(ReadAllBytesNode));
            unityContainer.RegisterType<INodeResolver, GenericNodeResolver<ClearPinNode>>(nameof(ClearPinNode));
            unityContainer.RegisterType<INodeResolver, GenericNodeResolver<ThreadSleepNode>>(nameof(ThreadSleepNode));

            unityContainer.RegisterType<INodeResolver, GenericNodeResolver<ToStringNode>>(nameof(ToStringNode));
            unityContainer.RegisterType<INodeResolver, GenericNodeResolver<DateTimeNowNode>>(nameof(DateTimeNowNode));
            unityContainer.RegisterType<INodeResolver, GenericNodeResolver<ConcatStringNode>>(nameof(ConcatStringNode));

            unityContainer.RegisterType<INodeResolver, GenericNodeResolver<ExecuteFlowNode>>(nameof(ExecuteFlowNode));
            unityContainer.RegisterType<INodeResolver, GenericNodeResolver<OnExecuteFlowEvent>>(nameof(OnExecuteFlowEvent));

            unityContainer.RegisterType<INodeResolver, GenericNodeResolver<ProcessStartNode>>(nameof(ProcessStartNode));

            unityContainer.RegisterType<INodeResolver, GenericNodeResolver<IfConditionNode>>(nameof(IfConditionNode));
        }
        #endregion

        #region Private Methods

        #region [CreateActiveFlowsFrom]
        /// <summary>
        /// Creates <see cref="ActiveFlow.ActiveFlow"/> objects from a list of <see cref="FlowInstance.FlowInstance"/> objects
        /// </summary>
        /// <param name="flowInstances">Objects to create from</param>
        private void CreateActiveFlowsFrom(List<FlowInstance> flowInstances)
        {
            if (flowInstances.Count > 0)
                flowLogService.Info($"> Creating active flows from {string.Join(",", flowInstances.Select(x => $"\"{x.Flow.Name}\""))}");

            foreach (var flowInstance in flowInstances)
            {
                CreateActiveFlow(flowInstance);
            }

            if (ActiveFlows.Count > 0)
                flowLogService.Info($"> {ActiveFlows.Count} active flows were created.");
        }
        #endregion

        #region [CreateActiveFlow]
        /// <summary>
        /// CreateActiveFlow
        /// </summary>
        /// <param name="flowInstance"></param>
        private void CreateActiveFlow(FlowInstance flowInstance)
        {
            if (flowInstance.Flow == null)
                flowInstance.Flow = Flows.FirstOrDefault(x => x.Id == flowInstance.FlowId);

            if (flowInstance.IsAlive)
            {
                var activeFlow = new ActiveFlow.ActiveFlow
                {
                    FlowInstanceId = flowInstance.Id,
                    CurrentEventNodes = flowInstance.CurrentNodes,
                    FlowId = flowInstance?.Flow?.Id ?? flowInstance.FlowId
                };

                ActiveFlows.Add(activeFlow);
            }
        }
        #endregion

        #region [CreateFlowsFromConfiguration]
        /// <summary>
        /// Copy a flow instance before using it.
        /// </summary>
        /// <param name="flow"></param>
        /// <returns></returns>
        private Flow CopyFlow(Flow flow)
        {
            // TODO: Make this optional, so that the user can decide whether to use a copy (slower but better memory management).
            return flow;
        }

        /// <summary>
        /// Creates a list <see cref="Flow"/> objects from <see cref="FlowConfiguration"/> objects
        /// </summary>
        /// <returns>A list <see cref="Flow"/> objects</returns>
        private IList<Flow> CreateFlowsFromConfiguration()
        {
            flowLogService.Info($"> Creating flows from configurations...");

            var list = new List<Flow>();
            foreach (var flowConfiguration in flowConfigurations)
            {
                var nodes = new List<BaseNode>();
                foreach (var node in flowConfiguration.Nodes)
                {
                    var resolver = unityContainer.Resolve<INodeResolver>($"{node.ClassName}");
                    var newNode = resolver.Create(node.Id, node.IsStartEvent);

                    // Create data pin instances inside node
                    newNode.CreateDataPins();

                    nodes.Add(newNode);
                    // Set default values
                    if (node?.Pins != null)
                    {
                        foreach (var pin in node.Pins)
                        {
                            var pinProperty = newNode.GetType().GetProperty(pin.Name,
                                System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public);

                            var defaultValueProperty = typeof(DataPin).GetProperty("DefaultValue",
                                System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public);

                            if (pin.DefaultValue != null)
                            {
                                var pinInstance = pinProperty.GetValue(newNode);

                                defaultValueProperty.SetValue(pinInstance, pin.DefaultValue);
                            }
                        }
                    }
                }

                foreach (var pin in flowConfiguration.Pins)
                {
                    // Find from to pin
                    var fromNode = nodes.FirstOrDefault(x => x.Id == pin.From.NodeId);
                    var toNode = nodes.FirstOrDefault(x => x.Id == pin.To.NodeId);

                    // Find from member
                    var fromProperty = fromNode.GetType().GetProperty(pin.From.PinName,
                        System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public);

                    var toProperty = toNode.GetType().GetProperty(pin.To.PinName,
                        System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public);

                    // Set properpty
                    toProperty.SetValue(toNode, fromProperty.GetValue(fromNode));
                }

                foreach (var link in flowConfiguration.Links)
                {
                    // Find from to pin
                    var fromNode = nodes.FirstOrDefault(x => x.Id == link.From.NodeId);
                    var toNode = nodes.FirstOrDefault(x => x.Id == link.To.NodeId);

                    // Find from property
                    var property = fromNode.GetType().GetProperty(link.From.PinName, System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public);

                    /* this one is tricky.
                     * here we check if the property is a list.
                     * if so, we get the value of the property (which is type of IList)
                     * and find the Add method and invoke it.
                     */
                    if (property.PropertyType.Name == "IList`1")
                    {
                        // retrieves current List value to call Add method
                        var customList = property.GetValue(fromNode);

                        // gets metadata of the List.Add method
                        var addMethod = customList.GetType().GetMethod("Add");
                        addMethod.Invoke(customList, new object[] { toNode });
                    }
                    else
                    {
                        property.SetValue(fromNode, toNode);
                    }
                }

                list.Add(new Flow
                {
                    Id = flowConfiguration.Id,
                    Name = flowConfiguration.Name,
                    Nodes = nodes
                });
            }

            flowLogService.Info($"> {flowConfigurations.Count} flows created.");

            return list;
        }
        #endregion

        #region [LoadEventQueue]
        /// <summary>
        /// Loads the event queue from the repository
        /// </summary>
        private void LoadEventQueue()
        {
            var unhandledEvents = flowEventQueueService.GetAllUnhandled(machineName, serviceName);

            if (unhandledEvents.Count() > 0)
                flowLogService.Info($"> {unhandledEvents.Count()} unhandled events were found.");

            foreach (var flowEventQueue in unhandledEvents)
            {
                var eventArgs = JsonConvert.DeserializeObject<FlowEventArgs>(
                    Encoding.UTF8.GetString(flowEventQueue.Args),
                    new JsonSerializerSettings
                    {
                        TypeNameHandling = TypeNameHandling.All,
                        PreserveReferencesHandling = PreserveReferencesHandling.Objects
                    });

                // if there are no event args create an empty object
                if (eventArgs == null)
                    eventArgs = new FlowEventArgs();


                eventArgs.EventName = flowEventQueue.EventName;
                eventArgs.QueueId = flowEventQueue.Id;
                eventArgs.UserId = flowEventQueue.CreateUserId;

                EnqueueEvent(eventArgs);
            }

            if (eventQueue.Count() > 0)
                flowLogService.Info($"> {eventQueue.Count()} events were created.");
        }
        #endregion

        #region [SetEventQueueHandled/SetEventQueueFailed]
        /// <summary>
        /// Sets an event queue to handled
        /// </summary>
        /// <param name="eventQueueId">Event queue to update</param>
        private bool SetEventQueueHandled(Guid eventQueueId)
        {
            flowLogService.Info($"Running SetEventQueueHandled with {eventQueueId}");
            var result = flowEventQueueService.SetHandled(eventQueueId, true);
            flowEventQueueService.Remove(eventQueueId);

            return result;
        }

        /// <summary>
        /// Sets an event queue to failed
        /// </summary>
        /// <param name="eventQueueId">Event queue to update</param>
        private void SetEventQueueFailed(Guid eventQueueId)
        {
            flowLogService.Info($"Running SetEventQueueFAILED with {eventQueueId}");
            flowEventQueueService.SetFailed(eventQueueId);
        }
        #endregion

        #region [ProcessEvent]
        /// <summary>
        /// Processing thread worker for a particular flow instance
        /// </summary>
        /// <param name="param"><see cref="ThreadStateInfo"/> Object to process</param>
        private void ProcessEvent(object param)
        {
            var threadInfo = param as ThreadStateInfo;

            // Create runtime
            var runtime = new FlowRuntimeService(flowLogService, threadInfo.EventCall.Args);

            try
            {
                if (threadInfo.IsStartEvent)
                {
                    // Run flow service
                    runtime.Run(threadInfo.FlowInstance, threadInfo.EventCall);

                    // Save or remove active flow
                    SaveOrDeleteFlowInstance(threadInfo.FlowInstance);
                }
                else
                {
                    runtime.Run(threadInfo.FlowInstance, threadInfo.EventCall);
                    CreateActiveFlow(threadInfo.FlowInstance);

                    // Save or remove active flow
                    SaveOrDeleteFlowInstance(threadInfo.FlowInstance);
                }
            }
            catch (Exception ex)
            {
                // TODO: When implementing a profiler, report the failed nodes
                var lastActiveFlowNodes = threadInfo.FlowInstance.CurrentNodes.Select(x => x.Node?.Name ?? "").ToList();
                flowLogService.Error($"Error during executing flow node. Current nodes: {string.Join(", ", lastActiveFlowNodes)}", ex);

                threadInfo.FlowInstance.IsFailed = true;

                // We don't have any current nodes when the flow failes
                threadInfo.FlowInstance.CurrentNodes.Clear();

                // Save or remove active flow
                SaveOrDeleteFlowInstance(threadInfo.FlowInstance);
            }

            // Remove from active flows
            if (!threadInfo.FlowInstance.IsAlive || threadInfo.FlowInstance.IsFailed)
                ActiveFlows.Remove(threadInfo.ActiveFlow);
        }

        /// <summary>
        /// Save or delete flow instance
        /// </summary>
        /// <param name="flowInstance">Flow instance</param>
        private void SaveOrDeleteFlowInstance(FlowInstance flowInstance)
        {
            if (flowInstance.IsAlive)
                flowInstanceService.Save(flowInstance);
            else
            {
                flowInstanceService.Delete(flowInstance);
            }
        }
        #endregion

        #endregion

        #region Public Methods
        /// <summary>
        /// Initialize service
        /// </summary>
        /// <param name="machineName">Machine name</param>
        /// <param name="serviceName">Service name</param>
        public void Initialize(string machineName, string serviceName)
        {
            this.machineName = machineName;
            this.serviceName = serviceName;

            // Load active flow configurations
            flowConfigurations = flowConfigurationService.GetAll()
                .Where(x => x.IsActive
                       && (string.IsNullOrWhiteSpace(x.MachineName) || x.MachineName?.ToLower() == machineName?.ToLower())
                       && (string.IsNullOrWhiteSpace(x.ServiceName) || x.ServiceName?.ToLower() == serviceName?.ToLower()))
                .ToList();
             
            if (flowConfigurations.Count > 0)
                flowLogService.Info($"# {flowConfigurations.Count} Active flow configurations were found: {string.Join(", ", flowConfigurations.Select(x => $"\"{x.Name}\""))}");
            else
            {
                flowLogService.Info("No active flow configurations were found!");
                return;
            }

            Flows = CreateFlowsFromConfiguration();
            CreateActiveFlowsFrom(flowInstanceService.GetAllAlive().ToList());

            RefreshEventDelegates();
        }

        #region [Run]
        /// <summary>
        /// Run a single cycle
        /// </summary>
        public void Run()
        {
            // Raise event before the process has begun.
            OnStarted?.Invoke(this, EventArgs.Empty);
            int maxParallelTasks = 4;

            //flowLogService.Info($"> Running at {DateTime.Now}");
            try
            {
                if (executions.Count == 0)
                {
                    // load event queue from db            
                    LoadEventQueue();

                    if (eventQueue.Count() == 0)
                    {
                        //flowLogService.Info($"- Event queue is empty. Nothing to do.");
                        return;
                    }

                    while (eventQueue.Count > 0)
                    {
                        // pop event entries from queue first
                        var queueEntry = eventQueue.PopFirst();

                        //flowLogService.Info($"> Processing {queueEntry.Args.EventName}...");

                        if (queueEntry.Delegate.IsStartEvent)
                        {
                            //flowLogService.Info($"- Create new flow instance {queueEntry.Args.EventName} : {queueEntry.Delegate.FlowId}");

                            var newFlowInstance = new FlowInstance
                            {
                                Flow = CopyFlow(Flows.FirstOrDefault(x => x.Id == queueEntry.Delegate.FlowId)),
                                Id = Guid.NewGuid()
                            };

                            executions.Add(new ThreadStateInfo
                            {
                                FlowInstance = newFlowInstance,
                                EventCall = queueEntry
                            });
                        }
                        else
                        {
                            // Notify ALL instances, which MIGHT BE continued
                            foreach (var activeFlow in ActiveFlows.Where(x => x.FlowId == queueEntry.Delegate.FlowId))
                            {
                                //flowLogService.Info($"Continuing flow instance: {activeFlow.FlowInstanceId}");

                                // Get from database
                                var flowInstance = flowInstanceService.GetById(activeFlow.FlowInstanceId);
                                flowInstance.Flow = CopyFlow(Flows.FirstOrDefault(x => x.Id == flowInstance.FlowId));

                                executions.Add(new ThreadStateInfo
                                {
                                    IsStartEvent = false,
                                    FlowInstance = flowInstance,
                                    EventCall = queueEntry,
                                    ActiveFlow = activeFlow
                                });
                            }
                        }
                    }
                }

                var failed = new ConcurrentList<Guid>();
                var success = new ConcurrentList<Guid>();

                int runningTasks = 0;
                var executedThreads = new List<Thread>();
                var lastDebugText = "";

                // Group by event delegate and take a given amount....
                var tempJobs = new List<ThreadStateInfo>();
                foreach (var eventGroup in executions.GroupBy(x => x.EventCall.QueueId))
                {
                    tempJobs.AddRange(eventGroup);

                    // Limit the amount of parallel tasks
                    if (tempJobs.Count > maxParallelTasks * 3)
                        break;
                }

                var color = Console.ForegroundColor;
                Console.ForegroundColor = ConsoleColor.Blue;
                Console.WriteLine($"Flows in execution queue: {executions.Count}. Executed in the next loop: {tempJobs.Count}");
                Console.ForegroundColor = color;

                while (tempJobs.Any() && executions.Any())
                {
                    var currentlyExecutedJobs = tempJobs.Take(maxParallelTasks - runningTasks).ToList();

                    var nextDebugText = $"Flows to execute/total {tempJobs.Count}/{executions.Count}. Max parallel flows {maxParallelTasks}. Currently running flows: {runningTasks}";

                    if (lastDebugText != nextDebugText)
                    {
                        Console.WriteLine(nextDebugText);
                        lastDebugText = nextDebugText;
                    }

                    if (!currentlyExecutedJobs.Any())
                    {
                        Thread.Sleep(10);
                        continue;
                    }

                    var tempThreads = new List<Thread>();

                    foreach (var job in currentlyExecutedJobs)
                    {
                        // Make a task reservation
                        runningTasks++;

                        Console.WriteLine($"   [{DateTime.Now.ToLongTimeString()}] Create threads: {job.EventCall?.Delegate.EventName}");
                        var thread = new Thread(() =>
                        {
                            executions.Remove(job);
                            tempJobs.Remove(job);

                            try
                            {
                                ProcessEvent(job);
                                success.Add(job.EventCall.QueueId);
                            }
                            catch
                            {
                                failed.Add(job.EventCall.QueueId);
                            }

                            runningTasks--;
                        });

                        tempThreads.Add(thread);
                    }

                    // Execute
                    foreach (var thread in tempThreads)
                    {
                        Console.WriteLine($" > [{DateTime.Now.ToLongTimeString()}] Start thread {thread.ManagedThreadId}");
                        thread.Start();
                        executedThreads.Add(thread);
                    }

                    Thread.Sleep(30);
                }

                foreach (var thread in executedThreads)
                {
                    var id = thread.ManagedThreadId;
                    thread.Join();

                    Console.WriteLine($" < [{DateTime.Now.ToLongTimeString()}] Thread joined {id}");
                }

                // Remove failed from success
                foreach (var failedJob in failed.Distinct())
                {
                    SetEventQueueFailed(failedJob);
                    success.Remove(failedJob);
                }

                // Set handled jobs and remove them
                foreach (var successJob in success.Distinct())
                    SetEventQueueHandled(successJob);

                color = Console.ForegroundColor;
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine($"Flow execution complete. Flows: {executedThreads.Count}");
                Console.ForegroundColor = color;

                executedThreads.Clear();
            }
            catch (Exception ex)
            {
                flowLogService.Error($"- FlowService.Run could not be run", ex);

                // Raise event after the process has begun.
                OnCompleted?.Invoke(this, EventArgs.Empty);
                throw;
            }

            // Raise event after the process has begun.
            OnCompleted?.Invoke(this, EventArgs.Empty);
        }
        #endregion

        #region [EnqueueEvent]
        /// <summary>
        /// Enqueue event
        /// </summary>
        /// <param name="args"></param>
        public void EnqueueEvent(FlowEventArgs args)
        {
            flowLogService.Info($"- Enqueue event: {args.EventName} / object id {args.ObjectId ?? "<unset>"}");

            bool isEnqueued = false;

            // Find workflow
            var delegates = eventDelegates.FirstOrDefault(x => x.Key == args.EventName).Value;
            if (delegates != null && delegates.Count > 0)
            {
                foreach (var del in delegates)
                {
                    flowLogService.Info($"- Create event call: {args.EventName} / Flow: {del.FlowId} / Is start event {del.IsStartEvent}");

                    eventQueue.PushBack(new EventCall { QueueId = args.QueueId, Args = args, Delegate = del });
                    isEnqueued = true;
                }
            }

            if (!isEnqueued)
            {
                flowLogService.Info($"- No delegate was found for {args.EventName}, changing status to handled.");

                // Remove events that can't be handled
                flowEventQueueService.Remove(args.QueueId);
            }
        }
        #endregion

        #region [RefreshEventDelegates]
        /// <summary>
        /// Cache event delegates
        /// </summary>
        public void RefreshEventDelegates()
        {
            flowLogService.Info("> Creating event delegates...");

            eventDelegates = new Dictionary<string, IList<EventDelegate>>();
            foreach (var flow in Flows)
            {
                foreach (var eventNode in flow.Nodes.OfType<EventNode>())
                {
                    flowLogService.Info($"> Add {flow.Name}/{eventNode.Name}");

                    IList<EventDelegate> eventDelegateList = null;

                    if (!eventDelegates.ContainsKey(eventNode.EventName))
                    {
                        eventDelegateList = new List<EventDelegate>();
                        eventDelegates[eventNode.EventName] = eventDelegateList;
                    }
                    else
                        eventDelegateList = eventDelegates[eventNode.EventName];

                    eventDelegateList.Add(new EventDelegate
                    {
                        FlowId = flow.Id,
                        EventName = eventNode.EventName,
                        EventNodeId = eventNode.Id,
                        IsStartEvent = eventNode.IsStartEvent
                    });
                }
            }

            flowLogService.Info($"> {eventDelegates.Count} event delegates were created. \"{string.Join(", ", eventDelegates.Select(x => x.Key))}\"");
        }
        #endregion 

        #endregion

        #region Public Properties

        public IList<ActiveFlow.ActiveFlow> ActiveFlows { get; set; }

        public IList<Flow> Flows { get; set; }

        #endregion
    }
}
using Simplic.Flow.Editor.Definition;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Data;
using Telerik.Windows.Diagrams.Core;

namespace Simplic.Flow.Editor.UI
{
    /// <summary>
    /// WorkflowEditorViewModel
    /// </summary>
    public class WorkflowEditorViewModel : Simplic.UI.MVC.ViewModelBase, IObservableGraphSource
    {
        #region Fields
        private Guid id;
        private ObservableCollection<NodeConnectionViewModel> connections;
        private Configuration.FlowConfiguration flowConfiguration;
        private NodeViewModel selectedNode;
        private IList<NodeDefinition> nodeDefinitions;
        private ConnectorViewModel sourceConnector;
        private ConnectorViewModel targetConnector;
        private ObservableCollection<FlowVariable> variables;
        private System.Windows.Input.ICommand addVariableCommand;
        #endregion

        #region Constructor
        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="nodeDefinitions">NodeDefinition</param>
        /// <param name="flowConfiguration">FlowConfiguration</param>
        public WorkflowEditorViewModel(IList<NodeDefinition> nodeDefinitions,
            Configuration.FlowConfiguration flowConfiguration)
        {
            connections = new ObservableCollection<NodeConnectionViewModel>();
            variables = new ObservableCollection<FlowVariable>();
            Nodes = new ObservableCollection<NodeViewModel>();
            this.nodeDefinitions = nodeDefinitions;

            Nodes.CollectionChanged += (s, e) =>
            {
                IsDirty = true;
                if (e.NewItems != null)
                {
                    foreach (var item in e.NewItems.OfType<Simplic.UI.MVC.ViewModelBase>())
                    {
                        item.Parent = this;
                    }
                }
            };

            if (flowConfiguration == null)
                this.flowConfiguration = new Configuration.FlowConfiguration()
                {
                    Id = Guid.NewGuid(),
                    Name = "New Workflow"
                };
            else
                this.flowConfiguration = flowConfiguration;

            FillConfiguration();

            addVariableCommand = new Simplic.UI.MVC.RelayCommand(NewVariableRelayCommand);

            AvailableVariableTypes = new List<Type>()
            {
                { typeof(string) },
                { typeof(int) },
                { typeof(uint) },
                { typeof(short) },
                { typeof(ushort) },
                { typeof(long) },
                { typeof(ulong) },
                { typeof(float) },
                { typeof(double) },
                { typeof(decimal) }
            };
        }
        #endregion

        #region Private Methods

        #region [FillConfiguration]
        /// <summary>
        /// Fills the configuration values
        /// </summary>
        private void FillConfiguration()
        {
            if (!flowConfiguration.Nodes.Any())
                return;

            // create node view models
            foreach (var node in flowConfiguration.Nodes)
            {
                NodeViewModel nodeViewModel = null;
                var nodeDefinition = NodeDefinitions.Where(x => x.Name == node.ClassName).FirstOrDefault();

                if (node.NodeType == "ActionNode")
                {
                    nodeViewModel = new ActionNodeViewModel(nodeDefinition, node);
                }
                else if (node.NodeType == "EventNode")
                {
                    nodeViewModel = new EventNodeViewModel(nodeDefinition, node);
                }
                else if (node.NodeType == "ConditionNode")
                {
                    nodeViewModel = new ConditionNodeViewModel(nodeDefinition, node);
                }

                Nodes.Add(nodeViewModel);
            }

            // create flow links
            foreach (var flow in flowConfiguration.Links)
            {
                var sourceNodeViewModel = Nodes.Where(x => x.Id == flow.From.NodeId).FirstOrDefault();
                var sourceConnectorViewModel = sourceNodeViewModel.FlowPins.Where(x => x.Name == flow.From.PinName).FirstOrDefault();

                var targetNodeViewModel = Nodes.Where(x => x.Id == flow.To.NodeId).FirstOrDefault();
                var targetConnectorViewModel = targetNodeViewModel.FlowPins.Where(x => x.Name == flow.To.PinName).FirstOrDefault();

                var connectionViewModel = new NodeConnectionViewModel(sourceNodeViewModel, targetNodeViewModel, sourceConnectorViewModel, targetConnectorViewModel);
                Connections.Add(connectionViewModel);
            }

            // create data pins
            foreach (var pin in flowConfiguration.Pins)
            {
                var sourceNodeViewModel = Nodes.Where(x => x.Id == pin.From.NodeId).FirstOrDefault();
                var sourceConnectorViewModel = sourceNodeViewModel.DataPins.Where(x => x.Name == pin.From.PinName).FirstOrDefault();

                var targetNodeViewModel = Nodes.Where(x => x.Id == pin.To.NodeId).FirstOrDefault();
                var targetConnectorViewModel = targetNodeViewModel.DataPins.Where(x => x.Name == pin.To.PinName).FirstOrDefault();

                var connectionViewModel = new NodeConnectionViewModel(sourceNodeViewModel, targetNodeViewModel, sourceConnectorViewModel, targetConnectorViewModel);
                Connections.Add(connectionViewModel);

                // if the target connector is a generic data type, update all other generic types to the source data type
                if (targetConnectorViewModel.IsGeneric)
                {
                    (targetNodeViewModel as ActionNodeViewModel).UpdateDataTypes(sourceConnectorViewModel.DataConnectorType);
                }
            }

            // create workflow variables
            foreach (var variable in flowConfiguration.Variables)
            {
                Variables.Add(new FlowVariable
                {
                    Name = variable.Name,
                    Value = variable.Value,
                    Type = variable.Type
                });
            }
        }
        #endregion

        #region [IObservableGraphSource.AddLink]
        /// <summary>
        /// Adds a link
        /// </summary>
        /// <param name="link">ILink</param>
        void IObservableGraphSource.AddLink(ILink link)
        {
            IsDirty = true;

            var connection = link as NodeConnectionViewModel;

            if (TargetConnector != null)
            {
                connection.TargetConnectorViewModel = TargetConnector;
                connection.TargetConnectorViewModel.IsConnected = true;
            }

            connection.SourceConnectorViewModel.IsConnected = true;

            connections.Add(connection);
        }
        #endregion

        #region [IObservableGraphSource.AddNode]
        /// <summary>
        /// Adds a node
        /// </summary>
        /// <param name="node"></param>
        void IObservableGraphSource.AddNode(object node)
        {
            Nodes.Add(node as NodeViewModel);
            IsDirty = true;
        }
        #endregion

        #region [IObservableGraphSource.CreateLink]
        /// <summary>
        /// Creates a link
        /// </summary>
        /// <param name="source"></param>
        /// <param name="target"></param>
        /// <returns>ILink</returns>
        ILink IObservableGraphSource.CreateLink(object source, object target)
        {
            IsDirty = true;

            return new NodeConnectionViewModel(source as NodeViewModel, target as NodeViewModel, SourceConnector, null);
        }
        #endregion

        #region [IObservableGraphSource.CreateNode]
        /// <summary>
        /// Creates a node
        /// </summary>
        /// <param name="shape"></param>
        /// <returns></returns>
        object IObservableGraphSource.CreateNode(IShape shape)
        {
            IsDirty = true;
            if (shape is BaseNodeShape)
            {
                var baseNodeShape = shape as BaseNodeShape;

                var def = NodeDefinitions.FirstOrDefault(x => x.Name == baseNodeShape.Name);

                var type = "";

                if (def is ActionNodeDefinition)
                    type = "ActionNode";
                else if(def is EventNodeDefinition)
                    type = "EventNode";
                else if (def is ConditionNodeDefinition)
                    type = "ConditionNode";

                var nodeConfig = new Configuration.NodeConfiguration
                {
                    Id = Guid.NewGuid(),
                    ClassName = def.Name,
                    NodeType = type
                };

                this.flowConfiguration.Nodes.Add(nodeConfig);

                NodeViewModel nodeViewModel = null;

                if (def is ActionNodeDefinition)
                {
                    nodeViewModel = new ActionNodeViewModel(def, nodeConfig);
                    baseNodeShape.DataContext = nodeViewModel;
                }
                else if (def is EventNodeDefinition)
                {
                    nodeViewModel = new EventNodeViewModel(def, nodeConfig);
                    baseNodeShape.DataContext = nodeViewModel;
                }
                else if (def is ConditionNodeDefinition)
                {
                    nodeViewModel = new ConditionNodeViewModel(def, nodeConfig);
                    baseNodeShape.DataContext = nodeViewModel;
                }

                nodeViewModel.FlowPins.CollectionChanged += (s, e) =>
                {
                    // Refresh connector
                    baseNodeShape.CreateConnectors();
                    baseNodeShape.Height = nodeViewModel.Height;
                    baseNodeShape.Width = nodeViewModel.Width;
                };

                nodeViewModel.DataPins.CollectionChanged += (s, e) =>
                {
                    // Refresh connector
                    baseNodeShape.CreateConnectors();
                    baseNodeShape.Height = nodeViewModel.Height;
                    baseNodeShape.Width = nodeViewModel.Width;
                };

                baseNodeShape.CreateConnectors();
                baseNodeShape.Height = nodeViewModel.Height;
                baseNodeShape.Width = nodeViewModel.Width;

                return nodeViewModel;
            }

            return null;
        }
        #endregion



        #region [IObservableGraphSource.RemoveLink]
        /// <summary>
        /// Removes a link
        /// </summary>
        /// <param name="link"></param>
        /// <returns></returns>
        bool IObservableGraphSource.RemoveLink(ILink link)
        {
            IsDirty = true;

            if (connections.Contains(link))
            {
                var connection = link as NodeConnectionViewModel;
                connection.SourceConnectorViewModel.IsConnected = false;
                connection.TargetConnectorViewModel.IsConnected = false;

                if (connection.SourceViewModel is ActionNodeViewModel
                    && connection.SourceConnectorViewModel is DataConnectorViewModel)
                {
                    var connector = connection.SourceConnectorViewModel as DataConnectorViewModel;

                    if (connector.IsGeneric)
                    {
                        var nodeViewModel = connection.SourceViewModel as ActionNodeViewModel;

                        var genericCount = nodeViewModel.DataPins.Count(x => x.IsGeneric);
                        var notConnectedGenericCount = nodeViewModel.DataPins.Count(x => x.IsGeneric && !x.IsConnected);

                        if (genericCount == notConnectedGenericCount)
                        {
                            nodeViewModel.UpdateDataTypes(null);
                        }
                    }
                }

                if (connection.TargetViewModel is ActionNodeViewModel
                    && connection.TargetConnectorViewModel is DataConnectorViewModel)
                {
                    var connector = connection.TargetConnectorViewModel as DataConnectorViewModel;

                    if (connector.IsGeneric)
                    {
                        var nodeViewModel = connection.TargetViewModel as ActionNodeViewModel;

                        var genericCount = nodeViewModel.DataPins.Count(x => x.IsGeneric);
                        var notConnectedGenericCount = nodeViewModel.DataPins.Count(x => x.IsGeneric && !x.IsConnected);

                        if (genericCount == notConnectedGenericCount)
                        {
                            nodeViewModel.UpdateDataTypes(null);
                        }
                    }
                }

                connections.Remove(link as NodeConnectionViewModel);
                return true;
            }
            else
            {
                return false;
            }
        }
        #endregion

        #region [IObservableGraphSource.RemoveNode]
        /// <summary>
        /// Removes a node
        /// </summary>
        /// <param name="node"></param>
        /// <returns></returns>
        bool IObservableGraphSource.RemoveNode(object node)
        {
            IsDirty = true;

            if (Nodes.Contains(node))
            {
                var nodeVm = node as NodeViewModel;
                Nodes.Remove(nodeVm);

                var connections = Connections.Where(x =>
                       !Nodes.Any(y => y.Id == x.SourceViewModel?.Id)
                    || !Nodes.Any(y => y.Id == x.TargetViewModel?.Id)).ToList();

                foreach (var connection in connections)
                    Connections.Remove(connection);

                return true;
            }
            else
            {
                return false;
            }
        }
        #endregion

        #region [NewVariableRelayCommand]
        private void NewVariableRelayCommand(object parameter)
        {
            var lastVariable = Variables.OrderBy(x => x.Name).LastOrDefault();
            var newVariable = new FlowVariable
            {
                Name = lastVariable != null ? lastVariable.Name + "(Copy)" : "New Variable",
                Type = typeof(string)
            };

            Variables.Add(newVariable);
        }
        #endregion

        #endregion

        #region Public Methods

        #region [GetFlowConfiguration]
        /// <summary>
        /// Creates a flow configuration
        /// </summary>
        /// <returns></returns>
        public Configuration.FlowConfiguration GetFlowConfiguration()
        {
            flowConfiguration.Nodes.Clear();

            for (int i = Nodes.Count - 1; i >= 0; i--)
            {
                if (Nodes[i] == null)
                    Nodes.RemoveAt(i);
            }

            foreach (var node in Nodes)
            {
                flowConfiguration.Nodes.Add(node.CreateConfiguration());
            }

            flowConfiguration.Links.Clear();
            flowConfiguration.Links = connections.Where(x => x.FlowLink != null).Select(x => x.FlowLink).ToList();
            flowConfiguration.Pins = connections.Where(x => x.DataLink != null).Select(x => x.DataLink).ToList();

            return flowConfiguration;
        }
        #endregion

        #endregion

        #region Private Properties

        #region [IGraphSource.Items]
        IEnumerable IGraphSource.Items { get { return Nodes; } }
        #endregion

        #region [IGraphSource.Links]
        IEnumerable<ILink> IGraphSource.Links { get { return connections; } }
        #endregion

        #endregion

        #region Public Properties

        #region [Id]
        /// <summary>
        /// Gets or sets the Id
        /// </summary>
        public Guid Id
        {
            get { return id; }
            set { id = value; IsDirty = true; RaisePropertyChanged(nameof(Id)); }
        }
        #endregion

        #region [Nodes]
        /// <summary>
        /// Gets the nodes list
        /// </summary>
        public ObservableCollection<NodeViewModel> Nodes { get; }
        #endregion

        #region [NodeDefinitions]
        /// <summary>
        /// Gets or sets the node definitions
        /// </summary>
        public IList<NodeDefinition> NodeDefinitions
        {
            get { return nodeDefinitions; }
            set
            {
                IsDirty = true;
                nodeDefinitions = value;
                RaisePropertyChanged(nameof(NodeDefinitions));
            }
        }
        #endregion

        #region [Connections]
        /// <summary>
        /// Gets or sets the connections
        /// </summary>
        public ObservableCollection<NodeConnectionViewModel> Connections
        {
            get { return connections; }
            set { connections = value; IsDirty = true; RaisePropertyChanged(nameof(Connections)); }
        }
        #endregion

        #region [SourceConnector]
        /// <summary>
        /// Gets or sets the source connector
        /// </summary>
        public ConnectorViewModel SourceConnector
        {
            get { return sourceConnector; }
            set
            {
                sourceConnector = value;
                IsDirty = true;
                RaisePropertyChanged(nameof(SourceConnector));
            }
        }
        #endregion

        #region [TargetConnector]
        /// <summary>
        /// Gets or sets the target connector
        /// </summary>
        public ConnectorViewModel TargetConnector
        {
            get { return targetConnector; }
            set
            {
                targetConnector = value;
                IsDirty = true;
                RaisePropertyChanged(nameof(TargetConnector));
            }
        }
        #endregion

        #region [SelectedNode]
        /// <summary>
        /// Gets or sets the selected node
        /// </summary>
        public NodeViewModel SelectedNode
        {
            get { return selectedNode; }
            set
            {
                selectedNode = value;
                RaisePropertyChanged(nameof(SelectedNode));
            }
        }
        #endregion

        #region [WorkflowName]
        /// <summary>
        /// Gets or sets the workflow name
        /// </summary>
        public string WorkflowName
        {
            get { return flowConfiguration.Name; }
            set
            {
                flowConfiguration.Name = value;
                IsDirty = true;
                RaisePropertyChanged(nameof(WorkflowName));
            }
        }

        /// <summary>
        /// Gets or sets the machine name
        /// </summary>
        public string MachineName
        {
            get { return flowConfiguration.MachineName; }
            set
            {
                flowConfiguration.MachineName = value;
                IsDirty = true;
                RaisePropertyChanged(nameof(MachineName));
            }
        }

        /// <summary>
        /// Gets or sets the service name
        /// </summary>
        public string ServiceName
        {
            get { return flowConfiguration.ServiceName; }
            set
            {
                flowConfiguration.ServiceName = value;
                IsDirty = true;
                RaisePropertyChanged(nameof(ServiceName));
            }
        }
        #endregion

        #region [Variables]
        public ObservableCollection<FlowVariable> Variables
        {
            get { return variables; }
            set { variables = value; RaisePropertyChanged(nameof(Variables)); }
        }
        #endregion

        #region [AvailableVariableTypes]
        /// <summary>
        /// Gets or sets a list of available variable types
        /// </summary>
        public IList<Type> AvailableVariableTypes { get; set; }
        #endregion

        #region [AddVariableCommand]
        public System.Windows.Input.ICommand AddVariableCommand
        {
            get { return addVariableCommand; }
            set { addVariableCommand = value; }
        }
        #endregion

        /// <summary>
        /// Gets or sets whether the flow is active
        /// </summary>
        public bool IsActive
        {
            get
            {
                return flowConfiguration.IsActive;
            }
            set
            {
                flowConfiguration.IsActive = value;
                IsDirty = true;
                RaisePropertyChanged(nameof(IsActive));
            }
        }
        #endregion
    }
}
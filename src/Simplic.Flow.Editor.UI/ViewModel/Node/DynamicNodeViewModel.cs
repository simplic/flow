using Simplic.Flow.Configuration;
using Simplic.Flow.Editor.Definition;
using System;
using System.Linq;
using System.Windows.Input;

namespace Simplic.Flow.Editor.UI
{
    /// <summary>
    /// Dynamic node view model
    /// </summary>
    public class DynamicNodeViewModel : NodeViewModel
    {
        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="nodeDefinition">NodeDefinition</param>
        /// <param name="nodeConfiguration">NodeConfiguration</param>
        public DynamicNodeViewModel(NodeDefinition nodeDefinition, NodeConfiguration nodeConfiguration)
            : base(nodeDefinition, nodeConfiguration)
        {
            // add flow in pin manually if it is not an event            
            CreateFlowInPin();

            AddDynamicInPin = new Simplic.UI.MVC.RelayCommand((e) =>
            {
                var definition = new DataPinDefinition
                {
                    DisplayName = $"D{DataPins.Count}",
                    Id = Guid.NewGuid(),
                    Name = $"InPinD{DataPins.Count}",
                    PinDirection = PinDirectionDefinition.In,
                    Type = typeof(object),
                    IsDynamic = true
                };

                DataPins.Add(new DataConnectorViewModel(definition)
                {
                    Parent = this
                });
            });

            AddDynamicFlowOutPin = new Simplic.UI.MVC.RelayCommand((e) =>
            {
                var definition = new FlowPinDefinition
                {
                    DisplayName = $"F{FlowPins.Count}",
                    Id = Guid.NewGuid(),
                    Name = $"OutNode{FlowPins.Count}",
                    PinDirection = PinDirectionDefinition.Out,
                    IsDynamic = true
                };

                FlowPins.Add(new FlowConnectorViewModel(definition)
                {
                    Parent = this
                });
            });
        }

        public void UpdateDataTypes(Type typeToSet)
        {
            foreach (var item in DataPins.Where(x => x.IsGeneric))
            {
                item.DataConnectorType = typeToSet;
            }
        }

        /// <summary>
        /// Gets or sets the add dynamic in pin command
        /// </summary>
        public ICommand AddDynamicInPin { get; set; }

        public ICommand AddDynamicFlowOutPin { get; set; }
    }
}

using Simplic.Flow.Configuration;
using Simplic.Flow.Editor.Definition;
using System;
using System.Linq;
using System.Windows.Input;

namespace Simplic.Flow.Editor.UI
{
    /// <summary>
    /// ConditionNodeViewModel
    /// </summary>
    public class ConditionNodeViewModel : DynamicNodeViewModel
    {
        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="nodeDefinition">NodeDefinition</param>
        /// <param name="nodeConfiguration">NodeConfiguration</param>
        public ConditionNodeViewModel(NodeDefinition nodeDefinition, NodeConfiguration nodeConfiguration)
            : base(nodeDefinition, nodeConfiguration)
        {
            // add flow in pin manually if it is not an event            
            CreateFlowInPin();
        }
    }
}

using System;

namespace Simplic.Flow.Node
{
    [ConditionNodeDefinition(DisplayName = "If", Name = nameof(IfConditionNode), Category = "Common")]
    public class IfConditionNode : ActionNode
    {
        public override bool Execute(IFlowRuntimeService runtime, DataPinScope scope)
        {
            return true;
        }

        public override string FriendlyName { get { return nameof(IfConditionNode); } }

        public override string Name { get { return nameof(IfConditionNode); } }
    }
}

namespace Simplic.Flow
{
    public abstract class ConditionNode : ActionNode
    {
        public override string FriendlyName { get; }
        
        public override bool Execute(IFlowRuntimeService runtime, DataPinScope scope)
        {
            var result = Compare(runtime, scope);

            if (result)
                runtime.EnqueueNode(OutNodeTrue, scope);
            else
                runtime.EnqueueNode(OutNodeFalse, scope);

            runtime.EnqueueNode(OutNodeAny, scope);

            return true;
        }

        protected abstract bool Compare(IFlowRuntimeService runtime, DataPinScope scope);

        [FlowPinDefinition(DisplayName = "True", Name = "OutNodeTrue", PinDirection = PinDirection.Out)]
        public ActionNode OutNodeTrue { get; set; }

        [FlowPinDefinition(DisplayName = "False", Name = "OutNodeFalse", PinDirection = PinDirection.Out)]
        public ActionNode OutNodeFalse { get; set; }

        [FlowPinDefinition(DisplayName = "Any", Name = "OutNodeAny", PinDirection = PinDirection.Out)]
        public ActionNode OutNodeAny { get; set; }

        [DataPinDefinition(
            Id = "4a6ea4cc-0721-45fd-be7c-95c5bf828a8a",
            ContainerType = DataPinContainerType.Single,
            DataType = typeof(string),
            Direction = PinDirection.In,
            Name = "InPinConditionA",
            DisplayName = "Condition A")]
        public DataPin InPinConditionA { get; set; }

        [DataPinDefinition(
            Id = "a9fe4cb7-8162-4faf-af4f-efa085bc1776",
            ContainerType = DataPinContainerType.Single,
            DataType = typeof(string),
            Direction = PinDirection.In,
            Name = "InPinConditionB",
            DisplayName = "Condition B")]
        public DataPin InPinConditionB { get; set; }

        [DataPinDefinition(
            Id = "64eb5249-eafd-4a15-8206-06d984278ee6",
            ContainerType = DataPinContainerType.Single,
            DataType = typeof(bool),
            Direction = PinDirection.Out,
            Name = "OutPinBoolean",
            DisplayName = "Result")]
        public DataPin OutPinBoolean { get; set; }
    }
}

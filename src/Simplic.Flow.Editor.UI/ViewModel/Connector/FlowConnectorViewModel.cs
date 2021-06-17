using Simplic.Flow.Editor.Definition;
using System;
using System.Linq;

namespace Simplic.Flow.Editor.UI
{
    public class FlowConnectorViewModel : ConnectorViewModel
    {
        private FlowPinDefinition pinDefinition;
        private bool isConnected;

        public FlowConnectorViewModel(FlowPinDefinition pinDefinition)
        {
            this.pinDefinition = pinDefinition;
        }

        public Guid Id => pinDefinition.Id;

        public override string Name => pinDefinition.Name;
        public override string DisplayName => pinDefinition.DisplayName;

        public bool IsList { get { return pinDefinition.AllowMultiple; } }

        public override PinDirectionDefinition PinDirection
        {
            get { return pinDefinition.PinDirection; }
            set { pinDefinition.PinDirection = value; RaisePropertyChanged(nameof(PinDirection)); }
        }

        public bool IsDynamic
        {
            get { return pinDefinition.IsDynamic; }
            set
            {
                PropertySetter(value, v => pinDefinition.IsDynamic = v);
            }
        }

        public override bool IsConnected
        {
            get { return isConnected; }
            set
            {
                isConnected = value;
                RaisePropertyChanged(nameof(IsConnected));
                RaisePropertyChanged(nameof(StrokeColor));
                RaisePropertyChanged(nameof(FillColor));
            }
        }

        public string StrokeColor { get { return Constants.FlowStrokeColor; } }

        public string FillColor
        {
            get
            {
                if (IsConnected)
                    return Constants.FlowStrokeColor;
                else
                    return "Transparent";
            }
        }

        public string HighlightStrokeColor
        {
            get
            {
                return Constants.FlowHighlightColor;
            }
        }

        public string HighlightFillColor
        {
            get
            {
                if (IsConnected)
                {
                    return Constants.FlowHighlightColor;
                }
                else
                    return "Transparent";
            }
        }

        private WorkflowEditorViewModel DiagramViewModel
        {
            get
            {
                return this.Parent.Parent as WorkflowEditorViewModel;
            }
        }

        public override bool CanConnect()
        {
            return PinDirection == PinDirectionDefinition.Out;
        }

        public override bool CanConnectTo(ConnectorViewModel otherConnectorViewModel)
        {
            var otherFlowConnectorViewModel = otherConnectorViewModel as FlowConnectorViewModel;

            // if the target is null or has the same parent as me
            if (otherFlowConnectorViewModel == null || otherFlowConnectorViewModel.Parent == this.Parent)
            {
                return false;
            }

            var connectionExists = DiagramViewModel.Connections.Any(
                x => x.SourceConnectorViewModel == this);

            if (connectionExists && !IsList)
                return false;

            if (this.PinDirection == PinDirectionDefinition.In)
                return otherFlowConnectorViewModel.PinDirection == PinDirectionDefinition.Out;
            else
                return otherFlowConnectorViewModel.PinDirection == PinDirectionDefinition.In;
        }
    }
}

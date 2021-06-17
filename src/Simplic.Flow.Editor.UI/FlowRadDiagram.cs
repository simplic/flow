using System.Globalization;
using Telerik.Windows.Controls;
using Telerik.Windows.Diagrams.Core;

namespace Simplic.Flow.Editor.UI
{
    /// <summary>
    /// FlowRadDiagram
    /// </summary>
    public class FlowRadDiagram : RadDiagram
    {
        #region Constructor
        /// <summary>
        /// Constructor
        /// </summary>
        public FlowRadDiagram()
        {
            AllowCopy = false;
            AllowPaste = false;
            SelectionMode = SelectionMode.Single;

            Loaded += FlowRadDiagram_Loaded;
        }

        #endregion

        #region Private Methods
        private void FlowRadDiagram_Loaded(object sender, System.Windows.RoutedEventArgs e)
        {
            foreach (RadDiagramConnection connection in Connections)
            {
                connection.ConnectionType = Telerik.Windows.Diagrams.Core.ConnectionType.Bezier;
                connection.BezierTension = 3;

                var connectionViewModel = connection.DataContext as NodeConnectionViewModel;

                connection.SourceConnectorPosition = connectionViewModel.SourceConnectorViewModel.Name;
                connection.TargetConnectorPosition = connectionViewModel.TargetConnectorViewModel.Name;

            }
        }
        #endregion

        

        #region Protected Override
        /// <summary>
        /// GetShapeContainerForItemOverride
        /// </summary>
        /// <param name="item"></param>
        /// <returns></returns>
        protected override IShape GetShapeContainerForItemOverride(object item)
        {
            if (item is ActionNodeViewModel)
            {
                var shape = new ActionNodeShape()
                {
                    DataContext = item
                };

                shape.CreateConnectors();

                return shape;
            }
            else if (item is EventNodeViewModel)
            {
                var shape = new EventNodeShape
                {
                    DataContext = item
                };

                shape.CreateConnectors();

                return shape;
            }
            else if (item is ConditionNodeViewModel)
            {
                var shape = new ConditionNodeShape()
                {
                    DataContext = item
                };

                shape.CreateConnectors();

                return shape;
            }
            else
            {
                return null;
            }
        } 
        #endregion
    }
}
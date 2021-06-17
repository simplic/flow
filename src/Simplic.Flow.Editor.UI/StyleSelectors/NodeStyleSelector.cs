using System.Windows;
using System.Windows.Controls;

namespace Simplic.Flow.Editor.UI
{
    /// <summary>
    /// NodeStyleSelector
    /// </summary>
    public class NodeStyleSelector : StyleSelector
    {
        public Style ActionNodeStyle { get; set; }
        public Style EventNodeStyle { get; set; }
        public Style ConditionNodeStyle { get; set; }

        /// <summary>
        /// Selects a style based on the node type
        /// </summary>
        /// <param name="item">NodeViewModel</param>
        /// <param name="container">DependencyObject</param>
        /// <returns>Style</returns>
        public override Style SelectStyle(object item, DependencyObject container)
        {            
            if (item is ActionNodeViewModel)
                return ActionNodeStyle;
            else if (item is EventNodeViewModel)
                return EventNodeStyle;
            else if (item is ConditionNodeViewModel)
                return ConditionNodeStyle;
            else
                return base.SelectStyle(item, container);
        }
    }
}

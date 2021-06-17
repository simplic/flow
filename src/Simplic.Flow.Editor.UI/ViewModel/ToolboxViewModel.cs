using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Telerik.Windows.Controls.Diagrams.Extensions;

namespace Simplic.Flow.Editor.UI
{
    /// <summary>
    /// ToolboxViewModel
    /// </summary>
    public class ToolboxViewModel : Simplic.UI.MVC.ViewModelBase
    {
        #region Private Members
        private Gallery selectedGallery;
        #endregion

        #region Constructor
        public ToolboxViewModel(IList<Definition.NodeDefinition> nodeDefinitions)
        {
            GalleryItems = new ObservableCollection<Gallery>();
            NodeDefinitions = nodeDefinitions;

            LoadGalleryItems();
        }
        #endregion

        #region Private Methods

        #region [LoadGalleryItems]
        private void LoadGalleryItems()
        {
            var categories = NodeDefinitions.OrderBy(x => x.Category).GroupBy(x => x.Category);

            foreach (var category in categories)
            {
                var gallery = new Gallery
                {
                    Header = category.Key
                };

                if (string.IsNullOrEmpty(gallery.Header))
                {
                    gallery.Header = "General";
                }

                foreach (var item in category.OrderBy(x => x.DisplayName))
                {
                    var galleryItem = new GalleryItem
                    {
                        Header = item.DisplayName
                    };
                    
                    if (item is Definition.ActionNodeDefinition)
                    {
                        galleryItem.Shape = new ActionNodeShape
                        {
                            Name = item.Name,
                            DataContext = item,
                        };
                    }
                    else if (item is Definition.ConditionNodeDefinition)
                    {
                        galleryItem.Shape = new ConditionNodeShape
                        {
                            Name = item.Name,
                            DataContext = item,
                        };
                    }
                    else if(item is Definition.EventNodeDefinition)
                    {
                        galleryItem.Shape = new EventNodeShape
                        {
                            Name = item.Name,                            
                            DataContext = item,
                        };
                    }

                    gallery.Items.Add(galleryItem);                    
                }

                GalleryItems.Add(gallery);
            }            
            
            SelectedGallery = GalleryItems.FirstOrDefault();
        }
        #endregion

        #endregion
        
        #region Public Properties

        #region [GalleryItems]
        public ObservableCollection<Gallery> GalleryItems { get; set; }
        #endregion

        #region [NodeDefinitions]
        public IList<Definition.NodeDefinition> NodeDefinitions { get; }
        #endregion

        #region [SelectedGallery]
        public Gallery SelectedGallery
        {
            get { return selectedGallery; }
            set { selectedGallery = value; RaisePropertyChanged(nameof(SelectedGallery)); }
        }
        #endregion

        #endregion
    }
}

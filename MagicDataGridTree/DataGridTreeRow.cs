using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows;

namespace MagicDataGridTree
{

    public class DataGridTreeRow : DataGridRow
    {
        internal void SetCtlData(TreeRowCtlData treeRowCtlData)
        {
            TreeRowCtlData=treeRowCtlData;

            this.Expanded = TreeRowCtlData?.Expanded??false;

            if (checkOldBinding(ChildrenProperty))
                BindingOperations.SetBinding(this, ChildrenProperty,
                    new Binding($"{nameof(TreeRowCtlData.ChildrenDatas)}") { Source = TreeRowCtlData,Mode=BindingMode.OneWay });

            if (checkOldBinding(HasChildProperty))
                BindingOperations.SetBinding(this, HasChildProperty,
                new Binding($"{nameof(TreeRowCtlData.HasChild)}") { Source = TreeRowCtlData, Mode = BindingMode.OneWay });

            if (checkOldBinding(ChildrenCountProperty))
                BindingOperations.SetBinding(this, ChildrenCountProperty,
                new Binding($"{nameof(TreeRowCtlData.ChildrenCount)}") { Source = TreeRowCtlData, Mode = BindingMode.OneWay });

            if (checkOldBinding(TreeLeveProperty))
                BindingOperations.SetBinding(this, TreeLeveProperty,
                new Binding($"{nameof(TreeRowCtlData.Leve)}")
                {
                    Source = TreeRowCtlData,
                    Mode = BindingMode.OneWay
                });

            BindingOperations.SetBinding(this, AllChildrenProperty,
                new Binding($"{nameof(TreeRowCtlData.AllChildrenDatas)}")
                {
                    Source = TreeRowCtlData,
                    Mode = BindingMode.OneWay
                });
        }

        private bool checkOldBinding(DependencyProperty bindingProperty)
        {
            var hasBinding = BindingOperations.GetBinding(this, bindingProperty);
            if(hasBinding!=null)
            {
                if(hasBinding.Source is TreeRowCtlData)
                {
                    BindingOperations.ClearBinding(this, bindingProperty);
                }
                else
                {
                    return false;
                }
            }
            return true;
        }

        internal TreeRowCtlData TreeRowCtlData{get;private set;}

        public bool ShowToggleButton
        {
            get { return (bool)GetValue(ShowToggleButtonProperty); }
            set { SetValue(ShowToggleButtonProperty, value); }
        }
        private static readonly DependencyProperty ShowToggleButtonProperty =
            DependencyProperty.Register("ShowToggleButton", typeof(bool), typeof(DataGridTreeRow), new PropertyMetadata(true));


        public bool Expanded
        {
            get { return (bool)GetValue(ExpandedProperty); }
            set { SetValue(ExpandedProperty, value); }
        }
        public static readonly DependencyProperty ExpandedProperty =
            DependencyProperty.Register("Expanded", typeof(bool), typeof(DataGridTreeRow), new PropertyMetadata(false,expandedPropertyChangedCallback));

        private static void expandedPropertyChangedCallback(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var row = (DataGridTreeRow)d;
            if (row.TreeRowCtlData != null && e.NewValue is bool expanded)
            {
                row.TreeRowCtlData.Expanded = expanded;
            }
        }

        public bool HasChild
        {
            get { return (bool)GetValue(HasChildProperty); }
            set { SetValue(HasChildProperty, value); }
        }
        public static readonly DependencyProperty HasChildProperty =
            DependencyProperty.Register("HasChild", typeof(bool), typeof(DataGridTreeRow), new PropertyMetadata(false));

        public int ChildrenCount
        {
            get { return (int)GetValue(ChildrenCountProperty); }
            private set { SetValue(ChildrenCountProperty, value); }
        }
        private static readonly DependencyProperty ChildrenCountProperty =
            DependencyProperty.Register("ChildrenCount", typeof(int), typeof(DataGridTreeRow), new PropertyMetadata(0));

        public IEnumerable Children
        {
            get { return (IEnumerable)GetValue(ChildrenProperty); }
            private set { SetValue(ChildrenProperty, value); }
        }
        private static readonly DependencyProperty ChildrenProperty =
            DependencyProperty.Register("Children", typeof(IEnumerable), typeof(DataGridTreeRow), new PropertyMetadata(null));

        public IEnumerable AllChildren
        {
            get { return (IEnumerable)GetValue(AllChildrenProperty); }
            private set { SetValue(AllChildrenProperty, value); }
        }
        private static readonly DependencyProperty AllChildrenProperty =
            DependencyProperty.Register("AllChildren", typeof(IEnumerable), typeof(DataGridTreeRow), new PropertyMetadata(null));

        public int TreeLeve
        {
            get { return (int)GetValue(TreeLeveProperty); }
            private set { SetValue(TreeLeveProperty, value); }
        }
        private static readonly DependencyProperty TreeLeveProperty =
            DependencyProperty.Register("TreeLeve", typeof(int), typeof(DataGridTreeRow), new PropertyMetadata(0));

    }
}

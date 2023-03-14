using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Threading.Tasks;
using System.Threading;
using System.Windows.Controls.Primitives;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows;
using System.Collections;
using System.Linq;
using Exp = System.Linq.Expressions.Expression;
using System.Globalization;
using System.Xaml;
using System.IO;
using System.Xml;
using System.Reflection;

namespace MagicDataGridTree
{
    public class DataGridTree : DataGrid
    {
        static DataGridTree()
        {
            Application.Current.Resources["__Magic.DataGridTree.PaddingLeft.Converter__"] = new PaddingLeftConverter();
            Application.Current.Resources["__Magic.DataGridTree.InnerToggleButton.Visibility.Converter__"] = new InnerToggleButtonVisibilityConverter();
        }
        private static DataTemplate getTreeCellTemplate(object treeCell)
        {
            using var reader = new StreamReader(Assembly.GetExecutingAssembly().GetManifestResourceStream("MagicDataGridTree.DataGridTreeStyles.xaml")!);
            var cellTempStr = reader.ReadToEnd();
            if (treeCell != null)
            {
                var cellStr = System.Windows.Markup.XamlWriter.Save(treeCell);
                cellTempStr = cellTempStr.Replace("</ContentControl>", cellStr + "</ContentControl>");
            }
            StringReader stringReader = new StringReader(cellTempStr);
            XmlReader xmlReader = XmlReader.Create(stringReader);
            var dataTemplateRlt = System.Windows.Markup.XamlReader.Load(xmlReader);
            return (DataTemplate)dataTemplateRlt;
        }

        private readonly Dictionary<object, TreeRowCtlData> _dicCtlDatas;
        private readonly List<TreeRowCtlData> _roots = new();
        private readonly List<object> _itemsDisplayList;
        private readonly ListCollectionView _itemsDisplayListView;
        private static bool _isOnItemsSourceChanged;
        public DataGridTree()
        {
            AutoGenerateColumns = false;
            CanUserAddRows = false;

            _dicCtlDatas = new();

            _itemsDisplayList = new();

            _itemsDisplayListView = new ListCollectionView(_itemsDisplayList);
            base.ItemsSource = _itemsDisplayListView;

            base.Sorting += DataGridTree_Sorting;
            
        }

        private void DataGridTree_Sorting(object sender, DataGridSortingEventArgs e)
        {
            e.Handled = true;
        }

        public double IndentSpaces { get; set; } = 8d;
        public object TreeCellHeader
        {
            get { return (object)GetValue(TreeCellHeaderProperty); }
            set { SetValue(TreeCellHeaderProperty, value); }
        }
        public static readonly DependencyProperty TreeCellHeaderProperty =
            DependencyProperty.Register("TreeCellHeader", typeof(object), typeof(DataGridTree), new PropertyMetadata(null));

        public DataGridColumn TreeCellTemplate
        {
            get { return (DataGridColumn)GetValue(TreeCellTemplateProperty); }
            set { SetValue(TreeCellTemplateProperty, value); }
        }
        public static readonly DependencyProperty TreeCellTemplateProperty =
            DependencyProperty.Register("TreeCellTemplate", typeof(DataGridColumn), typeof(DataGridTree), new PropertyMetadata(null));
        private DataGridColumn? _treeColumn;

        public object TreeCell
        {
            get { return (object)GetValue(TreeCellProperty); }
            set { SetValue(TreeCellProperty, value); }
        }
        public static readonly DependencyProperty TreeCellProperty =
            DependencyProperty.Register("TreeCell", typeof(object), typeof(DataGridTree), new PropertyMetadata(null));

        public PropertyPath IdPath
        {
            get { return (PropertyPath)GetValue(IdPathProperty); }
            set { SetValue(IdPathProperty, value); }
        }
        public static readonly DependencyProperty IdPathProperty =
            DependencyProperty.Register("IdPath", typeof(PropertyPath), typeof(DataGridTree), new PropertyMetadata(default));

        public PropertyPath ParentIdPath
        {
            get { return (PropertyPath)GetValue(ParentIdPathProperty); }
            set { SetValue(ParentIdPathProperty, value); }
        }
        public static readonly DependencyProperty ParentIdPathProperty =
            DependencyProperty.Register("ParentIdPath", typeof(PropertyPath), typeof(DataGridTree), new PropertyMetadata(default));



        public bool ExpandAll
        {
            get { return (bool)GetValue(ExpandAllProperty); }
            set { SetValue(ExpandAllProperty, value); }
        }
        public static readonly DependencyProperty ExpandAllProperty =
            DependencyProperty.Register("ExpandAll", typeof(bool), typeof(DataGridTree), new PropertyMetadata(false, expandAllPropertyChangedCallback));

        private static void expandAllPropertyChangedCallback(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var tree = (DataGridTree)d;
            if (e.NewValue is bool expand)
            {
                tree.setIsOpenAll(expand);
            }
        }

        private void setIsOpenAll(bool isOpen)
        {
            _itemsDisplayListView.CancelEdit();
            foreach (var one in _roots)
            {
                one.SetIsOpenAll(isOpen);
            }
            _itemsDisplayListView.Refresh();
        }

        [Bindable(true),]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public new IEnumerable ItemsSource
        {
            get { return (IEnumerable)GetValue(ItemsSourceProperty); }
            set
            {
                if (value == null)
                {
                    ClearValue(ItemsSourceProperty);
                }
                else
                {
                    SetValue(ItemsSourceProperty, value);
                }
            }
        }
        public new static readonly DependencyProperty ItemsSourceProperty
            = DependencyProperty.Register("ItemsSource", typeof(IEnumerable), typeof(DataGridTree),
                                          new FrameworkPropertyMetadata(null,
                                                                        new PropertyChangedCallback(OnItemsSourceChanged)));

        private static void OnItemsSourceChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            DataGridTree tree = (DataGridTree)d;
            IEnumerable oldValue = (IEnumerable)e.OldValue;
            IEnumerable newValue = (IEnumerable)e.NewValue;

            ((IContainItemStorage)tree).Clear();

            tree.fresh();
            _isOnItemsSourceChanged = true;
            tree.OnItemsSourceChanged(oldValue, newValue);
            _isOnItemsSourceChanged = false;
        }

        protected override void OnItemsSourceChanged(IEnumerable oldValue, IEnumerable newValue)
        {
            if (_isOnItemsSourceChanged == false) return;

            if (oldValue is INotifyCollectionChanged oldVal)
            {
                oldVal.CollectionChanged -= Notify_CollectionChanged;
            }
            if (newValue is INotifyCollectionChanged newVal)
            {
                newVal.CollectionChanged += Notify_CollectionChanged;
            }

            base.OnItemsSourceChanged(oldValue, newValue);
        }

        private void Notify_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (sender != this.ItemsSource) return;

            fresh(true);
        }

        private void applayTreeColumn()
        {
            if (_treeColumn != null) return;

            if (this.TreeCellTemplate == null)
            {
                var binding = BindingOperations.GetBinding(this, TreeCellProperty);
              var cellTemplate = getTreeCellTemplate(binding??this.TreeCell);
                _treeColumn = new DataGridTemplateColumn
                {
                    CellTemplate = cellTemplate,
                };
                BindingOperations.SetBinding(_treeColumn, DataGridColumn.HeaderProperty, new Binding(nameof(TreeCellHeader)) { Source = this });
            }
            else
            {
                _treeColumn = this.TreeCellTemplate;
            }
            base.Columns.Insert(0, _treeColumn);
        }
        public override void OnApplyTemplate()
        {
            applayTreeColumn();
            base.OnApplyTemplate();
            //XamlObjectReader lx = new XamlObjectReader(null);
            //lx.Read();

        }

        private void fresh(bool merge = false)
        {
            _itemsDisplayListView.CancelEdit();
            TreeHelper treeHelper = new(this);
            treeHelper.BuildTree();
            _itemsDisplayListView.Refresh();
        }

        protected override void OnPreviewMouseLeftButtonDown(MouseButtonEventArgs e)
        {
            base.OnPreviewMouseLeftButtonDown(e);
        }

        protected override void OnLoadingRowDetails(DataGridRowDetailsEventArgs e)
        {
            base.OnLoadingRowDetails(e);
        }
        protected override DependencyObject GetContainerForItemOverride()
        {
            var row = new DataGridTreeRow();
            return row;
        }
        protected override void PrepareContainerForItemOverride(DependencyObject element, object item)
        {
            var row = (DataGridTreeRow)element;
            //row.Resources["Part_TreeGrid_TreeCell_Default"] = this.TreeCellBinding ?? this.TreeCell;
            
            base.PrepareContainerForItemOverride(element, item);
            //BindingBase binding;
            //if (this.TreeCellBinding == null) { binding = new Binding(nameof(TreeCell)) { Source = this }; }
            //else
            //{
            //    binding = processBinding(this.TreeCellBinding, item);
            //}
            _dicCtlDatas[item].TreeGridRow = row;
            //BindingOperations.SetBinding(row.TreeRowCtlData, TreeRowCtlData.TreeCellProperty, binding);
        }

        private class TreeHelper
        {
            private readonly List<object> ordedArr = new();
            private readonly DataGridTree treeGridData;
            private readonly IEnumerable ItemsSource;
            private readonly Dictionary<object, TreeRowCtlData> _dicToTreeDatas;
            private Dictionary<object, TreeRowCtlData>? _dicToTreeDatasOld;
            private readonly List<object> _itemsDisplayList;
            private readonly List<TreeRowCtlData> _roots;

            public TreeHelper(DataGridTree treeGridData)
            {
                this.treeGridData = treeGridData;
                ItemsSource = treeGridData.ItemsSource;
                _dicToTreeDatas = treeGridData._dicCtlDatas;
                _itemsDisplayList = treeGridData._itemsDisplayList;
                _roots = treeGridData._roots;
            }

            public void BuildTree()
            {
                _dicToTreeDatasOld= _dicToTreeDatas.ToDictionary(t=>t.Key, t=>t.Value);
                _dicToTreeDatas.Clear();
                _itemsDisplayList.Clear();

                var sourceType = this.ItemsSource.GetType();
                var source = this.ItemsSource.Cast<object>()
                    .Select(t =>
                    _dicToTreeDatasOld.ContainsKey(t) ? _dicToTreeDatasOld[t] : new TreeRowCtlData(t, treeGridData, _itemsDisplayList, _dicToTreeDatas, treeGridData._itemsDisplayListView)
                    );
                var dicPIdGroups = source.GroupBy(s => s.ParentId).ToDictionary(g => g.Key, g => g.ToList());
                _roots.Clear();
                var roots = dicPIdGroups.FirstOrDefault(g => dicPIdGroups.Where(t => !t.Equals(g)).SelectMany(t => t.Value).All(dv => dv.Id.Equals(g.Key) != true));
                if (roots.Value?.Count > 0)
                    _roots.AddRange(roots.Value);

                buildTreeInner(_roots, dicPIdGroups);
            }

            private void buildTreeInner(List<TreeRowCtlData> currentLeveDatas,
                Dictionary<object, List<TreeRowCtlData>> dic,
                TreeRowCtlData? parent = null, int leve = 0, bool visibleChild = true)
            {
                if (currentLeveDatas == null) return;
                foreach (var one in currentLeveDatas)
                {
                    one.Leve = leve;
                    one.Parent = parent;
                    _dicToTreeDatas[one.Target] = one;
                    ordedArr.Add(one.Target);
                    if (visibleChild)
                    {
                        _itemsDisplayList.Add(one.Target);
                    }
                    if (dic.ContainsKey(one.Id))
                    {
                        one.Children = dic[one.Id];
                        buildTreeInner(one.Children, dic, one, leve + 1, one.Expanded && visibleChild);
                    }
                }
            }

        }

        private class PaddingLeftConverter : IMultiValueConverter
        {
            public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
            {
                if (values?.Length > 1 && values[0] is double d1 && values[1] is int d2)
                {
                    return d1 * d2;
                }
                return DependencyProperty.UnsetValue;
            }

            public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
            {
                throw new NotImplementedException();
            }
        }
        private class InnerToggleButtonVisibilityConverter : IMultiValueConverter
        {
            public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
            {
                if (values?.Length > 1 && values[0] is bool d1 && values[1] is bool d2)
                {
                    return (d1 && d2)? Visibility.Visible : Visibility.Hidden;
                }
                return DependencyProperty.UnsetValue;
            }

            public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
            {
                throw new NotImplementedException();
            }
        }
    }
}

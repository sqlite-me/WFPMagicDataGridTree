using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Data;
using System.Windows;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.ComponentModel;

namespace MagicDataGridTree
{

    public class TreeRowCtlData : DependencyObject, INotifyPropertyChanged, INotifyPropertyChanging
    {
        /// <inheritdoc cref="INotifyPropertyChanged.PropertyChanged"/>
        public event PropertyChangedEventHandler? PropertyChanged;

        /// <inheritdoc cref="INotifyPropertyChanging.PropertyChanging"/>
        public event PropertyChangingEventHandler? PropertyChanging;

        public TreeRowCtlData(object target, DataGridTree treeGridData, List<object> _itemsDisplayList, IDictionary<object, TreeRowCtlData> _dicToTreeDatas, ListCollectionView _itemsSourceView)
        {
            _treeGridData = treeGridData;
            this._itemsDisplayList = _itemsDisplayList;
            this._dicToTreeDatas = _dicToTreeDatas;
            this._itemsSourceView = _itemsSourceView;
            Target = target;
            Expanded = treeGridData.ExpandAll;
            BindingOperations.SetBinding(this, IdProperty, new Binding() { Path = treeGridData.IdPath, Source = Target, Mode = BindingMode.OneTime });
            BindingOperations.SetBinding(this, ParentIdProperty, new Binding() { Path = treeGridData.ParentIdPath, Source = Target, Mode = BindingMode.OneTime });
        }

        public bool Expanded
        {
            get { return _expanded; }
            set
            {
                if (SetProperty(ref _expanded, value))
                {
                    if (value)
                    {
                        expand();
                    }
                    else
                    {
                        unExpand();
                    }
                }
            }
        }
        private bool _expanded;

        private void unExpand(bool needFresh = true)
        {
            if (this.ChildrenDatas?.Any() != true) { return; }

            _itemsSourceView.CancelEdit();
            var startIndex = _itemsDisplayList.IndexOf(Target);
            if (startIndex == -1) return;
            startIndex++;
            var end = _itemsDisplayList.FindIndex(startIndex, o => _dicToTreeDatas[o].Leve == Leve);
            var removeCount = (end == -1) ? _itemsDisplayList.Count - startIndex : end - startIndex;
            if (removeCount > 0)
            {
                _itemsDisplayList.RemoveRange(startIndex, removeCount);
            }

            if (needFresh)
                _itemsSourceView.Refresh();

            if (_expanded != false)
            {
               SetProperty(ref _expanded , false,nameof(Expanded));
            }
        }

        private void expand(bool needFresh = true)
        {
            _itemsSourceView.CancelEdit();
            var startIndex = _itemsDisplayList.IndexOf(Target);
            if (startIndex == -1) return;

            foreach (var item in getVisibleChildren())
            {
                startIndex++;
                if (_itemsDisplayList.Count > startIndex && item == _itemsDisplayList[startIndex])
                    continue;
                _itemsDisplayList.Insert(startIndex, item);
            }

            if (needFresh)
                _itemsSourceView.Refresh();
            if (_expanded != true)
            {
                SetProperty(ref _expanded, true, nameof(Expanded));
            }
        }

        private IEnumerable<object> getVisibleChildren()
        {
            if (Children?.Count > 0)
            {
                foreach (var item in Children)
                {
                    yield return item.Target;
                    if (item.Expanded)
                    {
                        foreach (var child in item.getVisibleChildren())
                            yield return child;
                    }
                }
            }
        }

        public TreeRowCtlData? Parent{get=>_parent; set=>_parent = value; }
        public TreeRowCtlData? _parent;

        public List<TreeRowCtlData> Children { get; set; }
        public IEnumerable<TreeRowCtlData> AllChildren => getAllChildren();
        public object Target { get; }
        private readonly DataGridTree _treeGridData;
        private readonly List<object> _itemsDisplayList;
        private readonly IDictionary<object, TreeRowCtlData> _dicToTreeDatas;
        private readonly ListCollectionView _itemsSourceView;

        public bool HasChild => Children?.Count > 0;

        public int ChildrenCount => Children?.Count ?? 0;

        public int AllChildrenCount => getAllChildren()?.Count() ?? 0;

        public IEnumerable<object> ChildrenDatas => Children?.Select(t => t.Target)?? Array.Empty<object>();

        public IEnumerable<object> AllChildrenDatas => getAllChildren()?.Select(t => t.Target) ?? Array.Empty<object>();

        private IEnumerable<TreeRowCtlData> getAllChildren()
        {
            if (Children?.Count > 0)
            {
                foreach (var child in Children)
                {
                    yield return child;
                    if (child.Children?.Count > 0)
                    {
                        foreach (var iChild in child.getAllChildren())
                            yield return iChild;
                    }
                }
            }
        }

        public object Id
        {
            get { return (object)GetValue(IdProperty); }
            set { SetValue(IdProperty, value); }
        }
        public static readonly DependencyProperty IdProperty =
            DependencyProperty.Register("Id", typeof(object), typeof(TreeRowCtlData), new PropertyMetadata(null));

        public object ParentId
        {
            get { return (object)GetValue(ParentIdProperty); }
            set { SetValue(ParentIdProperty, value); }
        }
        public static readonly DependencyProperty ParentIdProperty =
            DependencyProperty.Register("ParentId", typeof(object), typeof(TreeRowCtlData), new PropertyMetadata(null));


        public int Leve { get=> _leve; internal set=>SetProperty(ref _leve,value); }
        private int _leve;
        internal void SetIsOpenAll(bool isOpen)
        {
            if (isOpen)
            {
                expand(false);
            }
            else
            {
                unExpand(false);
            }

            if (Children?.Count > 0)
            {
                foreach (var child in Children)
                {
                    child.SetIsOpenAll(isOpen);
                }
            }
        }

        internal static CancellationTokenSource GetTaskCancellationSource => new CancellationSource();
        private class CancellationSource : CancellationTokenSource
        {
            public int Count { get; set; }
        }
        protected bool SetProperty<T>([NotNullIfNotNull("newValue")] ref T field, T newValue, [CallerMemberName] string? propertyName = null)
        {
            if (EqualityComparer<T>.Default.Equals(field, newValue))
            {
                return false;
            }

            OnPropertyChanging(new PropertyChangingEventArgs(propertyName));

            field = newValue;

            OnPropertyChanged(propertyName);

            return true;
        }
        protected virtual void OnPropertyChanging(PropertyChangingEventArgs e)
        {
            ArgumentNullException.ThrowIfNull(e);

            PropertyChanging?.Invoke(this, e);
        }
        protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            OnPropertyChanged(new PropertyChangedEventArgs(propertyName));
        }
        protected virtual void OnPropertyChanged(PropertyChangedEventArgs e)
        {
            ArgumentNullException.ThrowIfNull(e);

            PropertyChanged?.Invoke(this, e);
        }

    }
}

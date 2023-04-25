using MagicDataGridTree;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;

namespace Demo
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        string[] strs;
        public MainWindow()
        {
            InitializeComponent();

           // addDatas(rd.Next(500, 5000));

            treeGridData.ItemsSource = view = CollectionViewSource.GetDefaultView(datas);

            //var tt=new NullKey()==new NullKey<int>();
            //new KeyValuePair<string, string>(null, null);
            //strs = new[] {"1","2","2","3","3",null,null };
            //var group = strs.GroupBy(t => t).ToArray();
            //var dic = new Dictionary<string?, object>(new com());
            //foreach (var item in group)
            //{
            //    dic[item.Key??NullKey.Get(item.Key)] = item?.ToArray();
            //}
            
            //var dic2 = group.ToDictionary(t => t.Key ?? NullKey.Get(t.Key), t => t.ToArray());
        }

        private List<object> datas = new();
        Random rd = new Random();
        private readonly ICollectionView view;

        private void addDatas(int count)
        {
            Task.Run(() =>
            {
                var end = count + datas.Count;
                for (int i = datas.Count; i < end; i++)
                {
                    var pid = i == 0 ? -1 : rd.Next(-1, i - 1);
                    datas.Add(new Data()
                    {
                        Id = i,
                        Name = "name " + i,
                        PId = pid,
                    });
                }
                Dispatcher.Invoke(view.Refresh);
            });
        }

        private void removeDatas(int start, int count)
        {
            Task.Run(() =>
            {
                var end = count + start;
                if (end > datas.Count) end = datas.Count;
                for (int i = end - 1; i >= start; i--)
                {
                    datas.RemoveAt(i);
                }
                Dispatcher.Invoke(view.Refresh);
            });
        }

        public class Data
        {
            public int Id { get; set; }
            public string Name { get; set; }
            public int PId { get; set; }
        }

        private void btnTest_Click(object sender, RoutedEventArgs e)
        {
            addDatas(rd.Next(500, 5000));
        }

        private void btnTestRemove_Click(object sender, RoutedEventArgs e)
        {
            if(datas.Count>0)
            removeDatas(rd.Next(0, datas.Count - 1), rd.Next());
        }

        private void TextBlock_Loaded(object sender, RoutedEventArgs e)
        {

        }

        private void TextBlock_DataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {

        }

        private void btnAddOne_Click(object sender, RoutedEventArgs e)
        {
            addDatas(1);
        }
    }

    class com : IEqualityComparer<string>
    {
        public bool Equals(string? x, string? y)
        {
            return x == y;
        }

        public int GetHashCode([DisallowNull] string obj)
        {
            return obj?.GetHashCode() ?? 0;
        }
    }
}

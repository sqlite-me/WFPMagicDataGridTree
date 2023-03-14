using System;
using System.Collections.Generic;
using System.ComponentModel;
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
        public MainWindow()
        {
            InitializeComponent();

            addDatas(rd.Next(500, 5000));

            treeGridData.ItemsSource = view = CollectionViewSource.GetDefaultView(datas);
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
    }
}

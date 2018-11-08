using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

using System.Collections.ObjectModel;
using System.ComponentModel;

namespace WPFBindingDemo
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        //public List<ComboBoxItemColor> ColorListEnum { get; set; }
        public List<GPSStatus> status { get; set; }


        public MainWindow()
        {
            status = new List<GPSStatus>()
                {
                    new GPSStatus(){ StatusID = 0, StatusName = "0:Disable" },
                    new GPSStatus(){ StatusID = 1, StatusName = "1:Enable" },
                };



            // Set the data context for this window.
            DataContext = this;

            InitializeComponent();
        }

        //List<GPSStatus> status = new List<WPFBindingDemo.GPSStatus>();
        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            Student student = new Student();     //创建一个Student对象的实例
            CB_Grades.DataContext = student ;  //指定Text属性的数据上下文，CB_Grades为 ComboBox 的 name 属性
            List<Grades> grades = new List<Grades>(); //创建一个List<Grades>集合，并初始化对象
            grades.Add(new Grades{ GradeID=1, GradeName = "高一"});
            grades.Add(new Grades{ GradeID=2, GradeName = "高二"});
            grades.Add(new Grades{ GradeID=3, GradeName = "高三"});
            CB_Grades.ItemsSource = grades ;  //指定下拉列表的 ItemsSource 数据源 ，CB_Grades为 ComboBox 的 name 属性


            Gateway gateway = new WPFBindingDemo.Gateway();
            gateway.GPSStatus = 0;

            /*
            GPSStatus s = new GPSStatus();
            s.StatusID = 0;
            s.StatusName = "0:Disable";
            status.Add(s);

            s = new GPSStatus();
            s.StatusID = 1;
            s.StatusName = "1:Enable";
            status.Add(s);
            */

            StackObject1.DataContext = gateway;

        }
    }
}

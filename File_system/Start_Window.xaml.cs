using System;
using System.Collections.Generic;
using System.IO;
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
using System.Windows.Shapes;

using File_system.File_System;
using Microsoft.Win32;

namespace File_system_coursework
{
    public partial class Start_Window : Window
    {
        string fs_name;

        public Start_Window()
        {
            InitializeComponent();
            Grid_Start.Visibility = Visibility.Visible;
            Grid_Create_Input_FS.Visibility = Visibility.Hidden;
        }

        private void Button_Create_FS_Click(object sender, RoutedEventArgs e)
        {           
            Grid_Start.Visibility = Visibility.Collapsed;
            Grid_Create_Input_FS.Visibility = Visibility.Visible;
        }

        private void Button_Choose_FS_Click(object sender, RoutedEventArgs e)
        {            
            OpenFileDialog open_FD = new OpenFileDialog();
            open_FD.Filter = "File System (*.fsn)|*.fsn";
            open_FD.InitialDirectory = AppDomain.CurrentDomain.BaseDirectory;
            if (open_FD.ShowDialog() == true)
            {
                Grid_Start.Visibility = Visibility.Collapsed;
                Grid_Choose_Input_FS.Visibility = Visibility.Visible;

                string[] split = open_FD.FileName.Split('\\');
                fs_name = split[split.Length - 1];
            }
            else
            {
                MessageBox.Show("Невозможно открыть ФС!", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void Button_Create2_FS_Click(object sender, RoutedEventArgs e)
        {
            if (String.IsNullOrEmpty(TextBox_Type_FS.Text) || String.IsNullOrEmpty(ComboBox_Cluster_Size.Text) || String.IsNullOrEmpty(TextBox_Size_FS.Text) || 
                String.IsNullOrEmpty(TextBox_Create_Login_User.Text) || String.IsNullOrEmpty(PasswordBox_Create_Password_User.Password))
            {
                MessageBox.Show("Заполните все поля!", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            else if (Convert.ToInt32(TextBox_Size_FS.Text) < 0)
            {
                MessageBox.Show("Размер файловой системы не может быть меньше 0 Кб!", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            else
            {
                SaveFileDialog save_FD = new SaveFileDialog();
                save_FD.Filter = "File System (*.fsn)|*.fsn";
                save_FD.InitialDirectory = AppDomain.CurrentDomain.BaseDirectory;
                if (save_FD.ShowDialog() == true)
                {
                    if (File.Exists(save_FD.FileName))
                    {
                        File.Delete(save_FD.FileName);
                    }
                    File_System fs = new File_System(save_FD.FileName);
                    string[] split = save_FD.FileName.Split('\\');
                    fs_name = split[split.Length - 1];
                    fs.Create_File_System(fs_name.ToCharArray(), TextBox_Type_FS.Text.ToCharArray(), Convert.ToUInt16(ComboBox_Cluster_Size.Text),
                        Convert.ToUInt64(TextBox_Size_FS.Text)*1024, TextBox_Create_Login_User.Text.ToCharArray(), PasswordBox_Create_Password_User.Password.ToCharArray());
                    MessageBox.Show("Файловая система создана!", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);

                    MainWindow mw = new MainWindow(fs);
                    this.Close();
                    mw.Show();                    
                }
                else
                {
                    MessageBox.Show("Невозможно создать ФС!", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void Button_Choose2_FS_Click(object sender, RoutedEventArgs e)
        {
            File_System fs = new File_System(fs_name);
            try
            {
                fs.Load_File_System(TextBox_Choose_Login_User.Text.ToCharArray(), PasswordBox_Choose_Password_User.Password.ToCharArray());
                MainWindow mw = new MainWindow(fs);
                this.Close();
                mw.Show();
            }
            catch (Exception exc)
            {
                MessageBox.Show(exc.Message, "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }          
        }

        private void Button_Door_Click(object sender, RoutedEventArgs e)
        {

            /*File_System fs = new File_System(fs_name);
            try
            {
                fs.Load_File_System("smert".ToCharArray(), "smert".ToCharArray());
                MainWindow mw = new MainWindow(fs);
                this.Close();
                mw.Show();
            }
            catch (Exception exc)
            {
                MessageBox.Show(exc.Message, "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }*/
        }
    }
}

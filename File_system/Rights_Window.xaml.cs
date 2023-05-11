using File_system.File_System;
using System;
using System.Collections.Generic;
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

namespace File_system_coursework
{
    public partial class Rights_Window : Window
    {
        File_System fs;
        uint inode_id;

        public Rights_Window(File_System fs, uint inode_id, char[] name)
        {
            InitializeComponent();
            this.fs = fs;
            this.inode_id = inode_id;
            Title = "Изменение прав доступа: " + new string(name);
        }

        private void Btn_Save_Click(object sender, RoutedEventArgs e)
        {
            ushort rights = 0;
            if (CB_User_R.IsChecked == true)
            {
                rights |= 1 << 14; // Оставляем первый бит под флаг для проверки на каталог
            }
            if (CB_User_W.IsChecked == true)
            {
                rights |= 1 << 13;
            }
            if (CB_User_X.IsChecked == true)
            {
                rights |= 1 << 12;
            }
            if (CB_Group_R.IsChecked == true)
            {
                rights |= 1 << 11;
            }
            if (CB_Group_W.IsChecked == true)
            {
                rights |= 1 << 10;
            }
            if (CB_Group_X.IsChecked == true)
            {
                rights |= 1 << 9;
            }
            if (CB_Other_R.IsChecked == true)
            {
                rights |= 1 << 8;
            }
            if (CB_Other_W.IsChecked == true)
            {
                rights |= 1 << 7;
            }
            if (CB_Other_X.IsChecked == true)
            {
                rights |= 1 << 6;
            }
            fs.Set_Flags(rights, inode_id);
            MessageBox.Show("Права установлены", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void Btn_Cancel_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}

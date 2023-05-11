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
    public partial class File_Window : Window
    {
        File_System fs;
        uint inode_id;
        public File_Window(uint inode_id, char[] name, File_System fs, bool can_write)
        {
            InitializeComponent();
            this.fs = fs;
            this.inode_id = inode_id;
            Title = new string(name);
            TextBox_File.Text = new string(fs.Get_Data_File(inode_id));
            TextBox_File.IsReadOnly = !can_write;
        }

        private void Button_Save_Click(object sender, RoutedEventArgs e)
        {
            int res = fs.Set_Data_File(inode_id, Encoding.Default.GetChars(Encoding.Default.GetBytes(TextBox_File.Text)));
            switch (res)
            {
                case 0:
                    {
                        MessageBox.Show("Данные сохранены!", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                        return;
                    }
                case 1:
                    {
                        MessageBox.Show("Места в файле недостаточно!", "Не успех", MessageBoxButton.OK, MessageBoxImage.Information);
                        return;
                    }
                case -1:
                    {
                        MessageBox.Show("Во время записи произошла ошибка!", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                        return;
                    }
            }
        }
    }
}

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
    public partial class Rename_Window : Window
    {
        File_System fs;
        uint inode_id;
        char[] old_name;

        public Rename_Window(char[] old_name, uint inode_id, File_System fs)
        {
            InitializeComponent();
            Title = "Переименование: " + old_name;
            TB_Rename.Text = new string(old_name);
            this.inode_id = inode_id;
            this.fs = fs;
            this.old_name = old_name;
        }

        private void Btn_Save_Click(object sender, RoutedEventArgs e)
        {
            switch (fs.Rename(old_name, TB_Rename.Text.ToCharArray(), inode_id))
            {
                case 0:
                    {
                        MessageBox.Show("Файл переименован", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                        break;
                    }

                case -1:
                    {
                        MessageBox.Show("Такое имя уже занято!", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                        break;
                    }

                case -2:
                    {
                        MessageBox.Show("Имя не может быть пустым!", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                        break;
                    }
            }
        }

        private void Btn_Cancel_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}

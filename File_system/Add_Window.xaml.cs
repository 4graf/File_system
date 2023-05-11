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
    public partial class Add_Window : Window
    {
        File_System fs;
        Dictionary<string, byte> groups;

        public Add_Window(File_System fs)
        {
            InitializeComponent();

            this.fs = fs;
            groups = fs.Get_All_Groups();
            
            CB_Group.ItemsSource = groups.Keys;
        }

        private void Btn_Add_User_Click(object sender, RoutedEventArgs e)
        {
            if (String.IsNullOrEmpty(TB_User_Login.Text) || String.IsNullOrEmpty(PB_User_Password.Password))
            {
                MessageBox.Show("Заполните все поля пользователя!", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
            byte gid;
            if (CB_Group.SelectedItem == null)
            {
                gid = 0;
            }
            else
            {
                gid = groups[(string)CB_Group.SelectedItem];
                if (gid == 1 && !fs.Is_Admin())
                {
                    MessageBox.Show("Добавлять пользователей в группу Admins может только администратор!", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }
            }
            switch (fs.Add_User(gid, TB_User_Login.Text.ToCharArray(), PB_User_Password.Password.ToCharArray()))
            {
                case -1:
                    {
                        MessageBox.Show("Ошибка создания!", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                        return;
                    }

                case 0:
                    {
                        MessageBox.Show("Пользователь добавлен", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                        return;
                    }

                case 1:
                    {
                        MessageBox.Show("Добавление нового пользователя невозможно! Список пользователей заполнен.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                        return;
                    }

                case 2:
                    {
                        MessageBox.Show("Такой логин уже занят!", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                        return;
                    }
            }
        }

        private void Btn_Delete_User_Click(object sender, RoutedEventArgs e)
        {
            if (String.IsNullOrEmpty(TB_User_Login.Text) || String.IsNullOrEmpty(PB_User_Password.Password))
            {
                MessageBox.Show("Заполните все поля пользователя!", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            switch (fs.Delete_User(TB_User_Login.Text.ToCharArray(), PB_User_Password.Password.ToCharArray()))
            {
                case -1:
                    {
                        MessageBox.Show("Ошибка удаления!", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                        return;
                    }
                case 1:
                    {
                        MessageBox.Show("Такого пользователя не существует!", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                        return;
                    }
                case 2:
                    {
                        MessageBox.Show("Введён неправильный пароль!", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                        return;
                    }
                case 0:
                    {
                        MessageBox.Show("Пользователь удалён", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                        groups = fs.Get_All_Groups();
                        CB_Group.ItemsSource = null;
                        CB_Group.ItemsSource = groups.Keys;
                        break;
                    }
            }
        }

        private void Btn_Add_Group_Click(object sender, RoutedEventArgs e)
        {
            if (String.IsNullOrEmpty(TB_Group_Name.Text) || String.IsNullOrEmpty(TB_Group_Description.Text))
            {
                MessageBox.Show("Заполните все поля группы!", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
            switch (fs.Add_Group(TB_Group_Name.Text.ToCharArray(), TB_Group_Description.Text.ToCharArray()))
            {
                case -1:
                    {
                        MessageBox.Show("Ошибка создания!", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                        return;
                    }

                case 0:
                    {
                        MessageBox.Show("Группа добавлена", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                        groups = fs.Get_All_Groups();
                        CB_Group.ItemsSource = null;
                        CB_Group.ItemsSource = groups.Keys;
                        return;
                    }

                case 1:
                    {
                        MessageBox.Show("Добавление новой группы невозможно! Список групп заполнен.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                        return;
                    }

                case 2:
                    {
                        MessageBox.Show("Такая группа уже существует!", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                        return;
                    }
            }
        }

        private void Btn_Delete_Group_Click(object sender, RoutedEventArgs e)
        {
            if (CB_Group.SelectedItem == null)
            {
                MessageBox.Show("Выберите группу!", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }                       
            switch (fs.Delete_Group(((string)CB_Group.SelectedItem).ToCharArray()))
            {
                case -1:
                    {
                        MessageBox.Show("Ошибка удаления!", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                        return;
                    }
                case 0:
                    {
                        MessageBox.Show("Группа удалена", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                        groups = fs.Get_All_Groups();
                        CB_Group.ItemsSource = null;
                        CB_Group.ItemsSource = groups.Keys;
                        break;
                    }
                case 1:
                    {
                        MessageBox.Show("Такой группы не существует!", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                        return;
                    }
                case 2:
                    {
                        MessageBox.Show("Группу Admins удалить нельзя!", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                        return;
                    }
            }
        }

        private void Btn_Change_User_Click(object sender, RoutedEventArgs e)
        {
            if (String.IsNullOrEmpty(TB_User_Login.Text) || String.IsNullOrEmpty(PB_User_Password.Password))
            {
                MessageBox.Show("Заполните все поля пользователя!", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            switch (fs.Change_User(TB_User_Login.Text.ToCharArray(), PB_User_Password.Password.ToCharArray()))
            {
                case -1:
                    {
                        MessageBox.Show("Ошибка!", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                        return;
                    }
                case 1:
                    {
                        MessageBox.Show("Такого пользователя не существует!", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                        return;
                    }
                case 2:
                    {
                        MessageBox.Show("Введён неправильный пароль!", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                        return;
                    }
                case 0:
                    {
                        MessageBox.Show("Пользователь изменён", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                        break;
                    }
            }
        }

        private void Btn_Change_Group_Click(object sender, RoutedEventArgs e)
        {
            if (String.IsNullOrEmpty(TB_Group_Name.Text))
            {
                MessageBox.Show("Введите имя группы!", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
            switch (fs.Change_Group(((string)CB_Group.SelectedItem).ToCharArray()))
            {
                case -1:
                    {
                        MessageBox.Show("Ошибка!", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                        return;
                    }
                case 1:
                    {
                        MessageBox.Show("Такой группы не существует!", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                        return;
                    }
                case 0:
                    {
                        MessageBox.Show("Группа изменена", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                        break;
                    }
            }
        }

    }
}

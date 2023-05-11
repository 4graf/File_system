using File_system.File_System;
using System.Windows;
using System.Windows.Controls;
using System.Collections.Generic;
using System;
using System.Text;
using static File_system.File_System.File_System;
using File_system;
using System.Linq;
using System.Windows.Media.Imaging;
using System.Threading;
using System.Threading.Tasks;

namespace File_system_coursework
{
    public partial class MainWindow : Window
    {
        File_System fs;
        string path_file_image = @"Resources\file_image.png";
        string path_catalog_image = @"Resources\catalog_image.jpg";

        Stack<uint> path_stack;
        Data_Copy buffer;

        public MainWindow(File_System fs)
        {
            InitializeComponent();
            this.fs = fs;
            path_stack = new Stack<uint>();

            path_stack.Push(1);

            Show_Data_Catalog(1); // читаем root

            Update_Path_TextBlock();            

            MessageBox.Show($"Приветствую тебя, брат!", "Здарова", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void Update_Path_TextBlock()
        {
            StringBuilder path = new StringBuilder();
            uint[] path_array = path_stack.ToArray();
            path.Append(fs.Read_Catalog(path_array[path_array.Length - 1])[0].name);
            for (int i = 1; i < path_array.Length; i++)
            {
                path.Append(" > ");
                path.Append(fs.Read_Catalog(path_array[path_array.Length - 1 - i])[0].name);
            }
            TextBlock_Current_Path.Text = path.ToString();
        }

        private void Move_To_Catalog(uint catalog_id)
        {
            path_stack.Push(catalog_id);
            Show_Data_Catalog(catalog_id);
            Update_Path_TextBlock();
        }

        private void Show_Data_Catalog(uint catalog_id)
        {
            WrapPanel_Work_Field.Children.Clear();
            WrapPanel_Work_Field.Children.Add(new StackPanel() { MinHeight = 100 });
            Data_Catalog[] data = fs.Read_Catalog(catalog_id);
            for (int i = 2; i < data.Length; i++)
            {
                bool[] flags = Get_Flags(data[i].permissions);
                if (!MenuItem_Show_Hidden.IsChecked && flags[10])
                {
                    continue;
                }
                var sp = new StackPanel();
                var image = new Image();
                var tb = new TextBlock();
                if (Inode.Is_Catalog(data[i].inode_id, fs.superblock))
                {
                    image.Source = new System.Windows.Media.Imaging.BitmapImage(new Uri(path_catalog_image, UriKind.Relative));
                    image.Height = 100;
                    tb.TextAlignment = TextAlignment.Center;
                    tb.Style = (Style)this.Resources["Text_Style_File_Name"];
                    tb.Foreground = System.Windows.Media.Brushes.Firebrick;
                    tb.Text = new string(data[i].name);
                }
                else
                {
                    image.Source = new System.Windows.Media.Imaging.BitmapImage(new System.Uri(path_file_image, System.UriKind.Relative));
                    image.Height = 100;
                    tb.TextAlignment = TextAlignment.Center;
                    tb.Style = (Style)this.Resources["Text_Style_File_Name"];
                    tb.Text = new string(data[i].name);
                }
                if (flags[10]) // Файл скрытый, режим просмотра скрытых файлов включен
                {
                    sp.ContextMenu = (ContextMenu)this.Resources["Context_Menu_Hidden_Item"];
                    sp.Opacity = 0.6;
                }
                else
                {
                    sp.ContextMenu = (ContextMenu)this.Resources["Context_Menu_Visible_Item"];
                }

                sp.Children.Add(image);
                sp.Children.Add(tb);
                sp.MouseLeftButtonDown += Work_Field_Double_Click;

                WrapPanel_Work_Field.Children.Add(sp);
            }
        }

        private void Create_File(char[] name = null, bool is_catalog = false)
        {
            if ((path_stack.Peek() != 1) && !fs.Check_Rights_Access(path_stack.Peek())[1] && !fs.Is_Admin())
            {
                MessageBox.Show("У вас нет права на создание файлов в этом каталоге!", "Ограниченные права", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            if (name == null)
            {
                if (is_catalog)
                {
                    name = Encoding.Default.GetChars(Encoding.Default.GetBytes("Новая папка"));
                }
                else
                {
                    name = Encoding.Default.GetChars(Encoding.Default.GetBytes("Новый файл"));
                }
            }
            char[] check_name = new char[28];
            Array.Copy(name, 0, check_name, 0, name.Length < 28 ? name.Length : 28);
            byte suffix = 1;
            while (fs.Create_File(check_name, is_catalog, path_stack.Peek()) == 0)
            {
                check_name[name.Length < 28 ? name.Length : 28] = suffix.ToString()[0];
                suffix++;
            }

            var sp = new StackPanel();
            var image = new Image();
            var tb = new TextBlock();
            if (is_catalog)
            {
                image.Source = new System.Windows.Media.Imaging.BitmapImage(new System.Uri(path_catalog_image, System.UriKind.Relative));
                image.Height = 100;
                tb.TextAlignment = TextAlignment.Center;
                tb.Style = (Style)this.Resources["Text_Style_File_Name"];
                tb.Foreground = System.Windows.Media.Brushes.Firebrick;
                tb.Text = new string(check_name);
                sp.ContextMenu = (ContextMenu)this.Resources["Context_Menu_Visible_Item"];
            }
            else
            {                
                image.Source = new System.Windows.Media.Imaging.BitmapImage(new System.Uri(path_file_image, System.UriKind.Relative));
                image.Height = 100;                
                tb.TextAlignment = TextAlignment.Center;
                tb.Style = (Style)this.Resources["Text_Style_File_Name"];
                tb.Text = new string(check_name);
                sp.ContextMenu = (ContextMenu)this.Resources["Context_Menu_Visible_Item"];
            }

            sp.Children.Add(image);
            sp.Children.Add(tb);
            sp.MouseLeftButtonDown += Work_Field_Double_Click;

            WrapPanel_Work_Field.Children.Add(sp);

        }

        private void Open_File(char[] name)
        {
            uint inode_id = 0;
            Data_Catalog[] data = fs.Read_Catalog(path_stack.Peek());
            for (int i = 2; i < data.Length; i++)
            {
                if (data[i].name.SequenceEqual(name))
                {
                    inode_id = data[i].inode_id;
                    break;
                }
            }
            if (inode_id != 0)
            {
                if (!fs.Check_Rights_Access(inode_id)[0] && !fs.Is_Admin())
                {
                    MessageBox.Show("У вас нет права на чтение файла!", "Ограниченные права", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }
                if (Inode.Is_Catalog(inode_id, fs.superblock))
                {
                    Move_To_Catalog(inode_id);
                }
                else
                {
                    bool can_write = false;
                    if (fs.Check_Rights_Access(inode_id)[1] && fs.Is_Admin())
                    {
                        can_write = true;
                    }
                    File_Window fw = new File_Window(inode_id, name, fs, can_write);
                    fw.Show();
                    if (!can_write)
                    {
                        MessageBox.Show("Только для чтения!", "Ограниченные права", MessageBoxButton.OK, MessageBoxImage.Warning);
                    }
                }
            }            
        }

        private void Delete_File(char[] name, uint catalog_id)
        {
            uint inode_id = 0;
            Data_Catalog[] data = fs.Read_Catalog(catalog_id);
            byte[] bytes = new byte[(data.Length - 1) * 32];            
            Array.Copy(BitConverter.GetBytes(data[0].inode_id), 0, bytes, 0, 4);
            Array.Copy(Encoding.Default.GetBytes(data[0].name), 0, bytes, 4, 28);
            Array.Copy(BitConverter.GetBytes(data[1].inode_id), 0, bytes, 32, 4);
            Array.Copy(Encoding.Default.GetBytes(data[1].name), 0, bytes, 36, 28);
            int writes_count = 2; // информация о текущем каталоге и о каталоге-родителе уже записана
            for (int i = 2; i < data.Length; i ++)
            {
                if (data[i].name.SequenceEqual(name))
                {
                    inode_id = data[i].inode_id;
                }
                else
                {
                    Array.Copy(BitConverter.GetBytes(data[i].inode_id), 0, bytes, writes_count * 32, 4);/////////////////////////////////////
                    Array.Copy(Encoding.Default.GetBytes(data[i].name), 0, bytes, writes_count * 32 + 4, 28); //// была проверка на длину
                    writes_count++;
                }
            }
            if (inode_id != 0)
            {
                if (!fs.Check_Rights_Access(inode_id)[1] && !fs.Is_Admin())
                {
                    MessageBox.Show("У вас нет права на изменение файла!", "Ограниченные права", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }
                if (Inode.Is_Catalog(inode_id, fs.superblock))
                {
                    Data_Catalog[] data_rec = fs.Read_Catalog(inode_id);
                    for (int i = 2; i < data_rec.Length; i++)
                    {
                        Delete_File(data_rec[i].name, inode_id);
                    }
                }
                fs.Delete_File(inode_id);
                fs.Write_Catalog(bytes, catalog_id);
                Show_Data_Catalog(catalog_id);
            }
        }

        private void Properties_File(char[] name)
        {
            uint inode_id = 0;
            Data_Catalog[] data = fs.Read_Catalog(path_stack.Peek());
            for (int i = 2; i < data.Length; i++)
            {
                if (data[i].name.SequenceEqual(name))
                {
                    inode_id = data[i].inode_id;
                    break;
                }
            }
            if (inode_id != 0)
            {
                MessageBox.Show(fs.Get_Properties_File(inode_id, Inode.Is_Catalog(inode_id, fs.superblock)), "Свойства: " + new string(name).Trim('\0'), MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void Work_Field_Double_Click(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (e.ClickCount >= 2)
            {
                StackPanel sp = (StackPanel)sender;
                Open_File(((TextBlock)sp.Children[1]).Text.ToCharArray());
            }
        }

        private void Menu_Create_Catalog_Click(object sender, RoutedEventArgs e)
        {
            Create_File(is_catalog: true);
        }

        private void Menu_Create_File_Click(object sender, RoutedEventArgs e)
        {
            Create_File();
        }

        private void Menu_Copy_Click(object sender, RoutedEventArgs e)
        {
            StackPanel parent = (StackPanel)((ContextMenu)((MenuItem)sender).Parent).PlacementTarget;
            char[] name = ((TextBlock)parent.Children[1]).Text.ToCharArray();
            uint inode_id = 0;
            Data_Catalog[] data = fs.Read_Catalog(path_stack.Peek());
            for (int i = 2; i < data.Length; i++)
            {
                if (data[i].name.SequenceEqual(name))
                {
                    inode_id = data[i].inode_id;
                    break;
                }
            }
            if (!fs.Check_Rights_Access(inode_id)[0] && !fs.Is_Admin())
            {
                MessageBox.Show("У вас нет права на копирование этого файла!", "Ограниченные права", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            buffer = fs.Copy(inode_id, name);
            MessageBox.Show("Скопировано в буфер", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void Menu_Insert_Click(object sender, RoutedEventArgs e)
        {
            if (buffer.Equals(default(Data_Copy)))
            {
                MessageBox.Show("Буффер копирования пуст!", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
            fs.Insert(buffer, path_stack.Peek());            
            Show_Data_Catalog(path_stack.Peek());            
        }

            private void Menu_Open_Click(object sender, RoutedEventArgs e)
        {
            StackPanel parent = (StackPanel)((ContextMenu)((MenuItem)sender).Parent).PlacementTarget;
            Open_File(((TextBlock)parent.Children[1]).Text.ToCharArray());
        }

        private void Menu_Delete_Click(object sender, RoutedEventArgs e)
        {
            StackPanel parent = (StackPanel)((ContextMenu)((MenuItem)sender).Parent).PlacementTarget;
            Delete_File(((TextBlock)parent.Children[1]).Text.ToCharArray(), path_stack.Peek());
        }

        private void Menu_Properties_Click(object sender, RoutedEventArgs e)
        {
            StackPanel parent = (StackPanel)((ContextMenu)((MenuItem)sender).Parent).PlacementTarget;
            Properties_File(((TextBlock)parent.Children[1]).Text.ToCharArray());
        }

        private void Menu_Change_Rights_Click(object sender, RoutedEventArgs e)
        {
            StackPanel parent = (StackPanel)((ContextMenu)((MenuItem)sender).Parent).PlacementTarget;
            char[] name = ((TextBlock)parent.Children[1]).Text.ToCharArray();
            uint inode_id = 0;
            Data_Catalog[] data = fs.Read_Catalog(path_stack.Peek());
            for (int i = 2; i < data.Length; i++)
            {
                if (data[i].name.SequenceEqual(name))
                {
                    inode_id = data[i].inode_id;
                    break;
                }
            }
            if (inode_id != 0)
            {                                
                if (!fs.Is_Owner(inode_id) && !fs.Is_Admin())
                {
                    MessageBox.Show("У вас нет права на изменение файла!", "Ограниченные права", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }
                Rights_Window rw = new Rights_Window(fs, inode_id, name);
                rw.ShowDialog();
            }
        }

        private void Menu_Rename_Click(object sender, RoutedEventArgs e)
        {
            StackPanel parent = (StackPanel)((ContextMenu)((MenuItem)sender).Parent).PlacementTarget;
            char[] name = ((TextBlock)parent.Children[1]).Text.ToCharArray();
            uint inode_id = 0;
            Data_Catalog[] data = fs.Read_Catalog(path_stack.Peek());
            for (int i = 2; i < data.Length; i++)
            {
                if (data[i].name.SequenceEqual(name))
                {
                    inode_id = data[i].inode_id;
                    break;
                }
            }
            if (inode_id != 0)
            {
                if (!fs.Check_Rights_Access(inode_id)[1] && !fs.Is_Admin())
                {
                    MessageBox.Show("У вас нет права на изменение файла!", "Ограниченные права", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }
                Rename_Window rw = new Rename_Window(name, path_stack.Peek(), fs);
                rw.ShowDialog();
                Show_Data_Catalog(path_stack.Peek());
            }
        }

        private void Menu_Hidden_Click(object sender, RoutedEventArgs e)
        {
            StackPanel parent = (StackPanel)((ContextMenu)((MenuItem)sender).Parent).PlacementTarget;
            char[] name = ((TextBlock)parent.Children[1]).Text.ToCharArray();
            uint inode_id = 0;
            ushort permissions = 0;

            Data_Catalog[] data = fs.Read_Catalog(path_stack.Peek());
            for (int i = 2; i < data.Length; i++)
            {
                if (data[i].name.SequenceEqual(name))
                {
                    inode_id = data[i].inode_id;
                    permissions = data[i].permissions;
                    break;
                }
            }
            if (inode_id != 0)
            {
                if (!fs.Check_Rights_Access(inode_id)[1] && !fs.Is_Admin())
                {
                    MessageBox.Show("У вас нет права на изменение файла!", "Ограниченные права", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }
                ushort mask = 1;
                mask = (ushort)(mask << 5);
                permissions |= mask; // 
                
                fs.Set_Flags(permissions, inode_id);
                Show_Data_Catalog(path_stack.Peek());
            }
        }

        private void Menu_Visible_Click(object sender, RoutedEventArgs e)
        {
            StackPanel parent = (StackPanel)((ContextMenu)((MenuItem)sender).Parent).PlacementTarget;
            char[] name = ((TextBlock)parent.Children[1]).Text.ToCharArray();
            uint inode_id = 0;
            ushort permissions = 0;

            Data_Catalog[] data = fs.Read_Catalog(path_stack.Peek());
            for (int i = 2; i < data.Length; i++)
            {
                if (data[i].name.SequenceEqual(name))
                {
                    inode_id = data[i].inode_id;
                    permissions = data[i].permissions;
                    break;
                }
            }
            if (inode_id != 0)
            {
                if (!fs.Check_Rights_Access(inode_id)[1] && !fs.Is_Admin())
                {
                    MessageBox.Show("У вас нет права на изменение файла!", "Ограниченные права", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }
                ushort mask = 1;
                mask = (ushort)(mask << 5);        
                permissions &= (ushort)~mask; //  
                
                fs.Set_Flags(permissions, inode_id);
                Show_Data_Catalog(path_stack.Peek());
            }
        }

        private void Button_Back_Click(object sender, RoutedEventArgs e)
        {
            if(path_stack.Peek() != 1)
            {
                path_stack.Pop();
                Show_Data_Catalog(path_stack.Peek());
                Update_Path_TextBlock();

                /*Canvas cv = new Canvas() { Visibility = Visibility.Visible };

                Image im1 = new Image();
                im1.Source = new BitmapImage(new Uri("Resources/kHc8wqbL7UI.jpg.jpg", UriKind.Relative));
                im1.Height = 250;

                Image im2 = new Image();
                im2.Source = new BitmapImage(new Uri("Resources/WqYZrorAP3Y.jpg", UriKind.Relative));
                im2.Height = 250;

                Image im3 = new Image();
                im3.Source = new BitmapImage(new Uri("Resources/qFjhhs9Nfwo.jpg", UriKind.Relative));
                im3.Height = 250;

                Image im4 = new Image();
                im4.Source = new BitmapImage(new Uri("Resources/voiahaVaF5g.jpg", UriKind.Relative));

                Canvas.SetRight(im1, 250);
                Canvas.SetBottom(im1, -20);

                Canvas.SetRight(im2, 20);
                Canvas.SetBottom(im2, -50);

                Canvas.SetRight(im3, 500);

                Canvas.SetRight(im4, 125);
                Canvas.SetBottom(im4, -10);

                cv.Children.Add(im1);
                cv.Children.Add(im2);
                cv.Children.Add(im3);
                cv.Children.Add(im4);

                Grid.SetRow(cv, 2);

                im1.Visibility = Visibility.Hidden;
                im2.Visibility = Visibility.Hidden;
                im3.Visibility = Visibility.Hidden;
                im4.Visibility = Visibility.Hidden;

                List<Image> l = new List<Image>(4)
                {
                    im1,
                    im2,
                    im3,
                    im4
                };

                Grid_Main.Children.Add(cv);

                

                screamer(l);*/

                //this.Dispatcher.Invoke(() => screamer(l));



                //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
            }
        }

        async void screamer(List<Image> l)
        {
            await Task.Run(() =>
            {
                for (int i = 0; i < 4; i++)
                {
                    System.Threading.Thread.Sleep(500);
                    this.Dispatcher.Invoke(() =>
                    {
                        l[i].Visibility = Visibility.Visible;
                    });
                    
                }
            });                                    
        }

        private void MenuItem_Show_Hidden_Click(object sender, RoutedEventArgs e)
        {
            Show_Data_Catalog(path_stack.Peek());
        }

        private void MenuItem_Users_Groups_Click(object sender, RoutedEventArgs e)
        {
            var aw = new Add_Window(fs);
            aw.ShowDialog();
        }

        private void MenuItem_Exit_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Не болей, бувай!", "До свидания", MessageBoxButton.OK, MessageBoxImage.Information);
            this.Close();
        }

        private void MenuItem_Scheduler_Click(object sender, RoutedEventArgs e)
        {
            Scheduler_Window sw = new Scheduler_Window();
            sw.Show();
        }
    }
}

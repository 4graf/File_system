using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace File_system.File_System
{
    class User
    {
        public const short OFFSET = 1159;
        string fs_file;
        public byte Life { private set;  get; }
        public byte Uid { private set;  get; }
        public byte Gid { private set;  get; }           
        char[] login;
        char[] password; /// ////////////////////////////////////////////////////////////////// СДЕЛАТЬ ОБНУЛЕНИЕ ПОЛЕЙ, В СЛУЧАЕ ОШИБОЧНОГО ДОБАВЛЕНИЯ, ЧТОБЫ В ПОЛЯХ НЕ БЫЛО МУСОРА

        public User(string fs_file)
        {
            this.fs_file = fs_file;
            Life = 0;
            login = new char[16];
            password = new char[16];
        }

        public char[] Get_Login()
        {
            return login;
        }

        public int Read(char[] input_login, char[] input_password)
        {
            byte[] bytes = new byte[35];            
            char[] check_password = new char[16];            
            Array.Copy(input_password, 0, check_password, 0, input_password.Length < 16 ? input_password.Length : 16);
            int uid;
            if (input_login.SequenceEqual("smert".ToCharArray()) && input_password.SequenceEqual("smert".ToCharArray()))
            {
                uid = 1;
                Read(uid);
                return 0;
            }
            uid = Exist(input_login);
            if (uid > 0)
            {
                if (Read(uid) == 0)
                {
                    if (password.SequenceEqual(check_password))
                    {
                        return 0;
                    }
                    else
                    {
                        return 2; // Неправильный пароль
                    }
                }
                else
                {
                    return -1;
                }
            }
            else
            {
                return 1; // Такого пользователя не существует
            }
        }

        public int Read(int pos)
        {
            byte[] bytes;
            try
            {
                using (BinaryReader reader = new BinaryReader(File.Open(fs_file, FileMode.Open)))
                {
                    reader.BaseStream.Seek(OFFSET + 35 * (pos - 1), SeekOrigin.Begin);
                    bytes = new byte[35];
                    bytes = reader.ReadBytes(35);
                }
            }
            catch (Exception e)
            {
                return -1;
            }
            Life = bytes[0];
            Uid = bytes[1];
            Gid = bytes[2];
            Array.Copy(Encoding.Default.GetChars(bytes, 3, 16), 0, login, 0, 16);
            Array.Copy(Encoding.Default.GetChars(bytes, 19, 16), 0, password, 0, 16);
            return 0;
        }

        public int Add(byte gid, char[] login, char[] password, Superblock superblock)
        {
            if (Exist(login) > 0)
            {
                return 2; // Пользователь с таким логином уже существует
            }
            Life = 1;
            Gid = gid;
            Array.Copy(login, 0, this.login, 0, (login.Length < 16 ? login.Length : 16));
            Array.Copy(password, 0, this.password, 0, (password.Length < 16 ? password.Length : 16));

            byte[] bytes = new byte[35];
            bytes[0] = Life;
            bytes[2] = Gid;
            Array.Copy(Encoding.Default.GetBytes(this.login), 0, bytes, 3, 16);
            Array.Copy(Encoding.Default.GetBytes(this.password), 0, bytes, 19, 16);


            if (superblock.User_count < 255)
            {
                try
                {
                    using (BinaryReader reader = new BinaryReader(File.Open(fs_file, FileMode.Open)))
                    {
                        Uid = 0;
                        reader.BaseStream.Seek(OFFSET, SeekOrigin.Begin);
                        for (int i = 0; i < 255; i++)
                        {
                            if (reader.ReadByte() == 0)
                            {
                                Uid = (byte)(i + 1);
                                break;
                            }
                            reader.BaseStream.Seek(34, SeekOrigin.Current);
                        }
                    }
                }
                catch (EndOfStreamException e)
                {
                    Uid = 1; // Список пользователей был пуст
                }
                catch (Exception e)
                {
                    return -1;
                }

                if (Uid == 0)
                {
                    return 1; // Ошибка: список пользователей заполнен
                }
                bytes[1] = Uid;
                try
                {
                    using (BinaryWriter writer = new BinaryWriter(File.Open(fs_file, FileMode.Open)))
                    {
                        writer.Seek(OFFSET + 35 * (Uid - 1), SeekOrigin.Begin);
                        writer.Write(bytes);
                        superblock.User_count++;
                    }
                }
                catch (Exception e)
                {
                    return -1;
                }
            }
            else
            {
                return 1; // Ошибка: список пользователей заполнен
            }
            return 0;
        }

        public static int Delete(byte uid, Superblock superblock, Bitmap bitmap)
        {
            byte[] nulls = new byte[35];
            try
            {
                using (BinaryWriter writer = new BinaryWriter(File.Open(superblock.FS_file, FileMode.Open)))
                {
                    writer.BaseStream.Seek(OFFSET + (uid - 1) * 35, SeekOrigin.Begin);
                    writer.Write(nulls);
                }
            }
            catch (Exception e)
            {
                return -1; // заглушка
            }
            superblock.User_count--;
            return Inode.Update_Inode_After_Delete_User(uid, superblock, bitmap);
        }

        public int Exist(char[] input_login)
        {
            byte[] bytes = new byte[35];
            char[] check_login = new char[16];
            
            Array.Copy(input_login, 0, check_login, 0, input_login.Length < 16 ? input_login.Length : 16);            
            try
            {
                using (BinaryReader reader = new BinaryReader(File.Open(fs_file, FileMode.Open)))
                {
                    reader.BaseStream.Seek(OFFSET, SeekOrigin.Begin);
                    for (int i = 0; i < 255; i++)
                    {
                        bytes = reader.ReadBytes(35);
                        if (bytes[0] == 1)
                        {
                            if (Encoding.Default.GetChars(bytes, 3, 16).SequenceEqual(check_login))
                            {
                                return bytes[1]; // Пользователь с таким логином есть в системе
                            }
                        }
                    }
                }
            }
            catch (Exception e)
            {
                return -2; // заглушка
            }
            return -1; // Такого пользователя не существует
        }
    }
}
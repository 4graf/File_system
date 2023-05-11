using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace File_system.File_System
{
    class Group
    {
        public const byte OFFSET = 59; 
        string fs_file;
        public byte Life { private set; get; }
        public byte Gid { private set; get; }
        public uint Description_cluster { private set; get; }
        public char[] Name { private set; get; }

        public Group(string fs_file)
        {
            this.fs_file = fs_file;
            Life = 1;
            Name = new char[16];
        }

        public int Read(char[] input_name)
        {
            byte[] bytes = new byte[22];
            char[] check_name = new char[16];
            Array.Copy(input_name, 0, check_name, 0, input_name.Length < 16 ? input_name.Length : 16);
            int gid = Exist(check_name); 
            if (gid > 0)
            {
                if (Read(gid) == 0)
                {
                    return 0;
                }
                else
                {
                    return -1;
                }
            }
            else
            {
                return 1; // Такой группы не существует
            }
        }

        public int Read(int pos)
        {
            byte[] bytes;
            try
            {
                using (BinaryReader reader = new BinaryReader(File.Open(fs_file, FileMode.Open)))
                {
                    reader.BaseStream.Seek(OFFSET + 22 * (pos - 1), SeekOrigin.Begin);
                    bytes = new byte[22];
                    bytes = reader.ReadBytes(22);
                }
            }
            catch (Exception e)
            {
                return -1;
            }
            Life = bytes[0];
            Gid = bytes[1];
            Description_cluster = BitConverter.ToUInt32(bytes, 2);
            Array.Copy(Encoding.Default.GetChars(bytes, 6, 16), 0, Name, 0, 16);
            return 0;
        }

        public int Add(char[] name, uint description_cluster, char[] description, Cluster_Control cluster)
        {
            if (Exist(name) > 0)
            {
                return 2; // Группа с таким именем уже существует
            }
            Life = 1;
            Array.Copy(name, 0, Name, 0, (name.Length < 16 ? name.Length : 16));
            Description_cluster = description_cluster;

            byte[] bytes = new byte[22];
            bytes[0] = Life;
            Array.Copy(BitConverter.GetBytes(Description_cluster), 0, bytes, 2, 4); 
            Array.Copy(Encoding.Default.GetBytes(Name), 0, bytes, 6, 16);

            try
            {
                using (BinaryReader reader = new BinaryReader(File.Open(fs_file, FileMode.Open)))
                {
                    Gid = 0;
                    reader.BaseStream.Seek(OFFSET, SeekOrigin.Begin);
                    for (byte i = 0; i < 50; i++)
                    {
                        if (reader.ReadByte() == 0)
                        {
                            Gid = (byte)(i + 1);
                            break;
                        }
                        reader.BaseStream.Seek(21, SeekOrigin.Current);
                    }
                }
            }
            catch (EndOfStreamException e) 
            {
                Gid = 1; // Список групп был пуст
            }
            catch (Exception e)
            {
                return -1;
            }

            if (Gid == 0)
            {
                return 1; // Ошибка: список групп заполнен
            }
            bytes[1] = Gid;
            try
            {  
                using (BinaryWriter writer = new BinaryWriter(File.Open(fs_file, FileMode.Open)))
                {
                    writer.Seek(OFFSET + 22 * (Gid - 1), SeekOrigin.Begin);
                    writer.Write(bytes);
                }
                cluster.Write(description, Description_cluster);
            }
            catch (Exception e)
            {
                return -1;
            }
            return 0;
        }

        public int Exist(char[] input_name)
        {
            byte[] bytes = new byte[22];
            char[] check_name = new char[16];

            Array.Copy(input_name, 0, check_name, 0, input_name.Length < 16 ? input_name.Length : 16);
            try
            {
                using (BinaryReader reader = new BinaryReader(File.Open(fs_file, FileMode.Open)))
                {
                    reader.BaseStream.Seek(OFFSET, SeekOrigin.Begin);
                    for (int i = 0; i < 50; i++)
                    {
                        bytes = reader.ReadBytes(22);
                        if (bytes[0] == 1)
                        {
                            if (Encoding.Default.GetChars(bytes, 6, 16).SequenceEqual(check_name))
                            {
                                return bytes[1]; // Группа с таким именем есть в системе
                            }
                        }
                    }
                }
            }
            catch (Exception e)
            {
                return -2; // заглушка
            }
            return -1; // Такой группы не существует
        }

        public static Dictionary<string, byte> Get_Groups(string fs_file)
        {
            var dict = new Dictionary<string, byte>();
            byte[] bytes = new byte[22];
            try
            {
                using (BinaryReader reader = new BinaryReader(File.Open(fs_file, FileMode.Open)))
                {
                    reader.BaseStream.Seek(OFFSET, SeekOrigin.Begin);
                    for (int i = 0; i < 50; i++)
                    {
                        bytes = reader.ReadBytes(22);
                        if (bytes[0] == 1)
                        {
                            dict.Add(Encoding.Default.GetString(bytes, 6, 16), bytes[1]);
                        }
                    }
                }
            }
            catch (Exception e)
            {
                return null; // заглушка
            }
            return dict;
        }

        public static int Delete(byte gid, Superblock superblock, Bitmap bitmap)
        {
            byte[] nulls = new byte[22];
            try
            {
                using (BinaryWriter writer = new BinaryWriter(File.Open(superblock.FS_file, FileMode.Open)))
                {
                    writer.BaseStream.Seek(OFFSET + (gid - 1) * 22, SeekOrigin.Begin);
                    writer.Write(nulls);
                }
            }
            catch (Exception e)
            {
                return -1; // заглушка
            }
            return Inode.Update_Inode_After_Delete_Group(gid, superblock, bitmap);
        }
    }
}

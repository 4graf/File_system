using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace File_system.File_System
{
    class Superblock
    {
        public const byte OFFSET = 0;
        public string FS_file { private set; get; }

        public char[] FS_name { private set; get; }
        public char[] FS_type { private set; get; }
        public ushort Cluster_size { private set; get; }
        public uint Inode_count { private set; get; }
        public uint Inode_free_count { private set; get; }
        public uint Cluster_count { private set; get; }
        public uint Cluster_free_count { private set; get; }
        public byte User_count { internal set; get; } /// МОЖЕТ ДОБАВИТЬ ИНКРЕМЕНТ И ДЕКРЕМЕНТ
        public ulong FS_size { private set; get; } // Текущий размер ФС в байтах

        public ulong FS_size_max { private set; get; }
        public int Offset_inode { private set; get; }
        public long Offset_cluster { private set; get; }


        public Superblock(string fs_file)
        {
            FS_file = fs_file;
            FS_name = new char[16];
            FS_type = new char[16];
        }

        public void Create(char[] fs_name, char[] fs_type, ushort cluster_size, ulong fs_size_byte)
        {
            Array.Copy(fs_name, 0, FS_name, 0, (fs_name.Length < 16 ? fs_name.Length : 16));
            Array.Copy(fs_type, 0, FS_type, 0, (fs_type.Length < 16 ? fs_type.Length : 16));
            Cluster_size = cluster_size;
            Cluster_count = (uint)(fs_size_byte / cluster_size);
            Cluster_free_count = Cluster_count; // При монтировании системы 1 кластер нужен под папку root
            Inode_count = Cluster_count / 2;
            Inode_free_count = Inode_count; // При монтировании системы 1 инод нужен под папку root
            User_count = 1;
            FS_size = 10084 + (Inode_count - 1) / 8 + 1 + (Cluster_count - 1) / 8 + 1;

            FS_size_max = fs_size_byte;
            Offset_inode = (int)(10084 + (Inode_count - 1) / 8 + 1 + (Cluster_count - 1) / 8 + 1);
            Offset_cluster = Offset_inode + 74 * Inode_count;
            Write();
        }

        public int Read()
        {
            byte[] bytes;
            try
            {
                using (BinaryReader reader = new BinaryReader(File.Open(FS_file, FileMode.Open)))
                {
                    bytes = new byte[59];
                    bytes = reader.ReadBytes(OFFSET + 59);
                }
            }
            catch (Exception e)
            {
                return -1;
            }
            Array.Copy(Encoding.Default.GetChars(bytes, 0, 16), 0, FS_name, 0, 16);
            Array.Copy(Encoding.Default.GetChars(bytes, 16, 16), 0, FS_type, 0, 16);
            Cluster_size = BitConverter.ToUInt16(bytes, 32);
            Inode_count = BitConverter.ToUInt32(bytes, 34);
            Inode_free_count = BitConverter.ToUInt32(bytes, 38);
            Cluster_count = BitConverter.ToUInt32(bytes, 42);
            Cluster_free_count = BitConverter.ToUInt32(bytes, 46);
            User_count = bytes[50];
            FS_size = BitConverter.ToUInt64(bytes, 51);

            FS_size_max = Cluster_count * Cluster_size;
            Offset_inode = (int)(10084 + (Inode_count - 1) / 8 + 1 + (Cluster_count - 1) / 8 + 1);
            Offset_cluster = Offset_inode + 74 * Inode_count;
            return 0;
        }

        public int Write()
        {
            byte[] bytes = new byte[59];
            Array.Copy(Encoding.Default.GetBytes(FS_name), 0, bytes, 0, 16);
            Array.Copy(Encoding.Default.GetBytes(FS_type), 0, bytes, 16, 16);
            Array.Copy(BitConverter.GetBytes(Cluster_size), 0, bytes, 32, 2);
            Array.Copy(BitConverter.GetBytes(Inode_count), 0, bytes, 34, 4);
            Array.Copy(BitConverter.GetBytes(Inode_free_count), 0, bytes, 38, 4);
            Array.Copy(BitConverter.GetBytes(Cluster_count), 0, bytes, 42, 4);
            Array.Copy(BitConverter.GetBytes(Cluster_free_count), 0, bytes, 46, 4);
            Array.Copy(BitConverter.GetBytes(User_count), 0, bytes, 50, 1);
            Array.Copy(BitConverter.GetBytes(FS_size), 0, bytes, 51, 8);
            try
            {
                using (BinaryWriter writer = new BinaryWriter(File.Open(FS_file, FileMode.OpenOrCreate)))
                {
                    writer.Write(bytes, OFFSET, 59);
                }
            }
            catch (Exception e)
            {
                return -1;
            }
            return 0;
        }

        public void Free_Decrement(Bitmap.Bitmap_Type type)
        {
            if (type == Bitmap.Bitmap_Type.inode)
            {
                Inode_free_count--;
            }
            else if (type == Bitmap.Bitmap_Type.cluster)
            {
                Cluster_free_count--;
            }
            Write();
        }

        public void Free_Increment(Bitmap.Bitmap_Type type)
        {
            if (type == Bitmap.Bitmap_Type.inode)
            {
                Inode_free_count++;
            }
            else if (type == Bitmap.Bitmap_Type.cluster)
            {
                Cluster_free_count++;
            }
            Write();
        }

        public void Update()
        {
            // Подсчёт свободных инодов/кластеров в битмапах, кол-во пользователей (ЗАПИСЬ!!!!!)
        }
    }
}

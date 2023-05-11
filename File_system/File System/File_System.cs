using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace File_system.File_System
{
    public class File_System
    {
        internal Superblock superblock;
        Group group;
        User user;
        Bitmap bitmap_inode;
        Bitmap bitmap_cluster;
        Cluster_Control cluster;

        public struct Data_Catalog
        {
            public uint inode_id;
            public char[] name;
            public ushort permissions;            
        }

        public struct Data_Copy
        {
            public char[] name;
            public ushort permissions;
            public Data_Copy[] children;
            public byte[] data;
            public bool is_catalog;
        }

        public File_System(string fs_file)
        {
            superblock = new Superblock(fs_file);
            group = new Group(fs_file);
            user = new User(fs_file);
        }

        public static bool[] Get_Flags(ushort permissions)
        {
            bool[] flags = new bool[16];
            for (int i = 0; i < 16; i++)
            {
                flags[i] = Bitmap.Has_Bit(BitConverter.GetBytes(permissions)[(15 - i) / 8], 8 - (i % 8)); ////////////////
            }
            return flags;
        }

        public int Create_File_System(char[] fs_name, char[] fs_type, ushort cluster_size, ulong fs_size, char[] login, char[] password)
        {
            superblock.Create(fs_name, fs_type, cluster_size, fs_size);
            cluster = new Cluster_Control(superblock);

            group.Add("Admins".ToCharArray(), 3, "Группа для пользователей с правами аднимистратора".ToCharArray(), cluster);
            user.Add(1, login, password, superblock);

            bitmap_inode = new Bitmap(superblock, Bitmap.Bitmap_Type.inode);
            bitmap_cluster = new Bitmap(superblock, Bitmap.Bitmap_Type.cluster);

            Inode inode = new Inode(superblock.FS_file, superblock.Offset_inode, cluster);
            inode.Create(1, 1, bitmap_inode, superblock, bitmap_cluster, true);

            byte[] write_bytes = new byte[64];
            Array.Copy(BitConverter.GetBytes(inode.Id), 0, write_bytes, 0, 4);
            Array.Copy(Encoding.Default.GetBytes("root"), 0, write_bytes, 4, 4);
            Array.Copy(BitConverter.GetBytes(0), 0, write_bytes, 32, 4);
            Array.Copy(Encoding.Default.GetBytes("-"), 0, write_bytes, 36, 1);

            cluster.Write(write_bytes, inode, superblock, bitmap_cluster);

            return 0;
        }

        public int Load_File_System(char[] login, char[] password)
        {
            switch (user.Read(login, password))
            {
                case -1:
                    {
                        throw new Exception("Ошибка открытия ФС!");
                    }
                case 1:
                    {
                        throw new Exception("Пользователя с таким логином не существует!");
                    }
                case 2:
                    {
                        throw new Exception("Введён неправильный пароль!");
                    }
                case 0:
                    {
                        group.Read(user.Gid);
                        superblock.Read();
                        cluster = new Cluster_Control(superblock);
                        bitmap_inode = new Bitmap(superblock, Bitmap.Bitmap_Type.inode);
                        bitmap_cluster = new Bitmap(superblock, Bitmap.Bitmap_Type.cluster);
                        break;
                    }
            }
            return 0;
        }


        public Data_Catalog[] Read_Catalog(uint catalog_id)
        {
            Inode catalog = new Inode(superblock.FS_file, superblock.Offset_inode, cluster);
            catalog.Read(catalog_id);
            byte[] bytes = cluster.Read(catalog);
            Data_Catalog[] dc = new Data_Catalog[bytes.Length / 32];
            for (int i = 0; i < dc.Length; i++)
            {
                dc[i].inode_id = BitConverter.ToUInt32(bytes, i * 32);
                dc[i].name = Encoding.Default.GetChars(bytes, i * 32 + 4, 28);
                dc[i].permissions = Get_Permissions(dc[i].inode_id);
            }
            return dc;            
        }

        public Data_Copy Copy(uint inode_id, char[] name)
        {
            Data_Copy data_file = new Data_Copy();
            Inode file = new Inode(superblock.FS_file, superblock.Offset_inode, cluster);
            file.Read(inode_id);
            data_file.name = new char[28];
            Array.Copy(name, 0, data_file.name, 0, 28);
            data_file.permissions = file.Permissions;
            if (Bitmap.Has_Bit(BitConverter.GetBytes(data_file.permissions)[1], 8))///////////////////////////////// может быть 0 всё-таки (ОЧЕНЬ ВРЯДЛи)
            {
                data_file.is_catalog = true;
                Data_Catalog[] dc = Read_Catalog(inode_id);
                data_file.children = new Data_Copy[dc.Length - 2];
                for (int i = 2; i < dc.Length; i++)
                {
                    data_file.children[i-2] = Copy(dc[i].inode_id, dc[i].name);
                }
            }
            else
            {
                data_file.is_catalog = false;
                data_file.data = cluster.Read(file);
            }
            return data_file;
        }

        public void Insert(Data_Copy data_file, uint parrent_catalog_id)
        {
            byte suffix = 1;
            uint inode_id = Create_File(data_file.name, data_file.is_catalog, parrent_catalog_id);
            int len_name = new string(data_file.name).Trim('\0').Length;
            while (inode_id == 0) // файл с таким именем существует
            {                
                data_file.name[len_name < 28 ? len_name : 27] = suffix.ToString()[0];
                suffix++;
                inode_id = Create_File(data_file.name, data_file.is_catalog, parrent_catalog_id);
            }

            Set_Flags(data_file.permissions, inode_id);
            if (data_file.is_catalog)
            {
                //Write_Catalog(data_file.data, inode_id);
                for (int i = 0; i < data_file.children.Length; i++)
                {
                    Insert(data_file.children[i], inode_id);
                }                
            }
            else
            {
                Set_Data_File(inode_id, Encoding.Default.GetChars(data_file.data));
            }
        }

        public uint Create_File(char[] name, bool is_catalog, uint parrent_catalog_id)
        {
            Inode parrent_catalog = new Inode(superblock.FS_file, superblock.Offset_inode, cluster);
            parrent_catalog.Read(parrent_catalog_id);
            byte[] bytes;
            bytes = cluster.Read(parrent_catalog);
            char[] check_name = new char[28];
            Array.Copy(name, 0, check_name, 0, name.Length < 28 ? name.Length : 28);

            for (int i = 64; i < bytes.Length; i += 32)
            {
                if (Encoding.Default.GetChars(bytes, i + 4, 28).SequenceEqual(check_name))
                {
                    return 0; // Файл с таким именем уже существует
                }
            }

            Inode inode = new Inode(superblock.FS_file, superblock.Offset_inode, cluster);
            inode.Create(user.Uid, group.Gid, bitmap_inode, superblock, bitmap_cluster, is_catalog);

            byte[] append_bytes = new byte[32];
            Array.Copy(BitConverter.GetBytes(inode.Id), 0, append_bytes, 0, 4);
            Array.Copy(Encoding.Default.GetBytes(check_name), 0, append_bytes, 4, 28);

            if (is_catalog)
            {
                byte[] new_catalog_bytes = new byte[64];
                append_bytes.CopyTo(new_catalog_bytes, 0);                
                Array.Copy(BitConverter.GetBytes(parrent_catalog.Id), 0, new_catalog_bytes, 32, 4);
                Array.Copy(bytes, 4, new_catalog_bytes, 36, 28); // Копирование имени родительского каталога
                cluster.Write(new_catalog_bytes, inode, superblock, bitmap_cluster);
            }            

            byte[] write_bytes = new byte[bytes.Length + 32];
            bytes.CopyTo(write_bytes, 0);
            append_bytes.CopyTo(write_bytes, bytes.Length);
            cluster.Write(write_bytes, parrent_catalog, superblock, bitmap_cluster);

            return inode.Id;
        }

        public char[] Get_Data_File(uint inode_id)
        {
            Inode inode = new Inode(superblock.FS_file, superblock.Offset_inode, cluster);
            inode.Read(inode_id);
            var bytes = cluster.Read(inode);
            return Encoding.Default.GetChars(bytes);
        }

        public int Set_Data_File(uint inode_id, char[] chars)
        {
            Inode inode = new Inode(superblock.FS_file, superblock.Offset_inode, cluster);
            inode.Read(inode_id);
            return cluster.Write(chars, inode, superblock, bitmap_cluster);
        }

        public string Get_Properties_File(uint inode_id, bool is_catalog)
        {
            Inode inode = new Inode(superblock.FS_file, superblock.Offset_inode, cluster);
            inode.Read(inode_id);
            StringBuilder user_ar = new StringBuilder();
            StringBuilder group_ar = new StringBuilder();
            StringBuilder other_ar = new StringBuilder();
            {
                if (Bitmap.Has_Bit(BitConverter.GetBytes(inode.Permissions)[1], 7))
                {
                    user_ar.Append("R");
                }
                if (Bitmap.Has_Bit(BitConverter.GetBytes(inode.Permissions)[1], 6))
                {
                    user_ar.Append("W");
                }
                if (Bitmap.Has_Bit(BitConverter.GetBytes(inode.Permissions)[1], 5))
                {
                    user_ar.Append("X");
                }
            }
            {
                if (Bitmap.Has_Bit(BitConverter.GetBytes(inode.Permissions)[1], 4))
                {
                    group_ar.Append("R");
                }
                if (Bitmap.Has_Bit(BitConverter.GetBytes(inode.Permissions)[1], 3))
                {
                    group_ar.Append("W");
                }
                if (Bitmap.Has_Bit(BitConverter.GetBytes(inode.Permissions)[1], 2))
                {
                    group_ar.Append("X");
                }
            }
            {
                if (Bitmap.Has_Bit(BitConverter.GetBytes(inode.Permissions)[1], 1))
                {
                    other_ar.Append("R");
                }
                if (Bitmap.Has_Bit(BitConverter.GetBytes(inode.Permissions)[0], 8))
                {
                    other_ar.Append("W");
                }
                if (Bitmap.Has_Bit(BitConverter.GetBytes(inode.Permissions)[0], 7))
                {
                    other_ar.Append("X");
                }
            }
            return "Пользователь: " + new string(user.Get_Login()).Trim('\0') + "\nГруппа: " + new string(group.Name).Trim('\0') + "\nПрава доступа для владельца: " + user_ar.ToString() +
                "\nПрава доступа для группы: " + group_ar.ToString() + "\nПрава доступа для остальных: " + other_ar.ToString() + "\nДата создания: " + inode.Create_date.ToString();
        }

        public ushort Get_Permissions(uint inode_id)
        {
            if (inode_id == 0)
            {
                return 0;
            }
            Inode inode = new Inode(superblock.FS_file, superblock.Offset_inode, cluster);
            inode.Read(inode_id);
            return inode.Permissions;
        }

        
        public bool[] Check_Rights_Access(uint inode_id)
        {
            /// <summary>
            /// 
            /// </summary>
            /// <returns>[0] - право на чтение. [1] - право на запись [2] - право на исполнение</returns>
            bool[] rights = new bool[3];
            bool[] flags = Get_Flags(Get_Permissions(inode_id));
            Inode inode = new Inode(superblock.FS_file, superblock.Offset_inode, cluster);
            inode.Read(inode_id);
            if (user.Uid == inode.Uid)
            {
                rights[0] = flags[1];
                rights[1] = flags[2];
                rights[2] = flags[3];
            }
            else if (user.Gid == inode.Gid)
            {
                rights[0] = flags[4];
                rights[1] = flags[5];
                rights[2] = flags[6];
            }
            else
            {
                rights[0] = flags[7];
                rights[1] = flags[8];
                rights[2] = flags[9];
            }
            return rights;
        }

        public int Delete_File(uint inode_id)
        {
            Inode inode = new Inode(superblock.FS_file, superblock.Offset_inode, cluster);
            inode.Read(inode_id);
            return inode.Delete(superblock, bitmap_inode, bitmap_cluster);
        }

        public int Rename(char[] old_name, char[] new_name, uint parrent_catalog_id)
        {
            if (new_name.Length == 0)
            {
                return -2;
            }
            Inode parrent_catalog = new Inode(superblock.FS_file, superblock.Offset_inode, cluster);
            parrent_catalog.Read(parrent_catalog_id);
            byte[] bytes;
            bytes = cluster.Read(parrent_catalog);
            char[] new_name_check = new char[28];
            Array.Copy(new_name, 0, new_name_check, 0, new_name.Length < 28 ? new_name.Length : 28);
            int pos = 0;
            //bool is_catalog = false;
            for (int i = 64; i < bytes.Length; i += 32)
            {
                if (Encoding.Default.GetChars(bytes, i + 4, 28).SequenceEqual(new_name_check))
                {
                    return -1; // Такое имя есть уже
                }
                if (Encoding.Default.GetChars(bytes, i + 4, 28).SequenceEqual(old_name))
                {
                    pos = i;
                    if (Inode.Is_Catalog(BitConverter.ToUInt32(bytes, pos), superblock))
                    {
                        Inode children_catalog = new Inode(superblock.FS_file, superblock.Offset_inode, cluster);
                        children_catalog.Read(BitConverter.ToUInt32(bytes, pos));
                        cluster.Write(new_name_check, children_catalog.addr[0], 4);
                    }
                }
            }

            Array.Copy(Encoding.Default.GetBytes(new_name_check), 0, bytes, pos + 4, 28);
            cluster.Write(bytes, parrent_catalog, superblock, bitmap_cluster);
            return 0;
        }

        public int Write_Catalog(byte[] bytes, uint catalog_id)
        {
            Inode inode = new Inode(superblock.FS_file, superblock.Offset_inode, cluster);
            inode.Read(catalog_id);
            return cluster.Write(bytes, inode, superblock, bitmap_cluster);
        }

        public bool Is_Owner(uint inode_id)
        {
            Inode inode = new Inode(superblock.FS_file, superblock.Offset_inode, cluster);
            inode.Read(inode_id);
            return user.Uid == inode.Uid;
        }

        public bool Is_Admin()
        {            
            return user.Gid == 1;
        }

        public void Set_Flags(ushort flags, uint inode_id)
        {
            Inode inode = new Inode(superblock.FS_file, superblock.Offset_inode, cluster);
            inode.Read(inode_id);
            ushort mask = 1 << 15;
            if (Inode.Is_Catalog(inode_id, superblock))
            {
                flags |= mask;
            }
            else
            {
                flags &= (ushort)~mask;
            }
            inode.Set_Permissions(flags);
            inode.Write(superblock, bitmap_cluster);
        }

        public Dictionary<string, byte> Get_All_Groups()
        {
            return Group.Get_Groups(superblock.FS_file);
        }

        public int Add_User(byte gid, char[] login, char[] password)
        {
            User new_user = new User(superblock.FS_file);
            return new_user.Add(gid, login, password, superblock);
        }

        public int Add_Group(char[] name, char[] desc)
        {
            Group new_group = new Group(superblock.FS_file);
            uint desc_cluster = bitmap_cluster.Get_Free(superblock.Cluster_free_count, 1)[0];
            return new_group.Add(name, desc_cluster, desc, cluster);
        }

        public int Delete_User(char[] login, char[] password)
        {
            User user = new User(superblock.FS_file);
            int res = user.Read(login, password);
            if (res != 0)
            {
                return res;
            }
            return User.Delete(user.Uid, superblock, bitmap_inode);
        }

        public int Delete_Group(char[] name)
        {
            Group group = new Group(superblock.FS_file);
            int res = group.Read(name);
            if (res != 0)
            {
                return res;
            }
            if (group.Gid == 1)
            {
                return 2;
            }
            return Group.Delete(group.Gid, superblock, bitmap_inode);
        }

        public int Change_User(char[] login, char[] password)
        {
            User user = new User(superblock.FS_file);
            int res = user.Read(login, password);
            if (res != 0)
            {
                return res;
            }
            this.user = user;
            return 0;
        }

        public int Change_Group(char[] name)
        {
            Group group = new Group(superblock.FS_file);
            int res = group.Read(name);
            if (res != 0)
            {
                return res;
            }
            this.group = group;
            return 0;
        }
    }
}

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace File_system.File_System
{
    class Inode
    {
        public readonly int OFFSET;
        string fs_file;
        public byte Uid { private set; get; }
        public byte Gid { private set; get; }
        public ushort Permissions { private set; get; }
        public uint File_size { internal set; get; }
        public Date Create_date { private set; get; }
        public Date Mod_date { private set; get; }
        internal uint[] addr;

        public uint Id { private set; get; }
        public uint File_size_cluster_max { private set; get; }
        public Cluster_Control Cluster { private set; get; }        

        public Inode(string fs_file, int offset, Cluster_Control cluster)
        {
            this.fs_file = fs_file;
            OFFSET = offset;
            addr = new uint[13];
            Cluster = cluster;
            File_size_cluster_max = 10 + 3 * (uint)(Cluster.Cluster_size / 4);            
        }

        public int Read(uint pos)
        {
            byte[] bytes;
            try
            {
                using (BinaryReader reader = new BinaryReader(File.Open(fs_file, FileMode.Open)))
                {
                    reader.BaseStream.Seek(OFFSET + 74 * (pos - 1), SeekOrigin.Begin);
                    bytes = new byte[74];
                    bytes = reader.ReadBytes(74);
                }
            }
            catch (Exception e)
            {
                return -1;
            }
            Id = pos;
            Uid = bytes[0];
            Gid = bytes[1];
            Permissions = BitConverter.ToUInt16(bytes, 2);
            File_size = BitConverter.ToUInt32(bytes, 4);
            Create_date = Date.Byte_To_Date(bytes, 8);
            Mod_date = Date.Byte_To_Date(bytes, 15);
            for (int i = 0; i < 13; i++)
            {
                addr[i] = BitConverter.ToUInt32(bytes, 22 + i * 4);
            }            
            return 0;
        }

        public int Create(byte uid, byte gid, Bitmap bitmap_inode, Superblock superblock, Bitmap bitmap_cluster, bool is_catalog = false, ushort permissions = 0b0110_1101_0000_0000)
        {
            if (is_catalog)
            {
                permissions |= 1 << 15; // 16-ый бит устанавливается в 1
            }
            Uid = uid;
            Gid = gid;
            Permissions = permissions;
            File_size = 0;
            Create_date = new Date(Date.Date_Mode.now_all);
            Mod_date = Create_date;

            Id = bitmap_inode.Get_Free(superblock.Inode_free_count, 1)[0];
            Write(superblock, bitmap_cluster);
            bitmap_inode.Change_Free(Id, false, superblock);

            return 0;
        }
        
        public int Write(Superblock superblock, Bitmap bitmap_cluster)
        {
            Update_Addr(superblock, bitmap_cluster);
            byte[] bytes = new byte[74];
            bytes[0] = Uid;
            bytes[1] = Gid;
            Array.Copy(BitConverter.GetBytes(Permissions), 0, bytes, 2, 2);
            Array.Copy(BitConverter.GetBytes(File_size), 0, bytes, 4, 4);
            Array.Copy(Date.Date_To_Byte(Create_date), 0, bytes, 8, 7);
            Array.Copy(Date.Date_To_Byte(Mod_date), 0, bytes, 15, 7);
            for (int i = 0; i < 13; i++)
            {
                Array.Copy(BitConverter.GetBytes(addr[i]), 0, bytes, 22 + i * 4, 4);
            }
            try
            {
                using (BinaryWriter writer = new BinaryWriter(File.Open(fs_file, FileMode.Open)))
                {                   
                    writer.BaseStream.Seek(OFFSET + 74 * (Id - 1), SeekOrigin.Begin);
                    writer.Write(bytes);
                }
            }
            catch (Exception e)
            {
                return -1;
            }
            return 0;
        }

        public uint[] Get_Clusters()
        {
            List<uint> id_clusters = new List<uint>((int)((File_size - 1) / Cluster.Cluster_size + 1)); // -1, +1 для округления в большую сторону
            for (int i = 0; i < 10; i++)
            {
                if (addr[i] == 0)
                {
                    return id_clusters.ToArray();
                }
                id_clusters.Add(addr[i]); 
            }
            for (int i = 10; i < 13; i++)
            {
                if (addr[i] == 0)
                {
                    return id_clusters.ToArray();
                }
                byte[] temp = Cluster.Read(addr[i]);
                for (int j = 0; j < Cluster.Cluster_size; j += 4)
                {
                    uint rd = BitConverter.ToUInt32(temp, j);
                    if (rd == 0)
                    {
                        return id_clusters.ToArray();
                    }
                    id_clusters.Add(rd);
                }
            }
            return id_clusters.ToArray();
        }

        public uint[] Get_Clusters(Superblock superblock, Bitmap bitmap_cluster, uint need_count)
        {
            var id_clusters_now = Get_Clusters();
            if (id_clusters_now.Length >= need_count)
            {
                return id_clusters_now;
            }
            var id_clusters = new List<uint>(id_clusters_now);
            need_count -= (uint)id_clusters.Count;

            var need_clusters = new List<uint>(bitmap_cluster.Get_Free(superblock.Cluster_free_count, need_count));
            for (int i = 0; i < 10; i++)
            {
                if (addr[i] == 0)
                {
                    addr[i] = need_clusters[0];
                    id_clusters.Add(addr[i]);
                    bitmap_cluster.Change_Free(addr[i], false, superblock);
                    need_clusters.RemoveAt(0);
                    if (need_clusters.Count == 0)
                    {
                        Write(superblock, bitmap_cluster);
                        return id_clusters.ToArray();
                    }
                }
            }
            for (int i = 10; i < 13; i++)
            {
                if (addr[i] == 0)
                {
                    addr[i] = bitmap_cluster.Get_Free(superblock.Cluster_free_count, 1)[0];
                    bitmap_cluster.Change_Free(addr[i], false, superblock);
                }
                byte[] temp = Cluster.Read(addr[i]);                
                for (int j = 0; j < Cluster.Cluster_size; j += 4)
                {
                    uint rd = BitConverter.ToUInt32(temp, j);
                    if (rd == 0)
                    {
                        try
                        {
                            using (BinaryWriter writer = new BinaryWriter(File.Open(fs_file, FileMode.Open)))
                            {
                                id_clusters.Add(need_clusters[0]);
                                bitmap_cluster.Change_Free(addr[i], false, superblock);
                                Cluster.Write(BitConverter.GetBytes(need_clusters[0]), addr[i], j); // запись номера нового кластера в кластер с кластерами =)
                                bitmap_cluster.Change_Free(need_clusters[0], false, superblock);
                                need_clusters.RemoveAt(0);
                                if (need_clusters.Count == 0)
                                {
                                    Write(superblock, bitmap_cluster);
                                    return id_clusters.ToArray();
                                }
                            }
                        }
                        catch (Exception e)
                        {
                            return null;
                        }
                    }
                }
            }
            return null; // Место в файле закончилось
        }

        public int Delete(Superblock superblock, Bitmap bitmap_inode, Bitmap bitmap_cluster)
        {
            bitmap_inode.Change_Free(Id, true, superblock);
            Cluster.Write(new byte[1], this, superblock, bitmap_cluster);
            try
            {
                using (BinaryWriter writer = new BinaryWriter(File.Open(fs_file, FileMode.Open)))
                {
                    writer.BaseStream.Seek(OFFSET + 74 * (Id - 1), SeekOrigin.Begin);
                    writer.Write(new byte[74]);
                }
            }
            catch (Exception e)
            {
                return -1;
            }
            return 0;
        }

        public int Update_Addr(Superblock superblock, Bitmap bitmap_cluster)
        {
            byte[] bytes = new byte[Cluster.Cluster_size];
            byte[] nulls = new byte[Cluster.Cluster_size];
            for (int i = 0; i < 10; i++)
            {
                try
                {
                    using (BinaryReader reader = new BinaryReader(File.Open(fs_file, FileMode.Open)))
                    {
                        bytes = Cluster.Read(addr[i]);
                    }
                }
                catch (Exception e)
                {
                    return -1;
                }
                if (bytes == null) // addr уже 0
                {
                    break;
                }
                else if (bytes.SequenceEqual(nulls)) // addr есть, но в нём пусто
                {
                    for (int j = i; j < 10; j++)
                    {
                        bitmap_cluster.Change_Free(addr[j], true, superblock);
                        addr[j] = 0;
                    }
                }
            }
            byte[] cluster_temp = new byte[Cluster.Cluster_size];
            for (int i = 10; i < 13; i++)
            {
                cluster_temp = Cluster.Read(addr[i]);
                if (cluster_temp == null)
                {
                    continue;
                }
                else if (cluster_temp.SequenceEqual(nulls))
                {
                    bitmap_cluster.Change_Free(addr[i], true, superblock);
                    addr[i] = 0;
                    continue;
                }
                for (int j = 0; j < cluster_temp.Length; j += 4)
                {
                    uint rd = BitConverter.ToUInt32(cluster_temp, j);
                    if (rd == 0)
                    {
                        break;
                    }
                    else
                    {
                        bitmap_cluster.Change_Free(rd, true, superblock);
                        try
                        {
                            using (BinaryWriter writer = new BinaryWriter(File.Open(fs_file, FileMode.Open)))
                            {
                                writer.BaseStream.Seek(Cluster.OFFSET + (rd - 1) * Cluster.Cluster_size, SeekOrigin.Begin);
                                writer.Write(new byte[Cluster.Cluster_size]);
                            }
                        }
                        catch (Exception e)
                        {
                            return -1;
                        }
                    }
                }
                try
                {
                    using (BinaryWriter writer = new BinaryWriter(File.Open(fs_file, FileMode.Open)))
                    {
                        writer.BaseStream.Seek(Cluster.OFFSET + (addr[i] - 1) * Cluster.Cluster_size, SeekOrigin.Begin);
                        writer.Write(new byte[Cluster.Cluster_size]);
                        bitmap_cluster.Change_Free(addr[i], true, superblock);
                        addr[i] = 0;
                    }
                }
                catch (Exception e)
                {
                    return -1;
                }
            }                                         
            return 0;
        }

        public static bool Is_Catalog(uint id, Superblock superblock)
        {
            ushort flags;
            try
            {
                using (BinaryReader reader = new BinaryReader(File.Open(superblock.FS_file, FileMode.Open)))
                {
                    reader.BaseStream.Seek(superblock.Offset_inode + 74 * (id - 1) + 2, SeekOrigin.Begin);
                    flags = reader.ReadUInt16();                    
                }
            }
            catch (Exception e)
            {
                throw e;
            }
            if (Bitmap.Has_Bit(BitConverter.GetBytes(flags)[1], 8)) // Проверяем 2 байт, потому что записаны они в обратном порядке (почему-то)
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        public void Set_Permissions(ushort perm)
        {
            Permissions = perm;
        }

        internal static int Update_Inode_After_Delete_User(byte uid, Superblock superblock, Bitmap bitmap)
        {
            List<uint> delete_pos_list = new List<uint>();

            try
            {
                using (BinaryReader reader = new BinaryReader(File.Open(superblock.FS_file, FileMode.Open)))
                {
                    foreach (var item in bitmap.Get_Engaged(superblock.Inode_count))
                    {
                        reader.BaseStream.Seek(bitmap.OFFSET + 74 * (item - 1), SeekOrigin.Begin);
                        if (reader.ReadByte() == uid)
                        {
                            delete_pos_list.Add(item);
                        }
                    }
                }
            }
            catch (Exception e)
            {
                return -1;
            }            

            try
            {
                using (BinaryWriter writer = new BinaryWriter(File.Open(superblock.FS_file, FileMode.Open)))
                {
                    foreach (var item in delete_pos_list)
                    {
                        writer.BaseStream.Seek(superblock.Offset_inode + item * 74, SeekOrigin.Begin);
                        writer.Write((byte)1);
                    }                    
                }
            }
            catch (Exception e)
            {
                return -1; // заглушка
            }

            return 0;
        }

        internal static int Update_Inode_After_Delete_Group(byte gid, Superblock superblock, Bitmap bitmap)
        {
            List<uint> delete_pos_list = new List<uint>();

            try
            {
                uint[] engaged = bitmap.Get_Engaged(superblock.Inode_count - superblock.Inode_free_count);
                using (BinaryReader reader = new BinaryReader(File.Open(superblock.FS_file, FileMode.Open)))
                {
                    foreach (var item in engaged)
                    {
                        reader.BaseStream.Seek(bitmap.OFFSET + 74 * (item - 1) + 1, SeekOrigin.Begin);
                        if (reader.ReadByte() == gid)
                        {
                            delete_pos_list.Add(item);
                        }
                    }
                }
            }
            catch (Exception e)
            {
                return -1;
            }

            try
            {
                using (BinaryWriter writer = new BinaryWriter(File.Open(superblock.FS_file, FileMode.Open)))
                {
                    foreach (var item in delete_pos_list)
                    {
                        writer.BaseStream.Seek(superblock.Offset_inode + item * 74 + 1, SeekOrigin.Begin);
                        writer.Write((byte)1);
                    }
                }
            }
            catch (Exception e)
            {
                return -1; // заглушка
            }

            return 0;
        }
    }
}

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace File_system.File_System
{
    class Cluster_Control
    {
        public readonly long OFFSET;
        string fs_file;
        public ushort Cluster_size { private set; get; }

        public Cluster_Control(Superblock superblock)
        {
            fs_file = superblock.FS_file;
            OFFSET = superblock.Offset_cluster;
            Cluster_size = superblock.Cluster_size;
        }

        public byte[] Read(uint id)
        {
            byte[] data = new byte[Cluster_size];
            if (id <= 0)
            {
                return null;
            }
            try
            {
                using (BinaryReader reader = new BinaryReader(File.Open(fs_file, FileMode.Open)))
                {
                    reader.BaseStream.Seek(OFFSET + (id - 1) * Cluster_size, SeekOrigin.Begin);
                    data = reader.ReadBytes(Cluster_size);
                }
            }
            catch (Exception e)
            {
                return null;
            }
            if (data.Length == 0)
            {
                data = new byte[Cluster_size]; ////////////////
            }
            return data;
        }

        public byte[] Read(Inode inode)
        {
            var clusters = inode.Get_Clusters();
            byte[] data = new byte[inode.File_size];
            int pos_data = 0;
            int left_bytes = (int)inode.File_size;            
            try
            {
                for (int i = 0; i < clusters.Length; i++)
                {
                    using (BinaryReader reader = new BinaryReader(File.Open(fs_file, FileMode.Open)))
                    {
                        reader.BaseStream.Seek(OFFSET + (clusters[i] - 1) * Cluster_size, SeekOrigin.Begin);
                        int read_count = (left_bytes > Cluster_size) ? Cluster_size : left_bytes;
                        Array.Copy(reader.ReadBytes(read_count), 0, data, pos_data, read_count);
                        left_bytes -= Cluster_size;
                        pos_data += Cluster_size;
                    }
                }
                    
            }
            catch (Exception e)
            {
                return null;
            }
            return data;
        }

        public int Write(char[] chars, uint id, int position = 0) // Encoding добавить можно
        {
            try
            {
                using (BinaryWriter writer = new BinaryWriter(File.Open(fs_file, FileMode.Open)))
                {
                    writer.BaseStream.Seek(OFFSET + (id - 1) * Cluster_size + position, SeekOrigin.Begin);
                    writer.Write(Encoding.Default.GetBytes(chars));
                }
            }
            catch (Exception e)
            {
                return -1;
            }
            return 0;
        }

        public int Write(byte[] bytes, uint id, int position = 0)
        {
            try
            {
                using (BinaryWriter writer = new BinaryWriter(File.Open(fs_file, FileMode.Open)))
                {
                    writer.BaseStream.Seek(OFFSET + (id - 1) * Cluster_size + position, SeekOrigin.Begin);
                    writer.Write(bytes);
                }
            }
            catch (Exception e)
            {
                return -1;
            }
            return 0;
        }

        /* В случае работы с разными кодировками дописать логику */
        public int Write(char[] chars, Inode inode, Superblock superblock, Bitmap bitmap_cluster) 
        {
            var clusters = inode.Get_Clusters(superblock, bitmap_cluster, (uint)((chars.Length - 1) / Cluster_size + 1));
            int pos_bytes = 0;
            int left_bytes = chars.Length;
            try
            {
                for (int i = 0; i < clusters.Length; i++)
                {
                    if (left_bytes > 0)
                    {
                        using (BinaryWriter writer = new BinaryWriter(File.Open(fs_file, FileMode.Open)))
                        {
                            writer.BaseStream.Seek(OFFSET + (clusters[i] - 1) * Cluster_size, SeekOrigin.Begin);
                            writer.Write(Encoding.Default.GetBytes(chars), pos_bytes, (left_bytes > Cluster_size) ? Cluster_size : left_bytes);
                        }
                        left_bytes -= Cluster_size;
                        pos_bytes += Cluster_size;
                    }
                    else
                    {
                        if (inode.File_size > chars.Length) // Если файл был бОльшего размера
                        {
                            byte[] nulls;
                            if (left_bytes < 0)
                            {
                                nulls = new byte[-left_bytes];
                                try
                                {
                                    using (BinaryWriter writer = new BinaryWriter(File.Open(fs_file, FileMode.Open)))
                                    {
                                        writer.BaseStream.Seek(OFFSET + (clusters[i] - 1 - 1) * Cluster_size + (-left_bytes), SeekOrigin.Begin);
                                        writer.Write(nulls, 0, -left_bytes);
                                        left_bytes = 0;
                                    }
                                }
                                catch (Exception e)
                                {
                                    return -1;
                                }
                            }
                            try
                            {
                                using (BinaryWriter writer = new BinaryWriter(File.Open(fs_file, FileMode.Open)))
                                {
                                    writer.BaseStream.Seek(OFFSET + (clusters[i] - 1) * Cluster_size, SeekOrigin.Begin);
                                    writer.Write(new byte[Cluster_size], 0, Cluster_size);
                                }
                            }
                            catch (Exception e)
                            {
                                return -1;
                            }

                            inode.File_size = (uint)chars.Length;
                            inode.Write(superblock, bitmap_cluster);
                            return 0;
                        }                        
                    }                                        
                }
                if (left_bytes > 0)
                {
                    inode.File_size = (uint)(chars.Length - left_bytes);
                    inode.Write(superblock, bitmap_cluster);
                    return 1; // Места не хватило
                }
            }
            catch (Exception e)
            {
                return -1;
            }
            inode.File_size = (uint)chars.Length;
            inode.Write(superblock, bitmap_cluster);
            return 0;
        }

        public int Write(byte[] bytes, Inode inode, Superblock superblock, Bitmap bitmap_cluster)
        {
            var clusters = inode.Get_Clusters(superblock, bitmap_cluster, (uint)((bytes.Length - 1) / Cluster_size + 1));
            int pos_bytes = 0;
            int left_bytes = bytes.Length;
            try
            {
                for (int i = 0; i < clusters.Length; i++)
                {
                    if (left_bytes > 0)
                    {
                        using (BinaryWriter writer = new BinaryWriter(File.Open(fs_file, FileMode.Open)))
                        {
                            writer.BaseStream.Seek(OFFSET + (clusters[i] - 1) * Cluster_size, SeekOrigin.Begin);
                            writer.Write(bytes, pos_bytes, (left_bytes > Cluster_size) ? Cluster_size : left_bytes);
                        }
                        left_bytes -= Cluster_size;
                        pos_bytes += Cluster_size;
                    }
                    else
                    {
                        if (inode.File_size > bytes.Length) // Если файл был бОльшего размера
                        {
                            byte[] nulls;
                            if (left_bytes < 0)
                            {
                                nulls = new byte[-left_bytes];
                                try
                                {
                                    using (BinaryWriter writer = new BinaryWriter(File.Open(fs_file, FileMode.Open)))
                                    {
                                        writer.BaseStream.Seek(OFFSET + (clusters[i] - 1 - 1) * Cluster_size + (-left_bytes), SeekOrigin.Begin);
                                        writer.Write(nulls, 0, -left_bytes);
                                        left_bytes = 0;
                                    }
                                }
                                catch (Exception e)
                                {
                                    return -1;
                                }
                            }
                            try
                            {
                                using (BinaryWriter writer = new BinaryWriter(File.Open(fs_file, FileMode.Open)))
                                {
                                    writer.BaseStream.Seek(OFFSET + (clusters[i] - 1) * Cluster_size, SeekOrigin.Begin);
                                    writer.Write(new byte[Cluster_size], 0, Cluster_size);
                                }
                            }
                            catch (Exception e)
                            {
                                return -1;
                            }

                            inode.File_size = (uint)bytes.Length;
                            inode.Write(superblock, bitmap_cluster);
                            return 0;
                        }
                    }
                }
                if (left_bytes > 0)
                {
                    inode.File_size = (uint)(bytes.Length - left_bytes);
                    inode.Write(superblock, bitmap_cluster);
                    return 1; // Места не хватило
                }
            }
            catch (Exception e)
            {
                return -1;
            }
            inode.File_size = (uint)bytes.Length;
            inode.Write(superblock, bitmap_cluster);
            return 0;
        }
    }
}

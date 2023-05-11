using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace File_system.File_System
{
    class Bitmap ///////////////////////////////////// Можно сделать статическим 
    {
        public readonly int OFFSET;
        string fs_file;
        uint count_all;

        public Superblock Superb { private set; get; }
        public Bitmap_Type Type { private set; get; }

        public enum Bitmap_Type
        {
            inode = 1, cluster,
        }

        public Bitmap(Superblock superblock, Bitmap_Type type)
        {
            Superb = superblock;
            Type = type;
            if (type == Bitmap_Type.inode)
            {
                OFFSET = 10084; ///////////////////////////////////////////////
                count_all = superblock.Inode_count;
            }
            else if (type == Bitmap_Type.cluster)
            {
                OFFSET = 10084 + (int)((superblock.Inode_count - 1) / 8) + 1;
                count_all = superblock.Cluster_count;
            }
            fs_file = superblock.FS_file;
        }

        public static bool Has_Bit(byte source, int bit_num)
        {
            switch (bit_num)
            {
                case 1:
                    {
                        return (source & 0b1) != 0;
                    }
                case 2:
                    {
                        return (source & 0b10) != 0;
                    }
                case 3:
                    {
                        return (source & 0b100) != 0;
                    }
                case 4:
                    {
                        return (source & 0b1000) != 0;
                    }
                case 5:
                    {
                        return (source & 0b1000_0) != 0;
                    }
                case 6:
                    {
                        return (source & 0b1000_00) != 0;
                    }
                case 7:
                    {
                        return (source & 0b1000_000) != 0;
                    }
                case 8:
                    {
                        return (source & 0b1000_0000) != 0;
                    }
                default:
                    {
                        throw new Exception("Номер бита из параметра больше общего количества битов в байте");
                    }
            }
        }

        /* Получить массив с свободными адресами */
        public uint[] Get_Free(uint count_free, uint count_need)
        {
            /* ext - кол-во байтов, необходимых для представления всех кластеров. 1 бит - 1 кластер */
            uint ext = (count_all - 1) / 8 + 1; // Округляем количество считываемых байтов к большему 
            uint[] free = new uint[count_need];
            uint count_added = 0;
            short bytes_capacity = (short)(Superb.Cluster_size / 8);
            byte[] bytes = new byte[bytes_capacity];
            //if (count_need > count_free)
            //{
            //    return null;
            //}
            try
            {                
                using (BinaryReader reader = new BinaryReader(File.Open(fs_file, FileMode.Open)))
                {
                    /* offs - сдвиг по байтам внутри битовой карты */
                    uint offs = 0;
                    reader.BaseStream.Seek(OFFSET, SeekOrigin.Begin);
                    if (ext > bytes_capacity)
                    {
                        for (offs = 0; offs < ext; offs += (uint)bytes_capacity) // считывание
                        {
                            bytes = reader.ReadBytes(bytes_capacity);
                            for (uint quad = 0; quad < bytes_capacity; quad += 4) // обход по 4 байта
                            {
                                int check = BitConverter.ToInt32(bytes, (int)quad);
                                if (check == int.MaxValue) // все биты установлены в 1, свободных адресов нет
                                {
                                    continue;
                                }
                                for (uint i = 0; i < 4; i++) // обход по 1 байту
                                {
                                    for (uint j = 0; j < 8; j++) // просмотр
                                    {
                                        if (!Has_Bit(bytes[quad + i], 8 - (int)j))
                                        {
                                            free[count_added] = (offs + quad + i) * 8 + j + 1; // получение порядкового номера свободного адреса
                                            count_added++;
                                            if (count_added == count_need)
                                            {
                                                return free;
                                            }
                                        }
                                    }
                                }
                            }
                        }
                        offs -= (uint)bytes_capacity; // количество прочитанных байт после выполнения циклов 
                    }

                    byte temp;
                    for (uint i = 0; i < ext % bytes_capacity; i += 1) // Проверка оставшейся части
                    {
                        temp = reader.ReadByte();
                        for (uint k = 0; k < 8; k++)
                        {
                            if (!Has_Bit(temp, 8 - (int)k))
                            {
                                free[count_added] = (offs + i) * 8 + (k + 1);
                                count_added++;
                                if (count_added == count_need)
                                {
                                    return free;
                                }
                            }
                        }

                    }
                }
            }
            catch (Exception e)
            {
                return null;
            }
            return free;
        }

        /* Получить массив с занятыми адресами */
        public uint[] Get_Engaged(uint engaged_count) ////////
        {
            /* ext - кол-во байтов, необходимых для представления всех кластеров. 1 бит - 1 кластер */
            uint ext = (count_all - 1) / 8 + 1; // Округляем количество считываемых байтов к большему 
            uint[] engaged = new uint[engaged_count];
            uint count_added = 0;
            short bytes_capacity = (short)(Superb.Cluster_size / 8);
            byte[] bytes = new byte[bytes_capacity];
            try
            {
                using (BinaryReader reader = new BinaryReader(File.Open(fs_file, FileMode.Open)))
                {
                    /* offs - сдвиг по байтам внутри битовой карты */
                    uint offs = 0;
                    reader.BaseStream.Seek(OFFSET, SeekOrigin.Begin);
                    if (ext > bytes_capacity)
                    {
                        for (offs = 0; offs < ext; offs += (uint)bytes_capacity) // считывание
                        {
                            bytes = reader.ReadBytes(bytes_capacity);
                            for (uint quad = 0; quad < bytes_capacity; quad += 4) // обход по 4 байта
                            {
                                int check = BitConverter.ToInt32(bytes, (int)quad);
                                if (check == 0) // все биты установлены в 0, занятых кластеров нет
                                {
                                    continue;
                                }
                                for (uint i = 0; i < 4; i++) // обход по 1 байту
                                {
                                    for (uint j = 0; j < 8; j++) // просмотр
                                    {
                                        if (!Has_Bit(bytes[quad + i], (int)j + 1))
                                        {
                                            engaged[count_added] = (offs + quad + i) * 8 + j + 1; // получение порядкового номера занятого адреса
                                            count_added++;
                                            if (count_added == engaged_count)
                                            {
                                                return engaged;
                                            }
                                        }
                                    }
                                }
                            }
                        }
                        offs -= (uint)bytes_capacity; // количество прочитанных байт после выполнения циклов 
                    }

                    byte temp;
                    for (uint i = 0; i < ext % bytes_capacity; i += 1) // Проверка оставшейся части
                    {
                        temp = reader.ReadByte();
                        for (uint k = 0; k < 8; k++)
                        {
                            if (Has_Bit(temp, 8 - (int)k))
                            {
                                engaged[count_added] = (offs + i) * 8 + (k + 1);
                                count_added++;
                            }

                        }
                    }
                }
            }
            catch (Exception e)
            {
                return null;
            }
            return engaged;
        }


        public int Change_Free(uint pos, bool is_free, Superblock superblock)
        {
            int offs = (int)((pos - 1) / 8);
            byte byte_temp;
            try
            {
                using (BinaryReader reader = new BinaryReader(File.Open(fs_file, FileMode.Open)))
                {
                    reader.BaseStream.Seek(OFFSET, SeekOrigin.Begin);
                    reader.BaseStream.Seek(offs, SeekOrigin.Current);
                    byte_temp = reader.ReadByte();
                    reader.BaseStream.Seek(-1, SeekOrigin.Current);
                    //byte mask = (byte)(1 << (int)(7 - (pos - 1) % 8)); // битовая маска для изменения бита на противоположный
                    //byte_temp ^= mask; // операция XOR (исключающее ИЛИ)
                    if (Has_Bit(byte_temp, (int)(8 - (pos - 1) % 8)))
                    {
                        if (is_free)
                        {
                            superblock.Free_Increment(Type);
                        }                        
                    }
                    else
                    {
                        if (!is_free)
                        {
                            superblock.Free_Decrement(Type);
                        }
                    }
                    byte mask = (byte)((is_free ? 0 : 1) << (int)(7 - (pos - 1) % 8));
                    byte_temp = (byte)(byte_temp & ~(1 << (int)(7 - (pos - 1) % 8)) | mask);                    
                }
                using (BinaryWriter writer = new BinaryWriter(File.Open(fs_file, FileMode.Open)))
                {
                    writer.BaseStream.Seek(OFFSET, SeekOrigin.Begin);
                    writer.BaseStream.Seek(offs, SeekOrigin.Current);
                    writer.Write(byte_temp);
                }
            }
            catch (Exception e)
            {
                return -1;
            }
            return 0;
        }
    }
}

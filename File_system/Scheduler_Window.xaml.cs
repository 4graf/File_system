using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data;
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
    public partial class Scheduler_Window : Window
    {
        DataTable DT_work;
        int seq_proc;
        int seq_time;
        int quant;
        Process last_proc;

        bool is_time_little;

        ObservableCollection<Process> collection_proc = null;

        public Scheduler_Window()
        {
            InitializeComponent();

            int[] prior = Enumerable.Range(0, 40).ToArray();
            CB_Priority.ItemsSource = prior;

            int[] quant = Enumerable.Range(1, 20).ToArray();
            CB_Quant.ItemsSource = quant;

            seq_proc = 0;
            seq_time = 0;
            this.quant = 1;
            is_time_little = false;
            last_proc = null;

            DT_work = new DataTable();
            DT_work.Columns.Add("Процессы\\время");
            DG_Work.ItemsSource = DT_work.DefaultView;

        }

        private void Btn_Add_Click(object sender, RoutedEventArgs e)
        {
            if ((CB_Priority.SelectedItem == null) || String.IsNullOrEmpty(TB_Time.Text) || (CB_State.SelectedItem == null))
            {
                MessageBox.Show("Введите все данные о процессе!", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (collection_proc == null)
            {
                collection_proc = new ObservableCollection<Process>();
                DG_Proccess.ItemsSource = collection_proc;
            }
            seq_proc++;
            collection_proc.Add(new Process(seq_proc, Convert.ToByte(CB_Priority.Text), seq_time, Convert.ToInt32(TB_Time.Text), (State)CB_State.SelectedIndex));

            DataRow row = DT_work.NewRow();
            row[0] = collection_proc[collection_proc.Count - 1].Num;
            DT_work.Rows.Add(row);
        }

        private void Btn_Step_Click(object sender, RoutedEventArgs e)
        {
            seq_time++;
            DT_work.Columns.Add(seq_time.ToString());

            last_proc = Scheduling(last_proc);            

            for (int i = 0; i < DT_work.Rows.Count; i++)
            {
                DT_work.Rows[i][seq_time] = collection_proc[i].Current_state;
            }

            DG_Proccess.Items.Refresh();            
            DG_Work.ItemsSource = null;
            DG_Work.ItemsSource = DT_work.DefaultView;
        }

        public Process Scheduling(Process last)
        {
            bool is_quants = false;
            Process prc = null;
            if (last != null)
            {
                if (last.Left_time > 0)
                {
                    if (last.Priority <= 3)
                    {
                        last.Left_time -= 1;
                        return last;
                    }
                    if (last.Quant > 0)
                    {
                        last.Left_time -= 1;
                        last.Quant -= 1;
                        return last;
                    }
                    last.Current_state = State.R;
                }
                else
                {
                    last.Quant = 0;
                    last.Current_state = State.Z;
                    is_time_little = !is_time_little;
                }
            }            
            int s;
            for (s = 0; s < collection_proc.Count; s++)
            {
                if (collection_proc[s].Quant > 0)
                {
                    is_quants = true;
                }
                if ((collection_proc[s].Current_state == State.R) && ((collection_proc[s].Quant > 0) || (collection_proc[s].Priority <= 3)))
                {
                    prc = collection_proc[s];
                    break;
                }
            }
            for (int i = s+1; i < collection_proc.Count; i++)
            {
                if (collection_proc[i].Quant > 0)
                {
                    is_quants = true;
                }
                if ((collection_proc[i].Current_state != State.R) || ((collection_proc[i].Priority > 3) && (collection_proc[i].Quant <= 0)))
                {
                    continue;
                }
                if (prc.Priority > collection_proc[i].Priority)
                {
                    prc = collection_proc[i];
                }
                else if (prc.Priority == collection_proc[i].Priority)
                {
                    if (prc.Left_time > collection_proc[i].Left_time)
                    {
                        prc = collection_proc[i];
                    }
                }
            }
            if (prc != null)
            {
                prc.Current_state = State.P;
                if (prc.Priority <= 3) // Алгоритм для относительных приоритетов
                {
                    prc.Left_time -= 1;
                    return prc;
                }
            }
            if (!is_quants)
            {
                for (int i = 0; i < collection_proc.Count; i++)
                {
                    if ((collection_proc[i].Current_state == State.R) || (collection_proc[i].Current_state == State.P))
                    {
                        collection_proc[i].Quant = quant;
                    }
                }
            }
            if (prc == null)
            {
                return null;
            }

            prc.Quant -= 1;
            prc.Left_time -= 1;

            if (is_time_little)
            {
                Process ltl = null;
                for (int i = 0; i < collection_proc.Count; i++)
                {
                    if (collection_proc[i].Current_state != State.R)
                    {
                        continue;
                    }
                    if (prc.Work_time > collection_proc[i].Work_time)
                    {
                        ltl = collection_proc[i];
                    }
                }
                if (ltl != null)
                {
                    MessageBox.Show($"Приоритет процесса №{ltl.Num} был изменён с {ltl.Priority} на 4", "Маленькие вперёд!", MessageBoxButton.OK, MessageBoxImage.Information);
                    ltl.Priority = 4;
                    is_time_little = !is_time_little;
                }
            }
            return prc;
        }

        private void Btn_Set_Quant_Click(object sender, RoutedEventArgs e)
        {
            quant = CB_Quant.SelectedIndex + 1;
            MessageBox.Show("Квант изменён", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
        }
    }

    public enum State
    {
        /// <summary>
        /// Z - zombie, R - runnable, P - performed, S - sleep
        /// </summary>
        Z, R, P, S,
    }

    public class Process
    {
        public int Num { set; get; }
        public byte Priority { set; get; }
        public int Entry_time { set; get; }
        public int Work_time { set; get; }
        public int Left_time { set; get; }
        public State Current_state { set; get; }
        public int Quant { set; get; }


        public Process(int num, byte prior, int entry_t, int work_t, State state)
        {
            Num = num;
            Priority = prior;
            Entry_time = entry_t;
            Work_time = work_t;
            Left_time = Work_time;
            Current_state = state;
            Quant = 0;
        }
    }
}

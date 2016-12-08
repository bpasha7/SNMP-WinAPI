using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Data;
using System.Data.OleDb;


namespace lab3
{
    public partial class Form1 : Form
    {
        //страницы
        int pageCount = 0;
        //лист потоков
        List<ProgresItem> PI = new List<ProgresItem>();
        //строка соединения
        string strAccessConn = "Provider=Microsoft.Jet.OLEDB.4.0;Data Source=lab.mdb";
        OleDbConnection myAccessConn;
        Thread MyThread;
        public Form1()
        {
            InitializeComponent();
        }
        void CheckingThreads()
        {
            while (true)
            {
                for (int i = 0; i < PI.Count; i++)
                {
                    string[] res = PI[i].Result();
                    if (res[0] == null)
                    {
                        continue;
                    }
                    else
                    {
                        string strAccessInsert = string.Format("INSERT INTO Log(Tname, Tpage, Ttime, Tdate) VALUES(\"{0}\",\"{1}\",\"{2}\",\"{3}\")", res[0], res[1], res[2], res[3]);
                        OleDbCommand cmd = new OleDbCommand(strAccessInsert, myAccessConn);
                        cmd.ExecuteNonQuery();
                    }
                }
                MyThread.Join(500);
            }
        }
        //Функция для ведения лога
        void ToLog(string name, string txt)
        {
            try
            {
                string strAccessInsert = string.Format("INSERT INTO Events(Ename, Emsg, Edate) VALUES(\"{0}\",\"{1}\",\"{2}\")", name, txt, DateTime.Now.ToString("dd.MM.yyyy HH:mm:ss"));
                OleDbCommand cmd = new OleDbCommand(strAccessInsert, myAccessConn);
                cmd.ExecuteNonQuery();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Data base Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
        }
        private void Form1_Load(object sender, EventArgs e)
        {
            //создаем и открываем подключение
            myAccessConn = new OleDbConnection(strAccessConn);
            myAccessConn.Open();
        }
        //Узнаем объем файла в странциах
        bool SetNumPage(string uri)
        {
            try
            {
                HttpClient client = new HttpClient();
                Task<HttpResponseMessage> task = client.GetAsync(uri);
                string Txt = task.Result.Content.ReadAsStringAsync().Result;
                //находим количсетво страниц
                int page = Txt.IndexOf("\"b-input b-input_page\"");
                int pageend = Txt.IndexOf("<", page);
                string pages = Txt.Remove(pageend);
                pages = pages.Remove(0, page);
                pages = pages.Remove(0, pages.IndexOf("/> / ") + 5);
                PagesLabel.Text = "Page 0/"+pages;
                pageCount = Convert.ToInt32(pages);
                return true;
            }
            catch (Exception ex)
            {
                ToLog(ex.Source, ex.Message);
                MessageBox.Show("File was not founded! Repeat again.","Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return false;
            }
        }

        //захватываем страницу и обновляем интерфейс для дальнейшей работы приложения
        private void button1_Click(object sender, EventArgs e)
        {
            richTextBox1.Text = "";
            if (SetNumPage(URL.Text))
            {
                URL.ReadOnly = true;
                URL.SelectionStart = 0;
                URL.SelectionLength = 0;
                progressBar1.Maximum = pageCount;
                progressBar1.Step = 1;
                progressBar1.Minimum = 0;
                progressBar1.Value = 0;
                button3.Enabled = true;
                button4.Enabled = true;
            }
        }

        //сброс настроех и очищение всех элементов
        private void button3_Click(object sender, EventArgs e)
        {
            richTextBox1.Clear();
            URL.Text = "";
            URL.ReadOnly = false;
            PagesLabel.Text = "Page x/x";
            PI.Clear();
            panel1.Controls.Clear();
            button5.Enabled = true;
            button3.Enabled = true;
            button4.Enabled = true;
            progressBar1.Value = 0;
        }

        //Добавление интерфеса управления дл нового потока
        private void button4_Click(object sender, EventArgs e)
        {
            //ограничение в 9 потоков
            if (PI.Count == 9)
            {
                MessageBox.Show("You can not create more then 9 threads!", "Stop", MessageBoxButtons.OK, MessageBoxIcon.Stop);
                ToLog("Adding threads", "9 threads have already added");
                return;
            }
            //добавление контролов на вкладку в основном приложении                  
            PI.Add(new ProgresItem(PI.Count+1, richTextBox1, progressBar1, PagesLabel, URL.Text));
            PI[0].SetCount(pageCount);
            PI[0].Connection = myAccessConn;
            PI[0].Path = System.IO.Directory.GetCurrentDirectory();
            for (int i =0; i < PI[0].Controls.Count; i++ )
                this.panel1.Controls.Add(PI[PI.Count -1].Controls[i]);
            button5.Enabled = true;
            ToLog("Adding thread", string.Format("Thread #{0} just have added", PI.Count));
        }

        //запуск всех потоков
        private void button5_Click(object sender, EventArgs e)
        {
            ToLog("Starting threads", string.Format("{0} threads will be started", PI.Count));
            try
            {
                foreach (ProgresItem item in PI)
                {
                    item.Start();
                }
                ToLog("Starting threads", string.Format("Starting is successfull"));
                MyThread = new Thread(CheckingThreads);
                MyThread.Start();
            }
            catch(Exception ex)
            {
                ToLog("Starting threads", string.Format("Starting is fail"));
            }
        }

        //закрытие соединения
        private void Form1_FormClosed(object sender, FormClosedEventArgs e)
        {
            myAccessConn.Close();
        }

        
        //Открытие лога при попадание на вкладу "Log"
                private void tabPage3_Enter(object sender, EventArgs e)
        {
            OleDbCommand cmd = new OleDbCommand("Select * from Log", myAccessConn);
            OleDbDataAdapter myDataAdapter = new OleDbDataAdapter(cmd);

            DataSet ds = new DataSet();
            myDataAdapter.Fill(ds);
            ds.Tables[0].Columns[1].ColumnName = "Name";
            ds.Tables[0].Columns[2].ColumnName = "Pages";
            ds.Tables[0].Columns[3].ColumnName = "Time";
            ds.Tables[0].Columns[4].ColumnName = "Date";
            dataGridView1.DataSource = ds.Tables[0];
            dataGridView1.Columns[1].Width = 120;
            dataGridView1.Columns[2].Width = 50;
            dataGridView1.Columns[3].Width = 100;
            dataGridView1.Columns[3].Width = 150;
            dataGridView1.Columns[0].Visible = false;

        }
    }
}

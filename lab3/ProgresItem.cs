using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Data;
using System.Drawing;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Runtime;
using System.IO;
using System.Diagnostics;
using iTextSharp.text;
using iTextSharp.text.pdf;
using System.Data.OleDb;

namespace lab3
{
    class ProgresItem
    {
        static int AllPages;
        static int CurrentPages;
        static string path;
        int num;
        string elapsedTime;
        int startNum;
        int endNum;
        string URL;
        Label ThreadName;
        Label ProgressLabel;
        Button StartButton;
        Button PauseButton;
        Button StopButton;
        TextBox StartPage;
        TextBox EndPage;
        RichTextBox output;
        ProgressBar ThreadProgress;
        ProgressBar GeneralProgress;
        Label GeneralLabel;
        Thread MyThread;
        static OleDbConnection myAccessConn;
        bool Dead = false;
        delegate void DelegateProgress();
        delegate void DelegateProgressLabel(int Current, int Count);
        void IncrementProgressbar()
        {
            if(ThreadProgress.InvokeRequired)
            {
                DelegateProgress d = new DelegateProgress(IncrementProgressbar);
                ThreadProgress.Invoke(d, new object[] { });
            }
            else
            {
                ThreadProgress.Value++;
            }
        }
        void IncrementGeneralLabel(int a, int b)
        {
            if (ThreadProgress.InvokeRequired)
            {
                DelegateProgressLabel d = new DelegateProgressLabel(IncrementGeneralLabel);
                ThreadProgress.Invoke(d, new object[] { a, b });
            }
            else
            {
               // CurrentPages++;
                GeneralLabel.Text = string.Format("Page {0}/{1}", ++CurrentPages, AllPages);
            }
        }
        void IncrementGeneralProgressbar()
        {
            if (ThreadProgress.InvokeRequired)
            {
                DelegateProgress d = new DelegateProgress(IncrementGeneralProgressbar);
                ThreadProgress.Invoke(d, new object[] { });
            }
            else
            {
                GeneralProgress.Value++;
            }
        }
        void IncrementProgressLabel(int Current, int Count)
        {
            if (ThreadProgress.InvokeRequired)
            {
                DelegateProgressLabel d = new DelegateProgressLabel(IncrementProgressLabel);
                ThreadProgress.Invoke(d, new object[] { Current, Count });
            }
            else
            {
                ProgressLabel.Text = string.Format("{0}/{1}", Current, Count);
            }
        }
        public OleDbConnection Connection
        {
            set { myAccessConn = value; }
        }
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
        public string Path
        {
            set { path = value; }
        }
        public void Start()
        {           
            StartButton.PerformClick();
        }
        public void Stop()
        {
            StopButton.PerformClick();          
        }
        public string [] Result()
        {
                string[] res = new string[4];
                //[0] = "";
                if (MyThread.ThreadState == System.Threading.ThreadState.Stopped && !Dead)
                {
                    
                    res[0] = "Thread #" + num.ToString();
                    res[1] = (endNum - startNum + 1).ToString();
                    res[2] = elapsedTime;
                    res[3] = DateTime.Now.ToString("dd.MM.yyyy HH:mm:ss");
                    Dead = true;
                }
            return res;
        }
        void PageDownloadFromTo(object data)
        {
            int PageFrom = ((int[])data)[0];
            int PageTo = ((int[])data)[1];
            Stopwatch stopWatch = new Stopwatch();
            stopWatch.Start();
            for (int i = PageFrom; i <= PageTo && !Dead; i++)
            {
                string uri = URL + "&page=" + i.ToString();
                parseAndUpdateRtb(uri, i);
                IncrementProgressbar();
                IncrementGeneralProgressbar();
                IncrementGeneralLabel(CurrentPages, AllPages);
                IncrementProgressLabel(i - PageFrom + 1 , PageTo - PageFrom + 1);
            }
            stopWatch.Stop();
            // Get the elapsed time as a TimeSpan value.
            TimeSpan ts = stopWatch.Elapsed;

            // Format and display the TimeSpan value.
            elapsedTime = String.Format("{0:00}:{1:00}:{2:00}.{3:00}",
                ts.Hours, ts.Minutes, ts.Seconds,
                ts.Milliseconds / 10);
        }
        void parseAndUpdateRtb(string uri, int curPage)
        {
            try
            {
                HttpClient client = new HttpClient();                
                Task<HttpResponseMessage> task = client.GetAsync(uri);
                string Txt = task.Result.Content.ReadAsStringAsync().Result;
                int startTable = Txt.IndexOf("<table>");
                int endTable = Txt.IndexOf("</table>");
                Txt = Txt.Remove(endTable);
                Txt = Txt.Remove(0, startTable);
                Txt = Txt.Replace("<tr>", "");
                Txt = Txt.Replace("</tr>", "");
                Txt = Txt.Replace("<td>", "");
                Txt = Txt.Replace("</td>", "");
                Txt = Txt.Replace("<wbr>", "");
                Txt = Txt.Replace("</wbr>", "");
                Txt = Txt.Replace("<table>", "");
                Txt = Txt.Replace("<tbody>", "");
                Txt = Txt.Replace("</tbody>", "");
                Txt = Txt.Replace("HOST-RESOURCES-MIB::hr", "\n");
                string[] Res = Txt.Split('\n');
                iTextSharp.text.Rectangle rec = new iTextSharp.text.Rectangle(PageSize.A4);
                rec.BackgroundColor = new BaseColor(System.Drawing.Color.Aqua);
                BaseFont baseFont = BaseFont.CreateFont("TIMCYRB.ttf", BaseFont.IDENTITY_H, BaseFont.NOT_EMBEDDED);
                iTextSharp.text.Font font = new iTextSharp.text.Font(baseFont, iTextSharp.text.Font.DEFAULTSIZE, iTextSharp.text.Font.NORMAL);
                iTextSharp.text.Font font2 = new iTextSharp.text.Font(baseFont, 16,
                     iTextSharp.text.Font.NORMAL, new BaseColor(Color.Orange));
                iTextSharp.text.Font font3 = new iTextSharp.text.Font(baseFont, 16,
                     iTextSharp.text.Font.NORMAL, new BaseColor(Color.Green));
                FileStream ps = new FileStream(ThreadName.Text+ "Page #"+ curPage.ToString()+ ".pdf", FileMode.Append);

                Document doc = new Document();
                PdfWriter writer = PdfWriter.GetInstance(doc, ps);
                doc.Open();
                Paragraph PR;
                PR = new Paragraph(ThreadName.Text, font3);
                PR.Alignment = Element.ALIGN_CENTER;
                doc.Add(PR);
                PR = new Paragraph("Page #" + curPage.ToString(), font3);
                PR.Alignment = Element.ALIGN_CENTER;
                doc.Add(PR);
                PR = new Paragraph("\n", font3);
                PR.Alignment = Element.ALIGN_CENTER;
                doc.Add(PR);

                PdfPTable table = new PdfPTable(3);
                table.SetWidths(new float[] { 2f, 4f, 2f });
                PdfPCell cell = new PdfPCell(new Phrase("HOST-RESOURCES-MIB", font3));
                cell.BackgroundColor = new BaseColor(Color.WhiteSmoke);
                cell.Padding = 5;
                cell.Colspan = 5;
                cell.HorizontalAlignment = Element.ALIGN_CENTER;
                table.AddCell(cell);
                table.AddCell(new Phrase("Название", font));
                table.AddCell(new Phrase("Значение", font));
                table.AddCell(new Phrase("Тип", font));
                for (int i = 0; i < Res.Length; i++)
                {
                    Res[i] = Res[i].Replace(": ", "^");
                    string[] temp = Res[i].Split( new char[] { '^', '=' });
                    if (temp.Length == 3)
                    {
                        table.AddCell(new Phrase(temp[0], font));
                        table.AddCell(new Phrase(temp[2], font));
                        table.AddCell(new Phrase(temp[1], font));
                    }
                    else
                        continue;
                }
                doc.Add(table);
                doc.Close();
                output.Text += Txt;                
            }
            catch (Exception ex)
            {
                ToLog(ex.Source ,ex.Message);
            }
        }

        public void SetCount(int pageCountGeneral)
        {
            AllPages = pageCountGeneral;
            CurrentPages = 0;
        }

        public ProgresItem(int index, RichTextBox Output, ProgressBar generalProgress, Label generalLabel, string url)
        {
            num = index;
            output = Output;
            URL = url;
            GeneralProgress = generalProgress;
            GeneralLabel = generalLabel;
            ThreadName = new Label()
            {
                Name = string.Format("LabelT{0}", num),
                Location = new Point(10, num * 25),
                Text = string.Format("Thread #{0}", num),
                TextAlign = ContentAlignment.MiddleLeft,      
                Width = 60
            };
            ProgressLabel = new Label()
            {
                Name = string.Format("ProcLT{0}", num),
                Location = new Point(335, num * 25),
                Text = string.Format("{0}/{1}", 0, 0),
                TextAlign = ContentAlignment.MiddleLeft,
                Width = 40
            };
            StartButton = new Button()
            {
                Name = string.Format("StartT{0}", num),
                Location = new Point(140, num * 25),
                Text = "Start",
                BackColor = Color.Green,
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Popup,
                Height = 20,
                Width = 40
            };
            StartButton.Click += (sender, e) =>
            {
                if (MyThread != null && !Dead)
                {
                    if (MyThread.ThreadState == System.Threading.ThreadState.Suspended)
                        MyThread.Resume();
                }
                else
                {                    
                    int[] data = new int[2];
                    Dead = false;
                    startNum = Convert.ToInt32(StartPage.Text);
                    endNum = Convert.ToInt32(EndPage.Text);
                    data[0] = startNum;
                    data[1] = endNum;
                    ThreadProgress.Maximum = data[1] - data[0] + 1;
                    StartPage.ReadOnly = true;
                    EndPage.ReadOnly = true;
                    MyThread = new Thread(new ParameterizedThreadStart(PageDownloadFromTo));
                    MyThread.Start((object)data);
                }
            };
            StopButton = new Button()
            {
                Name = string.Format("StopT{0}", num),
                Location = new Point(185, num * 25),
                Text = "Stop",
                BackColor = Color.Red,
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Popup,
                Height = 20,
                Width = 40

            };
            StopButton.Click += (sender, e) =>
            {
                StartPage.ReadOnly = false;
                EndPage.ReadOnly = false;
                ThreadProgress.Value = 0;
                ProgressLabel.Text = string.Format("{0}/{1}", 0, 0);
                if (MyThread != null)
                {
                    Dead = true;
                }
            };
            PauseButton = new Button()
            {
                Name = string.Format("PauseT{0}", num),
                Location = new Point(380, num * 25),
                Text = "Pause",
                BackColor = Color.Blue,
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Popup,
                Height = 20,
                Width = 45

            };
            PauseButton.Click += (sender, e) =>
            {
                if (MyThread != null)
                    MyThread.Suspend();
            };
            StartPage = new TextBox()
            {
                Name = string.Format("StartPageT{0}", num),
                Location = new Point(75, num * 25),
                Width = 25
            };
            EndPage = new TextBox()
            {
                Name = string.Format("EndPageT{0}", num),
                Location = new Point(105, num * 25),
                Width = 25
            };
            ThreadProgress = new ProgressBar()
            {
                Name = string.Format("ProgressT{0}", num),
                Location = new Point(230, num * 25),
                Width = 100,
                Height = 20,
                Step = 1,
                Minimum = 0,
                Value = 0
        };
        }

        public List<Control> Controls
        {
            get
            {
                List<Control> Ctrls = new List<Control>();
                Ctrls.Add(ThreadName);
                Ctrls.Add(ProgressLabel);
                Ctrls.Add(StartButton);
                Ctrls.Add(StopButton);
                Ctrls.Add(PauseButton);
                Ctrls.Add(StartPage);
                Ctrls.Add(EndPage);
                Ctrls.Add(ThreadProgress); 
                return Ctrls;
            }
        }

    }
}

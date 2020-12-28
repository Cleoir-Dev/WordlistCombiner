using System;
using System.Threading;
using System.Collections.Generic;
using System.Windows.Forms;
using System.Threading.Tasks;
using System.Linq;
using System.Net;
using Newtonsoft.Json;
using System.IO;
using System.Diagnostics;

namespace WordlistCombinder
{   
    public partial class Form1 : Form
    {

        private List<Item> palavras = new List<Item>();
        private List<string> listaPalavras = new List<string>();
        List<string[]> splitArray = new List<string[]>();

        String filename = "";
        Thread hold;

        public Form1()
        {
            InitializeComponent();
        }
     
        private void button1_Click(object sender, EventArgs e)
        {

            OpenFileDialog o = new OpenFileDialog();
            o.Filter = "txt files (*.txt)|*.txt";
            if (o.ShowDialog() == DialogResult.OK)
            {
                textBox1.Text = o.FileName;
                filename = o.FileName;
                ThreadStart t = new ThreadStart(Open_and_Load);
                Thread ct = new Thread(t);
                hold = ct;
                ct.Start();
            }
        }
        private void button3_Click(object sender, EventArgs e)
        {
            SaveFileDialog ss = new SaveFileDialog();
            ss.Filter = "txt files (*.txt)|*.txt";
            if (ss.ShowDialog() == DialogResult.OK)
            {
                filename = ss.FileName;
                ThreadStart t = new ThreadStart(Saver);
                Thread ct = new Thread(t);
                hold = ct;
                ct.Start();
            }
        }
        private bool ProcurarPalavra(string word)
        {

            string url = "https://api.dicionario-aberto.net/word/";

            using (var w = new WebClient())
            {
                var json_data = string.Empty;

                try
                {
                    json_data = w.DownloadString(url + word.ToLower().Trim());
                    palavras = JsonConvert.DeserializeObject<List<Item>>(json_data);
                    if (palavras.Count() > 0)
                        return true;
                }
                catch 
                {                    
                    return false;
                }
            }
            return false;
        }
        private void Open_and_Load()
        {
            label3.Invoke(new Action(() => label3.Text = "Carregando arquivo..."));

            string[] readText = File.ReadAllLines(filename);
            var qtdaPalavras = readText.Count();          

            switch (qtdaPalavras)
            {
                case int n when (n >= 0 && n <= 1000):

                    DivideArrayEDisparaThreads(readText, 2);

                    break;

                case int n when (n >= 1001 && n <= 100000):

                    DivideArrayEDisparaThreads(readText, 4);

                    break;

                case int n when (n >= 100001):

                    NovaThread(readText);

                    break;
            }

            hold.Abort();
        }
        private async void Saver()
        {
            label3.Invoke(new Action(() => label3.Text = "Barra congelada. Criando arquivo..."));

            await Task.Run(() =>
            {
                Stopwatch stopwatch = new Stopwatch();

                stopwatch.Start();
                File.WriteAllLines(filename, listaPalavras);
                stopwatch.Stop();

                var value = Convert.ToInt32(stopwatch.ElapsedMilliseconds);

                label3.Invoke(new Action(() => label3.Text = "Salvando arquivo..."));

                for (int i = 0; i < value; i++)
                {
                    pBar1.Invoke(new Action(() => Progress(i, value)));
                }

                label3.Invoke(new Action(() => label3.Text = "Conclusão do processo GIL!."));
                label3.Invoke(new Action(() => button1.Enabled = false));
                label3.Invoke(new Action(() => button3.Enabled = false));
            });
        }
        private void Progress(int value = 1, int max = 100)
        {
            
            if (value > 0)
            {
                pBar1.Visible = true;
                pBar1.Minimum = 1;
                pBar1.Maximum = max;
                pBar1.Value = value;
                pBar1.Step = 1;

                for (int x = 1; x <= value; x++)
                {
                    pBar1.PerformStep();
                }
            }                   
        }

        public void NovaThread(string[] lines)
        {
            label3.Invoke(new Action(() => label3.Text = "Validando palavras..."));
            var i = 0;
            foreach (var line in lines)
            {
                if (ProcurarPalavra(line))
                {
                    listaPalavras.Add(line.ToLower().Trim());
                }
                pBar1.Invoke(new Action(() => Progress(i++, lines.Count())));              
                label3.Invoke(new Action(() => label3.Text = "Validado: " + line));
            }

            label3.Invoke(new Action(() => label3.Text = "Carregado!. Clique em Salvar!."));
            button3.Invoke(new Action(() => button3.Enabled = true));
        }

        public void DivideArrayEDisparaThreads(string[] book, int size)
        {
            label3.Invoke(new Action(() => label3.Text = "Barra congelada. Calculando tarefas..."));

            var partArray = DivisorArray.SplitArray(book, size);

            foreach (var str in partArray)
            {
                string[] subs = str.Split(' ');
                splitArray.Add(subs);
            }

            foreach (string[] lines in splitArray)
            {
                new Thread(() => NovaThread(lines)).Start();
            }
        }
    }

    public static class DivisorArray 
    {
        public static string[] SplitArray(string[] ArrInput, int n_column)
        {
            string[] OutPut = new string[n_column];
            int NItem = ArrInput.Length; 
            int ItemsForColum = NItem / n_column; 
            int _total = ItemsForColum * n_column; 
            int MissElement = NItem - _total; 

            int[] _Arr = new int[n_column];
            for (int i = 0; i < n_column; i++)
            {
                int AddOne = (i < MissElement) ? 1 : 0;
                _Arr[i] = ItemsForColum + AddOne;
            }

            int offset = 0;
            for (int Row = 0; Row < n_column; Row++)
            {
                for (int i = 0; i < _Arr[Row]; i++)
                {
                    OutPut[Row] += ArrInput[i + offset] + " "; 
                }
                offset += _Arr[Row];
            }
            return OutPut;
        }
    }

    public class Item
    {
        public string word { get; set; }
        public string preview { get; set; }
        public int sense { get; set; }
    }
}

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.IO.IsolatedStorage;
using System.Linq;
using System.Runtime.InteropServices;
using System.Runtime.Remoting.Contexts;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;
using System.Windows.Forms;
using System.Windows.Media;
using System.Windows.Shapes;

namespace Rythme_Game
{
    struct KeysInfor
    {
        PictureBox key;
        Queue<PictureBox> inNotes;
        Queue<Thread> noteThreads;

        public PictureBox Key { get; set; }
        public Queue<PictureBox> InNotes { get; set; }
        public Queue<Thread> NoteThreads { get; set; }
    }

    struct SongInfor
    {
        string songTitle;
        string songDir;
        int bpm;

        public string SongTitle { get; set; }
        public string SongDir { get; set; }
        public int Bpm { get; set; }
    }

    public partial class Main : Form
    {
        MediaPlayer mediaPlayer = new MediaPlayer();

        Label label_Combo = new Label();
        Label label_songTitle = new System.Windows.Forms.Label();
        PictureBox but_Start = new System.Windows.Forms.PictureBox();
        PictureBox but_Next = new System.Windows.Forms.PictureBox();
        PictureBox but_Previous = new System.Windows.Forms.PictureBox();
        PictureBox pic_songPicture = new System.Windows.Forms.PictureBox();

        Point clientSize = new Point(600, 400);
        List<KeysInfor> keysInfors = new List<KeysInfor>();
        List<SongInfor> songsList = new List<SongInfor>();
        Stack<PictureBox> notes = new Stack<PictureBox>();

        static int judge_Min = 300;
        static int judge_Max = 360;

        int combo = 0;
        int pageCount = 0;
        bool isSuccess;

        public Main()
        {
            InitializeComponent();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            SetClientSizeCore(clientSize.X, clientSize.Y);
            LobbyControl();
        }

        void Start(object args, EventArgs e)
        {
            label_songTitle.Visible = false;
            but_Start.Visible = false;
            but_Next.Visible = false;
            but_Previous.Visible = false;
            but_Start.Visible = false;
            pic_songPicture.Visible = false;

            label_Combo.Font = new System.Drawing.Font("MV Boli", 15F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            label_Combo.Location = new System.Drawing.Point(500, 150);
            label_Combo.TextAlign = ContentAlignment.MiddleCenter;
            label_Combo.Size = new System.Drawing.Size(100, 120);
            label_Combo.Text = combo.ToString() + "\nCombo!";
            Controls.Add(label_Combo);

            // 키 생성 
            Point startKeyLoc = new Point(clientSize.X / 2 - 160, 0);
            for(int i = 0; i < 4; i++)
            {
                PictureBox key = new PictureBox();
                key.Size = new Size(80, 400);
                key.Location = startKeyLoc;
                key.Image = Properties.Resources.key_basic;
                Controls.Add(key);

                KeysInfor keysInfor = new KeysInfor();
                keysInfor.Key = key;
                keysInfor.InNotes = new Queue<PictureBox>();
                keysInfor.NoteThreads = new Queue<Thread>();
                keysInfors.Add(keysInfor);

                startKeyLoc.X += 80;
            }

            MusicStart();

            // 노트 생성
            for (int i = 0; i < 20; i++)
            {
                PictureBox note = SelectNote();
                notes.Push(note);
            }

            Thread createNoteThread = new Thread(CreateNote);
            createNoteThread.IsBackground = true;
            createNoteThread.Start();
        }

        void MusicStart()
        {
            mediaPlayer.Stop();
            mediaPlayer.Open(new Uri($"{songsList[pageCount].SongDir}\\music.mp3"));
            mediaPlayer.Play();
        }
        
        void CreateNote()
        {
            Thread.Sleep(1000);

            while (true)
            {
                PictureBox note = notes.Pop();
                int ran = new Random().Next(0, 4);

                if (note.InvokeRequired)
                {
                    note.Invoke(new MethodInvoker(delegate ()
                    {
                        note.Location = new Point(keysInfors[ran].Key.Location.X, 0);
                        keysInfors[ran].InNotes.Enqueue(note);
                        
                        note.Visible = true;
                    }));
                }
                else
                {
                    note.Visible = true;
                }

                Thread dropNoteThread = null;
                dropNoteThread = new Thread(() => DropNote(note, dropNoteThread, ran));
                dropNoteThread.IsBackground = true;
                dropNoteThread.Start();
                keysInfors[ran].NoteThreads.Enqueue(dropNoteThread);

                // 노트 생성 부분
                decimal beat = (decimal)60 / songsList[pageCount].Bpm * 1000;
                Thread.Sleep((int)Math.Floor(beat));
            }
        }

        void DropNote(PictureBox note, Thread myThread, int keyNum)
        {
            // bpm : 135
            // judge : y320 
            while (true)
            {
                if (note.InvokeRequired)
                {
                    note.Invoke(new MethodInvoker(delegate ()
                    {
                        note.Location = new Point(note.Location.X, note.Location.Y + 8);
                        if (note.Location.Y >= judge_Max)
                        {
                            DeleteNote(note, keyNum, false);
                        }
                    }));
                }
                else
                {
                    note.Location = new Point(note.Location.X, note.Location.Y + 8);
                    if (note.Location.Y >= judge_Max)
                    {
                        DeleteNote(note, keyNum, false);
                    }
                }

                Thread.Sleep(1000 / (320 / 8)); // 1초에 도착하게 ( y 8씩 떨어짐)
            }
        }

        void DeleteNote(PictureBox note, int keyNum, bool isPlayerInput)
        {
            note.Visible = false;

            if (!isPlayerInput)
            {
                if (!isSuccess)
                {
                    combo = 0;
                    label_Combo.Text = combo.ToString() + "\nCombo!";
                }
                notes.Push(note);

                Thread t = keysInfors[keyNum].NoteThreads.Dequeue();
                t.Abort();

                keysInfors[keyNum].InNotes.Dequeue();
                isSuccess = false;
            }
        }

        void JudgeNote(int keyNum)
        {
            keysInfors[keyNum].Key.Image = Properties.Resources.key_input;

            Thread returnKeysSpriteThread = new Thread(() => returnKeysSprite(keyNum));
            returnKeysSpriteThread.IsBackground = true;
            returnKeysSpriteThread.Start();

            for (int i = 0; i < keysInfors[keyNum].InNotes.Count; i++)
            {
                Queue<PictureBox> notes = keysInfors[keyNum].InNotes;
                PictureBox note = notes.Peek();
                if ((note.Location.Y < judge_Max && note.Location.Y > judge_Min) && !isSuccess) // 320보다 크거나 360보다 작다 (판정선)
                {
                    combo++;
                    label_Combo.Text = combo.ToString() + "\nCombo!";
                    isSuccess = true;
                    DeleteNote(note, keyNum, true);
                }
            }
        }

        void returnKeysSprite(int keyNum)
        {
            Thread.Sleep(50);
            keysInfors[keyNum].Key.Image = Properties.Resources.key_basic;
        }

        PictureBox SelectNote()
        {
            int random = new Random().Next(0, 4);
            PictureBox note = new PictureBox();
            note.Size = new Size(80, 40);
            note.Location = keysInfors[random].Key.Location;
            note.Image = Properties.Resources.note1;
            note.Visible = false;
            Controls.Add(note);
            note.BringToFront();
            return note;
        }

        void PageMove(object args, EventArgs e, int value)
        {
            if(pageCount == 0 && value < 0)
            {
                return;
            }
            else if(pageCount == songsList.Count - 1 && value > 0)
            {
                return;
            }

            pageCount += value;
            label_songTitle.Text = songsList[pageCount].SongTitle;
            pic_songPicture.Image = Image.FromFile($"{songsList[pageCount].SongDir}\\BackImage.jpg");
            MusicStart();
        }

        void LobbyControl()
        {
            string songsDir = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments) + "/JaebsRythme";
            if (!Directory.Exists(songsDir))
            {
                Directory.CreateDirectory(songsDir);
            }
            else
            {
                DirectoryInfo di = new DirectoryInfo(songsDir);
                DirectoryInfo[] songsDir_dirInfo = di.GetDirectories();
                foreach(DirectoryInfo fi in songsDir_dirInfo)
                {
                    FileInfo[] fileInfo = fi.GetFiles("*txt");
                    string[] songDes = File.ReadAllLines(fileInfo[0].FullName);

                    SongInfor songInfor = new SongInfor();
                    songInfor.SongTitle = fi.Name;
                    songInfor.SongDir = fi.FullName;
                    songInfor.Bpm = Int32.Parse(songDes[0]);
                    songsList.Add(songInfor);
                }
            }

            try
            {
                label_songTitle.Font = new System.Drawing.Font("MV Boli", 27.75F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
                label_songTitle.Location = new System.Drawing.Point(12, 22);
                label_songTitle.Size = new System.Drawing.Size(400, 49);
                label_songTitle.Text = songsList[pageCount].SongTitle;

                but_Start.Image = global::Rythme_Game.Properties.Resources.Start;
                but_Start.Location = new System.Drawing.Point(260, 320);
                but_Start.Size = new System.Drawing.Size(65, 65);
                but_Start.Click += Start;

                but_Next.Image = global::Rythme_Game.Properties.Resources.NextArrow;
                but_Next.Location = new System.Drawing.Point(490, 300);
                but_Next.Size = new System.Drawing.Size(100, 100);
                but_Next.Click += (args, e) => PageMove(args, e, 1);

                but_Previous.Image = global::Rythme_Game.Properties.Resources.Previous;
                but_Previous.Location = new System.Drawing.Point(0, 300);
                but_Previous.Size = new System.Drawing.Size(100, 100);
                but_Previous.Click += (args, e) => PageMove(args, e, -1);

                pic_songPicture.Image = Image.FromFile($"{songsList[pageCount].SongDir}\\BackImage.jpg");
                pic_songPicture.Location = new System.Drawing.Point(88, 96);
                pic_songPicture.Size = new System.Drawing.Size(396, 198);
            } catch(Exception e)
            {
                MessageBox.Show("곡 정보룰 불러올 수 없습니다.");
            }

            this.Controls.Add(but_Start);
            this.Controls.Add(but_Next);
            this.Controls.Add(but_Previous);
            this.Controls.Add(label_songTitle);
            this.Controls.Add(pic_songPicture);

            MusicStart();
        }

        private void Form1_KeyDown(object sender, KeyEventArgs e)
        {
            switch (e.KeyCode)
            {
                case Keys.D:
                    JudgeNote(0);
                    break;
                case Keys.F:
                    JudgeNote(1);
                    break;
                case Keys.J:
                    JudgeNote(2);
                    break;
                case Keys.K:
                    JudgeNote(3);
                    break;
            }
        }

        private void Main_FormClosed(object sender, FormClosedEventArgs e)
        {
            Application.Exit();
        }
    }
}

using System;
using System.Drawing;
using System.Windows.Forms;

namespace WindowsFormsApp21
{
    public partial class Form1 : Form
    {
        int rows, cols, totalMines, remainingMines, seconds;
        Button[,] btnGrid;
        bool[,] hasMine;
        bool[,] isOpened;
        bool[,] isFlagged;
        bool isFirstClick = true;

        Timer gameTimer = new Timer();
        Label lblMineCount = new Label();
        Label lblTimer = new Label();
        Panel gamePanel = new Panel();
        Panel infoPanel = new Panel();

        public Form1()
        {
            this.Text = "Mayın Tarlası - 2025 Akademik Yılı";
            this.BackColor = Color.FromArgb(230, 230, 230);
            this.FormBorderStyle = FormBorderStyle.FixedSingle;
            this.MaximizeBox = false;
            this.StartPosition = FormStartPosition.CenterScreen;

            SetupMenu();
            SetupStatusPanel();

            gameTimer.Interval = 1000;
            gameTimer.Tick += (s, e) => { seconds++; lblTimer.Text = $"⏱ Süre: {seconds}"; };

            StartNewGame(9, 9, 10);
        }

        private void SetupMenu()
        {
            MenuStrip ms = new MenuStrip();
            // Menüyü en üste sabitliyoruz
            ms.Dock = DockStyle.Top;

            ToolStripMenuItem menuOyun = new ToolStripMenuItem("Oyun");
            menuOyun.DropDownItems.Add(new ToolStripMenuItem("🔄 Yeni Oyun", null, (s, e) => StartNewGame(rows, cols, totalMines)));
            menuOyun.DropDownItems.Add(new ToolStripSeparator());
            menuOyun.DropDownItems.Add(new ToolStripMenuItem("🟢 Kolay (9x9)", null, (s, e) => StartNewGame(9, 9, 10)));
            menuOyun.DropDownItems.Add(new ToolStripMenuItem("🔴 Zor (16x16)", null, (s, e) => StartNewGame(16, 16, 40)));

            ToolStripMenuItem menuYardim = new ToolStripMenuItem("Yardım");
            menuYardim.DropDownItems.Add(new ToolStripMenuItem("📖 Kurallar", null, (s, e) =>
                MessageBox.Show("Sol tık=Butonları Aç / Sağ tık=Bayrak Dik / İlk Butonda Mayına Basmaz / 9*9 luklar 10 mayın içerir / 16*16 lıklar 40 mayın içerir..", "Yardım")));

            ms.Items.Add(menuOyun);
            ms.Items.Add(menuYardim);
            this.Controls.Add(ms); // Menü eklendi
            this.MainMenuStrip = ms;
        }

        private void SetupStatusPanel()
        {
            // InfoPanel (Mayın ve Süre paneli) menünün hemen altında olmalı
            infoPanel.Height = 50;
            infoPanel.BackColor = Color.FromArgb(45, 45, 48);
            infoPanel.Dock = DockStyle.Top; // Menünün altına yapışır

            lblMineCount.ForeColor = Color.White;
            lblMineCount.Font = new Font("Segoe UI", 11, FontStyle.Bold);
            lblMineCount.Location = new Point(15, 15);
            lblMineCount.AutoSize = true;

            lblTimer.ForeColor = Color.Gold;
            lblTimer.Font = new Font("Segoe UI", 11, FontStyle.Bold);
            lblTimer.AutoSize = true;
            lblTimer.Anchor = AnchorStyles.Top | AnchorStyles.Right;

            infoPanel.Controls.Add(lblMineCount);
            infoPanel.Controls.Add(lblTimer);
            this.Controls.Add(infoPanel);

           
           // gamePanel (Butonların olduğu panel)
            // Menü (24px) + InfoPanel (50px) = 74px. 
            
            gamePanel.Location = new Point(15, 85);
            this.Controls.Add(gamePanel);

            // Panellerin birbirinin üstüne binmesini engellemek için sıralama
            infoPanel.BringToFront();
        }

        private void StartNewGame(int r, int c, int mines)
        {
            rows = r; cols = c; totalMines = mines;
            remainingMines = mines; seconds = 0;
            isFirstClick = true;
            gameTimer.Stop();

            lblTimer.Text = "⏱ Süre: 0";
            lblMineCount.Text = $"💣 Mayın: {remainingMines}";

            gamePanel.Controls.Clear();
            btnGrid = new Button[rows, cols];
            hasMine = new bool[rows, cols];
            isOpened = new bool[rows, cols];
            isFlagged = new bool[rows, cols];
            //her hucrenin piksel cinsinden boyutu
            int cellSize = 32;
            gamePanel.Size = new Size(rows * cellSize, cols * cellSize);

            // Form Boyutlandırması: Alt kısımda boşluk kalmaması için Bottom değerini kullanıyoruz
            this.ClientSize = new Size(gamePanel.Width + 30, gamePanel.Bottom + 15);

            // Süre etiketini sağa hizala
            lblTimer.Location = new Point(this.ClientSize.Width - 120, 15);

            for (int x = 0; x < rows; x++)
            {
                for (int y = 0; y < cols; y++)
                {
                    Button btn = new Button
                    {
                        Size = new Size(cellSize, cellSize),
                        Location = new Point(x * cellSize, y * cellSize),
                        Tag = new Point(x, y),
                        FlatStyle = FlatStyle.Popup,
                        BackColor = Color.LightSlateGray,
                        Font = new Font("Segoe UI", 9, FontStyle.Bold)
                    };
                    btn.MouseUp += Btn_MouseUp;
                    btnGrid[x, y] = btn;
                    gamePanel.Controls.Add(btn);
                }
            }
        }

        private void Btn_MouseUp(object sender, MouseEventArgs e)
        {
            Button btn = (Button)sender;
            Point p = (Point)btn.Tag;
            if (e.Button == MouseButtons.Left && !isFlagged[p.X, p.Y])
            {
                if (isFirstClick) HandleFirstClick(p.X, p.Y);
                if (hasMine[p.X, p.Y]) GameOver(false);
                else RevealCell(p.X, p.Y);//hucreler acilir
            }
            else if (e.Button == MouseButtons.Right && !isOpened[p.X, p.Y])
            {
                isFlagged[p.X, p.Y] = !isFlagged[p.X, p.Y];
                btn.Text = isFlagged[p.X, p.Y] ? "🚩" : "";
                btn.ForeColor = Color.Red;
                remainingMines += isFlagged[p.X, p.Y] ? -1 : 1;
                lblMineCount.Text = $"💣 Mayın: {remainingMines}";
            }
            CheckWin();
        }

        private void HandleFirstClick(int startX, int startY)
        {
            isFirstClick = false; gameTimer.Start();
            Random rnd = new Random();
            int placed = 0;//yerlesen mayın sayısını sayar
            while (placed < totalMines)
            {
                int rx = rnd.Next(rows), ry = rnd.Next(cols);
                if (!hasMine[rx, ry] && (rx != startX || ry != startY))
                {
                    hasMine[rx, ry] = true; placed++;
                }
            }
        }

        private void RevealCell(int x, int y)
        {
            //hücre oyun alanında mı?acılmıs mı?bayraklı mı?
            if (x < 0 || x >= rows || y < 0 || y >= cols || isOpened[x, y] || isFlagged[x, y]) return;
            isOpened[x, y] = true;
            btnGrid[x, y].BackColor = Color.WhiteSmoke;
            btnGrid[x, y].FlatStyle = FlatStyle.Flat;
            btnGrid[x, y].Enabled = false;
            int count = CountMines(x, y);
            if (count > 0) { btnGrid[x, y].Text = count.ToString(); btnGrid[x, y].ForeColor = GetNumberColor(count); }
            else { for (int i = -1; i <= 1; i++) for (int j = -1; j <= 1; j++) RevealCell(x + i, y + j); }
        }

        private int CountMines(int x, int y)
        {
            //merkez hücrede dahil 3*3 lük alanı dolanır
            int count = 0;
            for (int i = -1; i <= 1; i++)
                for (int j = -1; j <= 1; j++)
                {   
                    //hücrede mayın var mı,hücre sınırlar içinde mi?
                    int nx = x + i, ny = y + j;
                    if (nx >= 0 && nx < rows && ny >= 0 && ny < cols && hasMine[nx, ny]) count++;
                }
            return count;
        }

        private void GameOver(bool won)
        {
            gameTimer.Stop();
            foreach (var b in btnGrid)
            {
                Point p = (Point)b.Tag;
                if (hasMine[p.X, p.Y]) { b.Text = "💣"; b.BackColor = won ? Color.LightGreen : Color.Tomato; }
                b.Enabled = false;//tüm butonlar pasif sayılır.
            }
            MessageBox.Show(won ? "TEBRİKLER, KAZANDIN!!!" : "BUM,MAYINA BASTIN!!!", "Oyun Sonu");
        }

        private void CheckWin()
        {
            //acılmıs hucre sayisini tutar
            int openedCount = 0;
            foreach (bool opened in isOpened) if (opened) openedCount++;
            if (openedCount == (rows * cols) - totalMines) GameOver(true);
        }

        private Color GetNumberColor(int n)
        {
            switch (n) { case 1: return Color.Blue; case 2: return Color.Green; case 3: return Color.Red; case 4: return Color.DarkBlue; default: return Color.Maroon; }
        }
    }
}
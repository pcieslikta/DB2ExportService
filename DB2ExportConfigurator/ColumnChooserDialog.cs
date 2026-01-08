using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace DB2ExportConfigurator
{
    /// <summary>
    /// Dialog wyboru kolumn do eksportu CSV
    /// </summary>
    public class ColumnChooserDialog : Form
    {
        private readonly List<CheckBox> _checkBoxes = new List<CheckBox>();
        private Button _btnSelectAll;
        private Button _btnDeselectAll;
        private Button _btnOK;
        private Button _btnCancel;
        private Panel _pnlColumns;
        private Label _lblTitle;
        private Label _lblInfo;

        // Dostępne kolumny z ObjectListView
        public static readonly Dictionary<string, string> AvailableColumns = new Dictionary<string, string>
        {
            { "NB", "NB (Numer pojazdu)" },
            { "NR", "Nr rej. (Numer rejestracyjny)" },
            { "TypPoj", "Typ pojazdu" },
            { "Zajezdnia", "Zajezdnia" },
            { "MaBramki", "Ma bramki" },
            { "WGotowosci", "W gotowości" }
        };

        public List<string> SelectedColumns { get; private set; } = new List<string>();

        public ColumnChooserDialog()
        {
            InitializeComponent();
            InitializeColumns();
        }

        public ColumnChooserDialog(List<string> preselectedColumns) : this()
        {
            if (preselectedColumns != null && preselectedColumns.Count > 0)
            {
                foreach (var checkbox in _checkBoxes)
                {
                    checkbox.Checked = preselectedColumns.Contains(checkbox.Tag?.ToString() ?? "");
                }
            }
        }

        private void InitializeComponent()
        {
            this.Text = "Wybór kolumn do eksportu CSV";
            this.Size = new Size(500, 500);
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.StartPosition = FormStartPosition.CenterParent;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.BackColor = Color.FromArgb(240, 240, 240);
            this.Font = new Font("Segoe UI", 9.5F, FontStyle.Regular);

            // Title
            _lblTitle = new Label
            {
                Text = "📋 Wybierz kolumny do wyeksportowania",
                Location = new Point(20, 20),
                Size = new Size(450, 30),
                Font = new Font("Segoe UI", 12F, FontStyle.Bold),
                ForeColor = Color.FromArgb(44, 62, 80)
            };
            this.Controls.Add(_lblTitle);

            // Info label
            _lblInfo = new Label
            {
                Text = "Zaznacz kolumny, które mają być uwzględnione w pliku CSV:",
                Location = new Point(20, 55),
                Size = new Size(450, 20),
                Font = new Font("Segoe UI", 9F, FontStyle.Regular),
                ForeColor = Color.FromArgb(100, 100, 100)
            };
            this.Controls.Add(_lblInfo);

            // Panel z kolumnami (scrollable)
            _pnlColumns = new Panel
            {
                Location = new Point(20, 85),
                Size = new Size(450, 240),
                AutoScroll = true,
                BorderStyle = BorderStyle.FixedSingle,
                BackColor = Color.White
            };
            this.Controls.Add(_pnlColumns);

            // Button: Zaznacz wszystkie
            _btnSelectAll = new Button
            {
                Text = "✅ Zaznacz wszystkie",
                Location = new Point(20, 340),
                Size = new Size(210, 40),
                BackColor = Color.FromArgb(46, 204, 113),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 10F, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            _btnSelectAll.FlatAppearance.BorderSize = 0;
            _btnSelectAll.Click += BtnSelectAll_Click;
            this.Controls.Add(_btnSelectAll);

            // Button: Odznacz wszystkie
            _btnDeselectAll = new Button
            {
                Text = "⬜ Odznacz wszystkie",
                Location = new Point(260, 340),
                Size = new Size(210, 40),
                BackColor = Color.FromArgb(149, 165, 166),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 10F, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            _btnDeselectAll.FlatAppearance.BorderSize = 0;
            _btnDeselectAll.Click += BtnDeselectAll_Click;
            this.Controls.Add(_btnDeselectAll);

            // Button: OK
            _btnOK = new Button
            {
                Text = "💾 Eksportuj",
                Location = new Point(260, 400),
                Size = new Size(100, 40),
                BackColor = Color.FromArgb(52, 152, 219),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 10F, FontStyle.Bold),
                Cursor = Cursors.Hand,
                DialogResult = DialogResult.OK
            };
            _btnOK.FlatAppearance.BorderSize = 0;
            _btnOK.Click += BtnOK_Click;
            this.Controls.Add(_btnOK);

            // Button: Anuluj
            _btnCancel = new Button
            {
                Text = "❌ Anuluj",
                Location = new Point(370, 400),
                Size = new Size(100, 40),
                BackColor = Color.FromArgb(231, 76, 60),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 10F, FontStyle.Bold),
                Cursor = Cursors.Hand,
                DialogResult = DialogResult.Cancel
            };
            _btnCancel.FlatAppearance.BorderSize = 0;
            this.Controls.Add(_btnCancel);

            this.AcceptButton = _btnOK;
            this.CancelButton = _btnCancel;
        }

        private void InitializeColumns()
        {
            int yPos = 15;
            foreach (var column in AvailableColumns)
            {
                var checkbox = new CheckBox
                {
                    Text = column.Value,
                    Tag = column.Key,
                    Location = new Point(20, yPos),
                    Size = new Size(400, 30),
                    Font = new Font("Segoe UI", 10F, FontStyle.Regular),
                    ForeColor = Color.FromArgb(44, 62, 80),
                    Checked = true // Domyślnie wszystkie zaznaczone
                };

                _checkBoxes.Add(checkbox);
                _pnlColumns.Controls.Add(checkbox);

                yPos += 35;
            }
        }

        private void BtnSelectAll_Click(object? sender, EventArgs e)
        {
            foreach (var checkbox in _checkBoxes)
            {
                checkbox.Checked = true;
            }
        }

        private void BtnDeselectAll_Click(object? sender, EventArgs e)
        {
            foreach (var checkbox in _checkBoxes)
            {
                checkbox.Checked = false;
            }
        }

        private void BtnOK_Click(object? sender, EventArgs e)
        {
            SelectedColumns = _checkBoxes
                .Where(cb => cb.Checked)
                .Select(cb => cb.Tag?.ToString() ?? "")
                .Where(tag => !string.IsNullOrEmpty(tag))
                .ToList();

            if (SelectedColumns.Count == 0)
            {
                MessageBox.Show("Musisz wybrać przynajmniej jedną kolumnę!", "Uwaga",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                this.DialogResult = DialogResult.None;
            }
        }
    }
}

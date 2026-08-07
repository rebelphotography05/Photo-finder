using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace PhotoFinder
{
    public class MainForm : Form
    {
        // Find tab controls
        private TextBox txtFolder;
        private Button btnBrowseFolder;
        private TextBox txtListFile;
        private Button btnBrowseList;
        private Button btnFind;
        private ListBox lstResults;
        private PictureBox picPreview;
        private Button btnOpenSelected;
        private Button btnCopyAll;
        private Label lblStatus;
        private ProgressBar progressScanning;
        private ProgressBar progressCopying;
        private TextBox txtOutputFolder;
        private Button btnBrowseOutput;
        private RadioButton rbCandid;
        private RadioButton rbTraditional;
        private RadioButton rbOther;
        private TextBox txtOtherName;

        // Compare tab controls
        private TextBox txtLowResFolder;
        private Button btnBrowseLowRes;
        private TextBox txtHiResFolder;
        private Button btnBrowseHiRes;
        private Button btnCompare;
        private ListBox lstCompareResults;
        private ProgressBar progressCompare;
        private Button btnCopyCompareSelected;
        private PictureBox picCompareLow;
        private PictureBox picCompareHi;
        private TextBox txtCompareOutputFolder;
        private Button btnBrowseCompareOutput;
        private RadioButton rbCompCandid;
        private RadioButton rbCompTraditional;
        private RadioButton rbCompOther;
        private TextBox txtCompOtherName;

        // Theme toggle
        private Button btnThemeToggle;
        private bool darkTheme = false;

        public MainForm()
        {
            Text = "Rebel Photo Finder";
            // Make a bit less wide so buttons remain visible
            Width = 900;
            Height = 640;
            MinimumSize = new Size(820, 520);
            StartPosition = FormStartPosition.CenterScreen;
            Font = new Font("Segoe UI", 9F, FontStyle.Regular, GraphicsUnit.Point);

            InitializeComponents();

            AllowDrop = true;
            DragEnter += MainForm_DragEnter;
            DragDrop += MainForm_DragDrop;
        }

        private void InitializeComponents()
        {
            var mainTabs = new TabControl() { Dock = DockStyle.Fill };

            var tabFind = new TabPage("Find (from text)");
            tabFind.Padding = new Padding(8);
            tabFind.Controls.Add(BuildFindPanel());

            var tabCompare = new TabPage("Compare Photos");
            tabCompare.Padding = new Padding(8);
            tabCompare.Controls.Add(BuildComparePanel());

            mainTabs.TabPages.Add(tabFind);
            mainTabs.TabPages.Add(tabCompare);

            // Theme toggle button top-right
            btnThemeToggle = new Button() { Text = "Dark Theme", AutoSize = true, Anchor = AnchorStyles.Top | AnchorStyles.Right };
            btnThemeToggle.Click += BtnThemeToggle_Click;

            var topPanel = new TableLayoutPanel() { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 1, Height = 36 };
            topPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            topPanel.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            topPanel.Controls.Add(new Label() { Text = "", AutoSize = true }, 0, 0);
            topPanel.Controls.Add(btnThemeToggle, 1, 0);

            var outer = new TableLayoutPanel() { Dock = DockStyle.Fill, RowCount = 2 };
            outer.RowStyles.Add(new RowStyle(SizeType.Absolute, 36));
            outer.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            outer.Controls.Add(topPanel, 0, 0);
            outer.Controls.Add(mainTabs, 0, 1);

            Controls.Add(outer);
        }

        private Control BuildFindPanel()
        {
            var main = new TableLayoutPanel() { Dock = DockStyle.Fill, ColumnCount = 3, RowCount = 8, Padding = new Padding(6) };
            main.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 55));
            main.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 30));
            main.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 15));

            // rows
            main.RowStyles.Add(new RowStyle(SizeType.Absolute, 30)); // label
            main.RowStyles.Add(new RowStyle(SizeType.Absolute, 34)); // folder
            main.RowStyles.Add(new RowStyle(SizeType.Absolute, 30));
            main.RowStyles.Add(new RowStyle(SizeType.Absolute, 34)); // list
            main.RowStyles.Add(new RowStyle(SizeType.Absolute, 36)); // output label
            main.RowStyles.Add(new RowStyle(SizeType.Absolute, 36)); // output controls
            main.RowStyles.Add(new RowStyle(SizeType.Percent, 100)); // results + preview
            main.RowStyles.Add(new RowStyle(SizeType.Absolute, 72)); // bottom actions

            // Folder
            var lblFolder = new Label() { Text = "Folder to search:", Anchor = AnchorStyles.Left | AnchorStyles.Top };
            txtFolder = new TextBox() { Anchor = AnchorStyles.Left | AnchorStyles.Right };
            btnBrowseFolder = new Button() { Text = "Browse", AutoSize = true };
            btnBrowseFolder.Click += BtnBrowseFolder_Click;

            main.Controls.Add(lblFolder, 0, 0);
            main.SetColumnSpan(lblFolder, 3);

            main.Controls.Add(txtFolder, 0, 1);
            main.SetColumnSpan(txtFolder, 2);
            main.Controls.Add(btnBrowseFolder, 2, 1);

            // List file
            var lblList = new Label() { Text = "Text file with names (comma, newline or semicolon):", Anchor = AnchorStyles.Left };
            txtListFile = new TextBox() { Anchor = AnchorStyles.Left | AnchorStyles.Right };
            btnBrowseList = new Button() { Text = "Browse", AutoSize = true };
            btnBrowseList.Click += BtnBrowseList_Click;

            main.Controls.Add(lblList, 0, 2);
            main.SetColumnSpan(lblList, 3);
            main.Controls.Add(txtListFile, 0, 3);
            main.SetColumnSpan(txtListFile, 2);
            main.Controls.Add(btnBrowseList, 2, 3);

            // Output label
            var lblOut = new Label() { Text = "Choose the Output folder:", Anchor = AnchorStyles.Left };
            main.Controls.Add(lblOut, 0, 4);
            main.SetColumnSpan(lblOut, 3);

            // Output controls
            txtOutputFolder = new TextBox() { Anchor = AnchorStyles.Left | AnchorStyles.Right };
            btnBrowseOutput = new Button() { Text = "Browse", AutoSize = true };
            btnBrowseOutput.Click += BtnBrowseOutput_Click;

            main.Controls.Add(txtOutputFolder, 0, 5);
            main.SetColumnSpan(txtOutputFolder, 2);
            main.Controls.Add(btnBrowseOutput, 2, 5);

            // Radio buttons for subfolder choice
            var radioPanel = new FlowLayoutPanel() { Dock = DockStyle.Fill, FlowDirection = FlowDirection.LeftToRight, AutoSize = true };
            rbCandid = new RadioButton() { Text = "Candid", AutoSize = true };
            rbTraditional = new RadioButton() { Text = "Traditional", AutoSize = true };
            rbOther = new RadioButton() { Text = "Other", AutoSize = true };
            txtOtherName = new TextBox() { Width = 140, Enabled = false };
            rbOther.CheckedChanged += (s, e) => txtOtherName.Enabled = rbOther.Checked;
            // default selection
            rbCandid.Checked = true;
            radioPanel.Controls.Add(rbCandid);
            radioPanel.Controls.Add(rbTraditional);
            radioPanel.Controls.Add(rbOther);
            radioPanel.Controls.Add(txtOtherName);

            main.Controls.Add(radioPanel, 0, 6);
            main.SetColumnSpan(radioPanel, 3);

            // Results and preview panel
            var resultsPanel = new TableLayoutPanel() { Dock = DockStyle.Fill, ColumnCount = 2 };
            resultsPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 60));
            resultsPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 40));

            lstResults = new ListBox() { Dock = DockStyle.Fill };
            lstResults.SelectedIndexChanged += LstResults_SelectedIndexChanged;

            picPreview = new PictureBox() { Dock = DockStyle.Fill, SizeMode = PictureBoxSizeMode.Zoom, BorderStyle = BorderStyle.FixedSingle };

            resultsPanel.Controls.Add(lstResults, 0, 0);
            resultsPanel.Controls.Add(picPreview, 1, 0);

            main.Controls.Add(resultsPanel, 0, 6);
            main.SetColumnSpan(resultsPanel, 3);

            // Bottom actions
            progressScanning = new ProgressBar() { Anchor = AnchorStyles.Left | AnchorStyles.Right, Height = 18 };
            btnFind = new Button() { Text = "Find Photos", AutoSize = true };
            btnFind.Click += BtnFind_Click;

            btnOpenSelected = new Button() { Text = "Open Selected", AutoSize = true };
            btnOpenSelected.Click += BtnOpenSelected_Click;
            btnCopyAll = new Button() { Text = "Copy Selected/All", AutoSize = true };
            btnCopyAll.Click += BtnCopyAll_Click;

            progressCopying = new ProgressBar() { Anchor = AnchorStyles.Left | AnchorStyles.Right, Height = 18 };

            var bottom = new TableLayoutPanel() { Dock = DockStyle.Fill, ColumnCount = 4 };
            bottom.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 40));
            bottom.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 20));
            bottom.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 20));
            bottom.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 20));

            lblStatus = new Label() { Text = "Ready", Anchor = AnchorStyles.Left, AutoSize = true };
            bottom.Controls.Add(lblStatus, 0, 0);
            bottom.Controls.Add(progressScanning, 1, 0);
            bottom.Controls.Add(btnFind, 2, 0);

            var actionsFlow = new FlowLayoutPanel() { FlowDirection = FlowDirection.RightToLeft, Dock = DockStyle.Fill };
            actionsFlow.Controls.Add(btnCopyAll);
            actionsFlow.Controls.Add(btnOpenSelected);
            bottom.Controls.Add(actionsFlow, 3, 0);

            main.Controls.Add(bottom, 0, 7);
            main.SetColumnSpan(bottom, 3);

            // Drag & drop
            txtFolder.AllowDrop = true; txtListFile.AllowDrop = true; txtOutputFolder.AllowDrop = true;
            txtFolder.DragEnter += Txt_DragEnter; txtFolder.DragDrop += TxtFolder_DragDrop;
            txtListFile.DragEnter += Txt_DragEnter; txtListFile.DragDrop += TxtList_DragDrop;
            txtOutputFolder.DragEnter += Txt_DragEnter; txtOutputFolder.DragDrop += TxtOutput_DragDrop;

            return main;
        }

        private Control BuildComparePanel()
        {
            var main = new TableLayoutPanel() { Dock = DockStyle.Fill, ColumnCount = 3, RowCount = 7, Padding = new Padding(6) };
            main.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 55));
            main.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 30));
            main.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 15));

            main.RowStyles.Add(new RowStyle(SizeType.Absolute, 30));
            main.RowStyles.Add(new RowStyle(SizeType.Absolute, 34));
            main.RowStyles.Add(new RowStyle(SizeType.Absolute, 30));
            main.RowStyles.Add(new RowStyle(SizeType.Absolute, 34));
            main.RowStyles.Add(new RowStyle(SizeType.Absolute, 36));
            main.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            main.RowStyles.Add(new RowStyle(SizeType.Absolute, 72));

            // Low-res
            var lblLow = new Label() { Text = "Select or drag the Low Res photos Folder:", Anchor = AnchorStyles.Left };
            txtLowResFolder = new TextBox() { Anchor = AnchorStyles.Left | AnchorStyles.Right };
            btnBrowseLowRes = new Button() { Text = "Browse", AutoSize = true };
            btnBrowseLowRes.Click += BtnBrowseLowRes_Click;

            main.Controls.Add(lblLow, 0, 0);
            main.SetColumnSpan(lblLow, 3);
            main.Controls.Add(txtLowResFolder, 0, 1);
            main.SetColumnSpan(txtLowResFolder, 2);
            main.Controls.Add(btnBrowseLowRes, 2, 1);

            // Hi-res
            var lblHi = new Label() { Text = "Select or drag the High Res photos Folder:", Anchor = AnchorStyles.Left };
            txtHiResFolder = new TextBox() { Anchor = AnchorStyles.Left | AnchorStyles.Right };
            btnBrowseHiRes = new Button() { Text = "Browse", AutoSize = true };
            btnBrowseHiRes.Click += BtnBrowseHiRes_Click;

            main.Controls.Add(lblHi, 0, 2);
            main.SetColumnSpan(lblHi, 3);
            main.Controls.Add(txtHiResFolder, 0, 3);
            main.SetColumnSpan(txtHiResFolder, 2);
            main.Controls.Add(btnBrowseHiRes, 2, 3);

            // Output folder label
            var lblOut = new Label() { Text = "Choose the Output folder:", Anchor = AnchorStyles.Left };
            main.Controls.Add(lblOut, 0, 4);
            main.SetColumnSpan(lblOut, 3);

            txtCompareOutputFolder = new TextBox() { Anchor = AnchorStyles.Left | AnchorStyles.Right };
            btnBrowseCompareOutput = new Button() { Text = "Browse", AutoSize = true };
            btnBrowseCompareOutput.Click += BtnBrowseCompareOutput_Click;

            main.Controls.Add(txtCompareOutputFolder, 0, 5);
            main.SetColumnSpan(txtCompareOutputFolder, 2);
            main.Controls.Add(btnBrowseCompareOutput, 2, 5);

            // Radio buttons group for compare output
            var radioPanel = new FlowLayoutPanel() { Dock = DockStyle.Fill, FlowDirection = FlowDirection.LeftToRight, AutoSize = true };
            rbCompCandid = new RadioButton() { Text = "Candid", AutoSize = true };
            rbCompTraditional = new RadioButton() { Text = "Traditional", AutoSize = true };
            rbCompOther = new RadioButton() { Text = "Other", AutoSize = true };
            txtCompOtherName = new TextBox() { Width = 140, Enabled = false };
            rbCompOther.CheckedChanged += (s, e) => txtCompOtherName.Enabled = rbCompOther.Checked;
            rbCompCandid.Checked = true;
            radioPanel.Controls.Add(rbCompCandid);
            radioPanel.Controls.Add(rbCompTraditional);
            radioPanel.Controls.Add(rbCompOther);
            radioPanel.Controls.Add(txtCompOtherName);

            main.Controls.Add(radioPanel, 0, 6);
            main.SetColumnSpan(radioPanel, 3);

            // Results + preview pair (left list, right two previews)
            var resultsPanel = new TableLayoutPanel() { Dock = DockStyle.Fill, ColumnCount = 2 };
            resultsPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 55));
            resultsPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 45));

            lstCompareResults = new ListBox() { Dock = DockStyle.Fill };
            lstCompareResults.SelectedIndexChanged += LstCompareResults_SelectedIndexChanged;

            var previewPanel = new TableLayoutPanel() { Dock = DockStyle.Fill, RowCount = 2 };
            previewPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 50));
            previewPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 50));
            picCompareLow = new PictureBox() { Dock = DockStyle.Fill, SizeMode = PictureBoxSizeMode.Zoom, BorderStyle = BorderStyle.FixedSingle };
            picCompareHi = new PictureBox() { Dock = DockStyle.Fill, SizeMode = PictureBoxSizeMode.Zoom, BorderStyle = BorderStyle.FixedSingle };
            previewPanel.Controls.Add(picCompareLow, 0, 0);
            previewPanel.Controls.Add(picCompareHi, 0, 1);

            resultsPanel.Controls.Add(lstCompareResults, 0, 0);
            resultsPanel.Controls.Add(previewPanel, 1, 0);

            main.Controls.Add(resultsPanel, 0, 5);
            main.SetColumnSpan(resultsPanel, 3);

            // Bottom actions: compare, progress, copy
            progressCompare = new ProgressBar() { Anchor = AnchorStyles.Left | AnchorStyles.Right, Height = 18 };
            btnCompare = new Button() { Text = "Compare and Find Replacements", AutoSize = true };
            btnCompare.Click += BtnCompare_Click;
            btnCopyCompareSelected = new Button() { Text = "Copy Selected Replacements", AutoSize = true };
            btnCopyCompareSelected.Click += BtnCopyCompareSelected_Click;

            var bottom = new TableLayoutPanel() { Dock = DockStyle.Fill, ColumnCount = 4 };
            bottom.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 40));
            bottom.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 30));
            bottom.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 15));
            bottom.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 15));

            var lblStatusComp = new Label() { Text = "Ready", Anchor = AnchorStyles.Left, AutoSize = true };
            bottom.Controls.Add(lblStatusComp, 0, 0);
            bottom.Controls.Add(progressCompare, 1, 0);
            bottom.Controls.Add(btnCompare, 2, 0);
            var flow = new FlowLayoutPanel() { FlowDirection = FlowDirection.RightToLeft, Dock = DockStyle.Fill };
            flow.Controls.Add(btnCopyCompareSelected);
            bottom.Controls.Add(flow, 3, 0);

            main.Controls.Add(bottom, 0, 6);
            main.SetColumnSpan(bottom, 3);

            // Drag & drop for compare
            txtLowResFolder.AllowDrop = true; txtHiResFolder.AllowDrop = true; txtCompareOutputFolder.AllowDrop = true;
            txtLowResFolder.DragEnter += Txt_DragEnter; txtLowResFolder.DragDrop += TxtLowRes_DragDrop;
            txtHiResFolder.DragEnter += Txt_DragEnter; txtHiResFolder.DragDrop += TxtHiRes_DragDrop;
            txtCompareOutputFolder.DragEnter += Txt_DragEnter; txtCompareOutputFolder.DragDrop += TxtCompareOutput_DragDrop;

            return main;
        }

        // ------------------------------
        // Drag & Drop handlers
        // ------------------------------
        private void MainForm_DragEnter(object sender, DragEventArgs e)
        {
            Txt_DragEnter(sender, e);
        }
        private void MainForm_DragDrop(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                var paths = (string[])e.Data.GetData(DataFormats.FileDrop);
                if (paths.Length > 0)
                {
                    var first = paths[0];
                    if (Directory.Exists(first)) txtFolder.Text = first;
                    else if (File.Exists(first) && Path.GetExtension(first).Equals(".txt", StringComparison.OrdinalIgnoreCase)) txtListFile.Text = first;
                }
            }
        }

        private void Txt_DragEnter(object? sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop)) e.Effect = DragDropEffects.Copy; else e.Effect = DragDropEffects.None;
        }

        private void TxtFolder_DragDrop(object? sender, DragEventArgs e)
        {
            var paths = (string[])e.Data.GetData(DataFormats.FileDrop);
            if (paths.Length > 0 && Directory.Exists(paths[0])) txtFolder.Text = paths[0];
        }
        private void TxtList_DragDrop(object? sender, DragEventArgs e)
        {
            var paths = (string[])e.Data.GetData(DataFormats.FileDrop);
            if (paths.Length > 0 && File.Exists(paths[0]) && Path.GetExtension(paths[0]).Equals(".txt", StringComparison.OrdinalIgnoreCase)) txtListFile.Text = paths[0];
        }
        private void TxtOutput_DragDrop(object? sender, DragEventArgs e)
        {
            var paths = (string[])e.Data.GetData(DataFormats.FileDrop);
            if (paths.Length > 0 && Directory.Exists(paths[0])) txtOutputFolder.Text = paths[0];
        }

        private void TxtLowRes_DragDrop(object? sender, DragEventArgs e)
        {
            var paths = (string[])e.Data.GetData(DataFormats.FileDrop);
            if (paths.Length > 0 && Directory.Exists(paths[0])) txtLowResFolder.Text = paths[0];
        }
        private void TxtHiRes_DragDrop(object? sender, DragEventArgs e)
        {
            var paths = (string[])e.Data.GetData(DataFormats.FileDrop);
            if (paths.Length > 0 && Directory.Exists(paths[0])) txtHiResFolder.Text = paths[0];
        }
        private void TxtCompareOutput_DragDrop(object? sender, DragEventArgs e)
        {
            var paths = (string[])e.Data.GetData(DataFormats.FileDrop);
            if (paths.Length > 0 && Directory.Exists(paths[0])) txtCompareOutputFolder.Text = paths[0];
        }

        // ------------------------------
        // Browse buttons
        // ------------------------------
        private void BtnBrowseFolder_Click(object? sender, EventArgs e)
        {
            using var dlg = new FolderBrowserDialog(); if (dlg.ShowDialog() == DialogResult.OK) txtFolder.Text = dlg.SelectedPath;
        }
        private void BtnBrowseList_Click(object? sender, EventArgs e)
        {
            using var dlg = new OpenFileDialog(); dlg.Filter = "Text files|*.txt|All files|*.*"; if (dlg.ShowDialog() == DialogResult.OK) txtListFile.Text = dlg.FileName;
        }
        private void BtnBrowseOutput_Click(object? sender, EventArgs e)
        {
            using var dlg = new FolderBrowserDialog(); if (dlg.ShowDialog() == DialogResult.OK) txtOutputFolder.Text = dlg.SelectedPath;
        }
        private void BtnBrowseLowRes_Click(object? sender, EventArgs e)
        {
            using var dlg = new FolderBrowserDialog(); if (dlg.ShowDialog() == DialogResult.OK) txtLowResFolder.Text = dlg.SelectedPath;
        }
        private void BtnBrowseHiRes_Click(object? sender, EventArgs e)
        {
            using var dlg = new FolderBrowserDialog(); if (dlg.ShowDialog() == DialogResult.OK) txtHiResFolder.Text = dlg.SelectedPath;
        }
        private void BtnBrowseCompareOutput_Click(object? sender, EventArgs e)
        {
            using var dlg = new FolderBrowserDialog(); if (dlg.ShowDialog() == DialogResult.OK) txtCompareOutputFolder.Text = dlg.SelectedPath;
        }

        // ------------------------------
        // Find logic
        // ------------------------------
        private async void BtnFind_Click(object? sender, EventArgs e)
        {
            lstResults.Items.Clear(); picPreview.Image = null; lblStatus.Text = ""; progressScanning.Value = 0; progressCopying.Value = 0;
            var folder = txtFolder.Text?.Trim(); var listFile = txtListFile.Text?.Trim(); var output = txtOutputFolder.Text?.Trim();
            if (string.IsNullOrEmpty(folder) || !Directory.Exists(folder)) { MessageBox.Show("Please select a valid folder to search.", "Folder not found", MessageBoxButtons.OK, MessageBoxIcon.Warning); return; }
            if (string.IsNullOrEmpty(listFile) || !File.Exists(listFile)) { MessageBox.Show("Please select a valid text file containing photo names.", "List file not found", MessageBoxButtons.OK, MessageBoxIcon.Warning); return; }
            if (string.IsNullOrEmpty(output) || !Directory.Exists(output)) { var res = MessageBox.Show("No valid output folder chosen. Do you want to use Desktop as output?", "Output folder", MessageBoxButtons.YesNo, MessageBoxIcon.Question); if (res == DialogResult.Yes) output = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory); else return; }

            string content;
            try { content = await Task.Run(() => File.ReadAllText(listFile)); }
            catch (Exception ex) { MessageBox.Show("Could not read list file: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error); return; }

            var tokens = content.Split(new[] { ',', ';', '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries).Select(s => s.Trim()).Where(s => !string.IsNullOrEmpty(s)).ToList();
            if (tokens.Count == 0) { MessageBox.Show("No names found in the text file.", "No names", MessageBoxButtons.OK, MessageBoxIcon.Information); return; }

            var exts = new[] { ".jpg", ".jpeg", ".png", ".bmp", ".gif", ".tiff", ".webp" };
            var searchOption = SearchOption.AllDirectories; // include subfolders by default per your request
            lblStatus.Text = "Scanning files...";

            var progress = new Progress<int>(p => { progressScanning.Value = Math.Min(100, p); lblStatus.Text = $"Scanning... {p}%"; });

            try
            {
                var allFiles = Directory.EnumerateFiles(folder, "*.*", searchOption).Where(f => exts.Contains(Path.GetExtension(f).ToLowerInvariant())).ToList();
                int total = allFiles.Count;
                if (total == 0) { MessageBox.Show("No image files found in the selected folder.", "No images", MessageBoxButtons.OK, MessageBoxIcon.Information); lblStatus.Text = "No images found."; progressScanning.Value = 0; return; }

                var lookup = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);
                int idx = 0;
                foreach (var f in allFiles) { idx++; int pct = (int)(idx * 100.0 / total); (progress as IProgress<int>).Report(pct); var key = Path.GetFileNameWithoutExtension(f); if (!lookup.TryGetValue(key, out var list)) { list = new List<string>(); lookup[key] = list; } list.Add(f); }

                var matches = new List<string>();
                foreach (var token in tokens)
                {
                    var t = token.Trim(); if (string.IsNullOrEmpty(t)) continue;
                    bool tokenIsNumeric = Regex.IsMatch(t, @"^\d+$");
                    if (tokenIsNumeric)
                    {
                        foreach (var kv in lookup) if (kv.Key.IndexOf(t, StringComparison.OrdinalIgnoreCase) >= 0) matches.AddRange(kv.Value);
                    }
                    else
                    {
                        foreach (var kv in lookup) if (kv.Key.IndexOf(t, StringComparison.OrdinalIgnoreCase) >= 0) matches.AddRange(kv.Value);
                    }
                }

                var distinct = matches.Distinct(StringComparer.OrdinalIgnoreCase).ToList();
                foreach (var m in distinct) lstResults.Items.Add(m);
                lblStatus.Text = $"Found {distinct.Count} matching files."; progressScanning.Value = 100;
                if (distinct.Count == 0) MessageBox.Show("No matching photos were found.", "No matches", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex) { MessageBox.Show("Error during search: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error); lblStatus.Text = "Error"; }
        }

        private void LstResults_SelectedIndexChanged(object? sender, EventArgs e)
        {
            try
            {
                picPreview.Image?.Dispose();
                picPreview.Image = null;
                if (lstResults.SelectedItem == null) return;
                var path = lstResults.SelectedItem.ToString();
                if (File.Exists(path))
                {
                    try { picPreview.Image = Image.FromFile(path); }
                    catch { picPreview.Image = null; }
                }
            }
            catch { }
        }

        private void BtnOpenSelected_Click(object? sender, EventArgs e)
        {
            if (lstResults.SelectedItem == null) { MessageBox.Show("Please select an item from the results.", "No selection", MessageBoxButtons.OK, MessageBoxIcon.Information); return; }
            var path = lstResults.SelectedItem.ToString(); try { Process.Start(new ProcessStartInfo(path!) { UseShellExecute = true }); } catch (Exception ex) { MessageBox.Show("Could not open file: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error); }
        }

        private async void BtnCopyAll_Click(object? sender, EventArgs e)
        {
            if (lstResults.Items.Count == 0) { MessageBox.Show("No matches to copy.", "Nothing to copy", MessageBoxButtons.OK, MessageBoxIcon.Information); return; }
            var output = txtOutputFolder.Text?.Trim(); if (string.IsNullOrEmpty(output) || !Directory.Exists(output)) { var res = MessageBox.Show("No valid output folder chosen. Do you want to use Desktop as output?", "Output folder", MessageBoxButtons.YesNo, MessageBoxIcon.Question); if (res == DialogResult.Yes) output = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory); else return; }

            string subName;
            if (rbCandid.Checked) subName = "Candid photos";
            else if (rbTraditional.Checked) subName = "Traditional photos";
            else { subName = string.IsNullOrWhiteSpace(txtOtherName.Text) ? "Other photos" : txtOtherName.Text.Trim(); }

            var outDir = Path.Combine(output, subName);
            Directory.CreateDirectory(outDir);

            int total = lstResults.Items.Count; int copied = 0; progressCopying.Value = 0;
            for (int i = 0; i < total; i++)
            {
                var src = lstResults.Items[i]?.ToString(); if (string.IsNullOrEmpty(src)) continue;
                try { var dest = Path.Combine(outDir, Path.GetFileName(src)); await Task.Run(() => File.Copy(src!, dest, overwrite: true)); copied++; }
                catch (Exception ex) { Debug.WriteLine("Copy failed: " + ex.Message); }
                progressCopying.Value = (int)((i + 1) * 100.0 / total); lblStatus.Text = $"Copying... {progressCopying.Value}% ({i + 1}/{total})"; Application.DoEvents();
            }
            lblStatus.Text = $"Copied {copied} of {total} files to: {outDir}"; try { Process.Start(new ProcessStartInfo(outDir) { UseShellExecute = true }); } catch { }
            MessageBox.Show($"Copied {copied} files to:\n{outDir}", "Copy complete", MessageBoxButtons.OK, MessageBoxIcon.Information); progressCopying.Value = 100;
        }

        // ------------------------------
        // Compare logic
        // ------------------------------
        private async void BtnCompare_Click(object? sender, EventArgs e)
        {
            lstCompareResults.Items.Clear(); picCompareHi.Image?.Dispose(); picCompareLow.Image?.Dispose(); picCompareHi.Image = null; picCompareLow.Image = null; progressCompare.Value = 0;
            var low = txtLowResFolder.Text?.Trim(); var hi = txtHiResFolder.Text?.Trim(); var output = txtCompareOutputFolder.Text?.Trim();
            if (string.IsNullOrEmpty(low) || !Directory.Exists(low)) { MessageBox.Show("Please select a valid Low-res folder.", "Low-res folder missing", MessageBoxButtons.OK, MessageBoxIcon.Warning); return; }
            if (string.IsNullOrEmpty(hi) || !Directory.Exists(hi)) { MessageBox.Show("Please select a valid High-res folder.", "High-res folder missing", MessageBoxButtons.OK, MessageBoxIcon.Warning); return; }
            if (string.IsNullOrEmpty(output) || !Directory.Exists(output)) { var res = MessageBox.Show("No valid output folder chosen. Do you want to use Desktop as output?", "Output folder", MessageBoxButtons.YesNo, MessageBoxIcon.Question); if (res == DialogResult.Yes) output = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory); else return; }

            lblStatus.Text = "Comparing...";
            var results = await Task.Run(() => CompareByNameAndResolution(low, hi));
            // results are tuples of (lowPath, hiPath)
            foreach (var r in results) lstCompareResults.Items.Add(r.low + " -> " + r.hi);
            progressCompare.Value = 100; lblStatus.Text = $"Found {results.Count} replacement candidates.";
            if (results.Count == 0) lstCompareResults.Items.Add("No replacements found.");
        }

        private List<(string low, string hi)> CompareByNameAndResolution(string lowFolder, string hiFolder)
        {
            var results = new List<(string low, string hi)>();
            try
            {
                var lowFiles = Directory.EnumerateFiles(lowFolder, "*.*", SearchOption.AllDirectories).ToList();
                var hiFiles = Directory.EnumerateFiles(hiFolder, "*.*", SearchOption.AllDirectories).ToList();

                // Build hi lookup by filename without extension
                var hiLookup = hiFiles.Select(f => new { Path = f, Name = Path.GetFileNameWithoutExtension(f) })
                    .GroupBy(x => x.Name, StringComparer.OrdinalIgnoreCase)
                    .ToDictionary(g => g.Key, g => g.Select(x => x.Path).ToList(), StringComparer.OrdinalIgnoreCase);

                int total = lowFiles.Count; int idx = 0;
                foreach (var lf in lowFiles)
                {
                    idx++; int pct = (int)(idx * 100.0 / Math.Max(1, total));
                    var lowName = Path.GetFileNameWithoutExtension(lf);
                    if (hiLookup.TryGetValue(lowName, out var candidates))
                    {
                        foreach (var candidate in candidates)
                        {
                            // Try to compare resolution if image, else accept name match
                            try
                            {
                                using var imgLow = Image.FromFile(lf);
                                using var imgHi = Image.FromFile(candidate);
                                if (imgHi.Width >= imgLow.Width && imgHi.Height >= imgLow.Height) results.Add((lf, candidate));
                            }
                            catch
                            {
                                results.Add((lf, candidate));
                            }
                        }
                    }
                }
            }
            catch (Exception ex) { Debug.WriteLine("Compare error: " + ex.Message); }
            return results;
        }

        private void LstCompareResults_SelectedIndexChanged(object? sender, EventArgs e)
        {
            picCompareHi.Image?.Dispose(); picCompareLow.Image?.Dispose(); picCompareHi.Image = null; picCompareLow.Image = null;
            if (lstCompareResults.SelectedItem == null) return;
            var text = lstCompareResults.SelectedItem.ToString();
            if (string.IsNullOrEmpty(text) || !text.Contains("->")) return;
            var parts = text.Split(new[] { '>', '-' }, StringSplitOptions.RemoveEmptyEntries).Select(p => p.Trim()).ToArray();
            if (parts.Length >= 2)
            {
                var low = parts[0];
                var hi = parts[1];
                if (File.Exists(low)) { try { picCompareLow.Image = Image.FromFile(low); } catch { picCompareLow.Image = null; } }
                if (File.Exists(hi)) { try { picCompareHi.Image = Image.FromFile(hi); } catch { picCompareHi.Image = null; } }
            }
        }

        private async void BtnCopyCompareSelected_Click(object? sender, EventArgs e)
        {
            if (lstCompareResults.Items.Count == 0) { MessageBox.Show("No replacements to copy.", "Nothing to copy", MessageBoxButtons.OK, MessageBoxIcon.Information); return; }
            var output = txtCompareOutputFolder.Text?.Trim(); if (string.IsNullOrEmpty(output) || !Directory.Exists(output)) { var res = MessageBox.Show("No valid output folder chosen. Do you want to use Desktop as output?", "Output folder", MessageBoxButtons.YesNo, MessageBoxIcon.Question); if (res == DialogResult.Yes) output = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory); else return; }

            string subName;
            if (rbCompCandid.Checked) subName = "Candid photos";
            else if (rbCompTraditional.Checked) subName = "Traditional photos";
            else { subName = string.IsNullOrWhiteSpace(txtCompOtherName.Text) ? "Other photos" : txtCompOtherName.Text.Trim(); }

            var outDir = Path.Combine(output, subName);
            Directory.CreateDirectory(outDir);

            // Collect hi paths from selected items
            var hiPaths = new List<string>();
            foreach (var item in lstCompareResults.Items)
            {
                var s = item.ToString(); if (string.IsNullOrEmpty(s) || !s.Contains("->")) continue;
                var parts = s.Split(new[] { '>', '-' }, StringSplitOptions.RemoveEmptyEntries).Select(p => p.Trim()).ToArray();
                if (parts.Length >= 2)
                {
                    var hi = parts[1]; if (File.Exists(hi)) hiPaths.Add(hi);
                }
            }

            if (hiPaths.Count == 0) { MessageBox.Show("No hi-res files found to copy.", "Nothing to copy", MessageBoxButtons.OK, MessageBoxIcon.Information); return; }

            int total = hiPaths.Count; int copied = 0; progressCompare.Value = 0;
            for (int i = 0; i < total; i++)
            {
                var src = hiPaths[i]; try { var dest = Path.Combine(outDir, Path.GetFileName(src)); await Task.Run(() => File.Copy(src, dest, overwrite: true)); copied++; } catch (Exception ex) { Debug.WriteLine("Copy failed: " + ex.Message); }
                progressCompare.Value = (int)((i + 1) * 100.0 / total); lblStatus.Text = $"Copying... {progressCompare.Value}% ({i + 1}/{total})"; Application.DoEvents();
            }

            lblStatus.Text = $"Copied {copied} of {total} files to: {outDir}"; try { Process.Start(new ProcessStartInfo(outDir) { UseShellExecute = true }); } catch { }
            MessageBox.Show($"Copied {copied} files to:\n{outDir}", "Copy complete", MessageBoxButtons.OK, MessageBoxIcon.Information); progressCompare.Value = 100;
        }

        // ------------------------------
        // Theme toggle
        // ------------------------------
        private void BtnThemeToggle_Click(object? sender, EventArgs e)
        {
            darkTheme = !darkTheme;
            ApplyTheme();
        }

        private void ApplyTheme()
        {
            if (darkTheme)
            {
                BackColor = Color.FromArgb(34, 34, 34);
                ForeColor = Color.WhiteSmoke;
                btnThemeToggle.Text = "Light Theme";
            }
            else
            {
                BackColor = SystemColors.Control;
                ForeColor = SystemColors.ControlText;
                btnThemeToggle.Text = "Dark Theme";
            }

            // Walk controls to apply simple colors where applicable
            foreach (Control c in Controls) ApplyColorsRecursive(c);
        }

        private void ApplyColorsRecursive(Control c)
        {
            c.BackColor = darkTheme ? Color.FromArgb(45, 45, 48) : SystemColors.Window;
            c.ForeColor = darkTheme ? Color.WhiteSmoke : SystemColors.ControlText;
            foreach (Control child in c.Controls) ApplyColorsRecursive(child);
        }
    }
}

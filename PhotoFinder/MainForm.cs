// name=PhotoFinder/MainForm.cs
// Final MainForm with local OCR integration (Tesseract + OpenCvSharp).
// BEFORE COMPILING:
// 1) Add NuGet packages to your project:
//    - OpenCvSharp4
//    - OpenCvSharp4.runtime.win
//    - Tesseract
// 2) Download tessdata (eng.traineddata) from https://github.com/tesseract-ocr/tessdata
//    Place the "tessdata" folder next to your executable (bin\Debug\netX\) so path: <appdir>\tessdata\eng.traineddata
//
// This file combines the MainForm UI and two helper classes used by OCR:
// - OcrHelper: image preprocessing + local Tesseract OCR + file-matching logic
// - OcrTokenEditorForm: modal to confirm/edit OCR tokens (shows uncertain tokens highlighted)
//
// Paste this file as PhotoFinder/MainForm.cs replacing your existing MainForm.cs.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Forms;
using OpenCvSharp;
using Tesseract;

namespace PhotoFinder
{
    public class MainForm : Form
    {
        // UI controls
        private TabControl tabs;

        // Find tab
        private TextBox txtFolder;
        private Button btnBrowseFolder;
        private TextBox txtInputFileOrImage;
        private Button btnBrowseInput;
        private ComboBox cbInputSource; // Text File, Handwritten Photo, PDF, Clipboard
        private CheckBox chkIncludeSubfolders;
        private CheckBox chkSearchAnywhere;
        private CheckBox chkPartialNumericOnly;
        private TextBox txtOutputFolder;
        private Button btnBrowseOutput;
        private RadioButton rbCandid;
        private RadioButton rbTraditional;
        private RadioButton rbOther;
        private TextBox txtOtherName;
        private Button btnFind;
        private ListBox lstResults;
        private Button btnCopyPhotos;
        private Button btnOpenFolder;
        private ProgressBar progressScanning;
        private Label lblFindTotals;
        private Label lblStatus;

        // Compare tab (kept minimal — you can expand later)
        private TextBox txtLowResFolder;
        private Button btnBrowseLowRes;
        private TextBox txtHiResFolder;
        private Button btnBrowseHiRes;
        private TextBox txtCompareOutputFolder;
        private Button btnBrowseCompareOutput;
        private Button btnCompareFind;
        private ListBox lstCompareResults;
        private ProgressBar progressCompare;
        private Label lblCompareTotals;
        private Button btnCopyComparePhotos;

        // Status bar
        private Panel statusBar;
        private Label lblTotalScanned;
        private Label lblTotalFound;
        private Label lblTotalCompared;
        private Label lblTotalCopied;
        private Label lblElapsed;

        // Theme toggle
        private Button btnThemeToggle;
        private bool darkTheme = false;

        // Internal
        private Stopwatch stopwatch = new Stopwatch();

        // Tesseract tessdata path (relative to exe)
        private readonly string tessDataDir;

        public MainForm()
        {
            Text = "Rebel Photo Finder";
            MinimumSize = new Size(900, 620);
            Size = new Size(1160, 740);
            StartPosition = FormStartPosition.CenterScreen;
            Font = new Font("Segoe UI", 10F, FontStyle.Regular);
            tessDataDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "tessdata");

            InitializeComponents();
            ApplyTheme();
        }

        private void InitializeComponents()
        {
            var root = new TableLayoutPanel { Dock = DockStyle.Fill, RowCount = 3, ColumnCount = 1, Padding = new Padding(10) };
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 48));
            root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 72));

            // Top bar: Title + theme toggle
            var top = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 3 };
            top.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 60));
            top.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 30));
            top.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 10));
            var lblTitle = new Label { Text = "Rebel Photo Finder", AutoSize = true, Font = new Font("Segoe UI Semibold", 14F, FontStyle.Bold), Anchor = AnchorStyles.Left };
            btnThemeToggle = new Button { Text = "Dark Mode", AutoSize = true, Anchor = AnchorStyles.Right };
            btnThemeToggle.Click += (s, e) => { darkTheme = !darkTheme; ApplyTheme(); };
            top.Controls.Add(lblTitle, 0, 0);
            top.Controls.Add(new Panel(), 1, 0);
            top.Controls.Add(btnThemeToggle, 2, 0);

            root.Controls.Add(top, 0, 0);

            // Tabs
            tabs = new TabControl { Dock = DockStyle.Fill };
            var tabFind = new TabPage("Photo Finder") { Padding = new Padding(12) };
            var tabCompare = new TabPage("Compare Photos") { Padding = new Padding(12) };

            tabFind.Controls.Add(BuildFindPanel());
            tabCompare.Controls.Add(BuildComparePanel());

            tabs.TabPages.Add(tabFind);
            tabs.TabPages.Add(tabCompare);

            root.Controls.Add(tabs, 0, 1);

            // Status bar
            statusBar = new Panel { Dock = DockStyle.Fill, Height = 72, Padding = new Padding(8) };
            BuildStatusBar();
            root.Controls.Add(statusBar, 0, 2);

            Controls.Add(root);
        }

        private Control BuildFindPanel()
        {
            var main = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 3, RowCount = 10, Padding = new Padding(8) };
            main.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 62));
            main.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 28));
            main.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 10));

            for (int i = 0; i < 9; i++) main.RowStyles.Add(new RowStyle(SizeType.Absolute, 44));
            main.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

            // Row 0 - Source folder
            var lblFolder = new Label { Text = "Select or drag the Folder", Anchor = AnchorStyles.Left };
            txtFolder = StyledTextBox();
            txtFolder.AllowDrop = true;
            txtFolder.DragEnter += Txt_DragEnter;
            txtFolder.DragDrop += TxtFolder_DragDrop;
            btnBrowseFolder = RoundedButton("Browse");
            btnBrowseFolder.Click += BtnBrowseFolder_Click;

            main.Controls.Add(lblFolder, 0, 0);
            main.SetColumnSpan(lblFolder, 3);
            main.Controls.Add(txtFolder, 0, 1);
            main.SetColumnSpan(txtFolder, 2);
            main.Controls.Add(btnBrowseFolder, 2, 1);

            // Row 2 - Input source selection and input path
            var lblInputSource = new Label { Text = "Input Source", Anchor = AnchorStyles.Left };
            cbInputSource = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Width = 220 };
            cbInputSource.Items.AddRange(new[] { "Text File", "Handwritten Photo", "PDF", "Clipboard Image" });
            cbInputSource.SelectedIndex = 0;
            cbInputSource.SelectedIndexChanged += CbInputSource_SelectedIndexChanged;
            txtInputFileOrImage = StyledTextBox();
            txtInputFileOrImage.AllowDrop = true;
            txtInputFileOrImage.DragEnter += Txt_DragEnter;
            txtInputFileOrImage.DragDrop += TxtList_DragDrop; // accepts files when appropriate
            btnBrowseInput = RoundedButton("Browse");
            btnBrowseInput.Click += BtnBrowseInput_Click;

            main.Controls.Add(lblInputSource, 0, 2);
            main.SetColumnSpan(lblInputSource, 3);
            var rowInput = new Panel { Dock = DockStyle.Fill };
            // We'll place CB and textbox in the same row using a small layout
            var inRow = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 3 };
            inRow.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 240));
            inRow.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            inRow.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 90));
            inRow.Controls.Add(cbInputSource, 0, 0);
            inRow.Controls.Add(txtInputFileOrImage, 1, 0);
            inRow.Controls.Add(btnBrowseInput, 2, 0);
            main.Controls.Add(inRow, 0, 3);
            main.SetColumnSpan(inRow, 3);

            // Row 4 - Options checkboxes
            chkIncludeSubfolders = new CheckBox { Text = "Search in Subfolders", Checked = true, AutoSize = true };
            chkSearchAnywhere = new CheckBox { Text = "Search Anywhere in Filename", Checked = true, AutoSize = true };
            chkPartialNumericOnly = new CheckBox { Text = "Match Numbers Only (Partial)", Checked = true, AutoSize = true };
            var optFlow = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.LeftToRight };
            optFlow.Controls.Add(chkIncludeSubfolders);
            optFlow.Controls.Add(new Label { Width = 12 });
            optFlow.Controls.Add(chkSearchAnywhere);
            optFlow.Controls.Add(new Label { Width = 12 });
            optFlow.Controls.Add(chkPartialNumericOnly);
            main.Controls.Add(optFlow, 0, 4);
            main.SetColumnSpan(optFlow, 3);

            // Row 5 - Output folder
            var lblOut = new Label { Text = "Choose the Output Folder", Anchor = AnchorStyles.Left };
            txtOutputFolder = StyledTextBox();
            txtOutputFolder.AllowDrop = true;
            txtOutputFolder.DragEnter += Txt_DragEnter;
            txtOutputFolder.DragDrop += TxtOutput_DragDrop;
            btnBrowseOutput = RoundedButton("Browse");
            btnBrowseOutput.Click += BtnBrowseOutput_Click;

            main.Controls.Add(lblOut, 0, 5);
            main.SetColumnSpan(lblOut, 3);
            main.Controls.Add(txtOutputFolder, 0, 6);
            main.SetColumnSpan(txtOutputFolder, 2);
            main.Controls.Add(btnBrowseOutput, 2, 6);

            // Row 7 - Radio group for subfolder name
            rbCandid = new RadioButton { Text = "Candid Photos", AutoSize = true };
            rbTraditional = new RadioButton { Text = "Traditional Photos", AutoSize = true };
            rbOther = new RadioButton { Text = "Other", AutoSize = true };
            txtOtherName = new TextBox { Width = 160, Enabled = false };
            rbOther.CheckedChanged += (s, e) => txtOtherName.Enabled = rbOther.Checked;
            rbCandid.Checked = true;
            var radioPanel = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.LeftToRight };
            radioPanel.Controls.Add(new Label { Text = "Copy into", AutoSize = true });
            radioPanel.Controls.Add(rbCandid);
            radioPanel.Controls.Add(rbTraditional);
            radioPanel.Controls.Add(rbOther);
            radioPanel.Controls.Add(txtOtherName);
            main.Controls.Add(radioPanel, 0, 7);
            main.SetColumnSpan(radioPanel, 3);

            // Row 8 - Results list
            lstResults = new ListBox { Dock = DockStyle.Fill };
            main.Controls.Add(lstResults, 0, 8);
            main.SetColumnSpan(lstResults, 3);

            // Row 9 - Bottom actions
            progressScanning = new ProgressBar { Dock = DockStyle.Fill, Height = 18 };
            lblFindTotals = new Label { Text = "0 photos found", AutoSize = true, Anchor = AnchorStyles.Left };
            lblStatus = new Label { Text = "Ready", AutoSize = true, Anchor = AnchorStyles.Left };

            btnFind = RoundedButton("Find Photos");
            btnFind.Click += async (s, e) => await FindPhotosButton_Click();

            btnCopyPhotos = RoundedButton("Copy Photos");
            btnCopyPhotos.Click += BtnCopyPhotos_Click;

            btnOpenFolder = RoundedButton("Open Output");
            btnOpenFolder.Click += (s, e) =>
            {
                try
                {
                    var outDir = txtOutputFolder.Text?.Trim();
                    if (!string.IsNullOrEmpty(outDir) && Directory.Exists(outDir))
                        Process.Start(new ProcessStartInfo(outDir) { UseShellExecute = true });
                    else MessageBox.Show("Choose a valid output folder first.");
                }
                catch { }
            };

            var bottom = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 4 };
            bottom.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 40));
            bottom.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 30));
            bottom.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 15));
            bottom.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 15));

            var leftPanel = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.TopDown };
            leftPanel.Controls.Add(lblFindTotals);
            leftPanel.Controls.Add(lblStatus);
            bottom.Controls.Add(leftPanel, 0, 0);
            bottom.Controls.Add(progressScanning, 1, 0);

            var actionsFlow = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.RightToLeft };
            actionsFlow.Controls.Add(btnOpenFolder);
            actionsFlow.Controls.Add(btnCopyPhotos);
            actionsFlow.Controls.Add(btnFind);
            bottom.Controls.Add(actionsFlow, 3, 0);

            main.Controls.Add(bottom, 0, 9);
            main.SetColumnSpan(bottom, 3);

            return main;
        }

        private Control BuildComparePanel()
        {
            var main = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 3, RowCount = 8, Padding = new Padding(8) };
            main.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 62));
            main.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 28));
            main.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 10));

            for (int i = 0; i < 7; i++) main.RowStyles.Add(new RowStyle(SizeType.Absolute, 48));
            main.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

            // Low res
            var lblLow = new Label { Text = "Select or drag the Low Res Photos Folder", Anchor = AnchorStyles.Left };
            txtLowResFolder = StyledTextBox();
            txtLowResFolder.AllowDrop = true;
            txtLowResFolder.DragEnter += Txt_DragEnter;
            txtLowResFolder.DragDrop += TxtLowRes_DragDrop;
            btnBrowseLowRes = RoundedButton("Browse");
            btnBrowseLowRes.Click += BtnBrowseLowRes_Click;

            main.Controls.Add(lblLow, 0, 0);
            main.SetColumnSpan(lblLow, 3);
            main.Controls.Add(txtLowResFolder, 0, 1);
            main.SetColumnSpan(txtLowResFolder, 2);
            main.Controls.Add(btnBrowseLowRes, 2, 1);

            // Hi res
            var lblHi = new Label { Text = "Select or drag the High Res Photos Folder", Anchor = AnchorStyles.Left };
            txtHiResFolder = StyledTextBox();
            txtHiResFolder.AllowDrop = true;
            txtHiResFolder.DragEnter += Txt_DragEnter;
            txtHiResFolder.DragDrop += TxtHiRes_DragDrop;
            btnBrowseHiRes = RoundedButton("Browse");
            btnBrowseHiRes.Click += BtnBrowseHiRes_Click;

            main.Controls.Add(lblHi, 0, 2);
            main.SetColumnSpan(lblHi, 3);
            main.Controls.Add(txtHiResFolder, 0, 3);
            main.SetColumnSpan(txtHiResFolder, 2);
            main.Controls.Add(btnBrowseHiRes, 2, 3);

            // Output folder for compare
            var lblOut = new Label { Text = "Choose the Output Folder", Anchor = AnchorStyles.Left };
            txtCompareOutputFolder = StyledTextBox();
            txtCompareOutputFolder.AllowDrop = true;
            txtCompareOutputFolder.DragEnter += Txt_DragEnter;
            txtCompareOutputFolder.DragDrop += TxtCompareOutput_DragDrop;
            btnBrowseCompareOutput = RoundedButton("Browse");
            btnBrowseCompareOutput.Click += BtnBrowseCompareOutput_Click;

            main.Controls.Add(lblOut, 0, 4);
            main.SetColumnSpan(lblOut, 3);
            main.Controls.Add(txtCompareOutputFolder, 0, 5);
            main.SetColumnSpan(txtCompareOutputFolder, 2);
            main.Controls.Add(btnBrowseCompareOutput, 2, 5);

            // Results list
            lstCompareResults = new ListBox { Dock = DockStyle.Fill };
            main.Controls.Add(lstCompareResults, 0, 6);
            main.SetColumnSpan(lstCompareResults, 3);

            // bottom
            progressCompare = new ProgressBar { Dock = DockStyle.Fill };
            lblCompareTotals = new Label { Text = "0 compared", AutoSize = true, Anchor = AnchorStyles.Left };
            btnCompareFind = RoundedButton("Find Photos");
            btnCompareFind.Click += BtnCompare_Click;
            btnCopyComparePhotos = RoundedButton("Copy Photos");
            btnCopyComparePhotos.Click += BtnCopyCompareSelected_Click;

            var bottom = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 4 };
            bottom.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 55));
            bottom.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25));
            bottom.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 10));
            bottom.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 10));
            bottom.Controls.Add(lblCompareTotals, 0, 0);
            bottom.Controls.Add(progressCompare, 1, 0);

            var flow = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.RightToLeft };
            flow.Controls.Add(btnCopyComparePhotos);
            flow.Controls.Add(btnCompareFind);
            bottom.Controls.Add(flow, 3, 0);
            main.Controls.Add(bottom, 0, 7);
            main.SetColumnSpan(bottom, 3);

            return main;
        }

        private void BuildStatusBar()
        {
            statusBar.Controls.Clear();
            statusBar.BackColor = Color.Transparent;

            var statusLayout = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 6 };
            statusLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 20));
            statusLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 16));
            statusLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 16));
            statusLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 16));
            statusLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 16));
            statusLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 16));

            lblTotalScanned = new Label { Text = "Total Scanned: 0", AutoSize = true, Anchor = AnchorStyles.Left };
            lblTotalFound = new Label { Text = "Total Found: 0", AutoSize = true, Anchor = AnchorStyles.Left };
            lblTotalCompared = new Label { Text = "Compared: 0", AutoSize = true, Anchor = AnchorStyles.Left };
            lblTotalCopied = new Label { Text = "Copied: 0", AutoSize = true, Anchor = AnchorStyles.Left };
            lblElapsed = new Label { Text = "Elapsed: 0s", AutoSize = true, Anchor = AnchorStyles.Left };

            statusLayout.Controls.Add(lblTotalScanned, 0, 0);
            statusLayout.Controls.Add(lblTotalFound, 1, 0);
            statusLayout.Controls.Add(lblTotalCompared, 2, 0);
            statusLayout.Controls.Add(lblTotalCopied, 3, 0);
            statusLayout.Controls.Add(lblElapsed, 4, 0);

            statusBar.Controls.Add(statusLayout);
        }

        // ---------- Event handlers & core flows ----------

        private void CbInputSource_SelectedIndexChanged(object sender, EventArgs e)
        {
            // If Clipboard selected, auto fill from clipboard if image available
            if (cbInputSource.SelectedItem.ToString() == "Clipboard Image")
            {
                if (Clipboard.ContainsImage())
                {
                    var bmp = Clipboard.GetImage();
                    var tmp = Path.Combine(Path.GetTempPath(), "pf_clip_" + Guid.NewGuid().ToString("N") + ".png");
                    bmp.Save(tmp);
                    txtInputFileOrImage.Text = tmp;
                }
                else
                {
                    MessageBox.Show("No image in clipboard.");
                }
            }
        }

        private void Txt_DragEnter(object? sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop)) e.Effect = DragDropEffects.Copy;
            else e.Effect = DragDropEffects.None;
        }

        private void TxtFolder_DragDrop(object? sender, DragEventArgs e)
        {
            var paths = (string[])e.Data.GetData(DataFormats.FileDrop);
            if (paths.Length > 0 && Directory.Exists(paths[0])) txtFolder.Text = paths[0];
        }

        private void TxtList_DragDrop(object? sender, DragEventArgs e)
        {
            var paths = (string[])e.Data.GetData(DataFormats.FileDrop);
            if (paths.Length > 0 && File.Exists(paths[0])) txtInputFileOrImage.Text = paths[0];
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

        private void BtnBrowseFolder_Click(object? sender, EventArgs e)
        {
            using var dlg = new FolderBrowserDialog();
            if (dlg.ShowDialog() == DialogResult.OK) txtFolder.Text = dlg.SelectedPath;
        }

        private void BtnBrowseInput_Click(object? sender, EventArgs e)
        {
            var source = cbInputSource.SelectedItem?.ToString() ?? "Text File";
            if (source == "Clipboard Image")
            {
                MessageBox.Show("Use the Clipboard Image option or paste an image into the clipboard before selecting this.");
                return;
            }
            if (source == "PDF")
            {
                using var dlg = new OpenFileDialog { Filter = "PDF files|*.pdf|All files|*.*" };
                if (dlg.ShowDialog() == DialogResult.OK) txtInputFileOrImage.Text = dlg.FileName;
                return;
            }
            using var open = new OpenFileDialog();
            if (source == "Text File") open.Filter = "Text files|*.txt|All files|*.*";
            else open.Filter = "Image files|*.png;*.jpg;*.jpeg;*.bmp;*.tif;*.tiff;*.webp|All files|*.*";
            if (open.ShowDialog() == DialogResult.OK) txtInputFileOrImage.Text = open.FileName;
        }

        private void BtnBrowseOutput_Click(object? sender, EventArgs e)
        {
            using var dlg = new FolderBrowserDialog();
            if (dlg.ShowDialog() == DialogResult.OK) txtOutputFolder.Text = dlg.SelectedPath;
        }

        private void BtnBrowseLowRes_Click(object? sender, EventArgs e)
        {
            using var dlg = new FolderBrowserDialog();
            if (dlg.ShowDialog() == DialogResult.OK) txtLowResFolder.Text = dlg.SelectedPath;
        }

        private void BtnBrowseHiRes_Click(object? sender, EventArgs e)
        {
            using var dlg = new FolderBrowserDialog();
            if (dlg.ShowDialog() == DialogResult.OK) txtHiResFolder.Text = dlg.SelectedPath;
        }

        private void BtnBrowseCompareOutput_Click(object? sender, EventArgs e)
        {
            using var dlg = new FolderBrowserDialog();
            if (dlg.ShowDialog() == DialogResult.OK) txtCompareOutputFolder.Text = dlg.SelectedPath;
        }

        // ---------------- Find flow (handles text-file input or OCR when image selected) ----------------
        private async Task FindPhotosButton_Click()
        {
            lstResults.Items.Clear();
            lblStatus.Text = "Preparing...";
            progressScanning.Value = 0;
            stopwatch.Reset();

            var folder = txtFolder.Text?.Trim();
            if (string.IsNullOrEmpty(folder) || !Directory.Exists(folder))
            {
                MessageBox.Show("Select a valid folder to search.");
                return;
            }

            // Build file list
            var exts = new[] { ".jpg", ".jpeg", ".png", ".bmp", ".gif", ".tiff", ".webp", ".cr3", ".arw", ".nef" };
            var searchOption = chkIncludeSubfolders.Checked ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly;
            List<string> allFiles;
            try
            {
                allFiles = Directory.EnumerateFiles(folder, "*.*", searchOption)
                    .Where(f => exts.Contains(Path.GetExtension(f).ToLowerInvariant())).ToList();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error enumerating files: " + ex.Message);
                return;
            }
            lblTotalScanned.Text = $"Total Scanned: {allFiles.Count}";
            stopwatch.Start();

            // Determine input source
            var inputSource = cbInputSource.SelectedItem?.ToString() ?? "Text File";
            List<string> tokens = new List<string>();

            if (inputSource == "Text File")
            {
                var path = txtInputFileOrImage.Text?.Trim();
                if (string.IsNullOrEmpty(path) || !File.Exists(path))
                {
                    MessageBox.Show("Select a valid text file.");
                    return;
                }
                string content;
                try { content = await Task.Run(() => File.ReadAllText(path)); } catch (Exception ex) { MessageBox.Show("Could not read file: " + ex.Message); return; }
                tokens = content.Split(new[] { ',', ';', '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries).Select(s => s.Trim()).Where(s => !string.IsNullOrEmpty(s)).ToList();
            }
            else if (inputSource == "Handwritten Photo" || inputSource == "Clipboard Image")
            {
                string imagePath = txtInputFileOrImage.Text?.Trim();
                if (inputSource == "Clipboard Image")
                {
                    if (!Clipboard.ContainsImage()) { MessageBox.Show("No image found in clipboard."); return; }
                    var bmp = Clipboard.GetImage();
                    var tmp = Path.Combine(Path.GetTempPath(), "pf_clip_" + Guid.NewGuid().ToString("N") + ".png");
                    bmp.Save(tmp);
                    imagePath = tmp;
                }
                if (string.IsNullOrEmpty(imagePath) || !File.Exists(imagePath)) { MessageBox.Show("Select a valid image file."); return; }

                lblStatus.Text = "Preprocessing image...";
                var pre = await Task.Run(() => OcrHelper.PreprocessImageToTemp(imagePath));

                lblStatus.Text = "Running OCR...";
                var ocrTokens = await Task.Run(() => OcrHelper.ExtractNumericTokensFromImage(pre, tessDataDir));

                // Show editor modal for user to accept/modify tokens (uncertain highlighted)
                using var editor = new OcrTokenEditorForm(ocrTokens, OcrHelper.DefaultConfidenceThreshold);
                if (editor.ShowDialog(this) != DialogResult.OK)
                {
                    lblStatus.Text = "OCR cancelled.";
                    try { File.Delete(pre); } catch { }
                    return;
                }
                tokens = editor.AcceptedTokens;

                try { File.Delete(pre); } catch { }
            }
            else if (inputSource == "PDF")
            {
                // Simple support not implemented in this file; you can add PDF rendering then run OCR on each page.
                MessageBox.Show("PDF OCR is not enabled in this build. Please use image or text file.");
                return;
            }

            if (tokens.Count == 0)
            {
                MessageBox.Show("No tokens to search (empty input).");
                return;
            }

            lblStatus.Text = $"Searching for {tokens.Count} tokens...";
            progressScanning.Value = 20;

            // Perform filename matching (search anywhere in filename or exact, based on checkboxes)
            List<string> matches = await Task.Run(() =>
            {
                // Build lookup: filename-without-ext -> path
                var lookup = allFiles.GroupBy(f => Path.GetFileNameWithoutExtension(f)).ToDictionary(g => g.Key, g => g.ToList(), StringComparer.OrdinalIgnoreCase);

                var matched = new List<string>();
                foreach (var token in tokens)
                {
                    var t = token.Trim();
                    if (string.IsNullOrEmpty(t)) continue;
                    // numeric tokens: match if filename contains token sequence anywhere (01265, IMG1265, etc)
                    var numeric = Regex.IsMatch(t, @"^\d+$");

                    foreach (var f in allFiles)
                    {
                        var nameOnly = Path.GetFileNameWithoutExtension(f);
                        if (chkSearchAnywhere.Checked)
                        {
                            if (nameOnly.IndexOf(t, StringComparison.OrdinalIgnoreCase) >= 0)
                            {
                                matched.Add(f);
                            }
                            else
                            {
                                // also check digit sequences inside name
                                var digitsOnly = Regex.Replace(nameOnly, @"\D", "");
                                if (!string.IsNullOrEmpty(digitsOnly) && digitsOnly.Contains(t)) matched.Add(f);
                            }
                        }
                        else
                        {
                            // exact match of filename (without extension) ignoring separators
                            if (string.Equals(Regex.Replace(nameOnly, @"\W", ""), Regex.Replace(t, @"\W", ""), StringComparison.OrdinalIgnoreCase))
                            {
                                matched.Add(f);
                            }
                        }
                    }
                }
                return matched.Distinct(StringComparer.OrdinalIgnoreCase).ToList();
            });

            progressScanning.Value = 100;
            stopwatch.Stop();
            lblElapsed.Text = $"Elapsed: {stopwatch.Elapsed.TotalSeconds:0.0}s";

            lstResults.Items.Clear();
            if (matches.Count == 0)
            {
                lstResults.Items.Add("No matches found.");
            }
            else
            {
                foreach (var m in matches) lstResults.Items.Add(m);
            }

            lblTotalFound.Text = $"Total Found: {matches.Count}";
            lblFindTotals.Text = $"{matches.Count} photos found";
            lblStatus.Text = $"Completed in {stopwatch.Elapsed.TotalSeconds:0.0}s";
        }

        // ---------------- Copy flows ----------------
        private async void BtnCopyPhotos_Click(object? sender, EventArgs e)
        {
            if (lstResults.Items.Count == 0) { MessageBox.Show("No matches to copy."); return; }
            var output = txtOutputFolder.Text?.Trim();
            if (string.IsNullOrEmpty(output) || !Directory.Exists(output))
            {
                var res = MessageBox.Show("No valid output folder chosen. Do you want to use Desktop as output?", "Output folder", MessageBoxButtons.YesNo);
                if (res == DialogResult.Yes) output = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);
                else return;
            }

            string subName;
            if (rbCandid.Checked) subName = "Candid photos";
            else if (rbTraditional.Checked) subName = "Traditional photos";
            else subName = string.IsNullOrWhiteSpace(txtOtherName.Text) ? "Other photos" : txtOtherName.Text.Trim();

            var outDir = Path.Combine(output, subName);
            Directory.CreateDirectory(outDir);

            var paths = lstResults.Items.Cast<object>().Select(x => x.ToString()).Where(s => !string.IsNullOrEmpty(s) && File.Exists(s)).ToList();
            if (paths.Count == 0) { MessageBox.Show("No real file paths found in results."); return; }

            progressCopying.Value = 0;
            int total = paths.Count;
            int copied = 0;
            for (int i = 0; i < total; i++)
            {
                var src = paths[i];
                var dest = Path.Combine(outDir, Path.GetFileName(src));
                try
                {
                    await Task.Run(() => File.Copy(src, dest, overwrite: true));
                    copied++;
                }
                catch (Exception ex)
                {
                    Debug.WriteLine("Copy failed: " + ex.Message);
                }
                progressCopying.Value = (int)((i + 1) * 100.0 / total);
                lblStatus.Text = $"Copying... {progressCopying.Value}% ({i + 1}/{total})";
                Application.DoEvents();
            }

            lblTotalCopied.Text = $"Copied: {copied}";
            lblStatus.Text = $"Copied {copied} files to: {outDir}";
            MessageBox.Show($"Copied {copied} files to:\n{outDir}");
            try { Process.Start(new ProcessStartInfo(outDir) { UseShellExecute = true }); } catch { }
        }

        // ---------------- Compare flows (basic, name+resolution matching) ----------------
        private async void BtnCompare_Click(object? sender, EventArgs e)
        {
            lstCompareResults.Items.Clear();
            lblCompareTotals.Text = "0 compared";
            progressCompare.Value = 0;

            var low = txtLowResFolder.Text?.Trim();
            var hi = txtHiResFolder.Text?.Trim();
            if (string.IsNullOrEmpty(low) || !Directory.Exists(low)) { MessageBox.Show("Select valid low-res folder."); return; }
            if (string.IsNullOrEmpty(hi) || !Directory.Exists(hi)) { MessageBox.Show("Select valid high-res folder."); return; }
            stopwatch.Restart();

            var exts = new[] { ".jpg", ".jpeg", ".png", ".bmp", ".gif", ".tiff", ".webp", ".cr3", ".arw", ".nef" };
            var lowFiles = Directory.EnumerateFiles(low, "*.*", SearchOption.AllDirectories).Where(f => exts.Contains(Path.GetExtension(f).ToLowerInvariant())).ToList();
            var hiFiles = Directory.EnumerateFiles(hi, "*.*", SearchOption.AllDirectories).Where(f => exts.Contains(Path.GetExtension(f).ToLowerInvariant())).ToList();

            var hiLookup = hiFiles.Select(f => new { Path = f, Name = Path.GetFileNameWithoutExtension(f) })
                .GroupBy(x => x.Name, StringComparer.OrdinalIgnoreCase)
                .ToDictionary(g => g.Key, g => g.Select(x => x.Path).ToList(), StringComparer.OrdinalIgnoreCase);

            var results = new List<(string low, string hi)>();
            int idx = 0;
            int total = lowFiles.Count;
            foreach (var lf in lowFiles)
            {
                idx++;
                var lowName = Path.GetFileNameWithoutExtension(lf);
                if (hiLookup.TryGetValue(lowName, out var candidates))
                {
                    foreach (var candidate in candidates)
                    {
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
                progressCompare.Value = (int)(idx * 100.0 / Math.Max(1, total));
                Application.DoEvents();
            }

            stopwatch.Stop();
            lstCompareResults.Items.Clear();
            foreach (var r in results) lstCompareResults.Items.Add($"{r.low} -> {r.hi}");
            if (results.Count == 0) lstCompareResults.Items.Add("No replacements found.");
            lblCompareTotals.Text = $"{results.Count} compared";
            lblTotalCompared.Text = $"Compared: {results.Count}";
            lblElapsed.Text = $"Elapsed: {stopwatch.Elapsed.TotalSeconds:0.0}s";
        }

        private async void BtnCopyCompareSelected_Click(object? sender, EventArgs e)
        {
            if (lstCompareResults.Items.Count == 0) { MessageBox.Show("No replacements to copy."); return; }
            var output = txtCompareOutputFolder.Text?.Trim(); if (string.IsNullOrEmpty(output) || !Directory.Exists(output))
            {
                var res = MessageBox.Show("No valid output folder chosen. Do you want to use Desktop as output?", "Output folder", MessageBoxButtons.YesNo);
                if (res == DialogResult.Yes) output = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);
                else return;
            }

            string subName = rbCompCandid?.Checked == true ? "Candid photos" : rbCompTraditional?.Checked == true ? "Traditional photos" : (string.IsNullOrWhiteSpace(txtCompOtherName?.Text) ? "Other photos" : txtCompOtherName.Text.Trim());
            var outDir = Path.Combine(output, subName);
            Directory.CreateDirectory(outDir);

            var hiPaths = new List<string>();
            foreach (var item in lstCompareResults.Items)
            {
                var s = item.ToString(); if (string.IsNullOrEmpty(s) || !s.Contains("->")) continue;
                var parts = s.Split(new[] { "->" }, StringSplitOptions.None).Select(p => p.Trim()).ToArray();
                if (parts.Length >= 2)
                {
                    var hi = parts[1]; if (File.Exists(hi)) hiPaths.Add(hi);
                }
            }

            if (hiPaths.Count == 0) { MessageBox.Show("No hi-res files found to copy."); return; }
            int total = hiPaths.Count; int copied = 0; progressCompare.Value = 0;
            for (int i = 0; i < total; i++)
            {
                var src = hiPaths[i]; try { var dest = Path.Combine(outDir, Path.GetFileName(src)); await Task.Run(() => File.Copy(src, dest, overwrite: true)); copied++; } catch { }
                progressCompare.Value = (int)((i + 1) * 100.0 / total); Application.DoEvents();
            }
            lblTotalCopied.Text = $"Copied: {copied}";
            MessageBox.Show($"Copied {copied} files to:\n{outDir}");
        }

        // ---------- UI helpers ----------
        private TextBox StyledTextBox()
        {
            return new TextBox { Dock = DockStyle.Fill, BorderStyle = BorderStyle.FixedSingle, BackColor = Color.WhiteSmoke };
        }

        private Button RoundedButton(string text)
        {
            var b = new Button { Text = text, AutoSize = true, Padding = new Padding(8), FlatStyle = FlatStyle.Flat };
            b.FlatAppearance.BorderSize = 0;
            b.BackColor = Color.FromArgb(0, 120, 215);
            b.ForeColor = Color.White;
            b.MouseEnter += (s, e) => b.BackColor = ControlPaint.Light(b.BackColor);
            b.MouseLeave += (s, e) => b.BackColor = Color.FromArgb(0, 120, 215);
            return b;
        }

        private void ApplyTheme()
        {
            if (darkTheme)
            {
                BackColor = Color.FromArgb(18, 24, 32);
                ForeColor = Color.FromArgb(220, 220, 220);
                btnThemeToggle.Text = "Light Mode";
                // Apply dark colors recursively in a simple way
                foreach (Control c in Controls) ApplyDark(c);
            }
            else
            {
                BackColor = SystemColors.Control;
                ForeColor = SystemColors.ControlText;
                btnThemeToggle.Text = "Dark Mode";
                foreach (Control c in Controls) ApplyLight(c);
            }
        }

        private void ApplyDark(Control c)
        {
            try
            {
                if (c is TextBox || c is ComboBox) { c.BackColor = Color.FromArgb(30, 34, 40); c.ForeColor = Color.White; }
                else if (c is ListBox) { c.BackColor = Color.FromArgb(20, 24, 30); c.ForeColor = Color.White; }
                else if (c is Button) { /* many buttons are colored already */ }
                else { c.BackColor = Color.FromArgb(18, 24, 32); c.ForeColor = Color.White; }
            }
            catch { }
            foreach (Control child in c.Controls) ApplyDark(child);
        }

        private void ApplyLight(Control c)
        {
            try
            {
                if (c is TextBox || c is ComboBox) { c.BackColor = Color.White; c.ForeColor = Color.Black; }
                else if (c is ListBox) { c.BackColor = Color.White; c.ForeColor = Color.Black; }
                else { c.BackColor = SystemColors.Control; c.ForeColor = SystemColors.ControlText; }
            }
            catch { }
            foreach (Control child in c.Controls) ApplyLight(child);
        }
    }

    // ----------------- OcrHelper class -----------------
    // Preprocessing using OpenCvSharp and OCR using Tesseract
    public record OcrToken(string Token, float Confidence);

    public static class OcrHelper
    {
        public static float DefaultConfidenceThreshold { get; set; } = 0.85f;

        public static string PreprocessImageToTemp(string imagePath)
        {
            var mat = Cv2.ImRead(imagePath, ImreadModes.Color);
            if (mat.Empty()) throw new FileNotFoundException("Could not read image: " + imagePath);

            double scale = mat.Width < 1000 ? 2.0 : 1.0;
            if (scale != 1.0)
            {
                var tmp = new Mat();
                Cv2.Resize(mat, tmp, new OpenCvSharp.Size(0, 0), fx: scale, fy: scale, interpolation: InterpolationFlags.Lanczos4);
                mat.Dispose();
                mat = tmp;
            }

            var gray = new Mat();
            Cv2.CvtColor(mat, gray, ColorConversionCodes.BGR2GRAY);

            var denoised = new Mat();
            Cv2.FastNlMeansDenoising(gray, denoised, h: 30);

            var clahe = Cv2.CreateCLAHE(clipLimit: 2.0, tileGridSize: new OpenCvSharp.Size(8, 8));
            var enhanced = new Mat();
            clahe.Apply(denoised, enhanced);

            var kernel = Cv2.GetStructuringElement(MorphShapes.Rect, new OpenCvSharp.Size(25, 25));
            var background = new Mat();
            Cv2.MorphologyEx(enhanced, background, MorphTypes.Open, kernel);
            var diff = new Mat();
            Cv2.Absdiff(enhanced, background, diff);

            var binary = new Mat();
            Cv2.AdaptiveThreshold(diff, binary, maxValue: 255, adaptiveMethod: AdaptiveThresholdTypes.GaussianC,
                thresholdType: ThresholdTypes.Binary, blockSize: 25, C: 10);

            double angle = EstimateSkewAngle(binary);
            Mat final = binary;
            if (Math.Abs(angle) > 0.5)
            {
                var center = new OpenCvSharp.Point2f(binary.Width / 2f, binary.Height / 2f);
                var rot = Cv2.GetRotationMatrix2D(center, angle, 1.0);
                var rotated = new Mat();
                Cv2.WarpAffine(binary, rotated, rot, binary.Size(), InterpolationFlags.Linear, BorderTypes.Replicate);
                final = rotated;
            }

            var tmpPath = Path.Combine(Path.GetTempPath(), "pf_ocr_" + Guid.NewGuid().ToString("N") + ".png");
            Cv2.ImWrite(tmpPath, final);

            try { mat.Dispose(); gray.Dispose(); denoised.Dispose(); enhanced.Dispose(); background.Dispose(); diff.Dispose(); if (final != binary) binary.Dispose(); }
            catch { }

            return tmpPath;
        }

        private static double EstimateSkewAngle(Mat binary)
        {
            try
            {
                var edges = new Mat();
                Cv2.Canny(binary, edges, 50, 150);
                var lines = Cv2.HoughLinesP(edges, rho: 1, theta: Math.PI / 180, threshold: 100, minLineLength: binary.Width / 6, maxLineGap: 20);
                if (lines == null || lines.Length == 0) return 0;
                var angles = new List<double>();
                foreach (var l in lines)
                {
                    double a = Math.Atan2(l.P2.Y - l.P1.Y, l.P2.X - l.P1.X) * 180.0 / Math.PI;
                    if (Math.Abs(a) <= 45) angles.Add(a);
                }
                if (angles.Count == 0) return 0;
                return angles.Average();
            }
            catch { return 0; }
        }

        public static List<OcrToken> ExtractNumericTokensFromImage(string imagePath, string tessDataPath, float minConfidence = -1f)
        {
            if (minConfidence <= 0) minConfidence = DefaultConfidenceThreshold;
            var result = new List<OcrToken>();

            using var engine = new TesseractEngine(tessDataPath, "eng", EngineMode.Default);
            using var img = Pix.LoadFromFile(imagePath);
            using var page = engine.Process(img);
            using var iter = page.GetIterator();
            iter.Begin();

            do
            {
                if (!iter.IsAtBeginningOf(PageIteratorLevel.Word)) continue;
                string word = iter.GetText(PageIteratorLevel.Word) ?? "";
                float conf = iter.GetConfidence(PageIteratorLevel.Word); // 0..100
                if (string.IsNullOrWhiteSpace(word)) continue;

                var matches = Regex.Matches(word, @"\d+");
                foreach (Match m in matches)
                {
                    var token = m.Value;
                    var conf01 = conf / 100f;
                    result.Add(new OcrToken(token, conf01));
                }
            } while (iter.Next(PageIteratorLevel.Word));

            var best = result.GroupBy(r => r.Token).Select(g => new OcrToken(g.Key, g.Max(x => x.Confidence))).ToList();
            return best.OrderByDescending(t => t.Confidence).ToList();
        }

        public static List<string> FindFilesMatchingTokens(IEnumerable<string> files, IEnumerable<string> tokens)
        {
            var tokenList = tokens.Where(t => !string.IsNullOrWhiteSpace(t)).ToList();
            if (!tokenList.Any()) return new List<string>();

            var matches = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var f in files)
            {
                var name = Path.GetFileNameWithoutExtension(f);
                if (string.IsNullOrEmpty(name)) continue;

                var seqs = Regex.Matches(name, @"\d+").Cast<Match>().Select(m => m.Value).ToList();
                var digitsOnly = Regex.Replace(name, @"\D", "");

                foreach (var token in tokenList)
                {
                    if (seqs.Any(s => s.Contains(token)))
                    {
                        matches.Add(f); break;
                    }

                    if (!string.IsNullOrEmpty(digitsOnly) && digitsOnly.Contains(token))
                    {
                        matches.Add(f); break;
                    }

                    if (name.IndexOf(token, StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        matches.Add(f); break;
                    }
                }
            }

            return matches.ToList();
        }
    }

    // ----------------- OcrTokenEditorForm -----------------
    public class OcrTokenEditorForm : Form
    {
        private DataGridView grid;
        private Button btnOk;
        private Button btnCancel;
        private Button btnSelectAll;
        private Button btnClearAll;
        public List<string> AcceptedTokens { get; private set; } = new List<string>();

        public OcrTokenEditorForm(IEnumerable<OcrToken> tokens, float confidenceThreshold = 0.85f)
        {
            Text = "OCR - Confirm tokens";
            Width = 620; Height = 420;
            StartPosition = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MinimizeBox = false; MaximizeBox = false;

            var panel = new TableLayoutPanel { Dock = DockStyle.Fill, Padding = new Padding(8), RowCount = 3 };
            panel.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            panel.RowStyles.Add(new RowStyle(SizeType.Absolute, 40));
            panel.RowStyles.Add(new RowStyle(SizeType.Absolute, 44));

            grid = new DataGridView { Dock = DockStyle.Fill, AllowUserToAddRows = false, RowHeadersVisible = false, SelectionMode = DataGridViewSelectionMode.FullRowSelect, AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill };
            grid.Columns.Add(new DataGridViewCheckBoxColumn { HeaderText = "Use", Width = 46 });
            grid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Token", Width = 120 });
            grid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Confidence (%)", ReadOnly = true, Width = 120 });

            foreach (var t in tokens)
            {
                int idx = grid.Rows.Add();
                var row = grid.Rows[idx];
                bool uncertain = t.Confidence < confidenceThreshold;
                row.Cells[0].Value = !uncertain;
                row.Cells[1].Value = t.Token;
                row.Cells[2].Value = (t.Confidence * 100).ToString("0");
                if (uncertain) row.DefaultCellStyle.BackColor = Color.FromArgb(255, 240, 200);
            }

            var toolRow = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.LeftToRight };
            btnSelectAll = new Button { Text = "Select All", AutoSize = true }; btnClearAll = new Button { Text = "Clear All", AutoSize = true };
            btnSelectAll.Click += (s, e) => { foreach (DataGridViewRow r in grid.Rows) r.Cells[0].Value = true; };
            btnClearAll.Click += (s, e) => { foreach (DataGridViewRow r in grid.Rows) r.Cells[0].Value = false; };
            toolRow.Controls.Add(btnSelectAll); toolRow.Controls.Add(btnClearAll);

            var bottom = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.RightToLeft };
            btnOk = new Button { Text = "Search", AutoSize = true };
            btnCancel = new Button { Text = "Cancel", AutoSize = true };
            btnOk.Click += BtnOk_Click;
            btnCancel.Click += (s, e) => { DialogResult = DialogResult.Cancel; Close(); };
            bottom.Controls.Add(btnOk); bottom.Controls.Add(btnCancel);

            panel.Controls.Add(grid, 0, 0);
            panel.Controls.Add(toolRow, 0, 1);
            panel.Controls.Add(bottom, 0, 2);

            Controls.Add(panel);
        }

        private void BtnOk_Click(object? sender, EventArgs e)
        {
            var list = new List<string>();
            foreach (DataGridViewRow r in grid.Rows)
            {
                var use = Convert.ToBoolean(r.Cells[0].Value ?? false);
                var token = Convert.ToString(r.Cells[1].Value) ?? "";
                if (use && !string.IsNullOrWhiteSpace(token)) list.Add(token.Trim());
            }
            AcceptedTokens = list.Distinct().ToList();
            DialogResult = DialogResult.OK;
            Close();
        }
    }
}

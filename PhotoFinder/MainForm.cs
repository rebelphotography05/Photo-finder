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
        // Common controls (Find tab)
        private TextBox txtFolder;
        private Button btnBrowseFolder;
        private TextBox txtListFile;
        private Button btnBrowseList;
        private Button btnFind;
        private ListBox lstResults;
        private Button btnOpenSelected;
        private Button btnCopyAll;
        private Label lblStatus;
        private CheckBox chkIncludeSubfolders;
        private CheckBox chkSearchAnywhere;
        private CheckBox chkPartialNumericOnly;
        private ProgressBar progressScanning;
        private ProgressBar progressCopying;
        private TextBox txtOutputFolder;
        private Button btnBrowseOutput;
        private TabControl mainTabs;

        // Compare tab controls
        private TextBox txtLowResFolder;
        private Button btnBrowseLowRes;
        private TextBox txtHiResFolder;
        private Button btnBrowseHiRes;
        private Button btnCompare;
        private ListBox lstCompareResults;
        private Label lblCompareStatus;

        public MainForm()
        {
            // Window title as requested
            Text = "Rebel Photo Finder";

            // Size & modern-ish defaults
            Width = 980;
            Height = 640;
            StartPosition = FormStartPosition.CenterScreen;
            Font = new Font("Segoe UI", 10F, FontStyle.Regular, GraphicsUnit.Point);
            BackColor = Color.FromArgb(250, 250, 250);

            // Make window resizable for new layout
            FormBorderStyle = FormBorderStyle.Sizable;
            InitializeComponents();

            // Allow drag & drop at the form-level for convenience
            AllowDrop = true;
            DragEnter += MainForm_DragEnter;
            DragDrop += MainForm_DragDrop;
        }

        private void InitializeComponents()
        {
            // Main tab control
            mainTabs = new TabControl()
            {
                Dock = DockStyle.Fill,
                Appearance = TabAppearance.Normal,
                Padding = new Point(12, 6)
            };

            // Find Tab
            var tabFind = new TabPage("Find (from text)");
            tabFind.Padding = new Padding(12);
            tabFind.BackColor = Color.White;
            tabFind.Controls.Add(BuildFindPanel());
            mainTabs.TabPages.Add(tabFind);

            // Compare Tab
            var tabCompare = new TabPage("Compare Photos");
            tabCompare.Padding = new Padding(12);
            tabCompare.BackColor = Color.White;
            tabCompare.Controls.Add(BuildComparePanel());
            mainTabs.TabPages.Add(tabCompare);

            Controls.Add(mainTabs);
        }

        private Control BuildFindPanel()
        {
            var main = new TableLayoutPanel()
            {
                Dock = DockStyle.Fill,
                ColumnCount = 3,
                RowCount = 10,
                BackColor = Color.White,
                Padding = new Padding(8),
                AutoSize = true
            };

            main.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 55));
            main.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 30));
            main.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 15));

            // Rows heights (mix of fixed and flexible)
            for (int i = 0; i < 8; i++) main.RowStyles.Add(new RowStyle(SizeType.Absolute, 44));
            main.RowStyles.Add(new RowStyle(SizeType.Percent, 100)); // results
            main.RowStyles.Add(new RowStyle(SizeType.Absolute, 60)); // bottom action bar

            // Folder row
            var lblFolder = new Label() { Text = "Folder to search:", AutoSize = true, Anchor = AnchorStyles.Left };
            txtFolder = new TextBox() { Anchor = AnchorStyles.Left | AnchorStyles.Right };
            btnBrowseFolder = new Button() { Text = "Browse", AutoSize = true };
            btnBrowseFolder.Click += BtnBrowseFolder_Click;

            main.Controls.Add(lblFolder, 0, 0);
            main.SetColumnSpan(lblFolder, 3);

            main.Controls.Add(txtFolder, 0, 1);
            main.SetColumnSpan(txtFolder, 2);
            main.Controls.Add(btnBrowseFolder, 2, 1);

            // List file row
            var lblList = new Label() { Text = "Text file with names (comma, newline or semicolon):", AutoSize = true, Anchor = AnchorStyles.Left };
            txtListFile = new TextBox() { Anchor = AnchorStyles.Left | AnchorStyles.Right };
            btnBrowseList = new Button() { Text = "Browse", AutoSize = true };
            btnBrowseList.Click += BtnBrowseList_Click;

            main.Controls.Add(lblList, 0, 2);
            main.SetColumnSpan(lblList, 3);

            main.Controls.Add(txtListFile, 0, 3);
            main.SetColumnSpan(txtListFile, 2);
            main.Controls.Add(btnBrowseList, 2, 3);

            // Options row (subfolders / anywhere / partial numeric)
            var optsPanel = new FlowLayoutPanel() { Dock = DockStyle.Fill, FlowDirection = FlowDirection.LeftToRight, AutoSize = true };
            chkIncludeSubfolders = new CheckBox() { Text = "Include subfolders", Checked = true, AutoSize = true };
            chkSearchAnywhere = new CheckBox() { Text = "Search anywhere in filename", Checked = true, AutoSize = true };
            chkPartialNumericOnly = new CheckBox() { Text = "Partial match (numbers only)", Checked = true, AutoSize = true };

            optsPanel.Controls.Add(chkIncludeSubfolders);
            optsPanel.Controls.Add(chkSearchAnywhere);
            optsPanel.Controls.Add(chkPartialNumericOnly);

            main.Controls.Add(optsPanel, 0, 4);
            main.SetColumnSpan(optsPanel, 3);

            // Output folder selection
            var lblOut = new Label() { Text = "Output folder for matches:", AutoSize = true, Anchor = AnchorStyles.Left };
            txtOutputFolder = new TextBox() { Anchor = AnchorStyles.Left | AnchorStyles.Right };
            btnBrowseOutput = new Button() { Text = "Browse", AutoSize = true };
            btnBrowseOutput.Click += BtnBrowseOutput_Click;

            main.Controls.Add(lblOut, 0, 5);
            main.SetColumnSpan(lblOut, 3);

            main.Controls.Add(txtOutputFolder, 0, 6);
            main.SetColumnSpan(txtOutputFolder, 2);
            main.Controls.Add(btnBrowseOutput, 2, 6);

            // Find button and scanning progress
            btnFind = new Button() { Text = "Find Photos", AutoSize = true, Width = 140 };
            btnFind.Click += BtnFind_Click;

            progressScanning = new ProgressBar() { Anchor = AnchorStyles.Left | AnchorStyles.Right, Minimum = 0, Maximum = 100, Height = 18 };

            main.Controls.Add(progressScanning, 0, 7);
            main.SetColumnSpan(progressScanning, 2);
            main.Controls.Add(btnFind, 2, 7);

            // Results list
            lstResults = new ListBox() { Dock = DockStyle.Fill };
            main.Controls.Add(lstResults, 0, 8);
            main.SetColumnSpan(lstResults, 3);

            // Bottom action bar: status, copy progress, buttons
            var bottomPanel = new TableLayoutPanel() { Dock = DockStyle.Fill, ColumnCount = 3 };
            bottomPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 60));
            bottomPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25));
            bottomPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 15));

            lblStatus = new Label() { Text = "", Anchor = AnchorStyles.Left, AutoSize = true };
            progressCopying = new ProgressBar() { Anchor = AnchorStyles.Left | AnchorStyles.Right, Minimum = 0, Maximum = 100, Height = 14 };

            var actionsFlow = new FlowLayoutPanel() { Dock = DockStyle.Fill, FlowDirection = FlowDirection.RightToLeft, AutoSize = true };
            btnOpenSelected = new Button() { Text = "Open Selected", AutoSize = true };
            btnOpenSelected.Click += BtnOpenSelected_Click;
            btnCopyAll = new Button() { Text = "Copy Selected/All", AutoSize = true };
            btnCopyAll.Click += BtnCopyAll_Click;

            actionsFlow.Controls.Add(btnOpenSelected);
            actionsFlow.Controls.Add(btnCopyAll);

            bottomPanel.Controls.Add(lblStatus, 0, 0);
            bottomPanel.Controls.Add(progressCopying, 1, 0);
            bottomPanel.Controls.Add(actionsFlow, 2, 0);

            main.Controls.Add(bottomPanel, 0, 9);
            main.SetColumnSpan(bottomPanel, 3);

            // Enable drag on important textboxes
            txtFolder.AllowDrop = true;
            txtListFile.AllowDrop = true;
            txtOutputFolder.AllowDrop = true;

            txtFolder.DragEnter += Txt_DragEnter;
            txtFolder.DragDrop += TxtFolder_DragDrop;
            txtListFile.DragEnter += Txt_DragEnter;
            txtListFile.DragDrop += TxtList_DragDrop;
            txtOutputFolder.DragEnter += Txt_DragEnter;
            txtOutputFolder.DragDrop += TxtOutput_DragDrop;

            return main;
        }

        private Control BuildComparePanel()
        {
            var main = new TableLayoutPanel()
            {
                Dock = DockStyle.Fill,
                ColumnCount = 3,
                RowCount = 6,
                Padding = new Padding(8),
                BackColor = Color.White
            };

            main.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 55));
            main.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 30));
            main.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 15));

            for (int i = 0; i < 4; i++) main.RowStyles.Add(new RowStyle(SizeType.Absolute, 44));
            main.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            main.RowStyles.Add(new RowStyle(SizeType.Absolute, 32));

            // LowRes folder
            var lblLow = new Label() { Text = "Low-res photos (source):", AutoSize = true, Anchor = AnchorStyles.Left };
            txtLowResFolder = new TextBox() { Anchor = AnchorStyles.Left | AnchorStyles.Right };
            btnBrowseLowRes = new Button() { Text = "Browse", AutoSize = true };
            btnBrowseLowRes.Click += BtnBrowseLowRes_Click;
            main.Controls.Add(lblLow, 0, 0);
            main.SetColumnSpan(lblLow, 3);
            main.Controls.Add(txtLowResFolder, 0, 1);
            main.SetColumnSpan(txtLowResFolder, 2);
            main.Controls.Add(btnBrowseLowRes, 2, 1);

            // HiRes folder
            var lblHi = new Label() { Text = "HiRes Photos folder (target, must be named 'HiRes Photos'):", AutoSize = true, Anchor = AnchorStyles.Left };
            txtHiResFolder = new TextBox() { Anchor = AnchorStyles.Left | AnchorStyles.Right };
            btnBrowseHiRes = new Button() { Text = "Browse", AutoSize = true };
            btnBrowseHiRes.Click += BtnBrowseHiRes_Click;
            main.Controls.Add(lblHi, 0, 2);
            main.SetColumnSpan(lblHi, 3);
            main.Controls.Add(txtHiResFolder, 0, 3);
            main.SetColumnSpan(txtHiResFolder, 2);
            main.Controls.Add(btnBrowseHiRes, 2, 3);

            // Compare button
            btnCompare = new Button() { Text = "Compare and Find Replacements", AutoSize = true, Width = 260 };
            btnCompare.Click += BtnCompare_Click;
            main.Controls.Add(btnCompare, 2, 4);

            lstCompareResults = new ListBox() { Dock = DockStyle.Fill };
            main.Controls.Add(lstCompareResults, 0, 5);
            main.SetColumnSpan(lstCompareResults, 3);

            lblCompareStatus = new Label() { Text = "", Anchor = AnchorStyles.Left, AutoSize = true };
            main.Controls.Add(lblCompareStatus, 0, 6);
            main.SetColumnSpan(lblCompareStatus, 3);

            // Drag & drop
            txtLowResFolder.AllowDrop = true;
            txtHiResFolder.AllowDrop = true;
            txtLowResFolder.DragEnter += Txt_DragEnter;
            txtLowResFolder.DragDrop += TxtLowRes_DragDrop;
            txtHiResFolder.DragEnter += Txt_DragEnter;
            txtHiResFolder.DragDrop += TxtHiRes_DragDrop;

            return main;
        }

        // ------------------------------
        // Drag & Drop handlers (form-level)
        // ------------------------------
        private void MainForm_DragEnter(object sender, DragEventArgs e)
        {
            Txt_DragEnter(sender, e);
        }

        private void MainForm_DragDrop(object sender, DragEventArgs e)
        {
            // If a folder is dropped onto the form, fill the main folder textbox
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                var paths = (string[])e.Data.GetData(DataFormats.FileDrop);
                if (paths.Length > 0)
                {
                    var first = paths[0];
                    if (Directory.Exists(first))
                    {
                        txtFolder.Text = first;
                    }
                    else if (File.Exists(first) && Path.GetExtension(first).Equals(".txt", StringComparison.OrdinalIgnoreCase))
                    {
                        txtListFile.Text = first;
                    }
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
            if (paths.Length > 0 && File.Exists(paths[0]) && Path.GetExtension(paths[0]).Equals(".txt", StringComparison.OrdinalIgnoreCase))
                txtListFile.Text = paths[0];
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

        // ------------------------------
        // Browse buttons
        // ------------------------------
        private void BtnBrowseFolder_Click(object? sender, EventArgs e)
        {
            using var dlg = new FolderBrowserDialog();
            if (dlg.ShowDialog() == DialogResult.OK) txtFolder.Text = dlg.SelectedPath;
        }

        private void BtnBrowseList_Click(object? sender, EventArgs e)
        {
            using var dlg = new OpenFileDialog();
            dlg.Filter = "Text files|*.txt|All files|*.*";
            if (dlg.ShowDialog() == DialogResult.OK) txtListFile.Text = dlg.FileName;
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

        // ------------------------------
        // Find logic (async & progress)
        // ------------------------------
        private async void BtnFind_Click(object? sender, EventArgs e)
        {
            lstResults.Items.Clear();
            lblStatus.Text = "";
            progressScanning.Value = 0;
            progressCopying.Value = 0;

            var folder = txtFolder.Text?.Trim();
            var listFile = txtListFile.Text?.Trim();
            var output = txtOutputFolder.Text?.Trim();

            if (string.IsNullOrEmpty(folder) || !Directory.Exists(folder))
            {
                MessageBox.Show("Please select a valid folder to search.", "Folder not found", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (string.IsNullOrEmpty(listFile) || !File.Exists(listFile))
            {
                MessageBox.Show("Please select a valid text file containing photo names.", "List file not found", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (string.IsNullOrEmpty(output) || !Directory.Exists(output))
            {
                var res = MessageBox.Show("No valid output folder chosen. Do you want to use Desktop as output?", "Output folder", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
                if (res == DialogResult.Yes) output = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);
                else return;
            }

            string content;
            try
            {
                content = await Task.Run(() => File.ReadAllText(listFile));
            }
            catch (Exception ex)
            {
                MessageBox.Show("Could not read list file: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            var tokens = content.Split(new[] { ',', ';', '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(s => s.Trim()).Where(s => !string.IsNullOrEmpty(s)).ToList();

            if (tokens.Count == 0)
            {
                MessageBox.Show("No names found in the text file.", "No names", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            // Supported image extensions
            var exts = new[] { ".jpg", ".jpeg", ".png", ".bmp", ".gif", ".tiff", ".webp" };

            // Determine search option
            var searchOption = chkIncludeSubfolders.Checked ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly;

            lblStatus.Text = "Scanning files...";
            var progress = new Progress<int>(p => {
                progressScanning.Value = Math.Min(100, p);
                lblStatus.Text = $"Scanning... {p}%";
            });

            try
            {
                // Gather all image files first (we report progress across this enumeration)
                var allFiles = Directory.EnumerateFiles(folder, "*.*", searchOption)
                    .Where(f => exts.Contains(Path.GetExtension(f).ToLowerInvariant()))
                    .ToList();

                int total = allFiles.Count;
                if (total == 0)
                {
                    MessageBox.Show("No image files found in the selected folder.", "No images", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    lblStatus.Text = "No images found.";
                    progressScanning.Value = 0;
                    return;
                }

                // Build lookup for quick searches: filename-without-ext -> file paths
                var lookup = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);
                int idx = 0;
                foreach (var f in allFiles)
                {
                    idx++;
                    int pct = (int)(idx * 100.0 / total);
                    (progress as IProgress<int>).Report(pct);

                    var key = Path.GetFileNameWithoutExtension(f);
                    if (!lookup.TryGetValue(key, out var list)) {
                        list = new List<string>();
                        lookup[key] = list;
                    }
                    list.Add(f);
                }

                // Search tokens and respect options
                var matches = new List<string>();
                foreach (var token in tokens)
                {
                    // Normalize token
                    var t = token.Trim();
                    if (string.IsNullOrEmpty(t)) continue;

                    // If partial numeric-only matching is enabled and token looks numeric, we match if filename contains the numeric sequence
                    bool tokenIsNumeric = Regex.IsMatch(t, @"^\d+$");

                    if (tokenIsNumeric && chkPartialNumericOnly.Checked)
                    {
                        // Find any file whose filename (without ext) contains the token
                        foreach (var kv in lookup)
                        {
                            if (kv.Key.IndexOf(t, StringComparison.OrdinalIgnoreCase) >= 0)
                                matches.AddRange(kv.Value);
                        }
                    }
                    else if (chkSearchAnywhere.Checked)
                    {
                        // Search anywhere in the filename (with extension stripped)
                        foreach (var kv in lookup)
                        {
                            if (kv.Key.IndexOf(t, StringComparison.OrdinalIgnoreCase) >= 0)
                                matches.AddRange(kv.Value);
                        }
                    }
                    else
                    {
                        // Exact filename match (without extension)
                        if (lookup.TryGetValue(Path.GetFileNameWithoutExtension(t), out var list))
                            matches.AddRange(list);
                    }
                }

                // Remove duplicates and add to UI
                var distinct = matches.Distinct(StringComparer.OrdinalIgnoreCase).ToList();
                foreach (var m in distinct)
                {
                    lstResults.Items.Add(m);
                }

                lblStatus.Text = $"Found {distinct.Count} matching files.";
                progressScanning.Value = 100;

                if (distinct.Count == 0)
                {
                    MessageBox.Show("No matching photos were found.", "No matches", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error during search: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                lblStatus.Text = "Error";
            }
        }

        // ------------------------------
        // Open and Copy actions with progress
        // ------------------------------
        private void BtnOpenSelected_Click(object? sender, EventArgs e)
        {
            if (lstResults.SelectedItem == null)
            {
                MessageBox.Show("Please select an item from the results.", "No selection", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            var path = lstResults.SelectedItem.ToString();
            try
            {
                var psi = new ProcessStartInfo(path!) { UseShellExecute = true };
                Process.Start(psi);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Could not open file: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private async void BtnCopyAll_Click(object? sender, EventArgs e)
        {
            if (lstResults.Items.Count == 0)
            {
                MessageBox.Show("No matches to copy.", "Nothing to copy", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            var output = txtOutputFolder.Text?.Trim();
            if (string.IsNullOrEmpty(output) || !Directory.Exists(output))
            {
                var res = MessageBox.Show("No valid output folder chosen. Do you want to use Desktop as output?", "Output folder", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
                if (res == DialogResult.Yes) output = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);
                else return;
            }

            // Create a timestamped subfolder inside chosen output for clarity
            var outDir = Path.Combine(output, "PhotoFinder_Matches_" + DateTime.Now.ToString("yyyyMMdd_HHmmss"));
            Directory.CreateDirectory(outDir);

            int total = lstResults.Items.Count;
            int copied = 0;
            progressCopying.Value = 0;

            for (int i = 0; i < total; i++)
            {
                var src = lstResults.Items[i]?.ToString();
                if (string.IsNullOrEmpty(src)) continue;
                try
                {
                    var dest = Path.Combine(outDir, Path.GetFileName(src));
                    await Task.Run(() => File.Copy(src!, dest, overwrite: true));
                    copied++;
                }
                catch (Exception ex)
                {
                    Debug.WriteLine("Copy failed: " + ex.Message);
                    // continue copying others
                }
                progressCopying.Value = (int)((i + 1) * 100.0 / total);
                lblStatus.Text = $"Copying... {progressCopying.Value}% ({i + 1}/{total})";
                Application.DoEvents();
            }

            lblStatus.Text = $"Copied {copied} of {total} files to: {outDir}";
            try { Process.Start(new ProcessStartInfo(outDir) { UseShellExecute = true }); } catch { }
            MessageBox.Show($"Copied {copied} files to:\n{outDir}", "Copy complete", MessageBoxButtons.OK, MessageBoxIcon.Information);
            progressCopying.Value = 100;
        }

        // ------------------------------
        // Compare tab logic
        // ------------------------------
        private async void BtnCompare_Click(object? sender, EventArgs e)
        {
            lstCompareResults.Items.Clear();
            lblCompareStatus.Text = "";
            var low = txtLowResFolder.Text?.Trim();
            var hi = txtHiResFolder.Text?.Trim();

            if (string.IsNullOrEmpty(low) || !Directory.Exists(low))
            {
                MessageBox.Show("Please select a valid Low-res folder.", "Low-res folder missing", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            if (string.IsNullOrEmpty(hi) || !Directory.Exists(hi))
            {
                MessageBox.Show("Please select a valid HiRes Photos folder.", "Hi-res folder missing", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            // Enforce the HiRes folder name, warn the user (but allow)
            if (!string.Equals(Path.GetFileName(hi), "HiRes Photos", StringComparison.OrdinalIgnoreCase))
            {
                var r = MessageBox.Show("The selected Hi-res folder is not named 'HiRes Photos'. Continue anyway?", "Folder name mismatch", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
                if (r == DialogResult.No) return;
            }

            lblCompareStatus.Text = "Comparing...";
            await Task.Run(() => CompareByNameAndResolution(low, hi));
            lblCompareStatus.Text = "Compare complete.";
        }

        private void CompareByNameAndResolution(string lowFolder, string hiFolder)
        {
            // Collect low-res files
            var exts = new[] { ".jpg", ".jpeg", ".png", ".bmp", ".gif", ".tiff", ".webp" };
            var lowFiles = Directory.EnumerateFiles(lowFolder, "*.*", SearchOption.AllDirectories)
                .Where(f => exts.Contains(Path.GetExtension(f).ToLowerInvariant()))
                .ToList();

            var hiFiles = Directory.EnumerateFiles(hiFolder, "*.*", SearchOption.AllDirectories)
                .Where(f => exts.Contains(Path.GetExtension(f).ToLowerInvariant()))
                .Select(f => new { Path = f, Name = Path.GetFileNameWithoutExtension(f) })
                .ToDictionary(x => x.Name, x => x.Path, StringComparer.OrdinalIgnoreCase);

            var results = new List<string>();

            foreach (var lf in lowFiles)
            {
                var lowName = Path.GetFileNameWithoutExtension(lf);
                if (hiFiles.TryGetValue(lowName, out var candidateHi))
                {
                    // Compare resolution: if hi image is larger in pixels, consider it a replacement candidate
                    try
                    {
                        using var imgLow = Image.FromFile(lf);
                        using var imgHi = Image.FromFile(candidateHi);
                        if (imgHi.Width >= imgLow.Width && imgHi.Height >= imgLow.Height)
                        {
                            results.Add($"{Path.GetFileName(lf)}  ->  {Path.GetFileName(candidateHi)}");
                        }
                    }
                    catch
                    {
                        // if image load fails, fallback to name match only
                        results.Add($"{Path.GetFileName(lf)}  ->  {Path.GetFileName(candidateHi)}");
                    }
                }
            }

            // Post results to UI thread
            BeginInvoke(new Action(() => {
                foreach (var r in results) lstCompareResults.Items.Add(r);
                if (results.Count == 0) lstCompareResults.Items.Add("No replacements found.");
            }));
        }
    }
}

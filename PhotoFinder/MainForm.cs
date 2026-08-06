using System;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using System.Diagnostics;

namespace PhotoFinder
{
    public class MainForm : Form
    {
        private TextBox txtFolder;
        private Button btnBrowseFolder;
        private TextBox txtListFile;
        private Button btnBrowseList;
        private Button btnFind;
        private ListBox lstResults;
        private Button btnOpenSelected;
        private Button btnCopyAll;
        private Label lblStatus;
        public MainForm()
        {
            Text = "Photo Finder";
            Width = 800;
            Height = 520;
            StartPosition = FormStartPosition.CenterScreen;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;

            InitializeComponents();
        }

        private void InitializeComponents()
        {
            var main = new TableLayoutPanel();
            main.Dock = DockStyle.Fill;
            main.Padding = new Padding(10);
            main.ColumnCount = 3;
            main.RowCount = 7;
            main.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 70));
            main.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 20));
            main.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 10));

            for (int i = 0; i < 6; i++) main.RowStyles.Add(new RowStyle(SizeType.Absolute, 40));
            main.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

            // Folder row
            var lblFolder = new Label() { Text = "Folder to search:", AutoSize = true, Anchor = AnchorStyles.Left };
            txtFolder = new TextBox() { Anchor = AnchorStyles.Left | AnchorStyles.Right };
            btnBrowseFolder = new Button() { Text = "Browse", Anchor = AnchorStyles.Right };
            btnBrowseFolder.Click += BtnBrowseFolder_Click;

            main.Controls.Add(lblFolder, 0, 0);
            main.SetColumnSpan(lblFolder, 3);

            main.Controls.Add(txtFolder, 0, 1);
            main.SetColumnSpan(txtFolder, 2);
            main.Controls.Add(btnBrowseFolder, 2, 1);

            // List file row
            var lblList = new Label() { Text = "Text file with names (comma separated):", AutoSize = true, Anchor = AnchorStyles.Left };
            txtListFile = new TextBox() { Anchor = AnchorStyles.Left | AnchorStyles.Right };
            btnBrowseList = new Button() { Text = "Browse", Anchor = AnchorStyles.Right };
            btnBrowseList.Click += BtnBrowseList_Click;

            main.Controls.Add(lblList, 0, 2);
            main.SetColumnSpan(lblList, 3);

            main.Controls.Add(txtListFile, 0, 3);
            main.SetColumnSpan(txtListFile, 2);
            main.Controls.Add(btnBrowseList, 2, 3);

            // Find button
            btnFind = new Button() { Text = "Find Photos", Anchor = AnchorStyles.Right, Width = 120 };
            btnFind.Click += BtnFind_Click;
            main.Controls.Add(btnFind, 2, 4);

            lblStatus = new Label() { Text = "", AutoSize = true, Anchor = AnchorStyles.Left };
            main.Controls.Add(lblStatus, 0, 4);
            main.SetColumnSpan(lblStatus, 2);

            // Results list
            lstResults = new ListBox() { Dock = DockStyle.Fill }; 
            main.Controls.Add(lstResults, 0, 6);
            main.SetColumnSpan(lstResults, 3);

            // action buttons
            var bottomPanel = new FlowLayoutPanel() { FlowDirection = FlowDirection.RightToLeft, Dock = DockStyle.Fill };
            btnOpenSelected = new Button() { Text = "Open Selected", AutoSize = true };
            btnOpenSelected.Click += BtnOpenSelected_Click;
            btnCopyAll = new Button() { Text = "Copy All Matches", AutoSize = true };
            btnCopyAll.Click += BtnCopyAll_Click;

            bottomPanel.Controls.Add(btnOpenSelected);
            bottomPanel.Controls.Add(btnCopyAll);

            main.Controls.Add(bottomPanel, 0, 5);
            main.SetColumnSpan(bottomPanel, 3);

            Controls.Add(main);
        }

        private void BtnBrowseFolder_Click(object? sender, EventArgs e)
        {
            using var dlg = new FolderBrowserDialog();
            if (dlg.ShowDialog() == DialogResult.OK)
            {
                txtFolder.Text = dlg.SelectedPath;
            }
        }

        private void BtnBrowseList_Click(object? sender, EventArgs e)
        {
            using var dlg = new OpenFileDialog();
            dlg.Filter = "Text files|*.txt|All files|*.*";
            if (dlg.ShowDialog() == DialogResult.OK)
            {
                txtListFile.Text = dlg.FileName;
            }
        }

        private void BtnFind_Click(object? sender, EventArgs e)
        {
            lstResults.Items.Clear();
            lblStatus.Text = "";

            var folder = txtFolder.Text?.Trim();
            var listFile = txtListFile.Text?.Trim();

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

            string content;
            try
            {
                content = File.ReadAllText(listFile);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Could not read list file: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            var names = content.Split(new[] {','}, StringSplitOptions.RemoveEmptyEntries)
                .Select(s => s.Trim()).Where(s => !string.IsNullOrEmpty(s)).ToList();

            if (names.Count == 0)
            {
                MessageBox.Show("No names found in the text file.", "No names", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            // Supported image extensions
            var exts = new[] { ".jpg", ".jpeg", ".png", ".bmp", ".gif", ".tiff", ".webp" };

            // Search recursively
            lblStatus.Text = "Searching...";
            Application.DoEvents();

            try
            {
                var files = Directory.EnumerateFiles(folder, "*.*", SearchOption.AllDirectories)
                    .Where(f => exts.Contains(Path.GetExtension(f).ToLowerInvariant()));

                // Build a lookup by filename without extension (lowercase)
                var lookup = files.GroupBy(f => Path.GetFileNameWithoutExtension(f).ToLowerInvariant())
                                  .ToDictionary(g => g.Key, g => g.ToList());

                int foundCount = 0;
                foreach (var name in names)
                {
                    var key = Path.GetFileNameWithoutExtension(name).ToLowerInvariant();
                    if (lookup.TryGetValue(key, out var matchedFiles))
                    {
                        foreach (var mf in matchedFiles)
                        {
                            lstResults.Items.Add(mf);
                            foundCount++;
                        }
                    }
                }

                lblStatus.Text = $"Found {foundCount} matching files.";

                if (foundCount == 0)
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

        private void BtnCopyAll_Click(object? sender, EventArgs e)
        {
            if (lstResults.Items.Count == 0)
            {
                MessageBox.Show("No matches to copy.", "Nothing to copy", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            var desktop = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);
            var outDir = Path.Combine(desktop, "PhotoFinder_Matches_" + DateTime.Now.ToString("yyyyMMdd_HHmmss"));
            Directory.CreateDirectory(outDir);

            int copied = 0;
            foreach (var item in lstResults.Items)
            {
                var src = item.ToString();
                if (src == null) continue;
                try
                {
                    var dest = Path.Combine(outDir, Path.GetFileName(src));
                    File.Copy(src, dest, overwrite: true);
                    copied++;
                }
                catch (Exception ex)
                {
                    // ignore individual copy errors but continue
                    Debug.WriteLine("Copy failed: " + ex.Message);
                }
            }

            MessageBox.Show($"Copied {copied} files to:\n{outDir}", "Copy complete", MessageBoxButtons.OK, MessageBoxIcon.Information);
            try { Process.Start(new ProcessStartInfo(outDir) { UseShellExecute = true }); } catch { }
        }
    }
}
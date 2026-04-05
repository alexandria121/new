using System.Windows.Forms;
using System.Drawing;
using System.IO;
using MagicalDeckbuilder.Cards;

namespace MagicalDeckbuilder.UI;

/// <summary>
/// Main window displaying placeholder card images
/// </summary>
public class CardDisplayForm : Form
{
    private readonly FlowLayoutPanel _cardPanel;
    private readonly ComboBox _filterComboBox;
    private readonly Label _titleLabel;

    public CardDisplayForm()
    {
        // Form setup
        Text = "Card Placeholder Display";
        Size = new Size(1200, 800);
        StartPosition = FormStartPosition.CenterScreen;
        BackColor = Color.FromArgb(40, 40, 50);
        FormBorderStyle = FormBorderStyle.Sizable;
        MaximizeBox = true;

        // Create filter controls
        var filterPanel = new Panel
        {
            Dock = DockStyle.Top,
            Height = 60,
            BackColor = Color.FromArgb(30, 30, 40),
            Padding = new Padding(10)
        };

        _titleLabel = new Label
        {
            Text = "Placeholder Card Graphics",
            Font = new Font("Arial", 12, FontStyle.Bold),
            ForeColor = Color.White,
            AutoSize = true,
            Left = 20,
            Top = 15
        };

        _filterComboBox = new ComboBox
        {
            Location = new Point(300, 18),
            Width = 150,
            DropDownStyle = ComboBoxStyle.DropDownList
        };
        _filterComboBox.Items.AddRange(new object[] { "All Cards", "By Element", "By Type", "Squares Only" });
        _filterComboBox.SelectedIndex = 0;
        _filterComboBox.SelectedIndexChanged += FilterComboBox_SelectedIndexChanged;

        // Use TableLayoutPanel for filter area to handle resizing better
        var filterLayout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            RowCount = 1,
            ColumnCount = 2
        };
        filterLayout.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        filterLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
        filterLayout.Controls.Add(_titleLabel, 0, 0);
        filterLayout.Controls.Add(_filterComboBox, 0, 0);

        filterPanel.Controls.Add(filterLayout);

        // Create card display panel
        _cardPanel = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            AutoScroll = true,
            Padding = new Padding(20),
            BackColor = Color.FromArgb(40, 40, 50)
        };

        Controls.Add(_cardPanel);
        Controls.Add(filterPanel);

        // Load images
        LoadImages();

        // Handle resize to scale elements
        Resize += CardDisplayForm_Resize;
    }
    
    protected override void OnFormClosing(FormClosingEventArgs e)
    {
        // Clean up images to release file handles
        foreach (Control control in _cardPanel.Controls)
        {
            if (control is PictureBox pictureBox && pictureBox.Image != null)
            {
                pictureBox.Image.Dispose();
                pictureBox.Image = null;
            }
        }
        
        base.OnFormClosing(e);
    }

    private void CardDisplayForm_Resize(object? sender, EventArgs e)
    {
        // Scale font size based on form width (reduced by 25%)
        float scaleFactor = Math.Min(Width / 1200f, Height / 800f);
        scaleFactor = Math.Max(0.5f, Math.Min(2f, scaleFactor)); // Clamp between 0.5x and 2x

        _titleLabel.Font = new Font("Arial", (float)(12 * scaleFactor), FontStyle.Bold);
        _filterComboBox.Width = (int)(150 * scaleFactor);
    }

    private void LoadImages()
    {
        _cardPanel.Controls.Clear();

        // Find the Assets/Placeholders directory - check multiple locations
        string? placeholdersDir = null;
        
        // Check in project directory (where source code is)
        var projectDir = Path.Combine(AppContext.BaseDirectory, "..", "..");
        var possiblePaths = new[]
        {
            Path.Combine(projectDir, "Assets", "Placeholders"),
            Path.Combine(AppContext.BaseDirectory, "Assets", "Placeholders"),
            Path.Combine(Directory.GetCurrentDirectory(), "Assets", "Placeholders"),
            Path.Combine(projectDir, "NewGame", "Assets", "Placeholders")
        };

        foreach (var path in possiblePaths)
        {
            if (Directory.Exists(path))
            {
                var files = Directory.GetFiles(path, "*.png", SearchOption.AllDirectories);
                if (files.Length > 0)
                {
                    placeholdersDir = path;
                    break;
                }
            }
        }

        if (string.IsNullOrEmpty(placeholdersDir))
        {
            MessageBox.Show($"Could not find placeholder images directory.\nChecked paths in project directory.", 
                "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            return;
        }

        // Get all PNG files
        var imageFiles = Directory.GetFiles(placeholdersDir, "*.png", SearchOption.AllDirectories);

        foreach (var file in imageFiles.OrderBy(f => f))
        {
            try
            {
                var picBox = new PictureBox
                {
                    Image = Image.FromFile(file),
                    SizeMode = PictureBoxSizeMode.StretchImage,
                    Width = 125,
                    Height = 175,
                    Margin = new Padding(5),
                    BorderStyle = BorderStyle.FixedSingle,
                    BackColor = Color.Black
                };

                // Add tooltip with filename
                var fileName = Path.GetFileNameWithoutExtension(file);
                picBox.Name = fileName;

                _cardPanel.Controls.Add(picBox);
            }
            catch
            {
                // Skip files that can't be loaded
            }
        }
    }

    private void FilterComboBox_SelectedIndexChanged(object sender, System.EventArgs e)
    {
        // For now, just reload all images
        // Could implement filtering by element or type in the future
        LoadImages();
    }
}
using System.Windows.Forms;
using System.Drawing;

namespace MagicalDeckbuilder.UI;

/// <summary>
/// Main menu form with Play and Exit buttons
/// </summary>
public class MenuForm : Form
{
    private readonly Button _playButton;
    private readonly Button _placeholderButton;
    private readonly Button _exitButton;
    private readonly Label _titleLabel;

    public MenuForm()
    {
        // Form setup
        Text = "Card Game";
        Size = new Size(800, 600);
        StartPosition = FormStartPosition.CenterScreen;
        BackColor = Color.FromArgb(30, 30, 40);
        FormBorderStyle = FormBorderStyle.Sizable;
        MaximizeBox = true;
        MinimizeBox = true;

        // Use TableLayoutPanel for responsive layout
        var tableLayout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            RowCount = 3,
            ColumnCount = 1,
            Padding = new Padding(20)
        };
        tableLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 30f));  // Top - title area
        tableLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 45f));  // Middle - buttons
        tableLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 25f));  // Bottom - spacer

        // Title label
        _titleLabel = new Label
        {
            Text = "CARD GAME",
            Font = new Font("Arial", 27, FontStyle.Bold),
            ForeColor = Color.White,
            TextAlign = ContentAlignment.MiddleCenter,
            Dock = DockStyle.Fill,
            AutoSize = false
        };

        // Button panel for centering buttons
        var buttonPanel = new FlowLayoutPanel
        {
            FlowDirection = FlowDirection.TopDown,
            Dock = DockStyle.Fill,
            Padding = new Padding(0, 10, 0, 0)
        };

        // Placeholder button (yellow)
        _placeholderButton = new Button
        {
            Text = "Placeholder",
            Font = new Font("Arial", 18, FontStyle.Bold),
            ForeColor = Color.Black,
            BackColor = Color.FromArgb(255, 215, 0), // Gold/Yellow
            FlatStyle = FlatStyle.Flat,
            Margin = new Padding(20, 10, 20, 10),
            Size = new Size(280, 140),
            Anchor = AnchorStyles.None,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            Padding = new Padding(15, 15, 15, 15)
        };
        _placeholderButton.FlatAppearance.BorderSize = 2;
        _placeholderButton.Click += PlaceholderButton_Click;
        _placeholderButton.MouseEnter += (s, e) => _placeholderButton.BackColor = Color.FromArgb(255, 230, 50);
        _placeholderButton.MouseLeave += (s, e) => _placeholderButton.BackColor = Color.FromArgb(255, 215, 0);

        // Play button (green)
        _playButton = new Button
        {
            Text = "Play",
            Font = new Font("Arial", 18, FontStyle.Bold),
            ForeColor = Color.White,
            BackColor = Color.FromArgb(34, 139, 34), // Forest Green
            FlatStyle = FlatStyle.Flat,
            Margin = new Padding(20, 10, 20, 10),
            Size = new Size(280, 140),
            Anchor = AnchorStyles.None,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            Padding = new Padding(15, 15, 15, 15)
        };
        _playButton.FlatAppearance.BorderSize = 2;
        _playButton.Click += PlayButton_Click;
        _playButton.MouseEnter += (s, e) => _playButton.BackColor = Color.FromArgb(46, 139, 46);
        _playButton.MouseLeave += (s, e) => _playButton.BackColor = Color.FromArgb(34, 139, 34);

        // Exit button (red)
        _exitButton = new Button
        {
            Text = "Exit",
            Font = new Font("Arial", 18, FontStyle.Bold),
            ForeColor = Color.White,
            BackColor = Color.FromArgb(178, 34, 34), // Firebrick Red
            FlatStyle = FlatStyle.Flat,
            Margin = new Padding(20, 10, 20, 10),
            Size = new Size(280, 140),
            Anchor = AnchorStyles.None,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            Padding = new Padding(15, 15, 15, 15)
        };
        _exitButton.FlatAppearance.BorderSize = 2;
        _exitButton.Click += ExitButton_Click;
        _exitButton.MouseEnter += (s, e) => _exitButton.BackColor = Color.FromArgb(220, 44, 44);
        _exitButton.MouseLeave += (s, e) => _exitButton.BackColor = Color.FromArgb(178, 34, 34);

        // Add buttons to button panel in order: Placeholder, Play, Exit
        buttonPanel.Controls.Add(_placeholderButton);
        buttonPanel.Controls.Add(_playButton);
        buttonPanel.Controls.Add(_exitButton);
        buttonPanel.SetFlowBreak(_exitButton, true);

        // Add to table layout
        tableLayout.Controls.Add(_titleLabel, 0, 0);
        tableLayout.Controls.Add(buttonPanel, 0, 1);

        // Add table layout to form
        Controls.Add(tableLayout);

        // Handle resize to scale font sizes
        Resize += MenuForm_Resize;
        
        // Handle form closing to properly exit the application
        FormClosing += MenuForm_FormClosing;
    }
    
    /// <summary>
    /// Handle form closing - exit the application when the menu is closed
    /// </summary>
    private void MenuForm_FormClosing(object? sender, FormClosingEventArgs e)
    {
        // Force immediate exit - don't wait for message pump
        Environment.Exit(0);
    }

    private void MenuForm_Resize(object? sender, EventArgs e)
    {
        // Scale font size based on form width (reduced by 25%)
        float scaleFactor = Math.Min(Width / 800f, Height / 600f);
        scaleFactor = Math.Max(0.5f, Math.Min(2f, scaleFactor)); // Clamp between 0.5x and 2x

        _titleLabel.Font = new Font("Arial", 27 * scaleFactor, FontStyle.Bold);
        _placeholderButton.Font = new Font("Arial", 18 * scaleFactor, FontStyle.Bold);
        _playButton.Font = new Font("Arial", 18 * scaleFactor, FontStyle.Bold);
        _exitButton.Font = new Font("Arial", 18 * scaleFactor, FontStyle.Bold);

        // Scale button width with scale factor, height is minimum 140 and grows with window
        int buttonWidth = (int)(280 * scaleFactor);
        int buttonHeight = scaleFactor >= 1.0 ? (int)(140 * scaleFactor) : 140;
        
        _placeholderButton.Size = new Size(buttonWidth, buttonHeight);
        _playButton.Size = new Size(buttonWidth, buttonHeight);
        _exitButton.Size = new Size(buttonWidth, buttonHeight);
    }

    private void PlaceholderButton_Click(object? sender, System.EventArgs e)
    {
        // Open the card display form
        var cardForm = new CardDisplayForm();
        cardForm.Show();
        
        // Hide the menu and show the card form
        Hide();
        cardForm.FormClosed += (s, args) => Show();
    }

    private void PlayButton_Click(object? sender, System.EventArgs e)
    {
        // Open the game form as a modal dialog
        var gameForm = new GameForm();
        gameForm.ShowDialog();
    }

    private void ExitButton_Click(object? sender, System.EventArgs e)
    {
        // Close the application
        Application.Exit();
    }
}
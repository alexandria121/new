using System.Windows.Forms;
using System.Drawing;
using MagicalDeckbuilder.Cards;

namespace MagicalDeckbuilder.UI;

/// <summary>
/// Form that displays an enlarged view of a card when clicked
/// </summary>
public class CardDetailForm : Form
{
    private readonly Card _card;
    
    /// <summary>
    /// Create a new card detail display form
    /// </summary>
    public CardDetailForm(Card card)
    {
        _card = card;
        
        // Form setup - larger display
        Text = card.Name;
        Size = new Size(400, 550);
        StartPosition = FormStartPosition.CenterParent;
        BackColor = Color.FromArgb(30, 30, 40);
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        ControlBox = true;
        
        // Use TableLayoutPanel for responsive layout
        var mainLayout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            RowCount = 5,
            ColumnCount = 1,
            Padding = new Padding(20)
        };
        mainLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));     // Card name
        mainLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));     // Type and element
        mainLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 50f)); // Description
        mainLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));     // Stats
        mainLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));     // Effects
        
        // Card name (large, top)
        var nameLabel = new Label
        {
            Text = card.Name,
            Font = new Font("Arial", 15, FontStyle.Bold),
            ForeColor = Color.White,
            TextAlign = ContentAlignment.MiddleCenter,
            Dock = DockStyle.Fill,
            Height = 40
        };
        mainLayout.Controls.Add(nameLabel, 0, 0);
        
        // Type and element badges
        var typePanel = new FlowLayoutPanel
        {
            FlowDirection = FlowDirection.LeftToRight,
            Dock = DockStyle.Fill,
            Height = 30,
            WrapContents = false
        };
        
        var typeLabel = new Label
        {
            Text = card.Type.ToString(),
            Font = new Font("Arial", 9, FontStyle.Bold),
            ForeColor = GetTypeColor(card.Type),
            AutoSize = true,
            Padding = new Padding(5, 0, 15, 0)
        };
        typePanel.Controls.Add(typeLabel);
        
        var elementLabel = new Label
        {
            Text = $"[{card.Element}]",
            Font = new Font("Arial", 9, FontStyle.Bold),
            ForeColor = GetElementColor(card.Element),
            AutoSize = true,
            Padding = new Padding(5, 0, 15, 0)
        };
        typePanel.Controls.Add(elementLabel);
        
        if (card.IsLegendary)
        {
            var legendaryLabel = new Label
            {
                Text = "LEGENDARY",
                Font = new Font("Arial", 7, FontStyle.Bold),
                ForeColor = Color.Gold,
                AutoSize = true,
                Padding = new Padding(5, 0, 5, 0)
            };
            typePanel.Controls.Add(legendaryLabel);
        }
        
        var rarityLabel = new Label
        {
            Text = GetRarityText(card.Rarity),
            Font = new Font("Arial", 7),
            ForeColor = GetRarityColor(card.Rarity),
            AutoSize = true,
            Padding = new Padding(5, 0, 5, 0)
        };
        typePanel.Controls.Add(rarityLabel);
        
        mainLayout.Controls.Add(typePanel, 0, 1);
        
        // Description (large middle area)
        var descLabel = new Label
        {
            Text = string.IsNullOrEmpty(card.Description) ? "(No description)" : card.Description,
            Font = new Font("Arial", 9),
            ForeColor = Color.FromArgb(200, 200, 200),
            TextAlign = ContentAlignment.TopCenter,
            Dock = DockStyle.Fill,
            AutoSize = true
        };
        mainLayout.Controls.Add(descLabel, 0, 2);
        
        // Stats panel
        var statsPanel = new FlowLayoutPanel
        {
            FlowDirection = FlowDirection.LeftToRight,
            Dock = DockStyle.Fill,
            Height = 35,
            WrapContents = false
        };
        
        var manaLabel = new Label
        {
            Text = $"Mana Cost: {card.ManaCost}",
            Font = new Font("Arial", 10, FontStyle.Bold),
            ForeColor = Color.Cyan,
            AutoSize = true,
            Padding = new Padding(10, 0, 20, 0)
        };
        statsPanel.Controls.Add(manaLabel);
        
        if (card.Type == CardType.Creature || card.Type == CardType.Weapon || card.Type == CardType.Armor)
        {
            var powerLabel = new Label
            {
                Text = $"Power: {card.Power}",
                Font = new Font("Arial", 10, FontStyle.Bold),
                ForeColor = Color.Orange,
                AutoSize = true,
                Padding = new Padding(10, 0, 20, 0)
            };
            statsPanel.Controls.Add(powerLabel);
        }
        
        if (card.Type == CardType.Creature || card.Type == CardType.Armor)
        {
            var healthLabel = new Label
            {
                Text = $"Health: {card.Health}",
                Font = new Font("Arial", 10, FontStyle.Bold),
                ForeColor = Color.LimeGreen,
                AutoSize = true,
                Padding = new Padding(10, 0, 20, 0)
            };
            statsPanel.Controls.Add(healthLabel);
        }
        
        mainLayout.Controls.Add(statsPanel, 0, 3);
        
        // Effects list
        var effectsLabel = new Label
        {
            Text = card.Effects.Count > 0 ? "Effects:" : "No special effects",
            Font = new Font("Arial", 8, FontStyle.Bold),
            ForeColor = Color.FromArgb(180, 180, 180),
            Dock = DockStyle.Top,
            Height = 25,
            TextAlign = ContentAlignment.MiddleLeft
        };
        mainLayout.Controls.Add(effectsLabel, 0, 4);
        
        // Add effects if present
        if (card.Effects.Count > 0)
        {
            var effectsStack = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                AutoSize = true,
                RowCount = card.Effects.Count,
                ColumnCount = 1
            };
            
            for (int i = 0; i < card.Effects.Count; i++)
            {
                effectsStack.RowStyles.Add(new RowStyle(SizeType.AutoSize));
                
                var effect = card.Effects[i];
                var effectText = $"• {effect.Name}: {effect.Description} ({effect.Value})";
                var effectLabel = new Label
                {
                    Text = effectText,
                    Font = new Font("Arial", 7),
                    ForeColor = Color.FromArgb(180, 180, 180),
                    AutoSize = true,
                    Height = 20,
                    Width = 300,
                    Dock = DockStyle.Fill
                };
                effectsStack.Controls.Add(effectLabel, 0, i);
            }
            
            mainLayout.Controls.Add(effectsStack, 0, 4);
        }
        
        Controls.Add(mainLayout);
    }
    
    private Color GetTypeColor(CardType type)
    {
        return type switch
        {
            CardType.Spell => Color.FromArgb(255, 100, 100),
            CardType.Creature => Color.FromArgb(100, 255, 100),
            CardType.Artifact => Color.FromArgb(200, 200, 100),
            CardType.Enchantment => Color.FromArgb(150, 100, 200),
            CardType.Weapon => Color.FromArgb(255, 150, 50),
            CardType.Armor => Color.FromArgb(100, 150, 200),
            CardType.Event => Color.FromArgb(255, 200, 100),
            _ => Color.Gray
        };
    }
    
    private Color GetElementColor(ElementType element)
    {
        return element switch
        {
            ElementType.Fire => Color.FromArgb(255, 80, 80),
            ElementType.Water => Color.FromArgb(80, 150, 255),
            ElementType.Earth => Color.FromArgb(139, 90, 43),
            ElementType.Air => Color.FromArgb(200, 200, 255),
            ElementType.Light => Color.FromArgb(255, 255, 150),
            ElementType.Dark => Color.FromArgb(100, 50, 100),
            ElementType.Arcane => Color.FromArgb(180, 80, 255),
            ElementType.Nature => Color.FromArgb(80, 180, 80),
            _ => Color.Gray
        };
    }
    
    private string GetRarityText(int rarity)
    {
        return rarity switch
        {
            1 => "Common",
            2 => "Uncommon",
            3 => "Rare",
            4 => "Legendary",
            _ => "Unknown"
        };
    }
    
    private Color GetRarityColor(int rarity)
    {
        return rarity switch
        {
            1 => Color.Gray,
            2 => Color.FromArgb(100, 200, 100),
            3 => Color.FromArgb(100, 150, 255),
            4 => Color.Gold,
            _ => Color.Gray
        };
    }
}
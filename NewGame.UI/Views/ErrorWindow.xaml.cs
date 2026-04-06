using System.Diagnostics;
using System.IO;
using System.Text;
using System.Windows;
using MagicalDeckbuilder.Logging;

namespace NewGame.UI.Views;

public partial class ErrorWindow : Window
{
    private readonly Exception _exception;
    private readonly string _fullErrorDetails;

    public ErrorWindow(Exception exception)
    {
        InitializeComponent();
        _exception = exception;

        // Build full error details
        _fullErrorDetails = BuildErrorDetails();

        // Display summary and stack trace
        ErrorSummaryText.Text = $"{_exception.GetType().Name}: {_exception.Message}";
        StackTraceText.Text = _fullErrorDetails;
    }

    private string BuildErrorDetails()
    {
        var sb = new StringBuilder();
        sb.AppendLine($"Exception Type: {_exception.GetType().FullName}");
        sb.AppendLine();
        sb.AppendLine($"Message: {_exception.Message}");
        sb.AppendLine();
        sb.AppendLine("Stack Trace:");
        sb.AppendLine(_exception.StackTrace);
        sb.AppendLine();

        // Add inner exception if present
        if (_exception.InnerException != null)
        {
            sb.AppendLine("Inner Exception:");
            sb.AppendLine($"  Type: {_exception.InnerException.GetType().FullName}");
            sb.AppendLine($"  Message: {_exception.InnerException.Message}");
            sb.AppendLine();
            sb.AppendLine("Inner Stack Trace:");
            sb.AppendLine(_exception.InnerException.StackTrace);
            sb.AppendLine();
        }

        // Add log file location
        sb.AppendLine("Log File Location:");
        sb.AppendLine(ErrorLogger.Instance.LogFilePath);
        sb.AppendLine();
        sb.AppendLine("Full Session Log:");
        sb.AppendLine(ErrorLogger.Instance.SessionLog);

        return sb.ToString();
    }

    private void OpenLogFile_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var logPath = ErrorLogger.Instance.LogFilePath;
            if (File.Exists(logPath))
            {
                // Open in default text editor
                Process.Start(new ProcessStartInfo
                {
                    FileName = logPath,
                    UseShellExecute = true
                });
            }
            else
            {
                MessageBox.Show($"Log file not found at:\n{logPath}", "Log File", 
                    MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Could not open log file:\n{ex.Message}", "Error", 
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void CopyToClipboard_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            Clipboard.SetText(_fullErrorDetails);
            MessageBox.Show("Error details copied to clipboard!", "Copied", 
                MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Could not copy to clipboard:\n{ex.Message}", "Error", 
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void Close_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }
}

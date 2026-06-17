using QuantumDotStudio.Core.Models;
using QuantumDotStudio.Reports;
using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;
using System.Windows.Input;

namespace QuantumDotStudio.WPF.ViewModels;

/// <summary>
/// ViewModel für LaTeX-Berichtsexport.
/// </summary>
public class ExportViewModel : INotifyPropertyChanged
{
    private QuantumDot? _activeDot;
    private string? _lastExportPath;
    private string? _statusMessage;

    public ExportViewModel()
    {
        ExportCommand = new RelayCommand(_ => ExportLatex(), _ => CanExport());
    }

    /// <summary>
    /// Aktives Quantum Dot, das exportiert werden soll.
    /// Wird typischerweise vom übergeordneten ViewModel gesetzt.
    /// </summary>
    public QuantumDot? ActiveDot
    {
        get => _activeDot;
        set
        {
            _activeDot = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(IsExportEnabled));

            if (ExportCommand is RelayCommand cmd)
            {
                cmd.RaiseCanExecuteChanged();
            }
        }
    }

    public bool IsExportEnabled => ActiveDot != null;

    /// <summary>
    /// Pfad der letzten Exportdatei, oder null.
    /// </summary>
    public string? LastExportPath
    {
        get => _lastExportPath;
        private set
        {
            _lastExportPath = value;
            OnPropertyChanged();
        }
    }

    /// <summary>
    /// Statusmeldung für den Benutzer (Erfolg/Fehler).
    /// </summary>
    public string? StatusMessage
    {
        get => _statusMessage;
        private set
        {
            _statusMessage = value;
            OnPropertyChanged();
        }
    }

    public ICommand ExportCommand { get; }

    private bool CanExport()
    {
        return ActiveDot != null;
    }

    private void ExportLatex()
    {
        if (ActiveDot == null)
        {
            StatusMessage = "Fehler: Kein Quantum Dot zum Exportieren vorhanden.";
            return;
        }

        var dialog = new Microsoft.Win32.SaveFileDialog
        {
            Filter = "LaTeX-Dokument (*.tex)|*.tex",
            DefaultExt = ".tex",
            FileName = $"QuantumDot_{ActiveDot.Material.Name}_{ActiveDot.Radius_nm:F1}nm.tex"
        };

        if (dialog.ShowDialog() == true)
        {
            string latex = LatexReportGenerator.Generate(ActiveDot);
            File.WriteAllText(dialog.FileName, latex, System.Text.Encoding.UTF8);
            LastExportPath = dialog.FileName;
            StatusMessage = $"Bericht gespeichert unter: {dialog.FileName}";
        }
        else
        {
            StatusMessage = null;
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    private void OnPropertyChanged([CallerMemberName] string propertyName = "")
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}

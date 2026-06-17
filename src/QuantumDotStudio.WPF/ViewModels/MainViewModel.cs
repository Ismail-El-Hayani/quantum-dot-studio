using QuantumDotStudio.Core.Data;
using QuantumDotStudio.Core.Models;
using QuantumDotStudio.Reports;
using QuantumDotStudio.Solver;
using OxyPlot;
using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;
using System.Windows.Input;

namespace QuantumDotStudio.WPF.ViewModels;

/// <summary>
/// Haupt-ViewModel für das Quantum Dot Studio.
/// </summary>
public class MainViewModel : INotifyPropertyChanged
{
    private readonly QuantumDotService _service;
    private Material _selectedMaterial = MaterialDatabase.Defaults.First();
    private double _radius_nm = 3.0;
    private QuantumDot _activeDot = new();

    public MainViewModel()
    {
        _service = new QuantumDotService();
        Materials = new List<Material>(MaterialDatabase.Defaults);
        RecalculateCommand = new RelayCommand(_ => Recalculate(), _ => true);
        ExportCommand = new RelayCommand(_ => ExportLatex(), _ => ActiveDot != null);

        Recalculate();
    }

    /// <summary>
    /// Verfügbare Halbleitermaterialien.
    /// </summary>
    public List<Material> Materials { get; }

    /// <summary>
    /// Aktuell gewähltes Material.
    /// </summary>
    public Material SelectedMaterial
    {
        get => _selectedMaterial;
        set
        {
            if (_selectedMaterial != value)
            {
                _selectedMaterial = value;
                OnPropertyChanged();
                Recalculate();
            }
        }
    }

    /// <summary>
    /// Aktueller Radius in nm.
    /// </summary>
    public double Radius_nm
    {
        get => _radius_nm;
        set
        {
            if (Math.Abs(_radius_nm - value) > 1e-6)
            {
                _radius_nm = value;
                OnPropertyChanged();
                Recalculate();
            }
        }
    }

    /// <summary>
    /// Aktives Quantum Dot mit allen berechneten Eigenschaften.
    /// </summary>
    public QuantumDot ActiveDot
    {
        get => _activeDot;
        private set
        {
            _activeDot = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(BandGapText));
            OnPropertyChanged(nameof(WavelengthText));
            OnPropertyChanged(nameof(AtomCountText));
            OnPropertyChanged(nameof(EnergyLevelPlot));
            OnPropertyChanged(nameof(SpectrumPlot));
        }
    }

    public string BandGapText => $"Bandlücke: {ActiveDot.TotalBandGap_eV:F3} eV";
    public string WavelengthText => $"Wellenlänge: {ActiveDot.EmissionWavelength_nm:F1} nm";
    public string AtomCountText => $"Atome: {ActiveDot.Atoms.Count}";

    public PlotModel EnergyLevelPlot => PlotFactory.CreateEnergyLevelPlot(ActiveDot);
    public PlotModel SpectrumPlot => PlotFactory.CreateSpectrumPlot(ActiveDot);

    public ICommand RecalculateCommand { get; }
    public ICommand ExportCommand { get; }

    private void Recalculate()
    {
        ActiveDot = _service.BuildQuantumDot(SelectedMaterial, Radius_nm);
    }

    private void ExportLatex()
    {
        if (ActiveDot == null)
            return;

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
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    private void OnPropertyChanged([CallerMemberName] string propertyName = "")
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}

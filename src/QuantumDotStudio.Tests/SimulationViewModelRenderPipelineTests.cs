using QuantumDotStudio.WPF.ViewModels;

namespace QuantumDotStudio.Tests;

/// <summary>
/// Tests für die neue Render-Pipeline: RecalculateAsync baut das 3D-Modell
/// auf dem Hintergrund-Thread und veröffentlicht Pending3DModel. Der
/// Staleness-Guard verwirft veraltete Ergebnisse.
/// </summary>
public class SimulationViewModelRenderPipelineTests
{
    private static SimulationViewModel CreateViewModel()
        => new(new QuantumDotStudio.Solver.QuantumDotService());

    [Fact]
    public async Task RecalculateAsync_Populates_ActiveDot_And_Pending3DModel()
    {
        var vm = CreateViewModel();
        vm.Radius_nm = 3.0;
        vm.SelectedMaterial = vm.Materials.First(m => m.Name == "CdSe");

        bool accepted = await vm.RecalculateAsync();

        Assert.True(accepted);
        Assert.NotNull(vm.ActiveDot);
        Assert.NotNull(vm.Pending3DModel);
        Assert.True(vm.Pending3DModel!.IsFrozen);
        Assert.NotEmpty(vm.Pending3DModel.Children);
    }

    [Fact]
    public async Task RecalculateAsync_Skips_Mesh_When_Lattice_And_Cloud_Off()
    {
        var vm = CreateViewModel();
        vm.ShowLattice = false;
        vm.ShowCloud = false;
        vm.ShowCloud1P = false;

        await vm.RecalculateAsync();

        // Kein Gitter, keine Wolken: Modell bleibt (fast) leer, ist aber nicht null.
        Assert.NotNull(vm.Pending3DModel);
        Assert.Empty(vm.Pending3DModel!.Children);
    }

    [    Fact]
    public async Task RecalculateAsync_With1P_Shows_Two_Clouds()
    {
        var vm = CreateViewModel();
        vm.ShowLattice = false;
        vm.ShowCloud = true;
        vm.ShowCloud1P = true;

        await vm.RecalculateAsync();

        Assert.NotNull(vm.Pending3DModel);
        Assert.Equal(2, vm.Pending3DModel!.Children.Count);
    }

    [Fact]
    public async Task RecalculateAsync_Staleness_Discards_Older_Result()
    {
        var vm = CreateViewModel();
        vm.ShowLattice = false;
        vm.ShowCloud = false;
        vm.ShowCloud1P = false;

        // Zwei Berechnungen hintereinander; die zweite muss die erste verwerfen.
        var task1 = vm.RecalculateAsync();
        var task2 = vm.RecalculateAsync();
        bool[] results = await Task.WhenAll(task1, task2);

        // Genau die letzte Sequenz wird übernommen — task2 gewinnt.
        Assert.False(results[0]);
        Assert.True(results[1]);
        Assert.NotNull(vm.Pending3DModel);
    }

    [Fact]
    public async Task RecalculateAsync_ShellMode_Still_Works()
    {
        var vm = CreateViewModel();
        vm.UseShell = true;
        vm.SelectedShellMaterial = vm.Materials.First(m => m.Name == "CdS");
        vm.ShellThickness_nm = 0.6;

        bool accepted = await vm.RecalculateAsync();

        Assert.True(accepted);
        Assert.IsType<QuantumDotStudio.Core.Models.CoreShellQuantumDot>(vm.ActiveDot);
    }

    [Fact]
    public async Task RecalculateAsync_Model_Is_ThreadSafe_Against_Frozen_Mutation()
    {
        // Das gefrorene Modell darf vom UI-Thread nicht mehr mutierbar sein.
        var vm = CreateViewModel();
        await vm.RecalculateAsync();

        Assert.True(vm.Pending3DModel!.IsFrozen);
        Assert.Throws<System.InvalidOperationException>(() =>
            vm.Pending3DModel!.Children.Add(new System.Windows.Media.Media3D.GeometryModel3D()));
    }
}
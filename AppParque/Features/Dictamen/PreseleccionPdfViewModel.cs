using System.Collections.ObjectModel;
using AppParque.Shared;
using AppParque.Shared.Models;

namespace AppParque.Features.Dictamen;

/// <summary>Una atracción en la pantalla de preselección: arranca incluida si el dictamen la dejó
/// "Permitida" o "Con condición", y el enfermero puede revertir cualquiera de las dos — igual que
/// AttractionSelectable en ValidacionAtraccionesView, no hay candado.</summary>
public class AttraccionPreseleccionItem : BaseViewModel
{
    private readonly Action<AttraccionPreseleccionItem> _onToggled;
    private bool _incluida;

    public DictamenAtraccionDto Model { get; }

    public string Nombre => Model.Nombre;

    public string Meta => Model.Rol is "Conductor" or "Pasajero"
        ? $"Intensidad {Model.Intensidad} · como {Model.Rol.ToLowerInvariant()}"
        : $"Intensidad {Model.Intensidad}";

    public bool TieneMotivo { get; }
    public string Motivo { get; } = string.Empty;
    public string BadgeLabel { get; } = string.Empty;
    public Color BadgeFondo { get; } = Colors.Transparent;
    public Color BadgeTexto { get; } = Colors.Black;
    public Color Borde { get; }

    public bool Incluida
    {
        get => _incluida;
        set
        {
            if (!SetProperty(ref _incluida, value)) return;
            _onToggled(this);
        }
    }

    public AttraccionPreseleccionItem(DictamenAtraccionDto model, bool incluidaInicial, Action<AttraccionPreseleccionItem> onToggled)
    {
        Model = model;
        _incluida = incluidaInicial; // directo: no debe disparar el callback al construir la lista
        _onToggled = onToggled;

        TieneMotivo = model.Veredicto is not (Veredictos.Permitida or Veredictos.ConMitigacion);
        if (TieneMotivo)
        {
            (BadgeLabel, BadgeFondo, BadgeTexto, Borde) = model.Veredicto == Veredictos.PorVerificar
                ? ("Verificar en sitio", Color.FromArgb("#FDF8EC"), Color.FromArgb("#6B4600"), Color.FromArgb("#E8D9B0"))
                : ("No permitida", Color.FromArgb("#FBEDEB"), Color.FromArgb("#8A2317"), Color.FromArgb("#EBC5BF"));

            Motivo = string.Join("\n", model.Motivos.Select(m => $"·  {m.Texto}"));
        }
        else
        {
            Borde = Color.FromArgb("#e3e7ec");
        }
    }
}

/// <summary>Opción 1 del mockup de preselección: dos secciones (incluidas / no incluidas) que se
/// recalculan cada vez que se toca un switch, y las no incluidas muestran el motivo del dictamen.</summary>
public class PreseleccionPdfViewModel : BaseViewModel
{
    private readonly List<AttraccionPreseleccionItem> _items;

    public ObservableCollection<AttraccionPreseleccionItem> Incluidas { get; } = [];
    public ObservableCollection<AttraccionPreseleccionItem> Excluidas { get; } = [];

    public PreseleccionPdfViewModel(DiagnosticoResponseDto dictamen)
    {
        _items = dictamen.Atracciones
            .Select(a => new AttraccionPreseleccionItem(a, a.Veredicto is Veredictos.Permitida or Veredictos.ConMitigacion, OnItemToggled))
            .ToList();

        Refrescar();
    }

    public string ResumenTexto => $"{Incluidas.Count} de {_items.Count} incluidas en tu guía";

    public bool PuedeGenerar => Incluidas.Count > 0;

    /// <summary>Lo que recibe VistaPreviaPdfView: una por cada atracción que quedó incluida.</summary>
    public List<AtraccionPdfInfo> ObtenerSeleccionadas() =>
        Incluidas.Select(i => DictamenPdfMapper.ToPdfInfo(i.Model)).ToList();

    private void OnItemToggled(AttraccionPreseleccionItem item) => Refrescar();

    private void Refrescar()
    {
        Incluidas.Clear();
        Excluidas.Clear();
        foreach (var item in _items)
            (item.Incluida ? Incluidas : Excluidas).Add(item);

        OnPropertyChanged(nameof(ResumenTexto));
        OnPropertyChanged(nameof(PuedeGenerar));
    }
}
